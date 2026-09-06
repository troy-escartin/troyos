using TroyOS.UI.Client.Features.PromptBuilder.Models;

namespace TroyOS.UI.Client.Features.PromptBuilder.Mock;

/// <summary>Static definition of one RACE slot's question, driven entirely by local data (no agent call).</summary>
public sealed record SlotDefinition(
    SlotType Type,
    string ShortLabel,
    string AriaLabel,
    string Question,
    string Helper,
    bool IsRequired,
    IReadOnlyList<ChipOption> Chips);
