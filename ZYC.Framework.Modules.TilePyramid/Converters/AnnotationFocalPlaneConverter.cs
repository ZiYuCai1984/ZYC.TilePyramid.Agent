using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using ZYC.TilePyramid.Annotations;

namespace ZYC.Framework.Modules.TilePyramid.Converters;

internal class AnnotationFocalPlaneConverter : MarkupExtension, IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        // Recognition results use their batch plane; manual annotations follow the current view.
        var currentPlane = values[2] is int index ? index : 0;
        return values[0] is RecognitionSource source && values[1] is RecognitionBatchCollection batches
            ? batches.Find(source.BatchId)?.FocalPlaneIndex ?? currentPlane
            : currentPlane;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}
