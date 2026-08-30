using Microsoft.EntityFrameworkCore;

namespace BlazorApp1.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<DataEntities> QuoteDataSets => Set<DataEntities>();
        public DbSet<StockQuote> StockQuotes => Set<StockQuote>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<DataEntities>(e =>
            {
                e.HasIndex(x => x.UserId);
            });

            builder.Entity<StockQuote>(e =>
            {
                e.HasIndex(x => new { x.DataSetId, x.Date }).IsUnique();
                e.HasOne(x => x.DataSet)
                 .WithMany(x => x.Quotes)
                 .HasForeignKey(x => x.DataSetId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.Property(x => x.Open).HasPrecision(18, 4);
                e.Property(x => x.High).HasPrecision(18, 4);
                e.Property(x => x.Low).HasPrecision(18, 4);
                e.Property(x => x.Close).HasPrecision(18, 4);
                e.Property(x => x.Volume).HasPrecision(18, 4);
            });
        }
    }
}
