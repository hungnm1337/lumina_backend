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

        [Fact]
        public async Task UpsertUserNote_KhiServiceTraVeTrue_TraVeOkVoiThongBaoThanhCong()
        {
            // Arrange
            var userNoteRequest = new UserNoteRequestDTO
            {
                UserId = 1,
                ArticleId = 100,
                NoteContent = "This is a test note"
            };

            _mockUserNoteService
                .Setup(s => s.UpsertUserNote(userNoteRequest))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.UpsertUserNote(userNoteRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic value = okResult.Value!;
            Assert.Equal("User note upserted successfully.", value.Message);
            _mockUserNoteService.Verify(s => s.UpsertUserNote(userNoteRequest), Times.Once);
        }

        [Fact]
        public async Task UpsertUserNote_KhiServiceTraVeFalse_TraVeBadRequest()
        {
            // Arrange
            var userNoteRequest = new UserNoteRequestDTO
            {
                UserId = 1,
                ArticleId = 999,
                NoteContent = "Invalid note"
            };

            _mockUserNoteService
                .Setup(s => s.UpsertUserNote(userNoteRequest))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UpsertUserNote(userNoteRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            dynamic value = badRequestResult.Value!;
            Assert.Equal("Could not save the user note. The item may not exist or data is invalid.", value.Message);
            _mockUserNoteService.Verify(s => s.UpsertUserNote(userNoteRequest), Times.Once);
        }

        [Fact]
        public async Task UpsertUserNote_KhiServiceThrowException_TraVeInternalServerError()
        {
            // Arrange
            var userNoteRequest = new UserNoteRequestDTO
            {
                UserId = 1,
                ArticleId = 100,
                NoteContent = "Test note"
            };

            _mockUserNoteService
                .Setup(s => s.UpsertUserNote(userNoteRequest))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.UpsertUserNote(userNoteRequest);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            dynamic value = statusCodeResult.Value!;
            Assert.Equal("An internal server error occurred.", value.Message);
        }

        [Fact]
        public async Task UpsertUserNote_KhiServiceThrowArgumentNullException_TraVeInternalServerError()
        {
            // Arrange
            var userNoteRequest = new UserNoteRequestDTO
            {
                UserId = 1,
                ArticleId = 100,
                NoteContent = null!
            };

            _mockUserNoteService
                .Setup(s => s.UpsertUserNote(userNoteRequest))
                .ThrowsAsync(new ArgumentNullException("noteContent"));

            // Act
            var result = await _controller.UpsertUserNote(userNoteRequest);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            dynamic value = statusCodeResult.Value!;
            Assert.Equal("An internal server error occurred.", value.Message);
        }

        [Fact]
        public async Task UpsertUserNote_KhiNoteContentRong_VaServiceTraVeTrue_TraVeOk()
        {
            // Arrange
            var userNoteRequest = new UserNoteRequestDTO
            {
                UserId = 1,
                ArticleId = 100,
                NoteContent = ""
            };

            _mockUserNoteService
                .Setup(s => s.UpsertUserNote(userNoteRequest))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.UpsertUserNote(userNoteRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic value = okResult.Value!;
            Assert.Equal("User note upserted successfully.", value.Message);
        }

        [Fact]
        public async Task UpsertUserNote_KhiUserIdBangKhong_VaServiceTraVeFalse_TraVeBadRequest()
        {
            // Arrange
            var userNoteRequest = new UserNoteRequestDTO
            {
                UserId = 0,
                ArticleId = 100,
                NoteContent = "Test note"
            };

            _mockUserNoteService
                .Setup(s => s.UpsertUserNote(userNoteRequest))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UpsertUserNote(userNoteRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            dynamic value = badRequestResult.Value!;
            Assert.Equal("Could not save the user note. The item may not exist or data is invalid.", value.Message);
        }
    }
}
