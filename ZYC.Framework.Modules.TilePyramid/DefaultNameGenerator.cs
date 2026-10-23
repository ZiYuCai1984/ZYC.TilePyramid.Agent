using System.Globalization;

namespace ZYC.Framework.Modules.TilePyramid;

internal static class DefaultNameGenerator
{
    public static string GetNext(string prefix, IEnumerable<string> names)
    {
        var namePrefix = prefix + " ";
        long maximum = 0;
        foreach (var name in names)
        {
            if (name.StartsWith(namePrefix, StringComparison.Ordinal)
                && long.TryParse(name.AsSpan(namePrefix.Length), NumberStyles.None,
                    CultureInfo.InvariantCulture, out var number))
            {
                maximum = Math.Max(maximum, number);
            }
        }

        return namePrefix + checked(maximum + 1).ToString(CultureInfo.InvariantCulture);
    }
}
