using System.Globalization;
using BlazorApp1.Features.Models;
using Skender.Stock.Indicators;

namespace BlazorApp1.Features.Services;

public sealed class TechnicalAnalysisService
{
    public List<IndicatorValue> BuildIndicatorSnapshot(List<Quote> quotes, AnalysisSettings? p = null)
    {
        p ??= new AnalysisSettings();

        var ma5 = quotes.GetSma(p.Ma5Period).LastOrDefault(x => x.Sma is not null);
        var ma10 = quotes.GetSma(p.Ma10Period).LastOrDefault(x => x.Sma is not null);
        var smaShort = quotes.GetSma(p.SmaShortPeriod).LastOrDefault(x => x.Sma is not null);
        var sma = quotes.GetSma(p.Sma20Period).LastOrDefault(x => x.Sma is not null);
        var rsi = quotes.GetRsi(p.RsiPeriod).LastOrDefault(x => x.Rsi is not null);
        var stoch = quotes.GetStoch(p.StochKPeriod, p.StochKSmooth, p.StochDSmooth).LastOrDefault(x => x.Oscillator is not null || x.Signal is not null);
        var macd = quotes.GetMacd(p.MacdFast, p.MacdSlow, p.MacdSignalPeriod).LastOrDefault(x => x.Macd is not null || x.Signal is not null || x.Histogram is not null);
        var stochRsi = quotes.GetStochRsi(p.StochRsiPeriod, p.StochRsiStochPeriod, p.StochRsiSmooth, p.StochRsiSignalPeriod).LastOrDefault(x => x.StochRsi is not null || x.Signal is not null);

        return
        [
            new($"MA ({p.Ma5Period})", FormatValue(ma5?.Sma)),
            new($"MA ({p.Ma10Period})", FormatValue(ma10?.Sma)),
            new($"SMA krótki ({p.SmaShortPeriod})", FormatValue(smaShort?.Sma)),
            new($"SMA długi ({p.Sma20Period})", FormatValue(sma?.Sma)),
            new($"RSI ({p.RsiPeriod})", FormatValue(rsi?.Rsi)),
            new($"STOCH %K ({p.StochKPeriod},{p.StochKSmooth},{p.StochDSmooth})", FormatValue(stoch?.Oscillator)),
            new($"STOCH %D ({p.StochKPeriod},{p.StochKSmooth},{p.StochDSmooth})", FormatValue(stoch?.Signal)),
            new($"MACD ({p.MacdFast},{p.MacdSlow},{p.MacdSignalPeriod})", FormatValue(macd?.Macd)),
            new($"MACD Signal", FormatValue(macd?.Signal)),
            new($"MACD Histogram", FormatValue(macd?.Histogram)),
            new($"StochRSI ({p.StochRsiPeriod},{p.StochRsiStochPeriod},{p.StochRsiSmooth},{p.StochRsiSignalPeriod})", FormatValue(stochRsi?.StochRsi)),
            new($"StochRSI Signal", FormatValue(stochRsi?.Signal))
        ];
    }

    public List<DailyIndicatorRow> BuildDailyIndicators(List<Quote> quotes, AnalysisSettings? p = null)
    {
        p ??= new AnalysisSettings();

        var ma5Results = quotes.GetSma(p.Ma5Period).ToList();
        var ma10Results = quotes.GetSma(p.Ma10Period).ToList();
        var smaShortResults = quotes.GetSma(p.SmaShortPeriod).ToList();
        var smaResults = quotes.GetSma(p.Sma20Period).ToList();
        var rsiResults = quotes.GetRsi(p.RsiPeriod).ToList();
        var stochResults = quotes.GetStoch(p.StochKPeriod, p.StochKSmooth, p.StochDSmooth).ToList();
        var macdResults = quotes.GetMacd(p.MacdFast, p.MacdSlow, p.MacdSignalPeriod).ToList();
        var stochRsiResults = quotes.GetStochRsi(p.StochRsiPeriod, p.StochRsiStochPeriod, p.StochRsiSmooth, p.StochRsiSignalPeriod).ToList();

        var rows = new List<DailyIndicatorRow>(quotes.Count);
        for (int i = 0; i < quotes.Count; i++)
        {
            rows.Add(new DailyIndicatorRow(
                quotes[i].Date,
                quotes[i].Close,
                ma5Results[i].Sma,
                ma10Results[i].Sma,
                smaShortResults[i].Sma,
                smaResults[i].Sma,
                rsiResults[i].Rsi,
                stochResults[i].Oscillator,
                stochResults[i].Signal,
                macdResults[i].Macd,
                macdResults[i].Signal,
                macdResults[i].Histogram,
                stochRsiResults[i].StochRsi,
                stochRsiResults[i].Signal));
        }

        return rows;
    }

    public static string FormatValue(object? value)
    {
        if (value is null)
        {
            return "brak danych";
        }

        if (value is IFormattable formattable)
        {
            return formattable.ToString("0.####", CultureInfo.InvariantCulture);
        }

        return value.ToString() ?? "brak danych";
    }
}
