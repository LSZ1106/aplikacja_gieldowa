using BlazorApp1.Features.Models;
using Skender.Stock.Indicators;

namespace BlazorApp1.Features.Services;

public sealed class TradingSimulationService
{
    private enum SignalType
    {
        Buy,
        Sell,
        None
    }

    // Individual indicator definitions 

    private static readonly (string Key, Func<IndicatorCaches, DateTime, DateTime, decimal, decimal, (bool buy, bool sell)?> Evaluate)[] IndicatorDefinitions =
    [
        ("MA", (c, d, pd, close, prevClose) =>
        {
            if (!c.Ma5.TryGetValue(d, out var maShortCurr) || !c.Ma10.TryGetValue(d, out var maLongCurr) ||
                !c.Ma5.TryGetValue(pd, out var maShortPrev) || !c.Ma10.TryGetValue(pd, out var maLongPrev))
            {
                return null;
            }

            return ToSignalTuple(GetMACrossoverAction(maShortCurr, maLongCurr, maShortPrev, maLongPrev, (double)close, (double)prevClose));
        }),
        ("SMA", (c, d, pd, close, prevClose) =>
        {
            if (!c.SmaShort.TryGetValue(d, out var maShortCurr) || !c.Sma20.TryGetValue(d, out var maLongCurr) ||
                !c.SmaShort.TryGetValue(pd, out var maShortPrev) || !c.Sma20.TryGetValue(pd, out var maLongPrev))
            {
                return null;
            }

            return ToSignalTuple(GetMACrossoverAction(maShortCurr, maLongCurr, maShortPrev, maLongPrev, (double)close, (double)prevClose));
        }),
        ("RSI", (c, d, pd, close, prevClose) =>
        {
            if (!c.Rsi.TryGetValue(d, out var rsiCurr) || !c.Rsi.TryGetValue(pd, out var rsiPrev)) return null;
            return ToSignalTuple(GetRSIAction(rsiCurr, rsiPrev, 30, 70, (double)close, (double)prevClose));
        }),
        ("STOCH", (c, d, pd, _, _) =>
        {
            if (!c.StochK.TryGetValue(d, out var kCurr) || !c.StochD.TryGetValue(d, out var dCurr) ||
                !c.StochK.TryGetValue(pd, out var kPrev) || !c.StochD.TryGetValue(pd, out var dPrev))
            {
                return null;
            }

            return ToSignalTuple(GetStochasticAction(kCurr, dCurr, kPrev, dPrev, 20, 80));
        }),
        ("MACD", (c, d, pd, _, _) =>
        {
            if (!c.Macd.TryGetValue(d, out var curr) || !c.Macd.TryGetValue(pd, out var prev)) return null;

            return ToSignalTuple(GetMACDAction(
                curr.Macd, curr.Signal, curr.Histogram,
                prev.Macd, prev.Signal, prev.Histogram));
        }),
        ("StochRSI", (c, d, pd, _, _) =>
        {
            if (!c.StochRsi.TryGetValue(d, out var kCurr) || !c.StochRsiSignal.TryGetValue(d, out var dCurr) ||
                !c.StochRsi.TryGetValue(pd, out var kPrev) || !c.StochRsiSignal.TryGetValue(pd, out var dPrev))
            {
                return null;
            }

            return ToSignalTuple(GetStochRSIAction(kCurr, dCurr, kPrev, dPrev, 20, 80));
        }),
    ];

    private static List<(string Name, Func<IndicatorCaches, DateTime, DateTime, decimal, decimal, (bool buy, bool sell)?> Evaluate)>
        GetEnabledIndicatorDefinitions(AnalysisSettings settings)
    {
        var enabled = new List<(string Name, Func<IndicatorCaches, DateTime, DateTime, decimal, decimal, (bool buy, bool sell)?> Evaluate)>();

        foreach (var def in IndicatorDefinitions)
        {
            if (def.Key is "MA" or "SMA")
            {
                if (!settings.UseSma) continue;

                string smaName = def.Key switch
                {
                    "MA" => $"MA({settings.Ma5Period},{settings.Ma10Period})",
                    _ => $"SMA({settings.SmaShortPeriod},{settings.Sma20Period})"
                };

                enabled.Add((smaName, def.Evaluate));
                continue;
            }

            if (def.Key == "RSI")
            {
                if (settings.UseRsi)
                {
                    enabled.Add(($"RSI({settings.RsiPeriod})", (c, d, pd, close, prevClose) =>
                    {
                        if (!c.Rsi.TryGetValue(d, out var rsiCurr) || !c.Rsi.TryGetValue(pd, out var rsiPrev)) return null;
                        return ToSignalTuple(GetRSIAction(rsiCurr, rsiPrev, settings.RsiBuyThreshold, settings.RsiSellThreshold, (double)close, (double)prevClose));
                    }));
                }
                continue;
            }

            if (def.Key == "MACD")
            {
                if (settings.UseMacd) enabled.Add(($"MACD({settings.MacdFast},{settings.MacdSlow},{settings.MacdSignalPeriod})", def.Evaluate));
                continue;
            }

            if (def.Key == "StochRSI")
            {
                if (settings.UseStochRsi)
                {
                    enabled.Add(($"StochRSI({settings.StochRsiPeriod},{settings.StochRsiStochPeriod},{settings.StochRsiSmooth},{settings.StochRsiSignalPeriod})", (c, d, pd, _, _) =>
                    {
                        if (!c.StochRsi.TryGetValue(d, out var kCurr) || !c.StochRsiSignal.TryGetValue(d, out var dCurr) ||
                            !c.StochRsi.TryGetValue(pd, out var kPrev) || !c.StochRsiSignal.TryGetValue(pd, out var dPrev))
                        {
                            return null;
                        }

                        return ToSignalTuple(GetStochRSIAction(kCurr, dCurr, kPrev, dPrev, settings.StochRsiBuyThreshold, settings.StochRsiSellThreshold));
                    }));
                }
                continue;
            }

            if (def.Key == "STOCH")
            {
                if (!settings.UseStoch) continue;
                enabled.Add(($"STOCH({settings.StochKPeriod},{settings.StochKSmooth},{settings.StochDSmooth})", (c, d, pd, _, _) =>
                {
                    if (!c.StochK.TryGetValue(d, out var kCurr) || !c.StochD.TryGetValue(d, out var dCurr) ||
                        !c.StochK.TryGetValue(pd, out var kPrev) || !c.StochD.TryGetValue(pd, out var dPrev))
                    {
                        return null;
                    }

                    return ToSignalTuple(GetStochasticAction(kCurr, dCurr, kPrev, dPrev, settings.StochBuyThreshold, settings.StochSellThreshold));
                }));
                continue;
            }

            enabled.Add((def.Key, def.Evaluate));
        }

        return enabled;
    }

    // Full simulation: individual ranking + group ranking

    public FullSimulationResult RunFullSimulation(List<Quote> quotes, AnalysisSettings settings)
    {
        var (simQuotes, totalDays, simDays) = PrepareSimulationQuotes(quotes, settings);
        var caches = BuildCaches(quotes, settings);
        var enabledIndicators = GetEnabledIndicatorDefinitions(settings);

        if (enabledIndicators.Count == 0)
        {
            throw new InvalidOperationException("Brak aktywnych wskaźników do symulacji. Włącz co najmniej jeden wskaźnik w Ustawieniach.");
        }

        var individual = BuildIndividualRanking(simQuotes, settings, caches, enabledIndicators);
        var groups = BuildGroupRanking(simQuotes, settings, caches, enabledIndicators);
        return new FullSimulationResult(individual, groups, totalDays, simDays);
    }

    public FullSimulationResult RunIndividualSimulation(List<Quote> quotes, AnalysisSettings settings)
    {
        var (simQuotes, totalDays, simDays) = PrepareSimulationQuotes(quotes, settings);
        var caches = BuildCaches(quotes, settings);
        var enabledIndicators = GetEnabledIndicatorDefinitions(settings);

        if (enabledIndicators.Count == 0)
        {
            throw new InvalidOperationException("Brak aktywnych wskaźników do symulacji. Włącz co najmniej jeden wskaźnik w Ustawieniach.");
        }

        var individual = BuildIndividualRanking(simQuotes, settings, caches, enabledIndicators);
        return new FullSimulationResult(individual, [], totalDays, simDays);
    }

    public FullSimulationResult RunGroupSimulation(List<Quote> quotes, AnalysisSettings settings)
    {
        var (simQuotes, totalDays, simDays) = PrepareSimulationQuotes(quotes, settings);
        var caches = BuildCaches(quotes, settings);
        var enabledIndicators = GetEnabledIndicatorDefinitions(settings);

        if (enabledIndicators.Count == 0)
        {
            throw new InvalidOperationException("Brak aktywnych wskaźników do symulacji. Włącz co najmniej jeden wskaźnik w Ustawieniach.");
        }

        var groups = BuildGroupRanking(simQuotes, settings, caches, enabledIndicators);
        return new FullSimulationResult([], groups, totalDays, simDays);
    }

    private static (List<Quote> simQuotes, int totalDays, int simDays) PrepareSimulationQuotes(List<Quote> quotes, AnalysisSettings settings)
    {
        int totalDays = quotes.Count;
        int simDays = settings.SimulationDays is > 0
            ? Math.Min(settings.SimulationDays.Value, totalDays)
            : totalDays;

        var simQuotes = quotes.Skip(totalDays - simDays).ToList();
        return (simQuotes, totalDays, simDays);
    }

    private static List<SingleIndicatorSimResult> BuildIndividualRanking(
        List<Quote> simQuotes,
        AnalysisSettings settings,
        IndicatorCaches caches,
        List<(string Name, Func<IndicatorCaches, DateTime, DateTime, decimal, decimal, (bool buy, bool sell)?> Evaluate)> enabledIndicators)
    {
        var individual = new List<SingleIndicatorSimResult>();
        for (int i = 0; i < enabledIndicators.Count; i++)
        {
            var (name, eval) = enabledIndicators[i];
            var res = RunSingleIndicatorBacktest(simQuotes, settings.InitialCapital, caches, eval, settings.MaxUnitsPerBuySignal);
            individual.Add(new SingleIndicatorSimResult(name, res.finalCapital, res.returnPct, res.trades, res.winRate, res.equity, res.signals));
        }

        return individual.OrderByDescending(x => x.ReturnPct).ToList();
    }

    private static List<IndicatorGroupSimResult> BuildGroupRanking(
        List<Quote> simQuotes,
        AnalysisSettings settings,
        IndicatorCaches caches,
        List<(string Name, Func<IndicatorCaches, DateTime, DateTime, decimal, decimal, (bool buy, bool sell)?> Evaluate)> enabledIndicators)
    {
        int count = enabledIndicators.Count;
        int totalCombinations = (1 << count) - 1;
        var groupResults = new List<IndicatorGroupSimResult>(totalCombinations);

        for (int mask = 1; mask <= totalCombinations; mask++)
        {
            var names = new List<string>();
            var evals = new List<Func<IndicatorCaches, DateTime, DateTime, decimal, decimal, (bool buy, bool sell)?>>();

            for (int bit = 0; bit < count; bit++)
            {
                if ((mask & (1 << bit)) != 0)
                {
                    names.Add(enabledIndicators[bit].Name);
                    evals.Add(enabledIndicators[bit].Evaluate);
                }
            }

            var res = RunGroupBacktest(simQuotes, settings.InitialCapital, caches, evals, settings.GroupSignalAgreementPercent, settings.MaxUnitsPerBuySignal);
            groupResults.Add(new IndicatorGroupSimResult(names, res.finalCapital, res.returnPct, res.trades, res.winRate, res.equity, res.signals));
        }

        return groupResults.OrderByDescending(x => x.ReturnPct).ToList();
    }


    private sealed class IndicatorCaches
    {
        public Dictionary<DateTime, double> Ma5 = [];
        public Dictionary<DateTime, double> Ma10 = [];
        public Dictionary<DateTime, double> SmaShort = [];
        public Dictionary<DateTime, double> Sma20 = [];
        public Dictionary<DateTime, double> Rsi = [];
        public Dictionary<DateTime, double> StochK = [];
        public Dictionary<DateTime, double> StochD = [];
        public Dictionary<DateTime, MacdTriple> Macd = [];
        public Dictionary<DateTime, double> StochRsi = [];
        public Dictionary<DateTime, double> StochRsiSignal = [];
    }

    private static IndicatorCaches BuildCaches(List<Quote> quotes, AnalysisSettings s)
    {
        var stoch = quotes.GetStoch(s.StochKPeriod, s.StochKSmooth, s.StochDSmooth).ToList();
        var macd = quotes.GetMacd(s.MacdFast, s.MacdSlow, s.MacdSignalPeriod).ToList();
        var stochRsi = quotes.GetStochRsi(s.StochRsiPeriod, s.StochRsiStochPeriod, s.StochRsiSmooth, s.StochRsiSignalPeriod).ToList();

        var c = new IndicatorCaches
        {
            Ma5 = quotes.GetSma(s.Ma5Period).Where(x => x.Sma is not null).ToDictionary(x => x.Date, x => x.Sma!.Value),
            Ma10 = quotes.GetSma(s.Ma10Period).Where(x => x.Sma is not null).ToDictionary(x => x.Date, x => x.Sma!.Value),
            SmaShort = quotes.GetSma(s.SmaShortPeriod).Where(x => x.Sma is not null).ToDictionary(x => x.Date, x => x.Sma!.Value),
            Sma20 = quotes.GetSma(s.Sma20Period).Where(x => x.Sma is not null).ToDictionary(x => x.Date, x => x.Sma!.Value),
            Rsi = quotes.GetRsi(s.RsiPeriod).Where(x => x.Rsi is not null).ToDictionary(x => x.Date, x => x.Rsi!.Value),
            StochK = stoch.Where(x => x.Oscillator is not null && x.Signal is not null).ToDictionary(x => x.Date, x => x.Oscillator!.Value),
            StochD = stoch.Where(x => x.Oscillator is not null && x.Signal is not null).ToDictionary(x => x.Date, x => x.Signal!.Value),
            Macd = macd.Where(x => x.Macd is not null && x.Signal is not null && x.Histogram is not null)
                .ToDictionary(x => x.Date, x => new MacdTriple(x.Macd!.Value, x.Signal!.Value, x.Histogram!.Value)),
            StochRsi = stochRsi.Where(x => x.StochRsi is not null && x.Signal is not null).ToDictionary(x => x.Date, x => x.StochRsi!.Value),
            StochRsiSignal = stochRsi.Where(x => x.StochRsi is not null && x.Signal is not null).ToDictionary(x => x.Date, x => x.Signal!.Value),
        };
        return c;
    }

    //  Single indicator backtest 

    private static (decimal finalCapital, decimal returnPct, int trades, decimal winRate, List<decimal> equity, List<TradeSignal> signals)
        RunSingleIndicatorBacktest(
            List<Quote> quotes,
            decimal startCapital,
            IndicatorCaches caches,
            Func<IndicatorCaches, DateTime, DateTime, decimal, decimal, (bool buy, bool sell)?> evaluate,
            int maxUnitsPerBuySignal)
    {
        decimal cash = startCapital;
        decimal units = 0;
        decimal entryPrice = 0;
        int closedTrades = 0, wins = 0;
        var signals = new List<TradeSignal>();
        var equity = new List<decimal>(quotes.Count);

        if (quotes.Count == 0)
        {
            return (startCapital, 0, 0, 0, equity, signals);
        }

        equity.Add(startCapital);

        for (int i = 1; i < quotes.Count; i++)
        {
            var q = quotes[i];
            var prev = quotes[i - 1];
            var result = evaluate(caches, q.Date, prev.Date, q.Close, prev.Close);

            if (result is not null)
            {
                var (buy, sell) = result.Value;

                if (units == 0 && buy && q.Close > 0)
                {
                    var buyUnits = GetBuyUnits(cash, q.Close, maxUnitsPerBuySignal);
                    if (buyUnits > 0)
                    {
                        units = buyUnits;
                        cash -= buyUnits * q.Close;
                        entryPrice = q.Close;
                        signals.Add(new TradeSignal(q.Date, "KUP", q.Close));
                    }
                }
                else if (units > 0 && sell)
                {
                    cash += units * q.Close;
                    closedTrades++;
                    if (q.Close > entryPrice) wins++;
                    units = 0;
                    signals.Add(new TradeSignal(q.Date, "SPRZEDAJ", q.Close));
                }
            }

            equity.Add(cash + units * q.Close);
        }

        if (units > 0)
        {
            var last = quotes[^1];
            cash += units * last.Close;
            closedTrades++;
            if (last.Close > entryPrice) wins++;
            signals.Add(new TradeSignal(last.Date, "SPRZEDAJ (koniec)", last.Close));
            equity[^1] = cash;
        }

        decimal final_ = cash + units * (quotes.Count > 0 ? quotes[^1].Close : 0);
        if (units == 0) final_ = cash;
        decimal ret = startCapital == 0 ? 0 : (final_ / startCapital - 1) * 100;
        decimal wr = closedTrades == 0 ? 0 : (decimal)wins / closedTrades * 100;

        return (final_, ret, closedTrades, wr, equity, signals);
    }

    // Group backtest (percent agreement for buy/sell)

    private static (decimal finalCapital, decimal returnPct, int trades, decimal winRate, List<decimal> equity, List<TradeSignal> signals)
        RunGroupBacktest(
            List<Quote> quotes,
            decimal startCapital,
            IndicatorCaches caches,
            List<Func<IndicatorCaches, DateTime, DateTime, decimal, decimal, (bool buy, bool sell)?>> evaluators,
            int agreementPercent,
            int maxUnitsPerBuySignal)
    {
        int thresholdPercent = Math.Clamp(agreementPercent, 1, 100);
        int minVotes = Math.Max(1, (int)Math.Ceiling(evaluators.Count * thresholdPercent / 100d));

        decimal cash = startCapital;
        decimal units = 0;
        decimal entryPrice = 0;
        int closedTrades = 0, wins = 0;
        var signals = new List<TradeSignal>();
        var equity = new List<decimal>(quotes.Count);

        if (quotes.Count == 0)
        {
            return (startCapital, 0, 0, 0, equity, signals);
        }

        equity.Add(startCapital);

        for (int i = 1; i < quotes.Count; i++)
        {
            var q = quotes[i];
            var prev = quotes[i - 1];
            bool allHaveData = true;
            int buyVotes = 0;
            int sellVotes = 0;

            foreach (var eval in evaluators)
            {
                var result = eval(caches, q.Date, prev.Date, q.Close, prev.Close);
                if (result is null) { allHaveData = false; break; }
                if (result.Value.buy) buyVotes++;
                if (result.Value.sell) sellVotes++;
            }

            if (allHaveData)
            {
                bool buySignal = buyVotes >= minVotes;
                bool sellSignal = sellVotes >= minVotes;

                if (units == 0 && buySignal && q.Close > 0)
                {
                    var buyUnits = GetBuyUnits(cash, q.Close, maxUnitsPerBuySignal);
                    if (buyUnits > 0)
                    {
                        units = buyUnits;
                        cash -= buyUnits * q.Close;
                        entryPrice = q.Close;
                        signals.Add(new TradeSignal(q.Date, "KUP", q.Close));
                    }
                }
                else if (units > 0 && sellSignal)
                {
                    cash += units * q.Close;
                    closedTrades++;
                    if (q.Close > entryPrice) wins++;
                    units = 0;
                    signals.Add(new TradeSignal(q.Date, "SPRZEDAJ", q.Close));
                }
            }

            equity.Add(cash + units * q.Close);
        }

        if (units > 0)
        {
            var last = quotes[^1];
            cash += units * last.Close;
            closedTrades++;
            if (last.Close > entryPrice) wins++;
            signals.Add(new TradeSignal(last.Date, "SPRZEDAJ (koniec)", last.Close));
            equity[^1] = cash;
        }

        decimal final_ = cash;
        decimal ret = startCapital == 0 ? 0 : (final_ / startCapital - 1) * 100;
        decimal wr = closedTrades == 0 ? 0 : (decimal)wins / closedTrades * 100;

        return (final_, ret, closedTrades, wr, equity, signals);
    }

    private static decimal GetBuyUnits(decimal cash, decimal price, int maxUnitsPerBuySignal)
    {
        if (cash <= 0 || price <= 0)
        {
            return 0;
        }

        var affordableUnits = Math.Floor(cash / price);
        if (affordableUnits <= 0)
        {
            return 0;
        }

        if (maxUnitsPerBuySignal <= 0)
        {
            return affordableUnits;
        }

        return Math.Min(affordableUnits, maxUnitsPerBuySignal);
    }

    // Legacy optimization helpers 
    private static (bool buy, bool sell) ToSignalTuple(SignalType signal)
    {
        return signal switch
        {
            SignalType.Buy => (true, false),
            SignalType.Sell => (false, true),
            _ => (false, false)
        };
    }

    private static SignalType GetMACrossoverAction(
        double maShortCurr, double maLongCurr,
        double maShortPrev, double maLongPrev,
        double priceCurr, double pricePrev)
    {
        if (double.IsNaN(maShortCurr) || double.IsNaN(maLongCurr) ||
            double.IsNaN(maShortPrev) || double.IsNaN(maLongPrev) ||
            double.IsNaN(priceCurr) || double.IsNaN(pricePrev))
        {
            return SignalType.None;
        }

        bool isBullishCrossover = maShortPrev < maLongPrev && maShortCurr > maLongCurr;
        bool isBearishCrossover = maShortPrev > maLongPrev && maShortCurr < maLongCurr;

        bool maShortRising = maShortCurr > maShortPrev;
        bool maLongRising = maLongCurr > maLongPrev;
        bool maShortFalling = maShortCurr < maShortPrev;
        bool maLongFalling = maLongCurr < maLongPrev;

        if (isBullishCrossover && maShortRising && maLongRising && priceCurr > maLongCurr)
        {
            return SignalType.Buy;
        }

        if (isBearishCrossover && maShortFalling && maLongFalling && priceCurr < maLongCurr)
        {
            return SignalType.Sell;
        }

        if (isBullishCrossover && pricePrev < maShortPrev && priceCurr > maShortCurr)
        {
            return SignalType.Buy;
        }

        if (isBearishCrossover && pricePrev > maShortPrev && priceCurr < maShortCurr)
        {
            return SignalType.Sell;
        }

        return SignalType.None;
    }

    private static SignalType GetRSIAction(
        double rsiCurr, double rsiPrev,
        double oversoldThreshold, double overboughtThreshold,
        double priceCurr, double pricePrev)
    {
        if (double.IsNaN(rsiCurr) || double.IsNaN(rsiPrev) ||
            double.IsNaN(priceCurr) || double.IsNaN(pricePrev))
        {
            return SignalType.None;
        }

        if (priceCurr < pricePrev && rsiCurr > rsiPrev && rsiCurr <= oversoldThreshold + 5)
        {
            return SignalType.Buy;
        }

        if (priceCurr > pricePrev && rsiCurr < rsiPrev && rsiCurr >= overboughtThreshold - 5)
        {
            return SignalType.Sell;
        }

        if (rsiPrev < oversoldThreshold && rsiCurr >= oversoldThreshold)
        {
            return SignalType.Buy;
        }

        if (rsiPrev > overboughtThreshold && rsiCurr <= overboughtThreshold)
        {
            return SignalType.Sell;
        }

        if (rsiPrev < 50 && rsiCurr >= 50)
        {
            return SignalType.Buy;
        }

        if (rsiPrev > 50 && rsiCurr <= 50)
        {
            return SignalType.Sell;
        }

        return SignalType.None;
    }

    private static SignalType GetOscillatorCrossoverAction(
        double kCurr, double dCurr,
        double kPrev, double dPrev,
        double oversoldThreshold, double overboughtThreshold)
    {
        if (double.IsNaN(kCurr) || double.IsNaN(dCurr) ||
            double.IsNaN(kPrev) || double.IsNaN(dPrev))
        {
            return SignalType.None;
        }

        bool isBullishCrossover = kPrev < dPrev && kCurr > dCurr;
        bool isBearishCrossover = kPrev > dPrev && kCurr < dCurr;

        if (isBullishCrossover && kPrev < oversoldThreshold && dPrev < oversoldThreshold)
        {
            return SignalType.Buy;
        }

        if (isBearishCrossover && kPrev > overboughtThreshold && dPrev > overboughtThreshold)
        {
            return SignalType.Sell;
        }

        if (isBullishCrossover && kCurr >= oversoldThreshold && kPrev < oversoldThreshold)
        {
            return SignalType.Buy;
        }

        if (isBearishCrossover && kCurr <= overboughtThreshold && kPrev > overboughtThreshold)
        {
            return SignalType.Sell;
        }

        return SignalType.None;
    }

    private static SignalType GetStochasticAction(
        double kCurr, double dCurr,
        double kPrev, double dPrev,
        double oversold, double overbought)
        => GetOscillatorCrossoverAction(kCurr, dCurr, kPrev, dPrev, oversold, overbought);

    private static SignalType GetStochRSIAction(
        double kCurr, double dCurr,
        double kPrev, double dPrev,
        double oversold, double overbought)
        => GetOscillatorCrossoverAction(kCurr, dCurr, kPrev, dPrev, oversold, overbought);

    private static SignalType GetMACDAction(
        double macdCurr, double signalCurr, double histogramCurr,
        double macdPrev, double signalPrev, double histogramPrev)
    {
        if (double.IsNaN(macdCurr) || double.IsNaN(signalCurr) || double.IsNaN(histogramCurr) ||
            double.IsNaN(macdPrev) || double.IsNaN(signalPrev) || double.IsNaN(histogramPrev))
        {
            return SignalType.None;
        }

        bool isBullishCrossover = macdPrev < signalPrev && macdCurr > signalCurr;
        bool isBearishCrossover = macdPrev > signalPrev && macdCurr < signalCurr;

        if (histogramPrev < 0 && histogramCurr > 0 && macdCurr < 0 && signalCurr < 0 && macdCurr > macdPrev)
        {
            return SignalType.Buy;
        }

        if (histogramPrev > 0 && histogramCurr < 0 && macdCurr > 0 && signalCurr > 0 && macdCurr < macdPrev)
        {
            return SignalType.Sell;
        }

        if (isBullishCrossover && macdCurr < 0 && signalCurr < 0)
        {
            return SignalType.Buy;
        }

        if (isBearishCrossover && macdCurr > 0 && signalCurr > 0)
        {
            return SignalType.Sell;
        }

        if (macdPrev < 0 && macdCurr >= 0)
        {
            return SignalType.Buy;
        }

        if (macdPrev > 0 && macdCurr <= 0)
        {
            return SignalType.Sell;
        }

        return SignalType.None;
    }

    private readonly record struct MacdTriple(double Macd, double Signal, double Histogram);
}
