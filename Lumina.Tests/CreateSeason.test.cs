using System;
using System.Threading.Tasks;
using DataLayer.DTOs.Leaderboard;
using lumina.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.Leaderboard;
using Xunit;

namespace Lumina.Tests
{
    public class CreateSeasonTests
    {
        private readonly Mock<ILeaderboardService> _mockLeaderboardService;
        private readonly LeaderboardController _controller;

        public CreateSeasonTests()
        {
            _mockLeaderboardService = new Mock<ILeaderboardService>();
            _controller = new LeaderboardController(_mockLeaderboardService.Object);
        }

        [Fact]
        public async Task Create_ValidCreateLeaderboardDTO_ReturnsCreatedAtActionWithLeaderboardId()
        {
            // Arrange
            var createDto = new CreateLeaderboardDTO
            {
                SeasonName = "Season 2025 Winter",
                SeasonNumber = 11,
                StartDate = DateTime.Now.AddDays(1),
                EndDate = DateTime.Now.AddDays(31),
                IsActive = false
            };
            int expectedLeaderboardId = 123;

            _mockLeaderboardService
                .Setup(s => s.CreateAsync(It.IsAny<CreateLeaderboardDTO>()))
                .ReturnsAsync(expectedLeaderboardId);

            // Act
            var result = await _controller.Create(createDto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(LeaderboardController.GetById), createdAtActionResult.ActionName);
            Assert.Equal(expectedLeaderboardId, createdAtActionResult.Value);
            Assert.Equal(expectedLeaderboardId, createdAtActionResult.RouteValues["leaderboardId"]);
            _mockLeaderboardService.Verify(s => s.CreateAsync(createDto), Times.Once);
        }

        [Fact]
        public async Task Create_ServiceReturnsZero_ReturnsCreatedAtActionWithZero()
        {
            // Arrange
            var createDto = new CreateLeaderboardDTO
            {
                SeasonName = "Test Season",
                SeasonNumber = 1,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(30),
                IsActive = true
            };
            int expectedLeaderboardId = 0;

            _mockLeaderboardService
                .Setup(s => s.CreateAsync(It.IsAny<CreateLeaderboardDTO>()))
                .ReturnsAsync(expectedLeaderboardId);

            // Act
            var result = await _controller.Create(createDto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(expectedLeaderboardId, createdAtActionResult.Value);
            _mockLeaderboardService.Verify(s => s.CreateAsync(createDto), Times.Once);
        }

        [Fact]
        public async Task Create_ServiceThrowsArgumentException_ReturnsBadRequestWithMessage()
        {
            // Arrange
            var createDto = new CreateLeaderboardDTO
            {
                SeasonName = "Invalid Season",
                SeasonNumber = -1,
                StartDate = DateTime.Now.AddDays(10),
                EndDate = DateTime.Now.AddDays(5), // EndDate before StartDate
                IsActive = true
            };
            string errorMessage = "EndDate must be after StartDate";

            _mockLeaderboardService
                .Setup(s => s.CreateAsync(It.IsAny<CreateLeaderboardDTO>()))
                .ThrowsAsync(new ArgumentException(errorMessage));

            // Act
            var result = await _controller.Create(createDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var responseValue = badRequestResult.Value;
            
            var messageProperty = responseValue?.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            var message = messageProperty?.GetValue(responseValue)?.ToString();
            Assert.Equal(errorMessage, message);
            
            _mockLeaderboardService.Verify(s => s.CreateAsync(createDto), Times.Once);
        }

        [Fact]
        public async Task Create_ServiceThrowsInvalidOperationException_PropagatesException()
        {
            // Arrange
            var createDto = new CreateLeaderboardDTO
            {
                SeasonName = "Test Season",
                SeasonNumber = 5,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(30),
                IsActive = false
            };

            _mockLeaderboardService
                .Setup(s => s.CreateAsync(It.IsAny<CreateLeaderboardDTO>()))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _controller.Create(createDto)
            );
            _mockLeaderboardService.Verify(s => s.CreateAsync(createDto), Times.Once);
        }

        [Fact]
        public async Task Create_ServiceReturnsNegativeId_ReturnsCreatedAtActionWithNegativeId()
        {
            // Arrange
            var createDto = new CreateLeaderboardDTO
            {
                SeasonName = "Boundary Test Season",
                SeasonNumber = 100,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(90),
                IsActive = false
            };
            int expectedLeaderboardId = -100;

            _mockLeaderboardService
                .Setup(s => s.CreateAsync(It.IsAny<CreateLeaderboardDTO>()))
                .ReturnsAsync(expectedLeaderboardId);

            // Act
            var result = await _controller.Create(createDto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(expectedLeaderboardId, createdAtActionResult.Value);
            _mockLeaderboardService.Verify(s => s.CreateAsync(createDto), Times.Once);
        }

        [Fact]
        public async Task Create_MinimalDTO_ReturnsCreatedAtActionWithLeaderboardId()
        {
            // Arrange
            var createDto = new CreateLeaderboardDTO
            {
                SeasonName = null,
                SeasonNumber = 0,
                StartDate = null,
                EndDate = null,
                IsActive = false
            };
            int expectedLeaderboardId = 999;

            _mockLeaderboardService
                .Setup(s => s.CreateAsync(It.IsAny<CreateLeaderboardDTO>()))
                .ReturnsAsync(expectedLeaderboardId);

            // Act
            var result = await _controller.Create(createDto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(expectedLeaderboardId, createdAtActionResult.Value);
            _mockLeaderboardService.Verify(s => s.CreateAsync(createDto), Times.Once);
        }
    }
}
