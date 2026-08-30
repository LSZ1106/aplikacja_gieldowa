namespace BlazorApp1.Data;

public class DataEntities
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public List<StockQuote> Quotes { get; set; } = [];
}

public class StockQuote
{
    public long Id { get; set; }
    public int DataSetId { get; set; }
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }

    // Pre-computed indicators
    public double? Ma5 { get; set; }
    public double? Ma10 { get; set; }
    public double? Sma20 { get; set; }
    public double? Rsi14 { get; set; }
    public double? StochK { get; set; }
    public double? StochD { get; set; }
    public double? Stoch2K { get; set; }
    public double? Stoch2D { get; set; }
    public double? MacdValue { get; set; }
    public double? MacdSignal { get; set; }
    public double? MacdHistogram { get; set; }
    public double? StochRsi { get; set; }
    public double? StochRsiSignal { get; set; }

    public DataEntities DataSet { get; set; } = null!;
}
