using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LightenUp.Web.Migrations
{
    /// <inheritdoc />
    public partial class FeatureAlignmentPayrollAndAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmployeeLimit",
                table: "CompanySubscriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PsychologistRevenuePercentage",
                table: "Assignments",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SlotValue",
                table: "Assignments",
                type: "decimal(14,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PatientAdminAssignmentRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    PreferredPsychologistId = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AssignedPsychologistId = table.Column<int>(type: "int", nullable: true),
                    AssignedByAdminUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DecisionAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientAdminAssignmentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientAdminAssignmentRequests_AspNetUsers_AssignedByAdminUserId",
                        column: x => x.AssignedByAdminUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientAdminAssignmentRequests_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientAdminAssignmentRequests_Psychologists_AssignedPsychologistId",
                        column: x => x.AssignedPsychologistId,
                        principalTable: "Psychologists",
                        principalColumn: "PsychologistId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientAdminAssignmentRequests_Psychologists_PreferredPsychologistId",
                        column: x => x.PreferredPsychologistId,
                        principalTable: "Psychologists",
                        principalColumn: "PsychologistId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientAdminAssignmentRequests_AssignedByAdminUserId",
                table: "PatientAdminAssignmentRequests",
                column: "AssignedByAdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAdminAssignmentRequests_AssignedPsychologistId",
                table: "PatientAdminAssignmentRequests",
                column: "AssignedPsychologistId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAdminAssignmentRequests_PatientId",
                table: "PatientAdminAssignmentRequests",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAdminAssignmentRequests_PreferredPsychologistId",
                table: "PatientAdminAssignmentRequests",
                column: "PreferredPsychologistId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatientAdminAssignmentRequests");

            migrationBuilder.DropColumn(
                name: "EmployeeLimit",
                table: "CompanySubscriptions");

            migrationBuilder.DropColumn(
                name: "PsychologistRevenuePercentage",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "SlotValue",
                table: "Assignments");
        }
    }
}
