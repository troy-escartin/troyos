namespace TroyOS.UI.Client.Features.PromptBuilder.Models;

/// <summary>A single transcript entry rendered as a message bubble.</summary>
public sealed class ChatMessage
{
    public Guid Id { get; } = Guid.NewGuid();

    public required ChatSender Sender { get; init; }

    public required string Text { get; init; }

    public string? HelperText { get; init; }

    public IReadOnlyList<ChipOption>? Chips { get; init; }

    /// <summary>Non-null when this message should show a "Skip" link for the given slot.</summary>
    public SlotType? SkipSlot { get; init; }

    /// <summary>True for the transient "assistant is thinking" bubble.</summary>
    public bool IsThinkingIndicator { get; init; }
}
