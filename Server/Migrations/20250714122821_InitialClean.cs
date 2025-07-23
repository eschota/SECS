using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialClean : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    MatchMaxTimeLimit = table.Column<float>(type: "REAL", nullable: false, defaultValue: 600f),
                    Status = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameMatches", x => x.MatchId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Avatar = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    OAuthToken = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    OAuthProvider = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    LastHeartbeat = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    GamesPlayed = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    GamesWon = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Score = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Level = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    MmrOneVsOne = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 500),
                    MmrTwoVsTwo = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 500),
                    MmrFourPlayerFFA = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 500)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameMatches_Status",
                table: "GameMatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameMatches");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
