using TroyOS.UI.Client.Features.PromptBuilder.Models;

namespace TroyOS.UI.Client.Features.PromptBuilder.Mock;

/// <summary>
/// The scripted RACE conversation (Role -&gt; Action -&gt; Context -&gt; Expectation), fully mocked so the
/// feature is clickable end to end without any LLM or backend call. Swap this out for a real
/// agent-driven question service later.
/// </summary>
public static class PromptConversationScript
{
    public static IReadOnlyList<SlotDefinition> Slots { get; } =
    [
        new SlotDefinition(
            SlotType.Role,
            "ROLE",
            "Role",
            "Who should the model be?",
            "Define the role or persona the model should take.",
            IsRequired: false,
            Chips:
            [
                new ChipOption("Use common roles", ChipKind.ShowSuggestions),
                new ChipOption("Show examples", ChipKind.ShowSuggestions)
            ]),

        new SlotDefinition(
            SlotType.Action,
            "ACTION",
            "Action",
            "What exactly should it do?",
            "Describe the specific task or action you want.",
            IsRequired: true,
            Chips:
            [
                new ChipOption("Use action verbs", ChipKind.ShowSuggestions),
                new ChipOption("Show examples", ChipKind.ShowSuggestions),
                new ChipOption("Surprise me", ChipKind.SurpriseMe)
            ]),

        new SlotDefinition(
            SlotType.Context,
            "CONTEXT",
            "Context",
            "What context should it know?",
            "Provide background, inputs, constraints, or details.",
            IsRequired: false,
            Chips: []),

        new SlotDefinition(
            SlotType.Expectation,
            "EXPECTATION",
            "Expectation",
            "What should the output look like?",
            "Format, length, tone, and success criteria.",
            IsRequired: false,
            Chips: [])
    ];
}
