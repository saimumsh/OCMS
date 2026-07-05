using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptimumCoaching.repo.Migrations
{
    /// <inheritdoc />
    public partial class paymentConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FeeDueDate",
                table: "Batches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FeeDueDays",
                table: "Batches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LateFeeFlat",
                table: "Batches",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LateFeePerDay",
                table: "Batches",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "PaymentSettingsRows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencySymbol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReceiptPrefix = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    NextReceiptNumber = table.Column<int>(type: "int", nullable: false),
                    EnabledMethodsCsv = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentSettingsRows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResultDiscountTiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    MinResultPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResultDiscountTiers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResultDiscountTiers_MinResultPercent",
                table: "ResultDiscountTiers",
                column: "MinResultPercent");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentSettingsRows");

            migrationBuilder.DropTable(
                name: "ResultDiscountTiers");

            migrationBuilder.DropColumn(
                name: "FeeDueDate",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "FeeDueDays",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "LateFeeFlat",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "LateFeePerDay",
                table: "Batches");
        }
    }
}
