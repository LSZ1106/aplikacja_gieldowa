namespace BlazorApp1.Features.Services;

public sealed class PresentationPreferencesStore
{
    private readonly object sync = new();
    private PresentationPreferences current = new();

    public PresentationPreferences GetCurrent()
    {
        lock (sync)
        {
            return current with { };
        }
    }

    public void Save(PresentationPreferences preferences)
    {
        lock (sync)
        {
            current = preferences with { };
        }
    }
}

public sealed record PresentationPreferences(
    bool ShowMa = false,
    bool ShowSma = false,
    bool ShowSignals = false,
    bool ShowRsi = false,
    bool ShowMacd = false,
    bool ShowStoch = false,
    bool ShowStochRsi = false,
    int ChartZoomPercent = 100,
    int EquityChartZoomPercent = 100,
    string SignalSource = "BestStrategy",
    string SelectedIndividualName = "",
    string SelectedGroupLabel = "");