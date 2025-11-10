using Xunit;
using DataLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumina.Tests
{
    /// <summary>
    /// Unit tests for Leaderboard entity model
    /// Note: Entity/Model classes typically don't require unit tests as they contain no logic.
    /// These tests are for demonstration purposes only.
    /// </summary>
    public class LeaderboardModelTests
    {
        [Fact]
        public void Leaderboard_SetAndGetAllProperties_WorksCorrectly()
        {
            // Arrange
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 12, 31);
            var createDate = DateTime.Now;
            var updateDate = DateTime.Now.AddDays(1);

            // Act
            var leaderboard = new Leaderboard
            {
                LeaderboardId = 1,
                StartDate = startDate,
                EndDate = endDate,
                SeasonName = "Season 2024 - Spring",
                SeasonNumber = 1,
                IsActive = true,
                CreateAt = createDate,
                UpdateAt = updateDate
            };

            // Assert
            Assert.Equal(1, leaderboard.LeaderboardId);
            Assert.Equal(startDate, leaderboard.StartDate);
            Assert.Equal(endDate, leaderboard.EndDate);
            Assert.Equal("Season 2024 - Spring", leaderboard.SeasonName);
            Assert.Equal(1, leaderboard.SeasonNumber);
            Assert.True(leaderboard.IsActive);
            Assert.Equal(createDate, leaderboard.CreateAt);
            Assert.Equal(updateDate, leaderboard.UpdateAt);
        }

        [Fact]
        public void Leaderboard_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var leaderboard = new Leaderboard();

            // Assert
            Assert.Equal(0, leaderboard.LeaderboardId);
            Assert.Null(leaderboard.StartDate);
            Assert.Null(leaderboard.EndDate);
            Assert.Null(leaderboard.SeasonName);
            Assert.Equal(0, leaderboard.SeasonNumber);
            Assert.False(leaderboard.IsActive); // bool default is false
            Assert.Null(leaderboard.CreateAt);
            Assert.Null(leaderboard.UpdateAt);
            Assert.NotNull(leaderboard.UserLeaderboards);
            Assert.Empty(leaderboard.UserLeaderboards);
        }

        [Fact]
        public void Leaderboard_WithNullStartDate_CanBeSet()
        {
            // Arrange & Act
            var leaderboard = new Leaderboard
            {
                SeasonName = "Future Season",
                StartDate = null
            };

            // Assert
            Assert.Null(leaderboard.StartDate);
        }

        [Fact]
        public void Leaderboard_WithNullEndDate_CanBeSet()
        {
            // Arrange & Act
            var leaderboard = new Leaderboard
            {
                SeasonName = "Ongoing Season",
                EndDate = null
            };

            // Assert
            Assert.Null(leaderboard.EndDate);
        }

        [Fact]
        public void Leaderboard_WithNullSeasonName_CanBeSet()
        {
            // Arrange & Act
            var leaderboard = new Leaderboard
            {
                SeasonNumber = 1,
                SeasonName = null
            };

            // Assert
            Assert.Null(leaderboard.SeasonName);
        }

        [Fact]
        public void Leaderboard_WithEmptySeasonName_CanBeSet()
        {
            // Arrange & Act
            var leaderboard = new Leaderboard
            {
                SeasonName = string.Empty
            };

            // Assert
            Assert.Equal(string.Empty, leaderboard.SeasonName);
        }

        [Fact]
        public void Leaderboard_IsActiveTrue_CanBeSet()
        {
            // Arrange & Act
            var leaderboard = new Leaderboard
            {
                SeasonName = "Active Season",
                IsActive = true
            };

            // Assert
            Assert.True(leaderboard.IsActive);
        }

        [Fact]
        public void Leaderboard_IsActiveFalse_CanBeSet()
        {
            // Arrange & Act
            var leaderboard = new Leaderboard
            {
                SeasonName = "Inactive Season",
                IsActive = false
            };

            // Assert
            Assert.False(leaderboard.IsActive);
        }

        [Fact]
        public void Leaderboard_WithZeroSeasonNumber_CanBeSet()
        {
            // Arrange & Act
            var leaderboard = new Leaderboard
            {
                SeasonName = "Season Zero",
                SeasonNumber = 0
            };

            // Assert
            Assert.Equal(0, leaderboard.SeasonNumber);
        }

        [Fact]
        public void Leaderboard_WithNegativeSeasonNumber_CanBeSet()
        {
            // Arrange & Act
            var leaderboard = new Leaderboard
            {
                SeasonName = "Negative Season",
                SeasonNumber = -1
            };

            // Assert
            Assert.Equal(-1, leaderboard.SeasonNumber);
        }

        [Fact]
        public void Leaderboard_WithLargeSeasonNumber_CanBeSet()
        {
            // Arrange & Act
            var leaderboard = new Leaderboard
            {
                SeasonName = "Season 100",
                SeasonNumber = 100
            };

            // Assert
            Assert.Equal(100, leaderboard.SeasonNumber);
        }

        [Fact]
        public void Leaderboard_DateRange_StartBeforeEnd_IsValid()
        {
            // Arrange & Act
            var leaderboard = new Leaderboard
            {
                SeasonName = "Valid Date Range",
                StartDate = new DateTime(2024, 1, 1),
                EndDate = new DateTime(2024, 12, 31)
            };

            // Assert
            Assert.True(leaderboard.StartDate < leaderboard.EndDate);
        }

        [Fact]
        public void Leaderboard_WithSameDayStartAndEnd_CanBeSet()
        {
            // Arrange
            var sameDay = new DateTime(2024, 6, 15);

            // Act
            var leaderboard = new Leaderboard
            {
                SeasonName = "One Day Season",
                StartDate = sameDay,
                EndDate = sameDay
            };

            // Assert
            Assert.Equal(leaderboard.StartDate, leaderboard.EndDate);
        }

        [Fact]
        public void Leaderboard_UserLeaderboardsCollection_CanBePopulated()
        {
            // Arrange
            var leaderboard = new Leaderboard
            {
                LeaderboardId = 1,
                SeasonName = "Season 1"
            };

            var userLeaderboard1 = new UserLeaderboard
            {
                UserLeaderboardId = 1,
                LeaderboardId = 1,
                UserId = 100,
                Score = 1000
            };

            var userLeaderboard2 = new UserLeaderboard
            {
                UserLeaderboardId = 2,
                LeaderboardId = 1,
                UserId = 200,
                Score = 2000
            };

            // Act
            leaderboard.UserLeaderboards.Add(userLeaderboard1);
            leaderboard.UserLeaderboards.Add(userLeaderboard2);

            // Assert
            Assert.Equal(2, leaderboard.UserLeaderboards.Count);
            Assert.Contains(userLeaderboard1, leaderboard.UserLeaderboards);
            Assert.Contains(userLeaderboard2, leaderboard.UserLeaderboards);
            Assert.Equal(1000, leaderboard.UserLeaderboards.First().Score);
            Assert.Equal(2000, leaderboard.UserLeaderboards.Last().Score);
        }

        [Fact]
        public void Leaderboard_WithLongSeasonName_CanBeSet()
        {
            // Arrange
            var longName = new string('A', 500);

            // Act
            var leaderboard = new Leaderboard
            {
                SeasonName = longName
            };

            // Assert
            Assert.Equal(500, leaderboard.SeasonName.Length);
            Assert.Equal(longName, leaderboard.SeasonName);
        }

        [Fact]
        public void Leaderboard_WithNullCreateAt_CanBeSet()
        {
            // Arrange & Act
            var leaderboard = new Leaderboard
            {
                SeasonName = "New Season",
                CreateAt = null
            };

            // Assert
            Assert.Null(leaderboard.CreateAt);
        }

        [Fact]
        public void Leaderboard_WithNullUpdateAt_CanBeSet()
        {
            // Arrange & Act
            var leaderboard = new Leaderboard
            {
                SeasonName = "New Season",
                UpdateAt = null
            };

            // Assert
            Assert.Null(leaderboard.UpdateAt);
        }
    }
}
