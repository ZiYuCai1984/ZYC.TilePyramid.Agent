using System.Diagnostics;
using ZYC.Framework.Modules.TilePyramid.Abstractions.Agent;
using ZYC.TilePyramid.Annotations;

namespace ZYC.Framework.Modules.TilePyramid;

internal class TilePyramidAgentTaskEntry
{
    public TilePyramidAgentTaskEntry(TilePyramidAgentTask snapshot, RecognitionBatch batch)
    {
        Snapshot = snapshot;
        Batch = batch;
    }

    public TilePyramidAgentTask Snapshot { get; set; }
    public RecognitionBatch Batch { get; }
    public CancellationTokenSource Cancellation { get; } = new();
    public long LeaseTimestamp { get; set; } = Stopwatch.GetTimestamp();
    public bool IsActive => Snapshot.State is TilePyramidAgentTaskState.Running
        or TilePyramidAgentTaskState.CancellationRequested;
}
