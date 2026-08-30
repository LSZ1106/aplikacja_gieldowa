using System.Globalization;
using System.Text;
using BlazorApp1.Features.Models;
using Skender.Stock.Indicators;

namespace BlazorApp1.Features.Services;

public interface IQuoteDataSource
{
    string SourceType { get; }

    Task<List<Quote>> LoadQuotesAsync(AnalysisSettings settings, CancellationToken cancellationToken = default);
}


public sealed class CsvQuoteDataSource(IWebHostEnvironment environment) : IQuoteDataSource
{
    public string SourceType => "Csv";

    public static string ResolveCsvPath(IWebHostEnvironment environment, AnalysisSettings settings)
    {
        if (string.IsNullOrWhiteSpace(environment.WebRootPath))
        {
            throw new InvalidOperationException("Nie można odczytać katalogu wwwroot.");
        }

        var relativePath = string.IsNullOrWhiteSpace(settings.CsvRelativePath) ? "data/quotes.csv" : settings.CsvRelativePath;
        return Path.Combine(environment.WebRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    public async Task<List<Quote>> LoadQuotesAsync(AnalysisSettings settings, CancellationToken cancellationToken = default)
    {
        var filePath = ResolveCsvPath(environment, settings);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Nie znaleziono pliku CSV: {filePath}");
        }

        using var stream = File.OpenRead(filePath);
        return await ParseStreamAsync(stream, cancellationToken);
    }

    public static async Task<List<Quote>> ParseStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var quotes = new List<Quote>();

        using var reader = new StreamReader(stream);
        var headerLine = await reader.ReadLineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            throw new InvalidOperationException("Plik CSV nie zawiera nagłówka.");
        }

        List<string> headers = SplitCsvLine(headerLine);
        int dateIndex = GetIndex(headers, "Date", "Data");
        int closeIndex = GetIndex(headers, "Close", "Price", "Ostatnio", "Zamkniecie", "Zamknięcie");
        int openIndex = GetIndexOptional(headers, "Open", "Otwarcie");
        int highIndex = GetIndexOptional(headers, "High", "Max", "Max.", "Najwyzszy", "Najwyższy");
        int lowIndex = GetIndexOptional(headers, "Low", "Min", "Min.", "Najnizszy", "Najniższy");
        int volumeIndex = GetIndexOptional(headers, "Volume", "Vol.", "Wol.", "Wolumen");

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            List<string> columns = SplitCsvLine(line);
            if (columns.Count <= Math.Max(dateIndex, closeIndex))
            {
                continue;
            }

            if (!TryParseDate(columns[dateIndex], out DateTime date) || !TryParseDecimal(columns[closeIndex], out decimal close))
            {
                continue;
            }

            decimal open = close;
            decimal high = close;
            decimal low = close;

            if (openIndex >= 0 && openIndex < columns.Count && TryParseDecimal(columns[openIndex], out decimal openValue))
            {
                open = openValue;
            }

            if (highIndex >= 0 && highIndex < columns.Count && TryParseDecimal(columns[highIndex], out decimal highValue))
            {
                high = highValue;
            }

            if (lowIndex >= 0 && lowIndex < columns.Count && TryParseDecimal(columns[lowIndex], out decimal lowValue))
            {
                low = lowValue;
            }

            decimal volume = 0;
            if (volumeIndex >= 0 && volumeIndex < columns.Count)
            {
                volume = ParseVolume(columns[volumeIndex]);
            }

            quotes.Add(new Quote
            {
                Date = date,
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = volume
            });
        }

        return quotes.OrderBy(x => x.Date).ToList();
    }

    private static bool TryParseDate(string value, out DateTime date)
    {
        string[] formats = ["yyyy-MM-dd", "MM/dd/yyyy", "dd.MM.yyyy", "dd/MM/yyyy", "MMM d, yyyy", "d MMM yyyy"];
        return DateTime.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
               || DateTime.TryParse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
               || DateTime.TryParse(value.Trim(), new CultureInfo("pl-PL"), DateTimeStyles.None, out date);
    }

    private static bool TryParseDecimal(string value, out decimal number)
    {
        string normalized = value.Trim().Replace(" ", string.Empty).Replace("%", string.Empty).Replace("\u00A0", string.Empty);

        if (normalized.Contains(',') && normalized.Contains('.'))
        {
            normalized = normalized.Replace(".", string.Empty).Replace(',', '.');
        }
        else if (normalized.Contains(','))
        {
            normalized = normalized.Replace(',', '.');
        }

        return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out number)
               || decimal.TryParse(normalized, NumberStyles.Any, new CultureInfo("pl-PL"), out number);
    }

    private static decimal ParseVolume(string value)
    {
        string normalized = value.Trim().Replace(" ", string.Empty).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || normalized == "-")
        {
            return 0;
        }

        decimal multiplier = 1;
        if (normalized.EndsWith("K"))
        {
            multiplier = 1_000;
            normalized = normalized[..^1];
        }
        else if (normalized.EndsWith("M"))
        {
            multiplier = 1_000_000;
            normalized = normalized[..^1];
        }
        else if (normalized.EndsWith("B"))
        {
            multiplier = 1_000_000_000;
            normalized = normalized[..^1];
        }

        return TryParseDecimal(normalized, out decimal parsed) ? parsed * multiplier : 0;
    }

    private static int GetIndex(IReadOnlyList<string> headers, params string[] names)
    {
        int index = GetIndexOptional(headers, names);
        if (index < 0)
        {
            throw new InvalidOperationException($"Brak wymaganej kolumny CSV. Oczekiwane nazwy: {string.Join(", ", names)}");
        }

        return index;
    }

    private static int GetIndexOptional(IReadOnlyList<string> headers, params string[] names)
    {
        for (int i = 0; i < headers.Count; i++)
        {
            string header = headers[i].Trim();
            foreach (string name in names)
            {
                if (string.Equals(header, name, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        foreach (char ch in line)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        result.Add(current.ToString());
        return result;
    }
}
