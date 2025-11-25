using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using lumina.Controllers;
using ServiceLayer.Article;
using ServiceLayer.UserNote;
using RepositoryLayer.UnitOfWork;
using DataLayer.DTOs.Article;
using DataLayer.DTOs.UserNote;

#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.

namespace Lumina.Tests.IntegrationTest
{
    public class IntegrationNoteArticles
    {
        private readonly Mock<IArticleService> _mockArticleService;
        private readonly Mock<IUserNoteService> _mockUserNoteService;
        private readonly Mock<ILogger<ArticlesController>> _mockArticlesLogger;
        private readonly Mock<ILogger<UserNoteController>> _mockUserNoteLogger;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly ArticlesController _articlesController;
        private readonly UserNoteController _userNoteController;

        public IntegrationNoteArticles()
        {
            _mockArticleService = new Mock<IArticleService>();
            _mockUserNoteService = new Mock<IUserNoteService>();
            _mockArticlesLogger = new Mock<ILogger<ArticlesController>>();
            _mockUserNoteLogger = new Mock<ILogger<UserNoteController>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            
            _articlesController = new ArticlesController(
                _mockArticleService.Object, 
                _mockArticlesLogger.Object, 
                _mockUnitOfWork.Object);
            
            _userNoteController = new UserNoteController(_mockUserNoteService.Object);
        }

        [Fact]
        public async Task IntegrationTest_LuongDocArticleVaTaoNote_ThanhCong()
        {
            // ========== STEP 1: GET PUBLIC ARTICLES (L?y danh s�ch article) ==========
            int userId = 1;
            int articleId = 10;
            int sectionId = 5;

            var publicArticles = new List<ArticleResponseDTO>
            {
                new ArticleResponseDTO
                {
                    ArticleId = 10,
                    Title = "TOEIC Reading Tips",
                    Summary = "Essential tips for TOEIC reading section",
                    IsPublished = true,
                    Status = "Published",
                    AuthorName = "John Doe",
                    CategoryName = "TOEIC Tips"
                },
                new ArticleResponseDTO
                {
                    ArticleId = 11,
                    Title = "TOEIC Listening Strategies",
                    Summary = "Effective strategies for TOEIC listening",
                    IsPublished = true,
                    Status = "Published",
                    AuthorName = "Jane Smith",
                    CategoryName = "TOEIC Tips"
                }
            };

            var pagedResult = new PagedResponse<ArticleResponseDTO>
            {
                Items = publicArticles,
                Total = 2,
                Page = 1,
                PageSize = 1000
            };

            _mockArticleService
                .Setup(s => s.QueryAsync(It.IsAny<ArticleQueryParams>()))
                .ReturnsAsync(pagedResult);

            // Act - Step 1: L?y danh s�ch article c�ng khai
            var articlesResult = await _articlesController.GetPublicArticles();

            // Assert - Step 1: Ki?m tra danh s�ch article
            var articlesOkResult = Assert.IsType<OkObjectResult>(articlesResult.Result);
            var articlesList = Assert.IsAssignableFrom<IEnumerable<ArticleResponseDTO>>(articlesOkResult.Value);
            Assert.Equal(2, articlesList.Count());
            Assert.Contains(articlesList, a => a.ArticleId == articleId);

            // ========== STEP 2: GET ARTICLE BY ID (V�o detail 1 article) ==========
            var articleDetail = new ArticleResponseDTO
            {
                ArticleId = articleId,
                Title = "TOEIC Reading Tips",
                Summary = "Essential tips for TOEIC reading section",
                IsPublished = true,
                Status = "Published",
                AuthorName = "John Doe",
                CategoryName = "TOEIC Tips",
                Sections = new List<ArticleSectionResponseDTO>
                {
                    new ArticleSectionResponseDTO
                    {
                        SectionId = sectionId,
                        SectionTitle = "Introduction",
                        SectionContent = "This section covers the basics...",
                        OrderIndex = 1
                    }
                }
            };

            _mockArticleService
                .Setup(s => s.GetArticleByIdAsync(articleId))
                .ReturnsAsync(articleDetail);

            // Act - Step 2: Xem chi ti?t article
            var detailResult = await _articlesController.GetArticleById(articleId);

            // Assert - Step 2: Ki?m tra chi ti?t article
            var detailOkResult = Assert.IsType<OkObjectResult>(detailResult);
            var articleResponse = Assert.IsType<ArticleResponseDTO>(detailOkResult.Value);
            Assert.Equal(articleId, articleResponse.ArticleId);
            Assert.NotEmpty(articleResponse.Sections);
            Assert.Equal(sectionId, articleResponse.Sections[0].SectionId);

            // ========== STEP 3: GET USER NOTE BY USER ID AND ARTICLE ID (L?y note cu) ==========
            // Tru?ng h?p chua c� note cu
            _mockUserNoteService
                .Setup(s => s.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId))
                .ReturnsAsync(null as UserNoteResponseDTO);

            // Act - Step 3: Ki?m tra note cu
            var oldNoteResult = await _userNoteController.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId);

            // Assert - Step 3: Chua c� note cu
            Assert.IsType<NotFoundObjectResult>(oldNoteResult);

            // ========== STEP 4: UPSERT USER NOTE (Ngu?i d�ng th�m note m?i) ==========
            var createNoteRequest = new UserNoteRequestDTO
            {
                NoteId = 0, // 0 = t?o m?i
                UserId = userId,
                ArticleId = articleId,
                SectionId = sectionId,
                NoteContent = "This is a very important tip for reading comprehension!"
            };

            _mockUserNoteService
                .Setup(s => s.UpsertUserNote(It.IsAny<UserNoteRequestDTO>()))
                .ReturnsAsync(true);

            // Act - Step 4: T?o note m?i
            var createNoteResult = await _userNoteController.UpsertUserNote(createNoteRequest);

            // Assert - Step 4: Note du?c t?o th�nh c�ng
            var createNoteOkResult = Assert.IsType<OkObjectResult>(createNoteResult);
            var createValue = createNoteOkResult.Value;
            Assert.NotNull(createValue);
            var messageProperty = createValue.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(createValue)?.ToString();
            Assert.Equal("User note upserted successfully.", message);

            // ========== STEP 5: GET ALL USER NOTES BY USER ID (Xem danh s�ch note) ==========
            int noteId = 100;
            var userNotes = new List<UserNoteResponseDTO>
            {
                new UserNoteResponseDTO
                {
                    NoteId = noteId,
                    UserId = userId,
                    User = "Test User",
                    ArticleId = articleId,
                    Article = "TOEIC Reading Tips",
                    SectionId = sectionId,
                    Section = "Introduction",
                    NoteContent = "This is a very important tip for reading comprehension!",
                    CreateAt = DateTime.UtcNow,
                    UpdateAt = null
                }
            };

            _mockUserNoteService
                .Setup(s => s.GetAllUserNotesByUserId(userId))
                .ReturnsAsync(userNotes);

            // Act - Step 5: L?y danh s�ch note c?a user
            var allNotesResult = await _userNoteController.GetAllUserNotesByUserId(userId);

            // Assert - Step 5: Ki?m tra danh s�ch note
            var allNotesOkResult = Assert.IsType<OkObjectResult>(allNotesResult);
            var notesList = Assert.IsAssignableFrom<IEnumerable<UserNoteResponseDTO>>(allNotesOkResult.Value);
            Assert.Single(notesList);
            Assert.Equal(noteId, notesList.First().NoteId);
            Assert.Equal(articleId, notesList.First().ArticleId);

            // ========== STEP 6: GET USER NOTE BY ID (Ngu?i d�ng ch?n 1 note) ==========
            var selectedNote = new UserNoteResponseDTO
            {
                NoteId = noteId,
                UserId = userId,
                User = "Test User",
                ArticleId = articleId,
                Article = "TOEIC Reading Tips",
                SectionId = sectionId,
                Section = "Introduction",
                NoteContent = "This is a very important tip for reading comprehension!",
                CreateAt = DateTime.UtcNow.AddHours(-1),
                UpdateAt = null
            };

            _mockUserNoteService
                .Setup(s => s.GetUserNoteByID(noteId))
                .ReturnsAsync(selectedNote);

            // Act - Step 6: Xem chi ti?t note
            var selectedNoteResult = await _userNoteController.GetUserNoteByID(noteId);

            // Assert - Step 6: Ki?m tra note du?c ch?n
            var selectedNoteOkResult = Assert.IsType<OkObjectResult>(selectedNoteResult);
            var noteResponse = Assert.IsType<UserNoteResponseDTO>(selectedNoteOkResult.Value);
            Assert.Equal(noteId, noteResponse.NoteId);
            Assert.Equal("This is a very important tip for reading comprehension!", noteResponse.NoteContent);

            // ========== STEP 7: UPSERT USER NOTE (Ngu?i d�ng s?a note) ==========
            var updateNoteRequest = new UserNoteRequestDTO
            {
                NoteId = noteId,
                UserId = userId,
                ArticleId = articleId,
                SectionId = sectionId,
                NoteContent = "Updated: This is an extremely important tip! Must remember for exam!"
            };

            _mockUserNoteService
                .Setup(s => s.UpsertUserNote(It.Is<UserNoteRequestDTO>(r => r.NoteId == noteId)))
                .ReturnsAsync(true);

            // Act - Step 7: C?p nh?t note
            var updateNoteResult = await _userNoteController.UpsertUserNote(updateNoteRequest);

            // Assert - Step 7: Note du?c c?p nh?t th�nh c�ng
            var updateNoteOkResult = Assert.IsType<OkObjectResult>(updateNoteResult);
            var updateValue = updateNoteOkResult.Value;
            Assert.NotNull(updateValue);
            var updateMessageProperty = updateValue.GetType().GetProperty("Message");
            Assert.NotNull(updateMessageProperty);
            var updateMessage = updateMessageProperty.GetValue(updateValue)?.ToString();
            Assert.Equal("User note upserted successfully.", updateMessage);

            // Verify t?t c? c�c services d� du?c g?i d�ng s? l?n
            _mockArticleService.Verify(s => s.QueryAsync(It.IsAny<ArticleQueryParams>()), Times.Once);
            _mockArticleService.Verify(s => s.GetArticleByIdAsync(articleId), Times.Once);
            _mockUserNoteService.Verify(s => s.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId), Times.Once);
            _mockUserNoteService.Verify(s => s.UpsertUserNote(It.IsAny<UserNoteRequestDTO>()), Times.Exactly(2)); // T?o + S?a
            _mockUserNoteService.Verify(s => s.GetAllUserNotesByUserId(userId), Times.Once);
            _mockUserNoteService.Verify(s => s.GetUserNoteByID(noteId), Times.Once);
        }

        [Fact]
        public async Task IntegrationTest_LuongDocArticleVaUpdateNoteCu_ThanhCong()
        {
            // Test khi ngu?i d�ng �� C� note cu v� mu?n update
            int userId = 1;
            int articleId = 10;
            int sectionId = 5;
            int existingNoteId = 50;

            // Step 1: Get public articles
            var publicArticles = new List<ArticleResponseDTO>
            {
                new ArticleResponseDTO { ArticleId = articleId, Title = "Test Article", IsPublished = true }
            };

            var pagedResult = new PagedResponse<ArticleResponseDTO>
            {
                Items = publicArticles,
                Total = 1,
                Page = 1,
                PageSize = 1000
            };

            _mockArticleService
                .Setup(s => s.QueryAsync(It.IsAny<ArticleQueryParams>()))
                .ReturnsAsync(pagedResult);

            var articlesResult = await _articlesController.GetPublicArticles();
            Assert.IsType<OkObjectResult>(articlesResult.Result);

            // Step 2: Get article detail
            var articleDetail = new ArticleResponseDTO
            {
                ArticleId = articleId,
                Title = "Test Article",
                Sections = new List<ArticleSectionResponseDTO>
                {
                    new ArticleSectionResponseDTO { SectionId = sectionId, SectionTitle = "Section 1" }
                }
            };

            _mockArticleService
                .Setup(s => s.GetArticleByIdAsync(articleId))
                .ReturnsAsync(articleDetail);

            var detailResult = await _articlesController.GetArticleById(articleId);
            Assert.IsType<OkObjectResult>(detailResult);

            // Step 3: Get existing note (�� C� note cu)
            var existingNote = new UserNoteResponseDTO
            {
                NoteId = existingNoteId,
                UserId = userId,
                ArticleId = articleId,
                SectionId = sectionId,
                NoteContent = "Old note content",
                CreateAt = DateTime.UtcNow.AddDays(-7)
            };

            _mockUserNoteService
                .Setup(s => s.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId))
                .ReturnsAsync(existingNote);

            var oldNoteResult = await _userNoteController.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId);

            // Assert: �� c� note cu
            var oldNoteOkResult = Assert.IsType<OkObjectResult>(oldNoteResult);
            var oldNote = Assert.IsType<UserNoteResponseDTO>(oldNoteOkResult.Value);
            Assert.Equal(existingNoteId, oldNote.NoteId);
            Assert.Equal("Old note content", oldNote.NoteContent);

            // Step 4: Update existing note
            var updateRequest = new UserNoteRequestDTO
            {
                NoteId = existingNoteId,
                UserId = userId,
                ArticleId = articleId,
                SectionId = sectionId,
                NoteContent = "Updated note content - added more details"
            };

            _mockUserNoteService
                .Setup(s => s.UpsertUserNote(It.IsAny<UserNoteRequestDTO>()))
                .ReturnsAsync(true);

            var updateResult = await _userNoteController.UpsertUserNote(updateRequest);
            var updateOkResult = Assert.IsType<OkObjectResult>(updateResult);
            Assert.NotNull(updateOkResult.Value);

            // Verify
            _mockUserNoteService.Verify(s => s.GetUserNoteByUserIDAndArticleId(userId, articleId, sectionId), Times.Once);
            _mockUserNoteService.Verify(s => s.UpsertUserNote(It.IsAny<UserNoteRequestDTO>()), Times.Once);
        }

        [Fact]
        public async Task IntegrationTest_LuongDocArticle_GetArticleByIdFailed_KhongTimThayArticle()
        {
            // Arrange
            int invalidArticleId = 999;

            _mockArticleService
                .Setup(s => s.GetArticleByIdAsync(invalidArticleId))
                .ReturnsAsync(null as ArticleResponseDTO);

            // Act
            var result = await _articlesController.GetArticleById(invalidArticleId);

            // Assert
            Assert.IsType<NotFoundResult>(result);

            // Verify service du?c g?i
            _mockArticleService.Verify(s => s.GetArticleByIdAsync(invalidArticleId), Times.Once);
        }

        [Fact]
        public async Task IntegrationTest_LuongDocArticle_UpsertNoteFailed_DuLieuKhongHopLe()
        {
            // Arrange
            var invalidNoteRequest = new UserNoteRequestDTO
            {
                NoteId = 0,
                UserId = 1,
                ArticleId = 10,
                SectionId = 5,
                NoteContent = "Test note"
            };

            _mockUserNoteService
                .Setup(s => s.UpsertUserNote(It.IsAny<UserNoteRequestDTO>()))
                .ReturnsAsync(false); // Service tr? v? false = kh�ng th�nh c�ng

            // Act
            var result = await _userNoteController.UpsertUserNote(invalidNoteRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var value = badRequestResult.Value;
            Assert.NotNull(value);
            var messageProperty = value.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("Could not save the user note. The item may not exist or data is invalid.", message);

            // Verify
            _mockUserNoteService.Verify(s => s.UpsertUserNote(It.IsAny<UserNoteRequestDTO>()), Times.Once);
        }

        [Fact]
        public async Task IntegrationTest_LuongDocArticle_GetAllNotesFailed_InvalidUserId()
        {
            // Arrange
            int invalidUserId = 0;

            // Act
            var result = await _userNoteController.GetAllUserNotesByUserId(invalidUserId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var value = badRequestResult.Value;
            Assert.NotNull(value);
            var messageProperty = value.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("Invalid user ID.", message);

            // Verify service KH�NG du?c g?i v� d� fail validation
            _mockUserNoteService.Verify(s => s.GetAllUserNotesByUserId(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task IntegrationTest_LuongDocArticle_GetUserNoteByIDFailed_KhongTimThayNote()
        {
            // Arrange
            int invalidNoteId = 999;

            _mockUserNoteService
                .Setup(s => s.GetUserNoteByID(invalidNoteId))
                .ReturnsAsync(null as UserNoteResponseDTO);

            // Act
            var result = await _userNoteController.GetUserNoteByID(invalidNoteId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var value = notFoundResult.Value;
            Assert.NotNull(value);
            var messageProperty = value.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("User note not found.", message);

            // Verify
            _mockUserNoteService.Verify(s => s.GetUserNoteByID(invalidNoteId), Times.Once);
        }
    }
}
