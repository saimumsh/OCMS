using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptimumCoaching.repo.Migrations
{
    /// <inheritdoc />
    public partial class AddBatchDepartmentAndUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "Batches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BatchId",
                table: "Students",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Batches_DepartmentId",
                table: "Batches",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_BatchId",
                table: "Students",
                column: "BatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Batches_Departments_DepartmentId",
                table: "Batches",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Batches_BatchId",
                table: "Students",
                column: "BatchId",
                principalTable: "Batches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.CreateTable(
                name: "BatchUpdates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PostedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchUpdates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BatchUpdates_Batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "Batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BatchUpdates_AspNetUsers_PostedByUserId",
                        column: x => x.PostedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BatchUpdates_BatchId_PostedAt",
                table: "BatchUpdates",
                columns: new[] { "BatchId", "PostedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BatchUpdates_PostedByUserId",
                table: "BatchUpdates",
                column: "PostedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BatchUpdates");

            migrationBuilder.DropForeignKey(name: "FK_Students_Batches_BatchId", table: "Students");
            migrationBuilder.DropIndex(name: "IX_Students_BatchId", table: "Students");
            migrationBuilder.DropColumn(name: "BatchId", table: "Students");

            migrationBuilder.DropForeignKey(name: "FK_Batches_Departments_DepartmentId", table: "Batches");
            migrationBuilder.DropIndex(name: "IX_Batches_DepartmentId", table: "Batches");
            migrationBuilder.DropColumn(name: "DepartmentId", table: "Batches");
        }
    }
}
