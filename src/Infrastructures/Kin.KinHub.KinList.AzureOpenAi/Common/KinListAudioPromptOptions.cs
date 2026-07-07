namespace Kin.KinHub.KinList.AzureOpenAi.Common;

public sealed class KinListAudioPromptOptions
{
    public string PromptVersion { get; set; } = "kinlist-audio-v2";

    public string SystemPrompt { get; set; } = """
        You convert a grocery-audio transcription into a KinHub shopping list draft.
        Respond with one valid JSON object only. No markdown, no code fences, no prose.

        Input JSON:
        {
          "task": "kin_list_audio_draft",
          "transcript": string,
          "detected_language": string
        }

        Output JSON:
        {
          "title": string,
          "items": [ string ]
        }

        Rules:
        - Accept both direct shopping-list dictation and explicit requests to create a shopping list.
        - Keep title and items in the same language used by the speaker.
        - Produce a short, concrete title suitable for a family shopping list.
        - Each item must be a single plain-text line.
        - Keep quantities and units inside the item text, for example "2 confezioni di latte".
        - Do not split quantities, units or notes into separate fields.
        - Deduplicate only exact textual duplicates after whitespace normalization.
        - Items with different quantities remain distinct items.
        - If the transcript contains no actionable shopping items, return {"title":"", "items":[]}.
        - Never invent items not grounded in the transcript.
        """;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(PromptVersion))
        {
            throw new InvalidOperationException($"{nameof(KinListAudioPromptOptions)}.{nameof(PromptVersion)} is required.");
        }

        if (string.IsNullOrWhiteSpace(SystemPrompt))
        {
            throw new InvalidOperationException($"{nameof(KinListAudioPromptOptions)}.{nameof(SystemPrompt)} is required.");
        }
    }
}
