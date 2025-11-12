// File: UpsertUserNote.test.cs
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
    public class UpsertUserNoteTests
    {
        private readonly Mock<IUserNoteService> _mockUserNoteService;
        private readonly UserNoteController _controller;

        public UpsertUserNoteTests()
        {
            _mockUserNoteService = new Mock<IUserNoteService>();
            _controller = new UserNoteController(_mockUserNoteService.Object);
        }

        // Test cases với UserId = 1, ArticleId = 1
        [Fact]
        public async Task UpsertUserNote_KhiUserIdMotArticleIdMotNoteContentInvalidNote_TraVeThanhCong()
        {
            // Arrange
            var userNoteRequest = new UserNoteRequestDTO
            {
                UserId = 1,
                ArticleId = 1,
                NoteContent = "Invalid note"
            };

            _mockUserNoteService
                .Setup(s => s.UpsertUserNote(It.IsAny<UserNoteRequestDTO>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.UpsertUserNote(userNoteRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = okResult.Value;
            Assert.NotNull(value);
            var messageProperty = value.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("User note upserted successfully.", message);
        }

        [Fact]
        public async Task UpsertUserNote_KhiUserIdMotArticleIdMotNoteContentNull_TraVeBadRequest()
        {
            // Arrange
            var userNoteRequest = new UserNoteRequestDTO
            {
                UserId = 1,
                ArticleId = 1,
                NoteContent = null
            };

            _mockUserNoteService
                .Setup(s => s.UpsertUserNote(It.IsAny<UserNoteRequestDTO>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UpsertUserNote(userNoteRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var value = badRequestResult.Value;
            Assert.NotNull(value);
            var messageProperty = value.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("Could not save the user note. The item may not exist or data is invalid.", message);
        }

        [Fact]
        public async Task UpsertUserNote_KhiUserIdMotArticleIdMotNoteContentRong_TraVeThanhCong()
        {
            // Arrange
            var userNoteRequest = new UserNoteRequestDTO
            {
                UserId = 1,
                ArticleId = 1,
                NoteContent = ""
            };

            _mockUserNoteService
                .Setup(s => s.UpsertUserNote(It.IsAny<UserNoteRequestDTO>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.UpsertUserNote(userNoteRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = okResult.Value;
            Assert.NotNull(value);
            var messageProperty = value.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("User note upserted successfully.", message);
        }

        // Test cases với UserId = 0
        [Fact]
        public async Task UpsertUserNote_KhiUserIdKhongArticleIdMotNoteContentInvalidNote_TraVeBadRequest()
        {
            // Arrange
            var userNoteRequest = new UserNoteRequestDTO
            {
                UserId = 0,
                ArticleId = 1,
                NoteContent = "Invalid note"
            };

            _mockUserNoteService
                .Setup(s => s.UpsertUserNote(It.IsAny<UserNoteRequestDTO>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UpsertUserNote(userNoteRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var value = badRequestResult.Value;
            Assert.NotNull(value);
            var messageProperty = value.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("Could not save the user note. The item may not exist or data is invalid.", message);
        }

        // Test cases với UserId = -1
        [Fact]
        public async Task UpsertUserNote_KhiUserIdAmMotArticleIdMotNoteContentInvalidNote_TraVeBadRequest()
        {
            // Arrange
            var userNoteRequest = new UserNoteRequestDTO
            {
                UserId = -1,
                ArticleId = 1,
                NoteContent = "Invalid note"
            };

            _mockUserNoteService
                .Setup(s => s.UpsertUserNote(It.IsAny<UserNoteRequestDTO>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UpsertUserNote(userNoteRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var value = badRequestResult.Value;
            Assert.NotNull(value);
            var messageProperty = value.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("Could not save the user note. The item may not exist or data is invalid.", message);
        }

        // Test cases với UserId = 999 (không tồn tại)
        [Fact]
        public async Task UpsertUserNote_KhiUserIdKhongTonTaiArticleIdMotNoteContentInvalidNote_TraVeBadRequest()
        {
            // Arrange
            var userNoteRequest = new UserNoteRequestDTO
            {
                UserId = 999,
                ArticleId = 1,
                NoteContent = "Invalid note"
            };

            _mockUserNoteService
                .Setup(s => s.UpsertUserNote(It.IsAny<UserNoteRequestDTO>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UpsertUserNote(userNoteRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var value = badRequestResult.Value;
            Assert.NotNull(value);
            var messageProperty = value.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("Could not save the user note. The item may not exist or data is invalid.", message);
        }

        // Test cases với ArticleId = 0
        [Fact]
        public async Task UpsertUserNote_KhiUserIdMotArticleIdKhongNoteContentInvalidNote_TraVeBadRequest()
        {
            // Arrange
            var userNoteRequest = new UserNoteRequestDTO
            {
                UserId = 1,
                ArticleId = 0,
                NoteContent = "Invalid note"
            };

            _mockUserNoteService
                .Setup(s => s.UpsertUserNote(It.IsAny<UserNoteRequestDTO>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UpsertUserNote(userNoteRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var value = badRequestResult.Value;
            Assert.NotNull(value);
            var messageProperty = value.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("Could not save the user note. The item may not exist or data is invalid.", message);
        }

        // Test cases với ArticleId = -1
        [Fact]
        public async Task UpsertUserNote_KhiUserIdMotArticleIdAmMotNoteContentInvalidNote_TraVeBadRequest()
        {
            // Arrange
            var userNoteRequest = new UserNoteRequestDTO
            {
                UserId = 1,
                ArticleId = -1,
                NoteContent = "Invalid note"
            };

            _mockUserNoteService
                .Setup(s => s.UpsertUserNote(It.IsAny<UserNoteRequestDTO>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UpsertUserNote(userNoteRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var value = badRequestResult.Value;
            Assert.NotNull(value);
            var messageProperty = value.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("Could not save the user note. The item may not exist or data is invalid.", message);
        }

        // Test cases với ArticleId = 999 (không tồn tại)
        [Fact]
        public async Task UpsertUserNote_KhiUserIdMotArticleIdKhongTonTaiNoteContentInvalidNote_TraVeBadRequest()
        {
            // Arrange
            var userNoteRequest = new UserNoteRequestDTO
            {
                UserId = 1,
                ArticleId = 999,
                NoteContent = "Invalid note"
            };

            _mockUserNoteService
                .Setup(s => s.UpsertUserNote(It.IsAny<UserNoteRequestDTO>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UpsertUserNote(userNoteRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var value = badRequestResult.Value;
            Assert.NotNull(value);
            var messageProperty = value.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("Could not save the user note. The item may not exist or data is invalid.", message);
        }

        // Test case exception handling
        [Fact]
        public async Task UpsertUserNote_KhiServiceThrowException_TraVeInternalServerError()
        {
            // Arrange
            var userNoteRequest = new UserNoteRequestDTO
            {
                UserId = 1,
                ArticleId = 1,
                NoteContent = "Invalid note"
            };

            _mockUserNoteService
                .Setup(s => s.UpsertUserNote(It.IsAny<UserNoteRequestDTO>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.UpsertUserNote(userNoteRequest);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var value = statusCodeResult.Value;
            Assert.NotNull(value);
            var messageProperty = value.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("An internal server error occurred.", message);
        }
    }
}
