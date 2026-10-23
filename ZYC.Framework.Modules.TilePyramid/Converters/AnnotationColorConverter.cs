using System.Windows.Media;
using ZYC.Framework.Core.Converters;

namespace ZYC.Framework.Modules.TilePyramid.Converters;

internal class AnnotationColorConverter : ValueConverterBase<uint, Brush>
{
    protected override Brush InternalConvert(uint value)
    {
        return AnnotationColorTools.CreateBrush(value);
    }

    protected override uint InternalConvertBack(Brush value)
    {
        throw new NotSupportedException();
    }
}
