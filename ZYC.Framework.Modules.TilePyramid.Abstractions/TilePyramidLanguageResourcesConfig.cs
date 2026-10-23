using ZYC.Framework.Modules.Language.Abstractions;
using ZYC.Framework.Modules.Translator.Abstractions;

namespace ZYC.Framework.Modules.TilePyramid.Abstractions;

#pragma warning disable CS1591
public class TilePyramidLanguageResourcesConfig : ILanguageResourcesConfig
{
    public Dictionary<LanguageType, Dictionary<string, string>> Resources { get; set; } = new();
}