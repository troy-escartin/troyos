using System.Net;
using System.Net.Http.Json;

namespace TroyOS.UI.Client.Features.PromptBuilder.Services;

public sealed class PromptRefinementService(HttpClient httpClient)
    : IPromptRefinementService
{
    public async Task<string> ImprovePromptAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new PromptRefinementException("There is no prompt to refine.");
        }

        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                "api/prompt/improve",
                new ImprovePromptRequest(prompt.Trim()),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new PromptRefinementException(response.StatusCode switch
                {
                    HttpStatusCode.BadRequest => "The prompt could not be refined.",
                    HttpStatusCode.RequestEntityTooLarge => "The prompt is too long to refine.",
                    HttpStatusCode.TooManyRequests => "Prompt refinement is temporarily busy. Please try again shortly.",
                    HttpStatusCode.GatewayTimeout => "Prompt refinement timed out. Please try again.",
                    _ => "Prompt refinement is temporarily unavailable. Please try again."
                });
            }

            var result = await response.Content.ReadFromJsonAsync<ImprovePromptResponse>(cancellationToken);
            if (string.IsNullOrWhiteSpace(result?.ImprovedPrompt))
            {
                throw new PromptRefinementException("Prompt refinement returned no result.");
            }

            return result.ImprovedPrompt.Trim();
        }
        catch (PromptRefinementException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            throw new PromptRefinementException("Prompt refinement is temporarily unavailable. Please try again.");
        }
        catch (System.Text.Json.JsonException)
        {
            throw new PromptRefinementException("Prompt refinement returned an invalid result.");
        }
    }

    private sealed record ImprovePromptRequest(string Prompt);
    private sealed record ImprovePromptResponse(string? ImprovedPrompt);
}

public sealed class PromptRefinementException(string message) : Exception(message);