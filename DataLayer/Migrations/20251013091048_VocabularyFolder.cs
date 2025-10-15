using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class VocabularyFolder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VocabularyFolderId",
                table: "Vocabularies",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VocabularyFolders",
                columns: table => new
                {
                    VocabularyFolderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParentFolderId = table.Column<int>(type: "int", nullable: true),
                    VocabularyListId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedByNavigationUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularyFolders", x => x.VocabularyFolderId);
                    table.ForeignKey(
                        name: "FK_VocabularyFolders_Users_CreatedByNavigationUserId",
                        column: x => x.CreatedByNavigationUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VocabularyFolders_VocabularyFolders_ParentFolderId",
                        column: x => x.ParentFolderId,
                        principalTable: "VocabularyFolders",
                        principalColumn: "VocabularyFolderId");
                    table.ForeignKey(
                        name: "FK_VocabularyFolders_VocabularyList_VocabularyListId",
                        column: x => x.VocabularyListId,
                        principalTable: "VocabularyList",
                        principalColumn: "VocabularyListId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vocabularies_VocabularyFolderId",
                table: "Vocabularies",
                column: "VocabularyFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyFolders_CreatedByNavigationUserId",
                table: "VocabularyFolders",
                column: "CreatedByNavigationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyFolders_ParentFolderId",
                table: "VocabularyFolders",
                column: "ParentFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyFolders_VocabularyListId",
                table: "VocabularyFolders",
                column: "VocabularyListId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vocabularies_VocabularyFolders_VocabularyFolderId",
                table: "Vocabularies",
                column: "VocabularyFolderId",
                principalTable: "VocabularyFolders",
                principalColumn: "VocabularyFolderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vocabularies_VocabularyFolders_VocabularyFolderId",
                table: "Vocabularies");

            migrationBuilder.DropTable(
                name: "VocabularyFolders");

            migrationBuilder.DropIndex(
                name: "IX_Vocabularies_VocabularyFolderId",
                table: "Vocabularies");

            migrationBuilder.DropColumn(
                name: "VocabularyFolderId",
                table: "Vocabularies");
        }
    }
}
