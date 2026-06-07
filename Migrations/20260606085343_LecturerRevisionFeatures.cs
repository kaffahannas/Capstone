using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LightenUp.Web.Migrations
{
    /// <inheritdoc />
    public partial class LecturerRevisionFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RequestedByHrUserId",
                table: "PsychologistRequests",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "RequestedByPatientUserId",
                table: "PsychologistRequests",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequesterRole",
                table: "PsychologistRequests",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Assignments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancellationRequestedAt",
                table: "Assignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationRequestedByUserId",
                table: "Assignments",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DecisionAt",
                table: "Assignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecisionByUserId",
                table: "Assignments",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecisionNote",
                table: "Assignments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestedByRole",
                table: "Assignments",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestedByUserId",
                table: "Assignments",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PayrollSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PsychologistId = table.Column<int>(type: "int", nullable: false),
                    SessionRate = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    PsychologistPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    UpdatedByAdminUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollSettings_AspNetUsers_UpdatedByAdminUserId",
                        column: x => x.UpdatedByAdminUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PayrollSettings_Psychologists_PsychologistId",
                        column: x => x.PsychologistId,
                        principalTable: "Psychologists",
                        principalColumn: "PsychologistId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PsychologistRequests_RequestedByPatientUserId",
                table: "PsychologistRequests",
                column: "RequestedByPatientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_CancellationRequestedByUserId",
                table: "Assignments",
                column: "CancellationRequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_DecisionByUserId",
                table: "Assignments",
                column: "DecisionByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_RequestedByUserId",
                table: "Assignments",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSettings_PsychologistId",
                table: "PayrollSettings",
                column: "PsychologistId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSettings_UpdatedByAdminUserId",
                table: "PayrollSettings",
                column: "UpdatedByAdminUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_AspNetUsers_CancellationRequestedByUserId",
                table: "Assignments",
                column: "CancellationRequestedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_AspNetUsers_DecisionByUserId",
                table: "Assignments",
                column: "DecisionByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_AspNetUsers_RequestedByUserId",
                table: "Assignments",
                column: "RequestedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PsychologistRequests_AspNetUsers_RequestedByPatientUserId",
                table: "PsychologistRequests",
                column: "RequestedByPatientUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_AspNetUsers_CancellationRequestedByUserId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_AspNetUsers_DecisionByUserId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_AspNetUsers_RequestedByUserId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_PsychologistRequests_AspNetUsers_RequestedByPatientUserId",
                table: "PsychologistRequests");

            migrationBuilder.DropTable(
                name: "PayrollSettings");

            migrationBuilder.DropIndex(
                name: "IX_PsychologistRequests_RequestedByPatientUserId",
                table: "PsychologistRequests");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_CancellationRequestedByUserId",
                table: "Assignments");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_DecisionByUserId",
                table: "Assignments");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_RequestedByUserId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "RequestedByPatientUserId",
                table: "PsychologistRequests");

            migrationBuilder.DropColumn(
                name: "RequesterRole",
                table: "PsychologistRequests");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "CancellationRequestedAt",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "CancellationRequestedByUserId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "DecisionAt",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "DecisionByUserId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "DecisionNote",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "RequestedByRole",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "RequestedByUserId",
                table: "Assignments");

            migrationBuilder.AlterColumn<string>(
                name: "RequestedByHrUserId",
                table: "PsychologistRequests",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
