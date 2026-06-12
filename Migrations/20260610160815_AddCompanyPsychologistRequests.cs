using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LightenUp.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyPsychologistRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyPsychologistRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    PsychologistId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HandledByAdminUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyPsychologistRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyPsychologistRequests_AspNetUsers_HandledByAdminUserId",
                        column: x => x.HandledByAdminUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CompanyPsychologistRequests_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanyPsychologistRequests_Psychologists_PsychologistId",
                        column: x => x.PsychologistId,
                        principalTable: "Psychologists",
                        principalColumn: "PsychologistId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyPsychologistRequests_CompanyId",
                table: "CompanyPsychologistRequests",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyPsychologistRequests_HandledByAdminUserId",
                table: "CompanyPsychologistRequests",
                column: "HandledByAdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyPsychologistRequests_PsychologistId",
                table: "CompanyPsychologistRequests",
                column: "PsychologistId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyPsychologistRequests");
        }
    }
}
