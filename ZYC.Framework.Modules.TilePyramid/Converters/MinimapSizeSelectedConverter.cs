using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using ZYC.Framework.Modules.TilePyramid.Abstractions;

namespace ZYC.Framework.Modules.TilePyramid.Converters;

internal class MinimapSizeSelectedConverter : MarkupExtension, IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        //!WARNING Exact equality is intentional: preset sizes (240x190 at 1x or 2x) are exactly representable as doubles.
        // SetMinimapSizeCommand assigns these same products; the bound values are stored sizes, not layout measurements.

        // ReSharper disable CompareOfFloatsByEqualityOperator
        return values[0] is double width && values[1] is double height && values[2] is double factor
               && width == TilePyramidDocumentState.DefaultMinimapWidth * factor
               && height == TilePyramidDocumentState.DefaultMinimapHeight * factor;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }
}