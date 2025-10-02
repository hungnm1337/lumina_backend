using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class v10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SampleAnswer",
                table: "Questions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SpeakingResults",
                columns: table => new
                {
                    SpeakingResultId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PronunciationScore = table.Column<float>(type: "real", nullable: true),
                    AccuracyScore = table.Column<float>(type: "real", nullable: true),
                    FluencyScore = table.Column<float>(type: "real", nullable: true),
                    CompletenessScore = table.Column<float>(type: "real", nullable: true),
                    GrammarScore = table.Column<float>(type: "real", nullable: true),
                    VocabularyScore = table.Column<float>(type: "real", nullable: true),
                    ContentScore = table.Column<float>(type: "real", nullable: true),
                    UserAnswerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpeakingResults", x => x.SpeakingResultId);
                    table.ForeignKey(
                        name: "FK_SpeakingResults_UserAnswers_UserAnswerId",
                        column: x => x.UserAnswerId,
                        principalTable: "UserAnswers",
                        principalColumn: "UserAnswerID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpeakingResults_UserAnswerId",
                table: "SpeakingResults",
                column: "UserAnswerId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpeakingResults");

            migrationBuilder.DropColumn(
                name: "SampleAnswer",
                table: "Questions");
        }
    }
}
