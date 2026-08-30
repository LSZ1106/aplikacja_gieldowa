using BlazorApp1.Data;
using BlazorApp1.Features.Models;
using Microsoft.EntityFrameworkCore;
using Skender.Stock.Indicators;

namespace BlazorApp1.Features.Services;

public sealed class DatabaseQuoteDataSource(ApplicationDbContext db, IWebHostEnvironment environment) : IQuoteDataSource
{
    public string SourceType => "Database";

    public async Task<List<Quote>> LoadQuotesAsync(AnalysisSettings settings, CancellationToken cancellationToken = default)
    {
        int? configuredDataSetId = settings.SelectedDataSetId;

        if (configuredDataSetId is null)
        {
            configuredDataSetId = await db.QuoteDataSets
                .OrderByDescending(x => x.UploadedAt)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (configuredDataSetId is not { } dataSetId)
        {
            return await LoadFallbackCsvAsync(settings, cancellationToken);
        }

        var rows = await db.StockQuotes
            .Where(x => x.DataSetId == dataSetId)
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            throw new InvalidOperationException($"Zbiór danych o ID={dataSetId} nie zawiera notowań.");
        }

        return rows.Select(x => new Quote
        {
            Date = x.Date,
            Open = x.Open,
            High = x.High,
            Low = x.Low,
            Close = x.Close,
            Volume = x.Volume
        }).ToList();
    }

    private async Task<List<Quote>> LoadFallbackCsvAsync(AnalysisSettings settings, CancellationToken cancellationToken)
    {
        var filePath = CsvQuoteDataSource.ResolveCsvPath(environment, settings);

        if (!File.Exists(filePath))
        {
            throw new InvalidOperationException("Brak danych w bazie oraz brak pliku fallback CSV.");
        }

        await using var stream = File.OpenRead(filePath);
        return await CsvQuoteDataSource.ParseStreamAsync(stream, cancellationToken);
    }
}
