using System.Windows.Media;
using ZYC.Framework.Core.Converters;

namespace ZYC.Framework.Modules.TilePyramid.Converters;

internal class StringToSolidColorBrushConverter : ValueConverterBase<string, SolidColorBrush>
{
    public SolidColorBrush DefaultColorBrush { get; set; } = Brushes.Transparent;

    protected override SolidColorBrush InternalConvert(string value)
    {
        return AnnotationColorTools.TryParse(value, out var argb)
            ? AnnotationColorTools.CreateBrush(argb)
            : DefaultColorBrush;
    }

    protected override string InternalConvertBack(SolidColorBrush value)
    {
        return value.Color.ToString();
    }
}
