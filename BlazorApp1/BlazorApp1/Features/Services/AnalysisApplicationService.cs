using BlazorApp1.Features.Models;

namespace BlazorApp1.Features.Services;

public sealed class AnalysisApplicationService(
    IEnumerable<IQuoteDataSource> dataSources,
    TechnicalAnalysisService technicalAnalysisService,
    TradingSimulationService simulationService)
{
    public async Task<AnalysisResult> AnalyzeAsync(AnalysisSettings settings, CancellationToken cancellationToken = default)
    {
        var quotes = await LoadQuotesAsync(settings, cancellationToken);
        var indicators = technicalAnalysisService.BuildIndicatorSnapshot(quotes, settings);
        var dailyIndicators = technicalAnalysisService.BuildDailyIndicators(quotes, settings);
        var simulation = simulationService.RunFullSimulation(quotes, settings);

        var config = new StrategyConfig(
            settings.UseSma,
            settings.Sma20Period,
            settings.UseRsi,
            settings.RsiPeriod,
            settings.RsiBuyThreshold,
            settings.RsiSellThreshold,
            settings.UseMacd,
            settings.UseStochRsi,
            settings.StochRsiBuyThreshold,
            settings.StochRsiSellThreshold);

        var bestIndividual = simulation.IndividualRanking.FirstOrDefault();
        var bestGroup = simulation.GroupRanking.FirstOrDefault();

        StrategyResult bestStrategy;
        if (bestGroup is not null && (bestIndividual is null || bestGroup.ReturnPct > bestIndividual.ReturnPct))
        {
            bestStrategy = new StrategyResult(config, bestGroup.FinalCapital, bestGroup.ReturnPct,
                bestGroup.Trades, bestGroup.WinRate, bestGroup.EquityCurve, bestGroup.Signals);
        }
        else if (bestIndividual is not null)
        {
            bestStrategy = new StrategyResult(config, bestIndividual.FinalCapital, bestIndividual.ReturnPct,
                bestIndividual.Trades, bestIndividual.WinRate, bestIndividual.EquityCurve, bestIndividual.Signals);
        }
        else
        {
            bestStrategy = new StrategyResult(config, settings.InitialCapital, 0m, 0, 0m,
                [settings.InitialCapital], []);
        }

        return new AnalysisResult(
            quotes[^1].Date,
            indicators,
            dailyIndicators,
            quotes.Select(x => new HistoricalQuoteRow(x.Date, x.Open, x.High, x.Low, x.Close, x.Volume)).ToList(),
            quotes.Select(x => x.Date).ToList(),
            quotes.Select(x => x.Close).ToList(),
            bestStrategy
            );
    }

    public async Task<FullSimulationResult> RunSimulationAsync(AnalysisSettings settings, CancellationToken cancellationToken = default)
    {
        var quotes = await LoadQuotesAsync(settings, cancellationToken);
        return simulationService.RunFullSimulation(quotes, settings);
    }

    public async Task<FullSimulationResult> RunIndividualSimulationAsync(AnalysisSettings settings, CancellationToken cancellationToken = default)
    {
        var quotes = await LoadQuotesAsync(settings, cancellationToken);
        return simulationService.RunIndividualSimulation(quotes, settings);
    }

    public async Task<FullSimulationResult> RunGroupSimulationAsync(AnalysisSettings settings, CancellationToken cancellationToken = default)
    {
        var quotes = await LoadQuotesAsync(settings, cancellationToken);
        return simulationService.RunGroupSimulation(quotes, settings);
    }

    public async Task<int> GetAvailableDaysAsync(AnalysisSettings settings, CancellationToken cancellationToken = default)
    {
        var quotes = await LoadQuotesAsync(settings, cancellationToken);
        return quotes.Count;
    }

    public async Task<int> CalendarDaysToTradingDaysAsync(AnalysisSettings settings, int calendarDays, CancellationToken cancellationToken = default)
    {
        var quotes = await LoadQuotesAsync(settings, cancellationToken);
        if (quotes.Count == 0 || calendarDays <= 0)
        {
            return 0;
        }

        var orderedQuotes = quotes
            .OrderBy(x => x.Date)
            .ToList();

        var endDate = orderedQuotes[^1].Date.Date;
        var startDate = endDate.AddDays(-(calendarDays - 1));

        return orderedQuotes.Count(x => x.Date.Date >= startDate && x.Date.Date <= endDate);
    }

    private async Task<List<Skender.Stock.Indicators.Quote>> LoadQuotesAsync(AnalysisSettings settings, CancellationToken cancellationToken)
    {
        var source = dataSources.FirstOrDefault(x => string.Equals(x.SourceType, settings.DataSourceType, StringComparison.OrdinalIgnoreCase));
        if (source is null)
        {
            throw new InvalidOperationException($"Nieznane źródło danych: {settings.DataSourceType}");
        }

        var quotes = await source.LoadQuotesAsync(settings, cancellationToken);
        if (quotes.Count < 35)
        {
            throw new InvalidOperationException("Za mało notowań. Potrzeba minimum 35 rekordów.");
        }

        return quotes;
    }
}
