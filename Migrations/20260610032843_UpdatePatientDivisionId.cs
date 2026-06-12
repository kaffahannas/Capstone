using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LightenUp.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePatientDivisionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Department",
                table: "PendingEmployees");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "Patients");

            migrationBuilder.AddColumn<int>(
                name: "DivisionId",
                table: "PendingEmployees",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DivisionId",
                table: "Patients",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingEmployees_DivisionId",
                table: "PendingEmployees",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_DivisionId",
                table: "Patients",
                column: "DivisionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_CompanyDivisions_DivisionId",
                table: "Patients",
                column: "DivisionId",
                principalTable: "CompanyDivisions",
                principalColumn: "DivisionId");

            migrationBuilder.AddForeignKey(
                name: "FK_PendingEmployees_CompanyDivisions_DivisionId",
                table: "PendingEmployees",
                column: "DivisionId",
                principalTable: "CompanyDivisions",
                principalColumn: "DivisionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Patients_CompanyDivisions_DivisionId",
                table: "Patients");

            migrationBuilder.DropForeignKey(
                name: "FK_PendingEmployees_CompanyDivisions_DivisionId",
                table: "PendingEmployees");

            migrationBuilder.DropIndex(
                name: "IX_PendingEmployees_DivisionId",
                table: "PendingEmployees");

            migrationBuilder.DropIndex(
                name: "IX_Patients_DivisionId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "DivisionId",
                table: "PendingEmployees");

            migrationBuilder.DropColumn(
                name: "DivisionId",
                table: "Patients");

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "PendingEmployees",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "Patients",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
