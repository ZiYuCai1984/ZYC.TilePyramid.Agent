using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace ZYC.Framework.Modules.TilePyramid.Converters;

internal class AnnotationLayerSelectedConverter : MarkupExtension, IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return values[0] is string id && values[1] is string selectedId && id == selectedId;
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
