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
    public class GetMyTOEICCalculationTests
    {
        private readonly Mock<ILeaderboardService> _mockService;
        private readonly LeaderboardController _controller;

        public GetMyTOEICCalculationTests()
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
        public async Task GetMyTOEICCalculation_ValidUserWithLeaderboardId_ReturnsOkWithCalculation()
        {
            // Arrange
            int userId = 1;
            int leaderboardId = 5;
            SetupUserClaim(userId.ToString());
            
            var expectedCalculation = new TOEICScoreCalculationDTO
            {
                UserId = userId,
                EstimatedTOEICScore = 750,
                ToeicLevel = "B2",
                BasePointsPerCorrect = 10,
                TimeBonus = 1.5m,
                AccuracyBonus = 0.8m,
                DifficultyMultiplier = 1.2m,
                TotalSeasonScore = 1200
            };
            
            _mockService.Setup(s => s.GetUserTOEICCalculationAsync(userId, leaderboardId))
                .ReturnsAsync(expectedCalculation);

            // Act
            var result = await _controller.GetMyTOEICCalculation(leaderboardId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var calculation = Assert.IsType<TOEICScoreCalculationDTO>(okResult.Value);
            Assert.Equal(userId, calculation.UserId);
            Assert.Equal(750, calculation.EstimatedTOEICScore);
            Assert.Equal("B2", calculation.ToeicLevel);
            Assert.Equal(10, calculation.BasePointsPerCorrect);
            Assert.Equal(1.5m, calculation.TimeBonus);
            Assert.Equal(1200, calculation.TotalSeasonScore);
            _mockService.Verify(s => s.GetUserTOEICCalculationAsync(userId, leaderboardId), Times.Once);
        }

        [Fact]
        public async Task GetMyTOEICCalculation_ValidUserWithoutLeaderboardId_ReturnsOkWithCalculation()
        {
            // Arrange
            int userId = 2;
            SetupUserClaim(userId.ToString());
            
            var expectedCalculation = new TOEICScoreCalculationDTO
            {
                UserId = userId,
                EstimatedTOEICScore = 600,
                ToeicLevel = "B1",
                BasePointsPerCorrect = 8,
                TimeBonus = 1.0m,
                AccuracyBonus = 0.7m,
                DifficultyMultiplier = 1.0m,
                TotalSeasonScore = 800
            };
            
            _mockService.Setup(s => s.GetUserTOEICCalculationAsync(userId, null))
                .ReturnsAsync(expectedCalculation);

            // Act
            var result = await _controller.GetMyTOEICCalculation(null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var calculation = Assert.IsType<TOEICScoreCalculationDTO>(okResult.Value);
            Assert.Equal(userId, calculation.UserId);
            Assert.Equal(600, calculation.EstimatedTOEICScore);
            _mockService.Verify(s => s.GetUserTOEICCalculationAsync(userId, null), Times.Once);
        }

        [Fact]
        public async Task GetMyTOEICCalculation_CalculationNotFound_ReturnsNotFound()
        {
            // Arrange
            int userId = 3;
            int leaderboardId = 10;
            SetupUserClaim(userId.ToString());
            
            _mockService.Setup(s => s.GetUserTOEICCalculationAsync(userId, leaderboardId))
                .ReturnsAsync((TOEICScoreCalculationDTO?)null);

            // Act
            var result = await _controller.GetMyTOEICCalculation(leaderboardId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
            _mockService.Verify(s => s.GetUserTOEICCalculationAsync(userId, leaderboardId), Times.Once);
        }

        [Fact]
        public async Task GetMyTOEICCalculation_NoUserIdClaim_ReturnsUnauthorized()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            // Act
            var result = await _controller.GetMyTOEICCalculation(1);

            // Assert
            Assert.IsType<UnauthorizedResult>(result.Result);
            _mockService.Verify(s => s.GetUserTOEICCalculationAsync(It.IsAny<int>(), It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task GetMyTOEICCalculation_EmptyUserIdClaim_ReturnsUnauthorized()
        {
            // Arrange
            SetupUserClaim(string.Empty);

            // Act
            var result = await _controller.GetMyTOEICCalculation(1);

            // Assert
            Assert.IsType<UnauthorizedResult>(result.Result);
            _mockService.Verify(s => s.GetUserTOEICCalculationAsync(It.IsAny<int>(), It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task GetMyTOEICCalculation_InvalidUserIdClaim_ReturnsUnauthorized()
        {
            // Arrange
            SetupUserClaim("not_a_number");

            // Act
            var result = await _controller.GetMyTOEICCalculation(1);

            // Assert
            Assert.IsType<UnauthorizedResult>(result.Result);
            _mockService.Verify(s => s.GetUserTOEICCalculationAsync(It.IsAny<int>(), It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task GetMyTOEICCalculation_ZeroLeaderboardId_CallsServiceWithZero()
        {
            // Arrange
            int userId = 4;
            int leaderboardId = 0;
            SetupUserClaim(userId.ToString());
            
            var expectedCalculation = new TOEICScoreCalculationDTO
            {
                UserId = userId,
                EstimatedTOEICScore = 0,
                ToeicLevel = "A1",
                BasePointsPerCorrect = 5,
                TimeBonus = 0m,
                AccuracyBonus = 0m,
                DifficultyMultiplier = 1.0m,
                TotalSeasonScore = 0
            };
            
            _mockService.Setup(s => s.GetUserTOEICCalculationAsync(userId, leaderboardId))
                .ReturnsAsync(expectedCalculation);

            // Act
            var result = await _controller.GetMyTOEICCalculation(leaderboardId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            _mockService.Verify(s => s.GetUserTOEICCalculationAsync(userId, leaderboardId), Times.Once);
        }

        [Fact]
        public async Task GetMyTOEICCalculation_ServiceThrowsException_PropagatesException()
        {
            // Arrange
            int userId = 5;
            int leaderboardId = 1;
            SetupUserClaim(userId.ToString());
            
            _mockService.Setup(s => s.GetUserTOEICCalculationAsync(userId, leaderboardId))
                .ThrowsAsync(new System.Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<System.Exception>(() => 
                _controller.GetMyTOEICCalculation(leaderboardId));
            _mockService.Verify(s => s.GetUserTOEICCalculationAsync(userId, leaderboardId), Times.Once);
        }
    }
}
