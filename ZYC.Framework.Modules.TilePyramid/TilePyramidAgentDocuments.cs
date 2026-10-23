using ZYC.CoreToolkit.Extensions.Autofac.Attributes;

namespace ZYC.Framework.Modules.TilePyramid;

/// <summary>Loaded documents, independent of workspace layout. Accessed only on the UI thread.</summary>
[RegisterSingleInstance]
internal class TilePyramidAgentDocuments
{
    private readonly Dictionary<Guid, TilePyramidTabItem> _documents = new();

    public IEnumerable<TilePyramidTabItem> Items => _documents.Values.Where(tab => tab.IsAgentAvailable);

    // Notify shared views when recognition contents or the set of loaded documents changes.
    public event EventHandler? LayerContentsChanged;
    public event EventHandler? AnnotationEditingChanged;
    public event Action<TilePyramidTabItem>? DocumentClosing;

    public void NotifyAnnotationEditingChanged()
    {
        AnnotationEditingChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Add(TilePyramidTabItem tab)
    {
        if (!_documents.Values.Any(other => ReferenceEquals(
                other.AnnotationImage.RecognitionBatches, tab.AnnotationImage.RecognitionBatches)))
        {
            tab.AnnotationImage.RecognitionBatches.Changed += OnRecognitionBatchesChanged;
        }

        _documents.Add(tab.TabReference.Id, tab);
        LayerContentsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Remove(TilePyramidTabItem tab)
    {
        DocumentClosing?.Invoke(tab);
        if (_documents.Remove(tab.TabReference.Id))
        {
            if (!_documents.Values.Any(other => ReferenceEquals(
                    other.AnnotationImage.RecognitionBatches, tab.AnnotationImage.RecognitionBatches)))
            {
                tab.AnnotationImage.RecognitionBatches.Changed -= OnRecognitionBatchesChanged;
            }

            LayerContentsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnRecognitionBatchesChanged(object? sender, EventArgs e)
    {
        LayerContentsChanged?.Invoke(this, EventArgs.Empty);
    }
}
