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
        public async Task GetAllUserNotesByUserId_KhiCoMotUserNote_TraVeOkVoiDanhSachMotPhanTu()
        {
            // Arrange
            int userId = 1;
            var singleNoteList = new List<UserNoteResponseDTO>
            {
                new UserNoteResponseDTO
                {
                    NoteId = 1,
                    UserId = userId,
                    ArticleId = 100,
                    NoteContent = "Single note"
                }
            };

            _mockUserNoteService
                .Setup(s => s.GetAllUserNotesByUserId(userId))
                .ReturnsAsync(singleNoteList);

            // Act
            var result = await _controller.GetAllUserNotesByUserId(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var userNotes = Assert.IsType<List<UserNoteResponseDTO>>(okResult.Value);
            Assert.Single(userNotes);
            Assert.Equal("Single note", userNotes.First().NoteContent);
        }

        [Fact]
        public async Task GetAllUserNotesByUserId_KhiServiceThrowException_TraVeInternalServerError()
        {
            // Arrange
            int userId = 1;

            _mockUserNoteService
                .Setup(s => s.GetAllUserNotesByUserId(userId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetAllUserNotesByUserId(userId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            dynamic value = statusCodeResult.Value!;
            Assert.Equal("An error occurred while retrieving user notes.", value.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public async Task GetAllUserNotesByUserId_KhiUserIdKhongHopLe_VaServiceTraVeDanhSachRong_TraVeOk(int invalidUserId)
        {
            // Arrange
            var emptyList = new List<UserNoteResponseDTO>();

            _mockUserNoteService
                .Setup(s => s.GetAllUserNotesByUserId(invalidUserId))
                .ReturnsAsync(emptyList);

            // Act
            var result = await _controller.GetAllUserNotesByUserId(invalidUserId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var userNotes = Assert.IsType<List<UserNoteResponseDTO>>(okResult.Value);
            Assert.Empty(userNotes);
        }

        [Fact]
        public async Task GetAllUserNotesByUserId_KhiUserIdLonNhat_VaCoNotes_TraVeOk()
        {
            // Arrange
            int userId = int.MaxValue;
            var expectedUserNotes = new List<UserNoteResponseDTO>
            {
                new UserNoteResponseDTO
                {
                    NoteId = 1,
                    UserId = userId,
                    ArticleId = 100,
                    NoteContent = "Max ID user note"
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
            Assert.Single(userNotes);
            Assert.Equal(userId, userNotes.First().UserId);
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
    }
}
