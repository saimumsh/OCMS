using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptimumCoaching.repo.Migrations
{
    /// <inheritdoc />
    public partial class addedfinanceconfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CourseFee",
                table: "Batches",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FullPaymentDiscountPercent",
                table: "Batches",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumEnrollment",
                table: "Batches",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "BatchTeachers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    IsLead = table.Column<bool>(type: "bit", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchTeachers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BatchTeachers_Batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "Batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BatchTeachers_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentFeeAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinalFee = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    DiscountReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FullyPaidOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentFeeAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentFeeAccounts_Batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "Batches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentFeeAccounts_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeacherSalaryPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodMonth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    PaidOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherSalaryPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherSalaryPayments_AspNetUsers_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TeacherSalaryPayments_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    PaidOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeePayments_AspNetUsers_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FeePayments_StudentFeeAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "StudentFeeAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BatchTeachers_BatchId_TeacherId",
                table: "BatchTeachers",
                columns: new[] { "BatchId", "TeacherId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BatchTeachers_TeacherId",
                table: "BatchTeachers",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_FeePayments_AccountId_PaidOn",
                table: "FeePayments",
                columns: new[] { "AccountId", "PaidOn" });

            migrationBuilder.CreateIndex(
                name: "IX_FeePayments_RecordedByUserId",
                table: "FeePayments",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentFeeAccounts_BatchId",
                table: "StudentFeeAccounts",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentFeeAccounts_StudentId_BatchId",
                table: "StudentFeeAccounts",
                columns: new[] { "StudentId", "BatchId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSalaryPayments_RecordedByUserId",
                table: "TeacherSalaryPayments",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSalaryPayments_TeacherId_PeriodMonth",
                table: "TeacherSalaryPayments",
                columns: new[] { "TeacherId", "PeriodMonth" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BatchTeachers");

            migrationBuilder.DropTable(
                name: "FeePayments");

            migrationBuilder.DropTable(
                name: "TeacherSalaryPayments");

            migrationBuilder.DropTable(
                name: "StudentFeeAccounts");

            migrationBuilder.DropColumn(
                name: "CourseFee",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "FullPaymentDiscountPercent",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "MinimumEnrollment",
                table: "Batches");
        }
    }
}
