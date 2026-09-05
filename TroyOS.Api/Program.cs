using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TroyOS.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));
builder.Services.Configure<PromptRefinementOptions>(builder.Configuration.GetSection("PromptRefinement"));
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection("Cors"));
builder.Services.Configure<RateLimitOptions>(builder.Configuration.GetSection("RateLimiting"));

var corsOptions = builder.Configuration.GetSection("Cors").Get<CorsOptions>() ?? new();
var rateLimitOptions = builder.Configuration.GetSection("RateLimiting").Get<RateLimitOptions>() ?? new();
var promptOptions = builder.Configuration.GetSection("PromptRefinement").Get<PromptRefinementOptions>() ?? new();

builder.Services.AddCors(options => options.AddPolicy("PromptBuilder", policy =>
{
    policy.WithOrigins(corsOptions.AllowedOrigins)
        .WithMethods("POST", "OPTIONS")
        .WithHeaders("Content-Type")
        .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
}));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = rateLimitOptions.WindowSeconds.ToString();
        await context.HttpContext.Response.WriteAsJsonAsync(
            new ApiErrorResponse("Prompt refinement is temporarily busy. Please try again shortly.", "rate_limit"),
            cancellationToken);
    };
    options.AddPolicy("PromptImprovement", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitOptions.PermitLimit,
                Window = TimeSpan.FromSeconds(rateLimitOptions.WindowSeconds),
                QueueLimit = rateLimitOptions.QueueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            }));
});

builder.Services.AddHttpClient<IGeminiPromptRefinementService, GeminiPromptRefinementService>(client =>
{
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    client.Timeout = Timeout.InfiniteTimeSpan;
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    var trustedProxyIps = builder.Configuration.GetSection("ForwardedHeaders:TrustedProxyIps").Get<string[]>() ?? [];
    foreach (var address in trustedProxyIps)
    {
        if (IPAddress.TryParse(address, out var ipAddress))
        {
            options.KnownProxies.Add(ipAddress);
        }
    }
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
});
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var traceId = context.TraceIdentifier;
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("TroyOS.Api.Errors");
    logger.LogError("Unhandled API error. TraceId: {TraceId}", traceId);
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(new ApiErrorResponse("The server could not complete the request.", "server_error"));
}));

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    if (context.Request.Path.StartsWithSegments("/api/prompt/improve"))
    {
        context.Response.Headers.CacheControl = "no-store";
    }
    await next();
});
app.UseCors("PromptBuilder");
app.UseRateLimiter();

app.MapPost("/api/prompt/improve", async (
    HttpContext context,
    IGeminiPromptRefinementService geminiService,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    var traceId = context.TraceIdentifier;
    var started = TimeProvider.System.GetTimestamp();

    if (!context.Request.ContentType?.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) ?? true)
    {
        return Results.Json(new ApiErrorResponse("JSON is required.", "invalid_content_type"), statusCode: StatusCodes.Status415UnsupportedMediaType);
    }

    ImprovePromptRequest? request;
    try
    {
        request = await context.Request.ReadFromJsonAsync<ImprovePromptRequest>(cancellationToken);
    }
    catch (JsonException)
    {
        return Results.Json(new ApiErrorResponse("The request body is invalid.", "invalid_request"), statusCode: StatusCodes.Status400BadRequest);
    }

    if (request is null || string.IsNullOrWhiteSpace(request.Prompt))
    {
        return Results.Json(new ApiErrorResponse("A non-empty prompt is required.", "invalid_prompt"), statusCode: StatusCodes.Status400BadRequest);
    }

    var prompt = request.Prompt.Trim();
    if (prompt.Length > promptOptions.MaximumPromptCharacters)
    {
        return Results.Json(new ApiErrorResponse("The prompt is too long.", "prompt_too_long"), statusCode: StatusCodes.Status413PayloadTooLarge);
    }

    logger.LogInformation("Prompt refinement received. Characters: {CharacterCount}; TraceId: {TraceId}", prompt.Length, traceId);

    try
    {
        var improvedPrompt = await geminiService.ImprovePromptAsync(prompt, cancellationToken);
        logger.LogInformation("Prompt refinement completed. Status: success; DurationMs: {DurationMs}; TraceId: {TraceId}",
            TimeProvider.System.GetElapsedTime(started).TotalMilliseconds, traceId);
        context.Response.Headers.CacheControl = "no-store";
        return Results.Json(new ImprovePromptResponse(improvedPrompt));
    }
    catch (GeminiTimeoutException)
    {
        logger.LogWarning("Prompt refinement timed out. Status: timeout; TraceId: {TraceId}", traceId);
        return Results.Json(new ApiErrorResponse("Prompt refinement timed out. Please try again.", "timeout"), statusCode: StatusCodes.Status504GatewayTimeout);
    }
    catch (GeminiAuthenticationException)
    {
        logger.LogError("Prompt refinement provider authentication failed. Status: authentication_failure; TraceId: {TraceId}", traceId);
        return Results.Json(new ApiErrorResponse("Prompt refinement is temporarily unavailable.", "provider_unavailable"), statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (GeminiUnavailableException)
    {
        logger.LogWarning("Prompt refinement provider unavailable. Status: provider_unavailable; TraceId: {TraceId}", traceId);
        return Results.Json(new ApiErrorResponse("Prompt refinement is temporarily unavailable.", "provider_unavailable"), statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (GeminiResponseException)
    {
        logger.LogWarning("Prompt refinement provider returned an invalid response. Status: invalid_provider_response; TraceId: {TraceId}", traceId);
        return Results.Json(new ApiErrorResponse("Prompt refinement returned an invalid result.", "invalid_provider_response"), statusCode: StatusCodes.Status502BadGateway);
    }
    finally
    {
        logger.LogDebug("Prompt refinement request finished. TraceId: {TraceId}", traceId);
    }
})
    .RequireRateLimiting("PromptImprovement")
    .WithMetadata(new RequestSizeLimitAttribute(promptOptions.MaximumRequestBytes))
    .WithName("ImprovePrompt");

app.Run();