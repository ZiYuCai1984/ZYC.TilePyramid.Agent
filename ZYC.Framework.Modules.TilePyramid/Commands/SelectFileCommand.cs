using Microsoft.Extensions.Logging;
using ZYC.CoreToolkit.Extensions.Autofac.Attributes;
using ZYC.Framework.Abstractions;
using ZYC.Framework.Abstractions.Notification.Toast;
using ZYC.Framework.Abstractions.Tab;
using ZYC.Framework.Core;
using ZYC.Framework.Core.Commands;
using ZYC.Framework.Core.Localizations;
using ZYC.Framework.Modules.TilePyramid.Abstractions;

namespace ZYC.Framework.Modules.TilePyramid.Commands;

[RegisterSingleInstance]
internal class SelectFileCommand : AsyncCommandBase
{
    public SelectFileCommand(
        ILogger<SelectFileCommand> logger,
        ITabManager tabManager,
        IToastManager toastManager)
    {
        Logger = logger;
        TabManager = tabManager;
        ToastManager = toastManager;
    }

    private ILogger<SelectFileCommand> Logger { get; }

    private ITabManager TabManager { get; }

    private IToastManager ToastManager { get; }

    protected override async Task InternalExecuteAsync(object? parameter)
    {
        try
        {
            // File dialog filters are formatted as "Description|*.ext". 
            // This translates the descriptions (even indices) using L.T() and keeps the extension patterns (odd indices) intact.
            var filter = string.Join("|", TilePyramidModuleConstants.FileDialogFilter.Split('|')
                .Select((part, index) => index % 2 == 0 ? L.T(part) : part));
            
            var filePath = DialogTools.SelectFileDialog(filter: filter);
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            var kind = TilePyramidDocumentTools.GetTilePyramidSourceKindFromFilePath(filePath);

            var uri = TilePyramidModuleConstants.CreateDocumentUri(kind, new Uri(filePath, UriKind.Absolute));
            await TabManager.NavigateAsync(uri);
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            ToastManager.PromptException(ex);
        }
    }
}