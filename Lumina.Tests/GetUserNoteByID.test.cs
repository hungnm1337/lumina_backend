// File: GetUserNoteByID.test.cs
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
    public class GetUserNoteByIDTests
    {
        private readonly Mock<IUserNoteService> _mockUserNoteService;
        private readonly UserNoteController _controller;

        public GetUserNoteByIDTests()
        {
            _mockUserNoteService = new Mock<IUserNoteService>();
            _controller = new UserNoteController(_mockUserNoteService.Object);
        }

        [Fact]
        public async Task GetUserNoteByID_KhiUserNoteTimThay_TraVeOkVoiUserNote()
        {
            // Arrange
            int userNoteId = 1;
            var expectedUserNote = new UserNoteResponseDTO
            {
                NoteId = userNoteId,
                UserId = 100,
                ArticleId = 50,
                NoteContent = "This is a test note"
            };

            _mockUserNoteService
                .Setup(s => s.GetUserNoteByID(userNoteId))
                .ReturnsAsync(expectedUserNote);

            // Act
            var result = await _controller.GetUserNoteByID(userNoteId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var userNote = Assert.IsType<UserNoteResponseDTO>(okResult.Value);
            Assert.Equal(userNoteId, userNote.NoteId);
            Assert.Equal("This is a test note", userNote.NoteContent);
            _mockUserNoteService.Verify(s => s.GetUserNoteByID(userNoteId), Times.Once);
        }

        [Fact]
        public async Task GetUserNoteByID_KhiUserNoteKhongTimThay_TraVeNotFound()
        {
            // Arrange
            int userNoteId = 999;

            _mockUserNoteService
                .Setup(s => s.GetUserNoteByID(userNoteId))
                .ReturnsAsync((UserNoteResponseDTO?)null);

            // Act
            var result = await _controller.GetUserNoteByID(userNoteId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            dynamic value = notFoundResult.Value!;
            Assert.Equal("User note not found.", value.Message);
            _mockUserNoteService.Verify(s => s.GetUserNoteByID(userNoteId), Times.Once);
        }

        [Fact]
        public async Task GetUserNoteByID_KhiServiceThrowException_TraVeInternalServerError()
        {
            // Arrange
            int userNoteId = 1;

            _mockUserNoteService
                .Setup(s => s.GetUserNoteByID(userNoteId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetUserNoteByID(userNoteId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            dynamic value = statusCodeResult.Value!;
            Assert.Equal("An error occurred while retrieving the user note.", value.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public async Task GetUserNoteByID_KhiUserNoteIdKhongHopLe_VaServiceTraVeNull_TraVeNotFound(int invalidId)
        {
            // Arrange
            _mockUserNoteService
                .Setup(s => s.GetUserNoteByID(invalidId))
                .ReturnsAsync((UserNoteResponseDTO?)null);

            // Act
            var result = await _controller.GetUserNoteByID(invalidId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            dynamic value = notFoundResult.Value!;
            Assert.Equal("User note not found.", value.Message);
        }

        [Fact]
        public async Task GetUserNoteByID_KhiUserNoteIdLonNhat_VaTimThay_TraVeOk()
        {
            // Arrange
            int userNoteId = int.MaxValue;
            var expectedUserNote = new UserNoteResponseDTO
            {
                NoteId = userNoteId,
                UserId = 1,
                ArticleId = 1,
                NoteContent = "Max ID note"
            };

            _mockUserNoteService
                .Setup(s => s.GetUserNoteByID(userNoteId))
                .ReturnsAsync(expectedUserNote);

            // Act
            var result = await _controller.GetUserNoteByID(userNoteId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var userNote = Assert.IsType<UserNoteResponseDTO>(okResult.Value);
            Assert.Equal(userNoteId, userNote.NoteId);
        }

        [Fact]
        public async Task GetUserNoteByID_KhiServiceThrowArgumentException_TraVeInternalServerError()
        {
            // Arrange
            int userNoteId = -1;

            _mockUserNoteService
                .Setup(s => s.GetUserNoteByID(userNoteId))
                .ThrowsAsync(new ArgumentException("Invalid user note ID"));

            // Act
            var result = await _controller.GetUserNoteByID(userNoteId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            dynamic value = statusCodeResult.Value!;
            Assert.Equal("An error occurred while retrieving the user note.", value.Message);
        }
    }
}
