using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class updateStatusVocabulary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "VocabularyList",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "VocabularyList",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByNavigationUserId",
                table: "VocabularyList",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyList_UpdatedByNavigationUserId",
                table: "VocabularyList",
                column: "UpdatedByNavigationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_VocabularyList_Users_UpdatedByNavigationUserId",
                table: "VocabularyList",
                column: "UpdatedByNavigationUserId",
                principalTable: "Users",
                principalColumn: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VocabularyList_Users_UpdatedByNavigationUserId",
                table: "VocabularyList");

            migrationBuilder.DropIndex(
                name: "IX_VocabularyList_UpdatedByNavigationUserId",
                table: "VocabularyList");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "VocabularyList");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "VocabularyList");

            migrationBuilder.DropColumn(
                name: "UpdatedByNavigationUserId",
                table: "VocabularyList");
        }
    }
}
