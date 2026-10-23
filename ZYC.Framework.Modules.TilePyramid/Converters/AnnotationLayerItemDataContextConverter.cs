using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using ZYC.Framework.Modules.TilePyramid.UI.Annotations;

namespace ZYC.Framework.Modules.TilePyramid.Converters;

internal class AnnotationLayerItemDataContextConverter : MarkupExtension, IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType,
        object parameter, CultureInfo culture)
    {
        //!WARNING values[0] is the LayerId key from CollectionViewGroup.Name; values[1] is the AnnotationLayers collection.
        return values[0] is string id
               && values[1] is IEnumerable<AnnotationLayerItem> items
            ? items.FirstOrDefault(item => item.Id == id)
            : null;
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