namespace ZYC.Framework.Modules.TilePyramid.UI;

internal partial class TilePyramidDocumentView
{
    private const int MaximumZoomPresetCount = 10;
    private double _presetMaximumScale;

    private double _presetReferenceScale;

    public IReadOnlyList<double> ZoomPresets { get; private set; } = [];

    private void MainView_OnMetricsChanged(object? sender, EventArgs e)
    {
        var metrics = MainView.Metrics;
        // Resizing or changing the zoom limit can change the available presets
        // without changing the navigation state. Tile-loading updates do not.
        if (!metrics.ReferenceScale.Equals(_presetReferenceScale)
            || !metrics.MaximumScale.Equals(_presetMaximumScale))
        {
            UpdateStatus();
        }
    }

    private void UpdateZoomPresets(double referenceScale, double maximumScale)
    {
        if (referenceScale.Equals(_presetReferenceScale) && maximumScale.Equals(_presetMaximumScale))
        {
            return;
        }

        _presetReferenceScale = referenceScale;
        _presetMaximumScale = maximumScale;
        var maximumZoom = Math.Min(maximumScale / referenceScale, double.MaxValue);

        var factors = new List<double>();
        double[] stepMultipliers = { 1.0, 2.0, 5.0 };

        // Generate the 1-2-5 sequence across powers of ten, up to the zoom limit.
        for (var decade = 1d; double.IsFinite(decade) && decade <= maximumZoom; decade *= 10)
        {
            foreach (var multiplier in stepMultipliers)
            {
                var factor = decade * multiplier;
                if (!double.IsFinite(factor) || factor > maximumZoom)
                {
                    goto FinishGeneration;
                }

                factors.Add(factor);
            }
        }

        FinishGeneration:
        // Include the exact maximum zoom even when it is outside the 1-2-5 sequence.
        if (factors.Count == 0 || factors.Last() < maximumZoom)
        {
            factors.Add(maximumZoom);
        }

        // Limit the preset count by sampling evenly across the candidate indices.
        if (factors.Count > MaximumZoomPresetCount && MaximumZoomPresetCount >= 2)
        {
            var sampledFactors = new List<double> { factors[0] }; // Always keep the first preset (1x).

            // Span the full index range while reserving both endpoints.
            var indexStep = (double)(factors.Count - 1) / (MaximumZoomPresetCount - 1);

            for (var i = 1; i < MaximumZoomPresetCount - 1; i++)
            {
                // Round each evenly spaced position to a candidate index.
                var index = (int)Math.Round(i * indexStep);
                sampledFactors.Add(factors[index]);
            }

            sampledFactors.Add(factors[^1]); // Always keep the exact maximum zoom.

            // Replace the candidates with the distinct sampled factors.
            factors = sampledFactors.Distinct().ToList();
        }

        // Rebuild the buttons only when their factors change to preserve keyboard focus.
        if (!factors.SequenceEqual(ZoomPresets))
        {
            ZoomPresets = factors.ToArray();
            OnPropertyChanged(nameof(ZoomPresets));
        }
    }

}
