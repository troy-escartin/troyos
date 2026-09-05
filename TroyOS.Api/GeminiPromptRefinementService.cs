using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace TroyOS.Api;

public interface IGeminiPromptRefinementService
{
    Task<string> ImprovePromptAsync(string prompt, CancellationToken cancellationToken);
}

public sealed class GeminiPromptRefinementService(
    HttpClient httpClient,
    IOptions<GeminiOptions> options,
    ILogger<GeminiPromptRefinementService> logger) : IGeminiPromptRefinementService
{
    private const string SystemInstruction = """
        You are an expert prompt engineer.

        Improve the supplied prompt while preserving the user's original intent.

        Requirements:
        - Improve clarity and specificity.
        - Add useful structure where appropriate.
        - Improve the expected output instructions.
        - Add reasonable constraints only when they improve the prompt.
        - Do not invent facts, requirements, audiences, technologies, or context.
        - Do not answer or execute the supplied prompt.
        - Return only the improved prompt.
        - Do not include commentary, scoring, explanations, introductions, or markdown code fences.
        """;

    private readonly GeminiOptions _options = options.Value;

    public async Task<string> ImprovePromptAsync(string prompt, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            logger.LogError("Gemini API key is not configured.");
            throw new GeminiUnavailableException();
        }

        using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellation.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds)));

        var endpoint = $"{_options.BaseUrl.TrimEnd('/')}/models/{Uri.EscapeDataString(_options.Model)}:generateContent";
        var request = new GeminiRequest(
            new GeminiSystemInstruction([new GeminiPart(SystemInstruction)]),
            [new GeminiContent("user", [new GeminiPart(prompt)])]);

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(request)
            };
            httpRequest.Headers.Add("x-goog-api-key", _options.ApiKey);

            using var response = await httpClient.SendAsync(httpRequest, timeoutCancellation.Token);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                throw new GeminiAuthenticationException();
            }

            if ((int)response.StatusCode == StatusCodes.Status429TooManyRequests || (int)response.StatusCode >= 500)
            {
                throw new GeminiUnavailableException();
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new GeminiResponseException();
            }

            var payload = await response.Content.ReadFromJsonAsync<GeminiResponse>(timeoutCancellation.Token);
            var improvedPrompt = payload?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(improvedPrompt))
            {
                throw new GeminiResponseException();
            }

            return improvedPrompt;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new GeminiTimeoutException();
        }
        catch (HttpRequestException)
        {
            throw new GeminiUnavailableException();
        }
        catch (JsonException)
        {
            throw new GeminiResponseException();
        }
    }

    private sealed record GeminiRequest(GeminiSystemInstruction SystemInstruction, GeminiContent[] Contents);
    private sealed record GeminiSystemInstruction(GeminiPart[] Parts);
    private sealed record GeminiContent(string Role, GeminiPart[] Parts);
    private sealed record GeminiPart(string Text);
    private sealed record GeminiResponse(GeminiCandidate[]? Candidates);
    private sealed record GeminiCandidate(GeminiContent? Content);
}

public sealed class GeminiTimeoutException : Exception;
public sealed class GeminiAuthenticationException : Exception;
public sealed class GeminiUnavailableException : Exception;
public sealed class GeminiResponseException : Exception;
