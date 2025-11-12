// File: GetAllUserNotesByUserId.test.cs
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using lumina.Controllers;
using ServiceLayer.UserNote;
using DataLayer.DTOs.UserNote;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lumina.Tests
{
    public class GetAllUserNotesByUserIdTests
    {
        private readonly Mock<IUserNoteService> _mockUserNoteService;
        private readonly UserNoteController _controller;

        public GetAllUserNotesByUserIdTests()
        {
            _mockUserNoteService = new Mock<IUserNoteService>();
            _controller = new UserNoteController(_mockUserNoteService.Object);
        }

        [Fact]
        public async Task GetAllUserNotesByUserId_KhiCoNhieuUserNotes_TraVeOkVoiDanhSach()
        {
            // Arrange
            int userId = 1;
            var expectedUserNotes = new List<UserNoteResponseDTO>
            {
                new UserNoteResponseDTO
                {
                    NoteId = 1,
                    UserId = userId,
                    ArticleId = 100,
                    NoteContent = "Note 1"
                },
                new UserNoteResponseDTO
                {
                    NoteId = 2,
                    UserId = userId,
                    ArticleId = 101,
                    NoteContent = "Note 2"
                },
                new UserNoteResponseDTO
                {
                    NoteId = 3,
                    UserId = userId,
                    ArticleId = 102,
                    NoteContent = "Note 3"
                }
            };

            _mockUserNoteService
                .Setup(s => s.GetAllUserNotesByUserId(userId))
                .ReturnsAsync(expectedUserNotes);

            // Act
            var result = await _controller.GetAllUserNotesByUserId(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var userNotes = Assert.IsType<List<UserNoteResponseDTO>>(okResult.Value);
            Assert.Equal(3, userNotes.Count);
            Assert.All(userNotes, note => Assert.Equal(userId, note.UserId));
            _mockUserNoteService.Verify(s => s.GetAllUserNotesByUserId(userId), Times.Once);
        }

        [Fact]
        public async Task GetAllUserNotesByUserId_KhiDanhSachRong_TraVeOkVoiDanhSachRong()
        {
            // Arrange
            int userId = 999;
            var emptyList = new List<UserNoteResponseDTO>();

            _mockUserNoteService
                .Setup(s => s.GetAllUserNotesByUserId(userId))
                .ReturnsAsync(emptyList);

            // Act
            var result = await _controller.GetAllUserNotesByUserId(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var userNotes = Assert.IsType<List<UserNoteResponseDTO>>(okResult.Value);
            Assert.Empty(userNotes);
            _mockUserNoteService.Verify(s => s.GetAllUserNotesByUserId(userId), Times.Once);
        }        


        [Fact]
        public async Task GetAllUserNotesByUserId_KhiServiceThrowArgumentException_TraVeInternalServerError()
        {
            // Arrange
            int userId = -1;

            _mockUserNoteService
                .Setup(s => s.GetAllUserNotesByUserId(userId))
                .ThrowsAsync(new ArgumentException("Invalid user ID"));

            // Act
            var result = await _controller.GetAllUserNotesByUserId(userId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            dynamic value = statusCodeResult.Value!;
            Assert.Equal("An error occurred while retrieving user notes.", value.Message);
        }

        [Fact]
        public async Task GetAllUserNotesByUserId_KhiUserIdBangKhong_TraVeBadRequest()
        {
            // Arrange
            int userId = 0;

            // Act
            var result = await _controller.GetAllUserNotesByUserId(userId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            dynamic value = badRequestResult.Value!;
            Assert.Equal("Invalid userId.", value.Message);
            _mockUserNoteService.Verify(s => s.GetAllUserNotesByUserId(It.IsAny<int>()), Times.Never);
        }
    }
}
