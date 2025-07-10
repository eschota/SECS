using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchSystemAndMMR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentMatchId",
                table: "Users",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsInQueue",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MmrFourPlayerFFA",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MmrOneVsOne",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MmrTwoVsTwo",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "GameMatches",
                columns: table => new
                {
                    MatchId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MatchType = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayersId = table.Column<string>(type: "TEXT", nullable: false),
                    TeamId = table.Column<string>(type: "TEXT", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MatchWin = table.Column<string>(type: "TEXT", nullable: true),
                    MatchLose = table.Column<string>(type: "TEXT", nullable: true),
                    MatchDraw = table.Column<string>(type: "TEXT", nullable: true),
                    MatchMaxTimeLimit = table.Column<float>(type: "REAL", nullable: false, defaultValue: 60f),
                    Status = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameMatches", x => x.MatchId);
                });

            migrationBuilder.CreateTable(
                name: "MatchQueues",
                columns: table => new
                {
                    QueueId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    MatchType = table.Column<int>(type: "INTEGER", nullable: false),
                    MmrRating = table.Column<int>(type: "INTEGER", nullable: false),
                    QueueTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    SearchThreshold = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 20)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchQueues", x => x.QueueId);
                    table.ForeignKey(
                        name: "FK_MatchQueues_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameMatches_Status",
                table: "GameMatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MatchQueues_MatchType_MmrRating",
                table: "MatchQueues",
                columns: new[] { "MatchType", "MmrRating" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchQueues_UserId",
                table: "MatchQueues",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameMatches");

            migrationBuilder.DropTable(
                name: "MatchQueues");

            migrationBuilder.DropColumn(
                name: "CurrentMatchId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsInQueue",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MmrFourPlayerFFA",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MmrOneVsOne",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MmrTwoVsTwo",
                table: "Users");
        }
    }
}
