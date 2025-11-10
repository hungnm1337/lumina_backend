using Xunit;
using DataLayer.Models;

namespace Lumina.Tests
{
    /// <summary>
    /// Unit tests for UserLeaderboard entity model
    /// Note: Entity/Model classes typically don't require unit tests as they contain no logic.
    /// These tests are for demonstration purposes only.
    /// </summary>
    public class UserLeaderboardModelTests
    {
        [Fact]
        public void UserLeaderboard_SetAndGetProperties_WorksCorrectly()
        {
            // Arrange & Act
            var userLeaderboard = new UserLeaderboard
            {
                UserLeaderboardId = 1,
                UserId = 100,
                LeaderboardId = 5,
                Score = 1500
            };

            // Assert
            Assert.Equal(1, userLeaderboard.UserLeaderboardId);
            Assert.Equal(100, userLeaderboard.UserId);
            Assert.Equal(5, userLeaderboard.LeaderboardId);
            Assert.Equal(1500, userLeaderboard.Score);
        }

        [Fact]
        public void UserLeaderboard_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var userLeaderboard = new UserLeaderboard();

            // Assert
            Assert.Equal(0, userLeaderboard.UserLeaderboardId);
            Assert.Equal(0, userLeaderboard.UserId);
            Assert.Equal(0, userLeaderboard.LeaderboardId);
            Assert.Equal(0, userLeaderboard.Score);
        }

        [Fact]
        public void UserLeaderboard_WithZeroScore_CanBeSet()
        {
            // Arrange & Act
            var userLeaderboard = new UserLeaderboard
            {
                Score = 0
            };

            // Assert
            Assert.Equal(0, userLeaderboard.Score);
        }

        [Fact]
        public void UserLeaderboard_WithNegativeScore_CanBeSet()
        {
            // Arrange & Act
            var userLeaderboard = new UserLeaderboard
            {
                Score = -100
            };

            // Assert
            Assert.Equal(-100, userLeaderboard.Score);
        }

        [Fact]
        public void UserLeaderboard_WithMaxScore_CanBeSet()
        {
            // Arrange & Act
            var userLeaderboard = new UserLeaderboard
            {
                Score = int.MaxValue
            };

            // Assert
            Assert.Equal(int.MaxValue, userLeaderboard.Score);
        }

        [Fact]
        public void UserLeaderboard_NavigationProperties_CanBeSet()
        {
            // Arrange
            var user = new User { UserId = 100, FullName = "Test User" };
            var leaderboard = new Leaderboard { LeaderboardId = 5, SeasonName = "Season 1" };

            // Act
            var userLeaderboard = new UserLeaderboard
            {
                UserId = 100,
                LeaderboardId = 5,
                User = user,
                Leaderboard = leaderboard
            };

            // Assert
            Assert.NotNull(userLeaderboard.User);
            Assert.NotNull(userLeaderboard.Leaderboard);
            Assert.Equal(100, userLeaderboard.User.UserId);
            Assert.Equal(5, userLeaderboard.Leaderboard.LeaderboardId);
        }
    }
}
