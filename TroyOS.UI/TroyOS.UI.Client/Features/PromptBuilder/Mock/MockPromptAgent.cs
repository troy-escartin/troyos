using TroyOS.UI.Client.Features.PromptBuilder.Models;

namespace TroyOS.UI.Client.Features.PromptBuilder.Mock;

/// <summary>
/// Local, in-memory stand-in for a real prompt-building agent. Every method here is a canned
/// response — no network calls, no LLM. Replace this class with a real agent service once the
/// backend is ready; nothing outside this feature depends on it.
/// </summary>
public static class MockPromptAgent
{
    public static IReadOnlyList<string> GetSuggestions(SlotType slot, string chipLabel) => (slot, chipLabel) switch
    {
        (SlotType.Role, "Use common roles") =>
        [
            "Senior software engineer",
            "Product strategist",
            "UX researcher",
            "Technical writer",
            "Data analyst"
        ],
        (SlotType.Role, "Show examples") =>
        [
            "A patient coding mentor for junior developers",
            "A pragmatic staff engineer reviewing architecture",
            "A concise API documentation writer"
        ],
        (SlotType.Action, "Use action verbs") =>
        [
            "Summarize",
            "Refactor",
            "Draft",
            "Compare",
            "Debug"
        ],
        (SlotType.Action, "Show examples") =>
        [
            "Summarize this article into 3 bullet points",
            "Refactor this function for readability",
            "Draft a project status update"
        ],
        _ => []
    };

    public static string SurpriseMeAnswer(SlotType slot) => slot switch
    {
        SlotType.Action => "Draft a concise release announcement highlighting the top three changes.",
        _ => "Something unexpected and delightful."
    };

    /// <summary>The mocked value used when a slot is skipped or "build it now" fills in the rest.</summary>
    public static string InferredValue(SlotType slot) => slot switch
    {
        SlotType.Role => "a helpful, knowledgeable assistant",
        SlotType.Action => "Complete the requested task clearly and accurately.",
        SlotType.Context => "No additional context was provided.",
        SlotType.Expectation => "A clear, well-structured response.",
        _ => string.Empty
    };

    /// <summary>
    /// TODO: This is a stub seam for out-of-order input. A real implementation would run the
    /// user's free-text message through a parser/agent call to detect whether it already answers
    /// more than one slot (e.g. "I need a technical writer to draft release notes for v2"), and
    /// return the extra slots it can infer so the conversation can skip questions already
    /// answered. For now this always returns null and every slot is asked individually.
    /// </summary>
    public static IReadOnlyDictionary<SlotType, string>? TryParseMultiSlotAnswer(string message) => null;

}
