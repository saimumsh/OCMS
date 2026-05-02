using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptimumCoaching.repo.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentAcademicRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "DiplomaCgpa", table: "Students");
            migrationBuilder.DropColumn(name: "SscPassingYear", table: "Students");
            migrationBuilder.DropColumn(name: "SscGroup", table: "Students");
            migrationBuilder.DropColumn(name: "SscResult", table: "Students");

            migrationBuilder.CreateTable(
                name: "StudentAcademicRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExaminationName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PassingYear = table.Column<int>(type: "int", nullable: false),
                    Group = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Result = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Institution = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAcademicRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentAcademicRecords_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentAcademicRecords_StudentId_SortOrder",
                table: "StudentAcademicRecords",
                columns: new[] { "StudentId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "StudentAcademicRecords");

            migrationBuilder.AddColumn<decimal>(
                name: "DiplomaCgpa", table: "Students",
                type: "decimal(4,2)", nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SscPassingYear", table: "Students",
                type: "int", nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SscGroup", table: "Students",
                type: "nvarchar(80)", maxLength: 80, nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SscResult", table: "Students",
                type: "nvarchar(50)", maxLength: 50, nullable: true);
        }
    }
}
