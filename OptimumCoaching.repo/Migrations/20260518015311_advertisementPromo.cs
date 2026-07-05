using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptimumCoaching.repo.Migrations
{
    /// <inheritdoc />
    public partial class advertisementPromo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OfferEndsAt",
                table: "Batches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfferLabel",
                table: "Batches",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OfferedPrice",
                table: "Batches",
                type: "decimal(12,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromoVideoUrl",
                table: "Batches",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OfferEndsAt",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "OfferLabel",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "OfferedPrice",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "PromoVideoUrl",
                table: "Batches");
        }
    }
}
