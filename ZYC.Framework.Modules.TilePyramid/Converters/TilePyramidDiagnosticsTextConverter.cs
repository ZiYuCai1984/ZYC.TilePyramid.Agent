using ZYC.Framework.Core.Converters;
using ZYC.Framework.Core.Localizations;
using ZYC.TilePyramid.Wpf;

namespace ZYC.Framework.Modules.TilePyramid.Converters;

internal class TilePyramidDiagnosticsTextConverter : ValueConverterBase<TilePyramidDiagnostics?, string>
{
    protected override string InternalConvert(TilePyramidDiagnostics? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (!value.HasSource)
        {
            return L.T("No tile source");
        }

        if (!value.IsViewInitialized)
        {
            return L.T("Waiting for viewport layout");
        }

        if (value.ErrorMessage is not null)
        {
            return string.Format(L.T("Load failed: {0}"), value.ErrorMessage);
        }

        var cacheSizeMegabytes = value.CachedBytes / 1024d / 1024d;
        return string.Join("  |  ",
                   string.Format(L.T("Plane {0}/{1}"), value.FocalPlaneIndex + 1, value.FocalPlaneCount),
                   string.Format(L.T("Level {0}"), value.Level),
                   string.Format(L.T("zoom {0:P1}"), value.Scale),
                   string.Format(L.T("rotation {0:F1}°"), value.RotationDegrees),
                   string.Format(L.T("{0}/{1} visible tiles"), value.LoadedVisibleTileCount, value.VisibleTileCount),
                   string.Format(L.T("prefetch {0}/{1}"), value.LoadedPrefetchTileCount, value.PrefetchTileCount),
                   string.Format(L.T("loads {0}/{1}"), value.ActiveLoadCount, value.MaximumConcurrentLoads),
                   string.Format(L.T("cache {0} ({1:F1} MB)"), value.CachedTileCount, cacheSizeMegabytes))
               + (value.FallbackTileCount > 0
                   ? "  |  " + string.Format(L.T("{0} fallback tiles"), value.FallbackTileCount)
                   : string.Empty);
    }

    protected override TilePyramidDiagnostics? InternalConvertBack(string value)
    {
        throw new NotSupportedException();
    }
}
