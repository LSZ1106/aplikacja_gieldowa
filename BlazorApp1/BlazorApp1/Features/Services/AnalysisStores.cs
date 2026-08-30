using BlazorApp1.Features.Models;

namespace BlazorApp1.Features.Services;

public sealed class AnalysisStores
{
    private readonly object sync = new();
    private AnalysisSettings current = new();

    public AnalysisSettings GetCurrent()
    {
        lock (sync)
        {
            return current.Clone();
        }
    }

    public void Save(AnalysisSettings settings)
    {
        lock (sync)
        {
            current = settings.Clone();
        }
    }
}


public sealed class SimulationResultsStore
{
    private readonly object sync = new();
    private FullSimulationResult? current;
    private AnalysisSettings? currentSettings;

    public FullSimulationResult? GetCurrent()
    {
        lock (sync)
        {
            return current;
        }
    }

    public FullSimulationResult? GetCurrent(AnalysisSettings settings)
    {
        lock (sync)
        {
            if (current is null || currentSettings is null)
            {
                return null;
            }

            return AreEquivalent(currentSettings, settings) ? current : null;
        }
    }

    public void Save(FullSimulationResult result, AnalysisSettings settings)
    {
        lock (sync)
        {
            current = result;
            currentSettings = settings.Clone();
        }
    }

    public void Clear()
    {
        lock (sync)
        {
            current = null;
            currentSettings = null;
        }
    }

    private static bool AreEquivalent(AnalysisSettings left, AnalysisSettings right)
        => left.DataSourceType == right.DataSourceType
           && left.SelectedDataSetId == right.SelectedDataSetId
           && left.CsvRelativePath == right.CsvRelativePath
           && left.InitialCapital == right.InitialCapital
           && left.SimulationDays == right.SimulationDays
           && left.UseSma == right.UseSma
           && left.UseRsi == right.UseRsi
           && left.UseStoch == right.UseStoch
           && left.UseMacd == right.UseMacd
           && left.UseStochRsi == right.UseStochRsi
           && left.GroupSignalAgreementPercent == right.GroupSignalAgreementPercent
           && left.Ma5Period == right.Ma5Period
           && left.Ma10Period == right.Ma10Period
           && left.Sma20Period == right.Sma20Period
           && left.RsiPeriod == right.RsiPeriod
           && left.RsiBuyThreshold == right.RsiBuyThreshold
           && left.RsiSellThreshold == right.RsiSellThreshold
           && left.StochKPeriod == right.StochKPeriod
           && left.StochKSmooth == right.StochKSmooth
           && left.StochDSmooth == right.StochDSmooth
           && left.StochBuyThreshold == right.StochBuyThreshold
           && left.StochSellThreshold == right.StochSellThreshold
           && left.MacdFast == right.MacdFast
           && left.MacdSlow == right.MacdSlow
           && left.MacdSignalPeriod == right.MacdSignalPeriod
           && left.StochRsiPeriod == right.StochRsiPeriod
           && left.StochRsiStochPeriod == right.StochRsiStochPeriod
           && left.StochRsiSmooth == right.StochRsiSmooth
           && left.StochRsiSignalPeriod == right.StochRsiSignalPeriod
           && left.StochRsiBuyThreshold == right.StochRsiBuyThreshold
           && left.StochRsiSellThreshold == right.StochRsiSellThreshold
           && left.MaxUnitsPerBuySignal == right.MaxUnitsPerBuySignal;
}

