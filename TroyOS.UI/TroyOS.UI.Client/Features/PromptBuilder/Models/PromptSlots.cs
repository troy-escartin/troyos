namespace TroyOS.UI.Client.Features.PromptBuilder.Models;

/// <summary>Holds the captured state for all four RACE slots.</summary>
public sealed class PromptSlots
{
    private readonly Dictionary<SlotType, PromptSlotState> _states = Enum.GetValues<SlotType>()
        .ToDictionary(type => type, _ => new PromptSlotState());

    public PromptSlotState this[SlotType type] => _states[type];

    public bool AllAnswered => _states.Values.All(state => state.IsAnswered);
}
