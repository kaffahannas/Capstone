using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LightenUp.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddHrEmployeeRemovalRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateTable(
                name: "HrEmployeeRemovalRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    RequestedByHrUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DecisionByAdminUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    DecisionAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecisionNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HrEmployeeRemovalRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HrEmployeeRemovalRequests_AspNetUsers_DecisionByAdminUserId",
                        column: x => x.DecisionByAdminUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HrEmployeeRemovalRequests_AspNetUsers_RequestedByHrUserId",
                        column: x => x.RequestedByHrUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HrEmployeeRemovalRequests_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HrEmployeeRemovalRequests_DecisionByAdminUserId",
                table: "HrEmployeeRemovalRequests",
                column: "DecisionByAdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HrEmployeeRemovalRequests_PatientId",
                table: "HrEmployeeRemovalRequests",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_HrEmployeeRemovalRequests_RequestedByHrUserId",
                table: "HrEmployeeRemovalRequests",
                column: "RequestedByHrUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HrEmployeeRemovalRequests");
        }
    }
}
