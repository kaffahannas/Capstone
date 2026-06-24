using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LightenUp.Web.Migrations
{
    /// <inheritdoc />
    public partial class RevisiModelBisnis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatientAdminAssignmentRequests");

            migrationBuilder.AddColumn<int>(
                name: "PsychologistId",
                table: "Subscriptions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMitraActive",
                table: "Psychologists",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MitraReferralCode",
                table: "Psychologists",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerMonth",
                table: "Psychologists",
                type: "decimal(14,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionTokensPerMonth",
                table: "Psychologists",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PsychologistSubscriptionId",
                table: "PaymentTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SponsorPsychologistId",
                table: "Patients",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SponsorType",
                table: "Patients",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "PsychologistSubscriptions",
                columns: table => new
                {
                    PsychologistSubscriptionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PsychologistId = table.Column<int>(type: "int", nullable: false),
                    PlanName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PatientLimit = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PsychologistSubscriptions", x => x.PsychologistSubscriptionId);
                    table.ForeignKey(
                        name: "FK_PsychologistSubscriptions_Psychologists_PsychologistId",
                        column: x => x.PsychologistId,
                        principalTable: "Psychologists",
                        principalColumn: "PsychologistId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PsychologistId",
                table: "Subscriptions",
                column: "PsychologistId");

            migrationBuilder.CreateIndex(
                name: "IX_Psychologists_MitraReferralCode",
                table: "Psychologists",
                column: "MitraReferralCode",
                unique: true,
                filter: "[MitraReferralCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_PsychologistSubscriptionId",
                table: "PaymentTransactions",
                column: "PsychologistSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_SponsorPsychologistId",
                table: "Patients",
                column: "SponsorPsychologistId");

            migrationBuilder.CreateIndex(
                name: "IX_PsychologistSubscriptions_PsychologistId",
                table: "PsychologistSubscriptions",
                column: "PsychologistId");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Psychologists_SponsorPsychologistId",
                table: "Patients",
                column: "SponsorPsychologistId",
                principalTable: "Psychologists",
                principalColumn: "PsychologistId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentTransactions_PsychologistSubscriptions_PsychologistSubscriptionId",
                table: "PaymentTransactions",
                column: "PsychologistSubscriptionId",
                principalTable: "PsychologistSubscriptions",
                principalColumn: "PsychologistSubscriptionId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_Psychologists_PsychologistId",
                table: "Subscriptions",
                column: "PsychologistId",
                principalTable: "Psychologists",
                principalColumn: "PsychologistId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Psychologists_SponsorPsychologistId",
                table: "Patients");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentTransactions_PsychologistSubscriptions_PsychologistSubscriptionId",
                table: "PaymentTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_Psychologists_PsychologistId",
                table: "Subscriptions");

            migrationBuilder.DropTable(
                name: "PsychologistSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_PsychologistId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Psychologists_MitraReferralCode",
                table: "Psychologists");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_PsychologistSubscriptionId",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Patients_SponsorPsychologistId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "PsychologistId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "IsMitraActive",
                table: "Psychologists");

            migrationBuilder.DropColumn(
                name: "MitraReferralCode",
                table: "Psychologists");

            migrationBuilder.DropColumn(
                name: "PricePerMonth",
                table: "Psychologists");

            migrationBuilder.DropColumn(
                name: "SessionTokensPerMonth",
                table: "Psychologists");

            migrationBuilder.DropColumn(
                name: "PsychologistSubscriptionId",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "SponsorPsychologistId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "SponsorType",
                table: "Patients");

            migrationBuilder.CreateTable(
                name: "PatientAdminAssignmentRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignedByAdminUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AssignedPsychologistId = table.Column<int>(type: "int", nullable: true),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    PreferredPsychologistId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DecisionAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
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
    }
}
