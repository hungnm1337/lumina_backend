using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class v44 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VocabularyId",
                table: "UserSpacedRepetition",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSpacedRepetition_VocabularyId",
                table: "UserSpacedRepetition",
                column: "VocabularyId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSpacedRepetition_Vocabulary",
                table: "UserSpacedRepetition",
                column: "VocabularyId",
                principalTable: "Vocabularies",
                principalColumn: "VocabularyID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSpacedRepetition_Vocabulary",
                table: "UserSpacedRepetition");

            migrationBuilder.DropIndex(
                name: "IX_UserSpacedRepetition_VocabularyId",
                table: "UserSpacedRepetition");

            migrationBuilder.DropColumn(
                name: "VocabularyId",
                table: "UserSpacedRepetition");
        }
    }
}
