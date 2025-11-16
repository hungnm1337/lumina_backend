using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotaTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add quota tracking columns to Users table
            migrationBuilder.AddColumn<int>(
                name: "MonthlyReadingAttempts",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MonthlyListeningAttempts",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastQuotaReset",
                table: "Users",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            // Add index for performance on subscription queries
            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_UserId_Status",
                table: "Subscriptions",
                columns: new[] { "UserId", "Status" },
                filter: "[Status] = 'Active'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_UserId_Status",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "MonthlyReadingAttempts",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MonthlyListeningAttempts",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastQuotaReset",
                table: "Users");
        }
    }
}
