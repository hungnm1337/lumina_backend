// File: GetUserNoteByUserIDAndArticleId.test.cs
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using lumina.Controllers;
using ServiceLayer.UserNote;
using DataLayer.DTOs.UserNote;
using System;
using System.Threading.Tasks;

namespace Lumina.Tests
{
    public class GetUserNoteByUserIDAndArticleIdTests
    {
        private readonly Mock<IUserNoteService> _mockUserNoteService;
        private readonly UserNoteController _controller;

        public GetUserNoteByUserIDAndArticleIdTests()
        {
            _mockUserNoteService = new Mock<IUserNoteService>();
            _controller = new UserNoteController(_mockUserNoteService.Object);
        }

        [Fact]
        public async Task GetUserNoteByUserIDAndArticleId_KhiUserNoteTimThay_TraVeOkVoiUserNote()
        {
            // Arrange
            int userId = 1;
            int articleId = 100;
            var expectedUserNote = new UserNoteResponseDTO
            {
                NoteId = 1,
                UserId = userId,
                ArticleId = articleId,
                NoteContent = "This is a test note for article 100"
            };

            _mockUserNoteService
                .Setup(s => s.GetUserNoteByUserIDAndArticleId(userId, articleId))
                .ReturnsAsync(expectedUserNote);

            // Act
            var result = await _controller.GetUserNoteByUserIDAndArticleId(userId, articleId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var userNote = Assert.IsType<UserNoteResponseDTO>(okResult.Value);
            Assert.Equal(userId, userNote.UserId);
            Assert.Equal(articleId, userNote.ArticleId);
            Assert.Equal("This is a test note for article 100", userNote.NoteContent);
            _mockUserNoteService.Verify(s => s.GetUserNoteByUserIDAndArticleId(userId, articleId), Times.Once);
        }

        [Fact]
        public async Task GetUserNoteByUserIDAndArticleId_KhiUserNoteKhongTimThay_TraVeNotFound()
        {
            // Arrange
            int userId = 999;
            int articleId = 888;

            _mockUserNoteService
                .Setup(s => s.GetUserNoteByUserIDAndArticleId(userId, articleId))
                .ReturnsAsync((UserNoteResponseDTO?)null);

            // Act
            var result = await _controller.GetUserNoteByUserIDAndArticleId(userId, articleId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            dynamic value = notFoundResult.Value!;
            Assert.Equal("User note not found.", value.Message);
            _mockUserNoteService.Verify(s => s.GetUserNoteByUserIDAndArticleId(userId, articleId), Times.Once);
        }

        [Fact]
        public async Task GetUserNoteByUserIDAndArticleId_KhiServiceThrowException_TraVeInternalServerError()
        {
            // Arrange
            int userId = 1;
            int articleId = 100;

            _mockUserNoteService
                .Setup(s => s.GetUserNoteByUserIDAndArticleId(userId, articleId))
                .ThrowsAsync(new Exception("Database connection error"));

            // Act
            var result = await _controller.GetUserNoteByUserIDAndArticleId(userId, articleId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            dynamic value = statusCodeResult.Value!;
            Assert.Equal("An error occurred while retrieving the user note.", value.Message);
        }

        [Theory]
        [InlineData(0, 100)]
        [InlineData(-1, 100)]
        [InlineData(1, 0)]
        [InlineData(1, -1)]
        [InlineData(0, 0)]
        [InlineData(-1, -1)]
        public async Task GetUserNoteByUserIDAndArticleId_KhiIdKhongHopLe_VaServiceTraVeNull_TraVeNotFound(int userId, int articleId)
        {
            // Arrange
            _mockUserNoteService
                .Setup(s => s.GetUserNoteByUserIDAndArticleId(userId, articleId))
                .ReturnsAsync((UserNoteResponseDTO?)null);

            // Act
            var result = await _controller.GetUserNoteByUserIDAndArticleId(userId, articleId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            dynamic value = notFoundResult.Value!;
            Assert.Equal("User note not found.", value.Message);
        }

        [Fact]
        public async Task GetUserNoteByUserIDAndArticleId_KhiIdLonNhat_VaTimThay_TraVeOk()
        {
            // Arrange
            int userId = int.MaxValue;
            int articleId = int.MaxValue;
            var expectedUserNote = new UserNoteResponseDTO
            {
                NoteId = 1,
                UserId = userId,
                ArticleId = articleId,
                NoteContent = "Max IDs note"
            };

            _mockUserNoteService
                .Setup(s => s.GetUserNoteByUserIDAndArticleId(userId, articleId))
                .ReturnsAsync(expectedUserNote);

            // Act
            var result = await _controller.GetUserNoteByUserIDAndArticleId(userId, articleId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var userNote = Assert.IsType<UserNoteResponseDTO>(okResult.Value);
            Assert.Equal(userId, userNote.UserId);
            Assert.Equal(articleId, userNote.ArticleId);
        }

        [Fact]
        public async Task GetUserNoteByUserIDAndArticleId_KhiServiceThrowInvalidOperationException_TraVeInternalServerError()
        {
            // Arrange
            int userId = 1;
            int articleId = 100;

            _mockUserNoteService
                .Setup(s => s.GetUserNoteByUserIDAndArticleId(userId, articleId))
                .ThrowsAsync(new InvalidOperationException("Invalid operation"));

            // Act
            var result = await _controller.GetUserNoteByUserIDAndArticleId(userId, articleId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            dynamic value = statusCodeResult.Value!;
            Assert.Equal("An error occurred while retrieving the user note.", value.Message);
        }
    }
}
