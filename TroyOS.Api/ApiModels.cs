namespace TroyOS.Api;

public sealed record ImprovePromptRequest(string? Prompt);

public sealed record ImprovePromptResponse(string ImprovedPrompt);

public sealed record ApiErrorResponse(string Error, string? Code = null);

public sealed class GeminiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-3.6-flash";
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
    public int TimeoutSeconds { get; set; } = 30;
}

public sealed class PromptRefinementOptions
{
    public int MaximumPromptCharacters { get; set; } = 12000;
    public long MaximumRequestBytes { get; set; } = 14000;
}

public sealed class CorsOptions
{
    public string[] AllowedOrigins { get; set; } = ["https://localhost:7195", "http://localhost:5195"];
}

public sealed class RateLimitOptions
{
    public int PermitLimit { get; set; } = 10;
    public int WindowSeconds { get; set; } = 60;
    public int QueueLimit { get; set; } = 0;
}
