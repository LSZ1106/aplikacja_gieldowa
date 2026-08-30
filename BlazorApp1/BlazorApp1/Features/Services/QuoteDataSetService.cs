using BlazorApp1.Data;
using BlazorApp1.Features.Models;
using Microsoft.EntityFrameworkCore;
using Skender.Stock.Indicators;

namespace BlazorApp1.Features.Services;

public sealed class QuoteDataSetService(
    ApplicationDbContext db,
    TechnicalAnalysisService technicalAnalysisService)
{
    public async Task<int> ImportCsvAsync(string name, Stream csvStream, string? userId, CancellationToken ct = default)
    {
        var quotes = await CsvQuoteDataSource.ParseStreamAsync(csvStream, ct);
        if (quotes.Count < 5)
        {
            throw new InvalidOperationException("Plik CSV zawiera za mało danych (minimum 5 rekordów).");
        }

        var dataSet = new DataEntities
        {
            Name = name,
            UserId = userId,
            UploadedAt = DateTime.UtcNow
        };

        var dailyIndicators = technicalAnalysisService.BuildDailyIndicators(quotes);
        var indicatorsByDate = dailyIndicators.ToDictionary(x => x.Date);

        foreach (var quote in quotes)
        {
            var row = new StockQuote
            {
                Date = quote.Date,
                Open = quote.Open,
                High = quote.High,
                Low = quote.Low,
                Close = quote.Close,
                Volume = quote.Volume
            };

            if (indicatorsByDate.TryGetValue(quote.Date, out var ind))
            {
                row.Ma5 = ind.Ma5;
                row.Ma10 = ind.Ma10;
                row.Sma20 = ind.Sma20;
                row.Rsi14 = ind.Rsi14;
                row.StochK = ind.StochK;
                row.StochD = ind.StochD;
                row.MacdValue = ind.Macd;
                row.MacdSignal = ind.MacdSignal;
                row.MacdHistogram = ind.MacdHistogram;
                row.StochRsi = ind.StochRsi;
                row.StochRsiSignal = ind.StochRsiSignal;
            }

            dataSet.Quotes.Add(row);
        }

        db.QuoteDataSets.Add(dataSet);
        await db.SaveChangesAsync(ct);

        return dataSet.Id;
    }

    public async Task<List<DataEntities>> GetAvailableDataSetsAsync(string? userId, CancellationToken ct = default)
    {
        return await db.QuoteDataSets
            .Where(x => x.UserId == null || x.UserId == userId)
            .OrderByDescending(x => x.UploadedAt)
            .Select(x => new DataEntities
            {
                Id = x.Id,
                Name = x.Name,
                UserId = x.UserId,
                UploadedAt = x.UploadedAt
            })
            .ToListAsync(ct);
    }

    public async Task DeleteDataSetAsync(int dataSetId, string? userId, CancellationToken ct = default)
    {
        var ds = await db.QuoteDataSets.FirstOrDefaultAsync(x => x.Id == dataSetId, ct);
        if (ds is null)
        {
            return;
        }

        if (ds.UserId != userId && ds.UserId is not null)
        {
            throw new InvalidOperationException("Nie masz uprawnień do usunięcia tego zbioru danych.");
        }

        db.QuoteDataSets.Remove(ds);
        await db.SaveChangesAsync(ct);
    }

    public async Task SeedExampleDataAsync(IWebHostEnvironment env, CancellationToken ct = default)
    {
        if (await db.QuoteDataSets.AnyAsync(x => x.UserId == null, ct))
        {
            return;
        }

        var csvPath = Path.Combine(env.WebRootPath, "data", "quotes.csv");
        if (!File.Exists(csvPath))
        {
            return;
        }

        using var stream = File.OpenRead(csvPath);
        await ImportCsvAsync("Przykładowe notowania", stream, null, ct);
    }
}
