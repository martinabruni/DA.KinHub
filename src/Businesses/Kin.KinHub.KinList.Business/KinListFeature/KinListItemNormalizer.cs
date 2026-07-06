namespace Kin.KinHub.KinList.Business.KinListFeature;

internal static class KinListItemNormalizer
{
    public static string Normalize(string text) => text.Trim();

    public static IReadOnlyList<string> NormalizeDistinct(IEnumerable<string> items) =>
        items.Where(i => !string.IsNullOrWhiteSpace(i))
             .Select(Normalize)
             .Distinct(StringComparer.OrdinalIgnoreCase)
             .ToList();
}
