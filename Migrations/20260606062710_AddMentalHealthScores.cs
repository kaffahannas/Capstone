using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LightenUp.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMentalHealthScores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnxietyScore",
                table: "MoodTrackers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmotionScore",
                table: "MoodTrackers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FocusScore",
                table: "MoodTrackers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MindLoadScore",
                table: "MoodTrackers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SleepScore",
                table: "MoodTrackers",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnxietyScore",
                table: "MoodTrackers");

            migrationBuilder.DropColumn(
                name: "EmotionScore",
                table: "MoodTrackers");

            migrationBuilder.DropColumn(
                name: "FocusScore",
                table: "MoodTrackers");

            migrationBuilder.DropColumn(
                name: "MindLoadScore",
                table: "MoodTrackers");

            migrationBuilder.DropColumn(
                name: "SleepScore",
                table: "MoodTrackers");
        }
    }
}
