using System.Globalization;
using System.Windows.Media;

namespace ZYC.Framework.Modules.TilePyramid;

internal static class AnnotationColorTools
{
    public static bool TryParse(string? text, out uint argb)
    {
        var hex = text.AsSpan().Trim();
        if (!hex.IsEmpty && hex[0] == '#')
        {
            hex = hex[1..];
        }

        argb = 0;
        if (hex.Length is not (6 or 8)
            || !uint.TryParse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out argb))
        {
            return false;
        }

        if (hex.Length == 6)
        {
            argb |= 0xFF000000;
        }

        return true;
    }

    public static SolidColorBrush CreateBrush(uint argb)
    {
        var brush = new SolidColorBrush(Color.FromArgb(
            (byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb));
        brush.Freeze();
        return brush;
    }
}
