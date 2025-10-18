using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class m36 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreateAt",
                table: "Leaderboard",
                type: "datetime2(3)",
                precision: 3,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Leaderboard",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SeasonName",
                table: "Leaderboard",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeasonNumber",
                table: "Leaderboard",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateAt",
                table: "Leaderboard",
                type: "datetime2(3)",
                precision: 3,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreateAt",
                table: "Leaderboard");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Leaderboard");

            migrationBuilder.DropColumn(
                name: "SeasonName",
                table: "Leaderboard");

            migrationBuilder.DropColumn(
                name: "SeasonNumber",
                table: "Leaderboard");

            migrationBuilder.DropColumn(
                name: "UpdateAt",
                table: "Leaderboard");
        }
    }
}
