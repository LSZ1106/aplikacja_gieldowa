using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorApp1.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuoteDataSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteDataSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockQuotes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataSetId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Open = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    High = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Low = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Close = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Volume = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Ma5 = table.Column<double>(type: "float", nullable: true),
                    Ma10 = table.Column<double>(type: "float", nullable: true),
                    Sma20 = table.Column<double>(type: "float", nullable: true),
                    Rsi14 = table.Column<double>(type: "float", nullable: true),
                    StochK = table.Column<double>(type: "float", nullable: true),
                    StochD = table.Column<double>(type: "float", nullable: true),
                    Stoch2K = table.Column<double>(type: "float", nullable: true),
                    Stoch2D = table.Column<double>(type: "float", nullable: true),
                    MacdValue = table.Column<double>(type: "float", nullable: true),
                    MacdSignal = table.Column<double>(type: "float", nullable: true),
                    MacdHistogram = table.Column<double>(type: "float", nullable: true),
                    StochRsi = table.Column<double>(type: "float", nullable: true),
                    StochRsiSignal = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockQuotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockQuotes_QuoteDataSets_DataSetId",
                        column: x => x.DataSetId,
                        principalTable: "QuoteDataSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuoteDataSets_UserId",
                table: "QuoteDataSets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockQuotes_DataSetId_Date",
                table: "StockQuotes",
                columns: new[] { "DataSetId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockQuotes");

            migrationBuilder.DropTable(
                name: "QuoteDataSets");
        }
    }
}
