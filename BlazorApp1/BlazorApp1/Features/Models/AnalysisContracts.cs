namespace BlazorApp1.Features.Models;

public sealed record IndicatorValue(string Name, string Value);

public sealed record TradeSignal(DateTime Date, string Side, decimal Price);

public sealed record HistoricalQuoteRow(
    DateTime Date,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal Volume);

public sealed record DailyIndicatorRow(
    DateTime Date,
    decimal Close,
    double? Ma5,
    double? Ma10,
    double? SmaShort,
    double? Sma20,
    double? Rsi14,
    double? StochK,
    double? StochD,
    double? Macd,
    double? MacdSignal,
    double? MacdHistogram,
    double? StochRsi,
    double? StochRsiSignal);

public sealed record StrategyConfig(
    bool UseSma,
    int SmaPeriod,
    bool UseRsi,
    int RsiPeriod,
    double RsiBuyThreshold,
    double RsiSellThreshold,
    bool UseMacd,
    bool UseStochRsi,
    double StochRsiBuyThreshold,
    double StochRsiSellThreshold);

public sealed record StrategyResult(
    StrategyConfig Config,
    decimal FinalCapital,
    decimal ReturnPct,
    int Trades,
    decimal WinRate,
    List<decimal> EquityCurve,
    List<TradeSignal> Signals);

public sealed record AnalysisResult(
    DateTime LastQuoteDate,
    List<IndicatorValue> Indicators,
    List<DailyIndicatorRow> DailyIndicators,
    List<HistoricalQuoteRow> HistoricalQuotes,
    List<DateTime> Dates,
    List<decimal> Closes,
    StrategyResult BestStrategy);

public sealed record SingleIndicatorSimResult(
    string IndicatorName,
    decimal FinalCapital,
    decimal ReturnPct,
    int Trades,
    decimal WinRate,
    List<decimal> EquityCurve,
    List<TradeSignal> Signals);

public sealed record IndicatorGroupSimResult(
    List<string> IndicatorNames,
    decimal FinalCapital,
    decimal ReturnPct,
    int Trades,
    decimal WinRate,
    List<decimal> EquityCurve,
    List<TradeSignal> Signals)
{
    public string GroupLabel => string.Join(" + ", IndicatorNames);
}

public sealed record FullSimulationResult(
    List<SingleIndicatorSimResult> IndividualRanking,
    List<IndicatorGroupSimResult> GroupRanking,
    int TotalAvailableDays,
    int SimulatedDays);
