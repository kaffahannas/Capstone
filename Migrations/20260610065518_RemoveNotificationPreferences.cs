using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LightenUp.Web.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNotificationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HrNotificationPreferences");

            migrationBuilder.DropTable(
                name: "PatientNotificationPreferences");

            migrationBuilder.DropTable(
                name: "PsyNotificationPreferences");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HrNotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HrId = table.Column<int>(type: "int", nullable: false),
                    AllowEmployeePsyNotif = table.Column<bool>(type: "bit", nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    RemindCounselingSession = table.Column<bool>(type: "bit", nullable: false),
                    RemindEmployeeCheck = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HrNotificationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HrNotificationPreferences_HrStaffs_HrId",
                        column: x => x.HrId,
                        principalTable: "HrStaffs",
                        principalColumn: "HrId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientNotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    AllowHrPsychologistNotif = table.Column<bool>(type: "bit", nullable: false),
                    RemindCounselingSession = table.Column<bool>(type: "bit", nullable: false),
                    RemindMoodCheck = table.Column<bool>(type: "bit", nullable: false),
                    ReminderTime = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientNotificationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientNotificationPreferences_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PsyNotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PsychologistId = table.Column<int>(type: "int", nullable: false),
                    AllowHrPatientNotif = table.Column<bool>(type: "bit", nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    RemindFollowUp = table.Column<bool>(type: "bit", nullable: false),
                    RemindNewReports = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PsyNotificationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PsyNotificationPreferences_Psychologists_PsychologistId",
                        column: x => x.PsychologistId,
                        principalTable: "Psychologists",
                        principalColumn: "PsychologistId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HrNotificationPreferences_HrId",
                table: "HrNotificationPreferences",
                column: "HrId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientNotificationPreferences_PatientId",
                table: "PatientNotificationPreferences",
                column: "PatientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PsyNotificationPreferences_PsychologistId",
                table: "PsyNotificationPreferences",
                column: "PsychologistId",
                unique: true);
        }
    }
}
