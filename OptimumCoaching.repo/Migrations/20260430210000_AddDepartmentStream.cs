using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptimumCoaching.repo.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentStream : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Stream",
                table: "Departments",
                type: "int",
                nullable: false,
                defaultValue: 1); // EducationStream.Academic

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Stream_Name",
                table: "Departments",
                columns: new[] { "Stream", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Departments_Stream_Name",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "Stream",
                table: "Departments");
        }
    }
}
