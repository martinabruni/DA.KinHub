namespace Kin.KinHub.Core.OpenAi.RecipeAssistantFeature;

internal sealed record AdaptationResponse(
    string TaskType,
    RecipeJson OriginalRecipe,
    IReadOnlyList<StepJson> AdaptedSteps,
    IReadOnlyList<ChangeJson> Changes);
