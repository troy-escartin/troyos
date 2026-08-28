namespace TroyOS.UI.Client.Features.PromptBuilder.Models;

/// <summary>The captured value for a single RACE slot.</summary>
public sealed class PromptSlotState
{
    public string? Value { get; set; }

    /// <summary>True when the value was inferred (skipped or "build now") rather than typed by the user.</summary>
    public bool IsAssumption { get; set; }

    public bool IsAnswered => !string.IsNullOrWhiteSpace(Value);
}
