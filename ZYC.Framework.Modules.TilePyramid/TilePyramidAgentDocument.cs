using ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;
using ZYC.TilePyramid.Annotations;
using ZYC.TilePyramid.Core.Sources;

namespace ZYC.Framework.Modules.TilePyramid;

/// <summary>Tab-owned crops and source leases over shared image recognition data. Accessed on the UI thread.</summary>
internal class TilePyramidAgentDocument : IDisposable
{
    private readonly object _gate = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly ITilePyramidSource _source;
    private bool _cancellationCompleted;
    private bool _closed;
    private int _readers;

    public TilePyramidAgentDocument(ITilePyramidSource source, AnnotationImage image)
    {
        _source = source;
        Image = image;
        Workspace = new AnnotationWorkspace(new Dictionary<string, AnnotationImage> { [image.ImageId] = image });
    }

    public AnnotationImage Image { get; }
    public AnnotationWorkspace Workspace { get; }
    public Dictionary<Guid, TilePyramidAgentCrop> Crops { get; } = new();

    public void Dispose()
    {
        lock (_gate)
        {
            if (_closed)
            {
                return;
            }

            _closed = true;
        }

        try
        {
            _lifetime.Cancel();
        }
        finally
        {
            bool release;
            lock (_gate)
            {
                _cancellationCompleted = true;
                release = _readers == 0;
            }

            if (release)
            {
                DisposeSource();
            }
        }
    }

    public RecognitionBatch BeginRecognition(string model, int focalPlane)
    {
        return Workspace.BeginRecognition(Image.ImageId, model, focalPlane);
    }

    public RecognitionBatch RequireBatch(Guid id, bool requireOpen = false)
    {
        var batch = Image.RecognitionBatches.Find(id);
        if (batch is null || (requireOpen && !batch.IsOpen))
        {
            throw new InvalidOperationException(
                "The recognition batch does not exist for this image or is closed for submissions.");
        }

        return batch;
    }

    public SourceReadLease AcquireSource()
    {
        lock (_gate)
        {
            if (_closed)
            {
                throw new ObjectDisposedException(nameof(TilePyramidAgentDocument));
            }

            _readers++;
            return new SourceReadLease(this, _source, _lifetime.Token);
        }
    }

    private void ReleaseSource()
    {
        bool release;
        lock (_gate)
        {
            release = --_readers == 0 && _cancellationCompleted;
        }

        if (release)
        {
            DisposeSource();
        }
    }

    private void DisposeSource()
    {
        try
        {
            _source.Dispose();
        }
        finally
        {
            _lifetime.Dispose();
        }
    }

    public class SourceReadLease : IDisposable
    {
        private TilePyramidAgentDocument? _owner;

        public SourceReadLease(TilePyramidAgentDocument owner, ITilePyramidSource source, CancellationToken token)
        {
            _owner = owner;
            Source = source;
            CancellationToken = token;
        }

        public ITilePyramidSource Source { get; }
        public CancellationToken CancellationToken { get; }

        public void Dispose()
        {
            Interlocked.Exchange(ref _owner, null)?.ReleaseSource();
        }
    }
}
