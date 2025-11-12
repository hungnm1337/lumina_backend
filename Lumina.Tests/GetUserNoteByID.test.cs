// File: GetUserNoteByID.test.cs
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using lumina.Controllers;
using ServiceLayer.UserNote;
using DataLayer.DTOs.UserNote;
using System;
using System.Threading.Tasks;

#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.

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
        public async Task GetUserNoteByID_KhiUserNoteIdMotVaTimThay_TraVeOkVoiUserNote()
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
        public async Task GetUserNoteByID_KhiUserNoteIdKhong_TraVeNotFound()
        {
            // Arrange
            int userNoteId = 0;

            _mockUserNoteService
                .Setup(s => s.GetUserNoteByID(userNoteId))
                .ReturnsAsync(null as UserNoteResponseDTO);

            // Act
            var result = await _controller.GetUserNoteByID(userNoteId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var value = notFoundResult.Value;
            Assert.NotNull(value);
            var messageProperty = value.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("User note not found.", message);
            _mockUserNoteService.Verify(s => s.GetUserNoteByID(userNoteId), Times.Once);
        }

        [Fact]
        public async Task GetUserNoteByID_KhiUserNoteIdAmMot_TraVeNotFound()
        {
            // Arrange
            int userNoteId = -1;

            _mockUserNoteService
                .Setup(s => s.GetUserNoteByID(userNoteId))
                .ReturnsAsync(null as UserNoteResponseDTO);

            // Act
            var result = await _controller.GetUserNoteByID(userNoteId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var value = notFoundResult.Value;
            Assert.NotNull(value);
            var messageProperty = value.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("User note not found.", message);
            _mockUserNoteService.Verify(s => s.GetUserNoteByID(userNoteId), Times.Once);
        }

        [Fact]
        public async Task GetUserNoteByID_KhiUserNoteIdKhongTonTai_TraVeNotFound()
        {
            // Arrange
            int userNoteId = 999;

            _mockUserNoteService
                .Setup(s => s.GetUserNoteByID(userNoteId))
                .ReturnsAsync(null as UserNoteResponseDTO);

            // Act
            var result = await _controller.GetUserNoteByID(userNoteId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var value = notFoundResult.Value;
            Assert.NotNull(value);
            var messageProperty = value.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("User note not found.", message);
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
            var value = statusCodeResult.Value;
            Assert.NotNull(value);
            var messageProperty = value.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("An error occurred while retrieving the user note.", message);
        }
    }
}
