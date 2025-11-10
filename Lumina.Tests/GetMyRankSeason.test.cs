using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using lumina.Controllers;
using ServiceLayer.Leaderboard;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Lumina.Tests
{
    public class GetMyRankSeasonTests
    {
        private readonly Mock<ILeaderboardService> _mockService;
        private readonly LeaderboardController _controller;

        public GetMyRankSeasonTests()
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
        public async Task GetMyRank_ValidUserWithLeaderboardId_ReturnsOkWithRank()
        {
            // Arrange
            int userId = 1;
            int leaderboardId = 5;
            int expectedRank = 10;
            SetupUserClaim(userId.ToString());
            
            _mockService.Setup(s => s.GetUserRankAsync(userId, leaderboardId))
                .ReturnsAsync(expectedRank);

            // Act
            var result = await _controller.GetMyRank(leaderboardId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var value = okResult.Value;
            var rankProp = value.GetType().GetProperty("rank");
            Assert.NotNull(rankProp);
            Assert.Equal(expectedRank, rankProp.GetValue(value));
            _mockService.Verify(s => s.GetUserRankAsync(userId, leaderboardId), Times.Once);
        }

        [Fact]
        public async Task GetMyRank_ValidUserWithoutLeaderboardId_ReturnsOkWithRank()
        {
            // Arrange
            int userId = 2;
            int expectedRank = 1;
            SetupUserClaim(userId.ToString());
            
            _mockService.Setup(s => s.GetUserRankAsync(userId, null))
                .ReturnsAsync(expectedRank);

            // Act
            var result = await _controller.GetMyRank(null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var value = okResult.Value;
            var rankProp = value.GetType().GetProperty("rank");
            Assert.NotNull(rankProp);
            Assert.Equal(expectedRank, rankProp.GetValue(value));
            _mockService.Verify(s => s.GetUserRankAsync(userId, null), Times.Once);
        }

        [Fact]
        public async Task GetMyRank_UserNotRanked_ReturnsOkWithZero()
        {
            // Arrange
            int userId = 3;
            int leaderboardId = 10;
            int expectedRank = 0;
            SetupUserClaim(userId.ToString());
            
            _mockService.Setup(s => s.GetUserRankAsync(userId, leaderboardId))
                .ReturnsAsync(expectedRank);

            // Act
            var result = await _controller.GetMyRank(leaderboardId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var value = okResult.Value;
            var rankProp = value.GetType().GetProperty("rank");
            Assert.Equal(0, rankProp.GetValue(value));
            _mockService.Verify(s => s.GetUserRankAsync(userId, leaderboardId), Times.Once);
        }

        [Fact]
        public async Task GetMyRank_HighRank_ReturnsOkWithLargeNumber()
        {
            // Arrange
            int userId = 4;
            int leaderboardId = 1;
            int expectedRank = 9999;
            SetupUserClaim(userId.ToString());
            
            _mockService.Setup(s => s.GetUserRankAsync(userId, leaderboardId))
                .ReturnsAsync(expectedRank);

            // Act
            var result = await _controller.GetMyRank(leaderboardId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var value = okResult.Value;
            var rankProp = value.GetType().GetProperty("rank");
            Assert.Equal(9999, rankProp.GetValue(value));
            _mockService.Verify(s => s.GetUserRankAsync(userId, leaderboardId), Times.Once);
        }

        [Fact]
        public async Task GetMyRank_NoUserIdClaim_ReturnsUnauthorized()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            // Act
            var result = await _controller.GetMyRank(1);

            // Assert
            Assert.IsType<UnauthorizedResult>(result.Result);
            _mockService.Verify(s => s.GetUserRankAsync(It.IsAny<int>(), It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task GetMyRank_EmptyUserIdClaim_ReturnsUnauthorized()
        {
            // Arrange
            SetupUserClaim(string.Empty);

            // Act
            var result = await _controller.GetMyRank(1);

            // Assert
            Assert.IsType<UnauthorizedResult>(result.Result);
            _mockService.Verify(s => s.GetUserRankAsync(It.IsAny<int>(), It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task GetMyRank_InvalidUserIdClaim_ReturnsUnauthorized()
        {
            // Arrange
            SetupUserClaim("invalid_id");

            // Act
            var result = await _controller.GetMyRank(1);

            // Assert
            Assert.IsType<UnauthorizedResult>(result.Result);
            _mockService.Verify(s => s.GetUserRankAsync(It.IsAny<int>(), It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task GetMyRank_ZeroLeaderboardId_CallsServiceWithZero()
        {
            // Arrange
            int userId = 5;
            int leaderboardId = 0;
            int expectedRank = 50;
            SetupUserClaim(userId.ToString());
            
            _mockService.Setup(s => s.GetUserRankAsync(userId, leaderboardId))
                .ReturnsAsync(expectedRank);

            // Act
            var result = await _controller.GetMyRank(leaderboardId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            _mockService.Verify(s => s.GetUserRankAsync(userId, leaderboardId), Times.Once);
        }

        [Fact]
        public async Task GetMyRank_ServiceThrowsException_PropagatesException()
        {
            // Arrange
            int userId = 6;
            int leaderboardId = 1;
            SetupUserClaim(userId.ToString());
            
            _mockService.Setup(s => s.GetUserRankAsync(userId, leaderboardId))
                .ThrowsAsync(new System.Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<System.Exception>(() => 
                _controller.GetMyRank(leaderboardId));
            _mockService.Verify(s => s.GetUserRankAsync(userId, leaderboardId), Times.Once);
        }
    }
}
