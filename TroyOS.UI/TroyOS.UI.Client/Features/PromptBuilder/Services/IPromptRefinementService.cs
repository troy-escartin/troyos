namespace TroyOS.UI.Client.Features.PromptBuilder.Services;

public interface IPromptRefinementService
{
    Task<string> ImprovePromptAsync(string prompt, CancellationToken cancellationToken = default);
}