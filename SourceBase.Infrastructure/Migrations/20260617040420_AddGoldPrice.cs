using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SourceBase.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGoldPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoldPrices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    BuyPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    SellPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoldPrices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoldPrices_Source_RecordedAt",
                table: "GoldPrices",
                columns: new[] { "Source", "RecordedAt" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoldPrices");
        }
    }
}
