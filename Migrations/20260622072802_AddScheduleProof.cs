using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LightenUp.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleProof : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProofOfCompletionPath",
                table: "Schedules",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProofOfCompletionPath",
                table: "Schedules");
        }
    }
}
