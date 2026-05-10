using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptimumCoaching.repo.Migrations
{
    /// <inheritdoc />
    public partial class AddNotices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Audience = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsPinned = table.Column<bool>(type: "bit", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_Notices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notices_AspNetUsers_PostedByUserId",
                        column: x => x.PostedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notices_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notices_Audience_PostedAt",
                table: "Notices",
                columns: new[] { "Audience", "PostedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notices_DepartmentId",
                table: "Notices",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Notices_ExpiresAt",
                table: "Notices",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notices_PostedByUserId",
                table: "Notices",
                column: "PostedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notices");
        }
    }
}
