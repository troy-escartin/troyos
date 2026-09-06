namespace TroyOS.UI.Client.Features.PromptBuilder.Models;

/// <summary>What happens when a quick-reply chip is clicked.</summary>
public enum ChipKind
{
    /// <summary>Ask the mock agent for a list of suggestions, rendered as follow-up chips.</summary>
    ShowSuggestions,

    /// <summary>Immediately answer the current slot with a mocked "surprise" value.</summary>
    SurpriseMe,

    /// <summary>Answer the current slot with this chip's own value (used for suggestion chips).</summary>
    SelectValue
}

public sealed record ChipOption(string Label, ChipKind Kind, string? Value = null);
