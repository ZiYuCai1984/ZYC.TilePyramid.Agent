using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Modules.TilePyramid.Abstractions;

namespace ZYC.Framework.Modules.TilePyramid.UI;

[Register]
internal partial class TopToolbarView
{
    public TopToolbarView(TilePyramidConfig tilePyramidConfig)
    {
        TilePyramidConfig = tilePyramidConfig;
        
        InitializeComponent();
    }

    public TilePyramidConfig TilePyramidConfig { get; }
}