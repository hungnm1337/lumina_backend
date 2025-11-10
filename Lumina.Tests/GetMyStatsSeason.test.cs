using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using lumina.Controllers;
using ServiceLayer.Leaderboard;
using DataLayer.DTOs.Leaderboard;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Lumina.Tests
{
    public class GetMyStatsSeasonTests
    {
        private readonly Mock<ILeaderboardService> _mockService;
        private readonly LeaderboardController _controller;

        public GetMyStatsSeasonTests()
        {
            _mockService = new Mock<ILeaderboardService>();
            _controller = new LeaderboardController(_mockService.Object);
        }

        private void SetupUserClaim(string userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Fact]
        public async Task GetMyStats_ValidUserWithLeaderboardId_ReturnsOkWithStats()
        {
            // Arrange
            int userId = 1;
            int leaderboardId = 5;
            SetupUserClaim(userId.ToString());
            
            var expectedStats = new UserSeasonStatsDTO
            {
                UserId = userId,
                CurrentRank = 10,
                CurrentScore = 850,
                EstimatedTOEICScore = 650,
                ToeicLevel = "B1",
                TotalAttempts = 20,
                CorrectAnswers = 150,
                AccuracyRate = 0.75m,
                IsReadyForTOEIC = true
            };
            
            _mockService.Setup(s => s.GetUserStatsAsync(userId, leaderboardId))
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _controller.GetMyStats(leaderboardId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var stats = Assert.IsType<UserSeasonStatsDTO>(okResult.Value);
            Assert.Equal(userId, stats.UserId);
            Assert.Equal(10, stats.CurrentRank);
            Assert.Equal(850, stats.CurrentScore);
            Assert.Equal(650, stats.EstimatedTOEICScore);
            _mockService.Verify(s => s.GetUserStatsAsync(userId, leaderboardId), Times.Once);
        }

        [Fact]
        public async Task GetMyStats_ValidUserWithoutLeaderboardId_ReturnsOkWithStats()
        {
            // Arrange
            int userId = 2;
            SetupUserClaim(userId.ToString());
            
            var expectedStats = new UserSeasonStatsDTO
            {
                UserId = userId,
                CurrentRank = 5,
                CurrentScore = 1200,
                EstimatedTOEICScore = 750,
                ToeicLevel = "B2",
                TotalAttempts = 50,
                CorrectAnswers = 400,
                AccuracyRate = 0.80m,
                IsReadyForTOEIC = true
            };
            
            _mockService.Setup(s => s.GetUserStatsAsync(userId, null))
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _controller.GetMyStats(null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var stats = Assert.IsType<UserSeasonStatsDTO>(okResult.Value);
            Assert.Equal(userId, stats.UserId);
            _mockService.Verify(s => s.GetUserStatsAsync(userId, null), Times.Once);
        }

        [Fact]
        public async Task GetMyStats_StatsNotFound_ReturnsNotFoundWithMessage()
        {
            // Arrange
            int userId = 3;
            int leaderboardId = 10;
            SetupUserClaim(userId.ToString());
            
            _mockService.Setup(s => s.GetUserStatsAsync(userId, leaderboardId))
                .ReturnsAsync((UserSeasonStatsDTO?)null);

            // Act
            var result = await _controller.GetMyStats(leaderboardId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var value = notFoundResult.Value;
            var messageProp = value.GetType().GetProperty("message");
            Assert.NotNull(messageProp);
            Assert.Equal("Không tìm thấy thông tin mùa giải", messageProp.GetValue(value));
            _mockService.Verify(s => s.GetUserStatsAsync(userId, leaderboardId), Times.Once);
        }

        [Fact]
        public async Task GetMyStats_NoUserIdClaim_ReturnsUnauthorizedWithMessage()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            // Act
            var result = await _controller.GetMyStats(1);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            var value = unauthorizedResult.Value;
            var messageProp = value.GetType().GetProperty("message");
            Assert.NotNull(messageProp);
            Assert.Equal("Không thể xác định user", messageProp.GetValue(value));
            _mockService.Verify(s => s.GetUserStatsAsync(It.IsAny<int>(), It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task GetMyStats_EmptyUserIdClaim_ReturnsUnauthorizedWithMessage()
        {
            // Arrange
            SetupUserClaim(string.Empty);

            // Act
            var result = await _controller.GetMyStats(1);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            var value = unauthorizedResult.Value;
            var messageProp = value.GetType().GetProperty("message");
            Assert.NotNull(messageProp);
            Assert.Equal("Không thể xác định user", messageProp.GetValue(value));
            _mockService.Verify(s => s.GetUserStatsAsync(It.IsAny<int>(), It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task GetMyStats_InvalidUserIdClaim_ReturnsUnauthorizedWithMessage()
        {
            // Arrange
            SetupUserClaim("invalid_user_id");

            // Act
            var result = await _controller.GetMyStats(1);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            var value = unauthorizedResult.Value;
            var messageProp = value.GetType().GetProperty("message");
            Assert.NotNull(messageProp);
            Assert.Equal("Không thể xác định user", messageProp.GetValue(value));
            _mockService.Verify(s => s.GetUserStatsAsync(It.IsAny<int>(), It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task GetMyStats_ZeroLeaderboardId_CallsServiceWithZero()
        {
            // Arrange
            int userId = 4;
            int leaderboardId = 0;
            SetupUserClaim(userId.ToString());
            
            var expectedStats = new UserSeasonStatsDTO
            {
                UserId = userId,
                CurrentRank = 1,
                CurrentScore = 100,
                EstimatedTOEICScore = 400,
                ToeicLevel = "A2",
                TotalAttempts = 5,
                CorrectAnswers = 30,
                AccuracyRate = 0.60m,
                IsReadyForTOEIC = false
            };
            
            _mockService.Setup(s => s.GetUserStatsAsync(userId, leaderboardId))
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _controller.GetMyStats(leaderboardId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            _mockService.Verify(s => s.GetUserStatsAsync(userId, leaderboardId), Times.Once);
        }

        [Fact]
        public async Task GetMyStats_ServiceThrowsException_PropagatesException()
        {
            // Arrange
            int userId = 5;
            int leaderboardId = 1;
            SetupUserClaim(userId.ToString());
            
            _mockService.Setup(s => s.GetUserStatsAsync(userId, leaderboardId))
                .ThrowsAsync(new System.Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<System.Exception>(() => _controller.GetMyStats(leaderboardId));
            _mockService.Verify(s => s.GetUserStatsAsync(userId, leaderboardId), Times.Once);
        }
    }
}
