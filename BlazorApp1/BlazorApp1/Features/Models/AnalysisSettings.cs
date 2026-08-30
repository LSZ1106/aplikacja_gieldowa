namespace BlazorApp1.Features.Models;

public sealed class AnalysisSettings
{
    public string DataSourceType { get; set; } = "Database";
    public string CsvRelativePath { get; set; } = "data/quotes.csv";
    public decimal InitialCapital { get; set; } = 10000m;
    public int? SelectedDataSetId { get; set; }
    public int? SimulationDays { get; set; }
    public int? SimulationCalendarDays { get; set; } = 365;
    public int MaxUnitsPerBuySignal { get; set; } = 0;

    public bool UseSma { get; set; } = true;
    public bool UseRsi { get; set; } = true;
    public bool UseMacd { get; set; } = true;
    public bool UseStochRsi { get; set; } = true;
    public bool UseStoch { get; set; } = true;
    public int GroupSignalAgreementPercent { get; set; } = 50;
    public int RsiBuyThreshold { get; set; } = 30;
    public int RsiSellThreshold { get; set; } = 70;
    public int StochBuyThreshold { get; set; } = 20;
    public int StochSellThreshold { get; set; } = 80;
    public int StochRsiBuyThreshold { get; set; } = 20;
    public int StochRsiSellThreshold { get; set; } = 80;

    public int Ma5Period { get; set; } = 5;
    public int Ma10Period { get; set; } = 10;
    public int SmaShortPeriod { get; set; } = 10;
    public int Sma20Period { get; set; } = 20;
    public int RsiPeriod { get; set; } = 14;
    public int StochKPeriod { get; set; } = 9;
    public int StochKSmooth { get; set; } = 3;
    public int StochDSmooth { get; set; } = 6;
    public int MacdFast { get; set; } = 12;
    public int MacdSlow { get; set; } = 26;
    public int MacdSignalPeriod { get; set; } = 9;
    public int StochRsiPeriod { get; set; } = 14;
    public int StochRsiStochPeriod { get; set; } = 14;
    public int StochRsiSmooth { get; set; } = 3;
    public int StochRsiSignalPeriod { get; set; } = 3;

    public AnalysisSettings Clone()
    {
        return new AnalysisSettings
        {
            DataSourceType = DataSourceType,
            CsvRelativePath = CsvRelativePath,
            InitialCapital = InitialCapital,
            SelectedDataSetId = SelectedDataSetId,
            SimulationDays = SimulationDays,
            SimulationCalendarDays = SimulationCalendarDays,
            MaxUnitsPerBuySignal = MaxUnitsPerBuySignal,
            UseSma = UseSma,
            UseRsi = UseRsi,
            UseMacd = UseMacd,
            UseStochRsi = UseStochRsi,
            UseStoch = UseStoch,
            GroupSignalAgreementPercent = GroupSignalAgreementPercent,
            RsiBuyThreshold = RsiBuyThreshold,
            RsiSellThreshold = RsiSellThreshold,
            StochBuyThreshold = StochBuyThreshold,
            StochSellThreshold = StochSellThreshold,
            StochRsiBuyThreshold = StochRsiBuyThreshold,
            StochRsiSellThreshold = StochRsiSellThreshold,
            Ma5Period = Ma5Period,
            Ma10Period = Ma10Period,
            SmaShortPeriod = SmaShortPeriod,
            Sma20Period = Sma20Period,
            RsiPeriod = RsiPeriod,
            StochKPeriod = StochKPeriod,
            StochKSmooth = StochKSmooth,
            StochDSmooth = StochDSmooth,
            MacdFast = MacdFast,
            MacdSlow = MacdSlow,
            MacdSignalPeriod = MacdSignalPeriod,
            StochRsiPeriod = StochRsiPeriod,
            StochRsiStochPeriod = StochRsiStochPeriod,
            StochRsiSmooth = StochRsiSmooth,
            StochRsiSignalPeriod = StochRsiSignalPeriod
        };
    }
}
