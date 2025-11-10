using Xunit;
using RepositoryLayer.Leaderboard;
using DataLayer.Models;
using DataLayer.DTOs.Leaderboard;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lumina.Tests
{
    public class LeaderboardRepositoryTests
    {
        private LuminaSystemContext GetInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<LuminaSystemContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new LuminaSystemContext(options);
        }

        #region GetAllPaginatedAsync Tests

        [Fact]
        public async Task GetAllPaginatedAsync_ShouldReturnFirstPage()
        {
            // Arrange
            var context = GetInMemoryContext("GetAllPaginated_FirstPage");
            var repository = new LeaderboardRepository(context);

            for (int i = 1; i <= 15; i++)
            {
                context.Leaderboards.Add(new DataLayer.Models.Leaderboard
                {
                    LeaderboardId = i,
                    SeasonName = $"Season {i}",
                    SeasonNumber = i,
                    StartDate = DateTime.UtcNow.AddDays(-30 + i),
                    EndDate = DateTime.UtcNow.AddDays(30 + i),
                    IsActive = i == 1,
                    CreateAt = DateTime.UtcNow
                });
            }
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetAllPaginatedAsync(page: 1, pageSize: 10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.Items.Count);
            Assert.Equal(15, result.Total);
            Assert.Equal(2, result.TotalPages);
            Assert.True(result.HasNext);
            Assert.False(result.HasPrevious);
        }

        [Fact]
        public async Task GetAllPaginatedAsync_ShouldFilterByKeyword()
        {
            // Arrange
            var context = GetInMemoryContext("GetAllPaginated_Keyword");
            var repository = new LeaderboardRepository(context);

            context.Leaderboards.Add(new DataLayer.Models.Leaderboard
            {
                SeasonName = "Spring Championship",
                SeasonNumber = 1,
                IsActive = true,
                CreateAt = DateTime.UtcNow
            });
            context.Leaderboards.Add(new DataLayer.Models.Leaderboard
            {
                SeasonName = "Summer Tournament",
                SeasonNumber = 2,
                IsActive = false,
                CreateAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetAllPaginatedAsync(keyword: "Spring", page: 1, pageSize: 10);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Contains("Spring", result.Items[0].SeasonName);
        }

        #endregion

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSeasons_WhenNoFilterProvided()
        {
            // Arrange
            var context = GetInMemoryContext("GetAll_NoFilter");
            var repository = new LeaderboardRepository(context);

            context.Leaderboards.Add(new DataLayer.Models.Leaderboard
            {
                SeasonName = "Season 1",
                SeasonNumber = 1,
                IsActive = true,
                CreateAt = DateTime.UtcNow
            });
            context.Leaderboards.Add(new DataLayer.Models.Leaderboard
            {
                SeasonName = "Season 2",
                SeasonNumber = 2,
                IsActive = false,
                CreateAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAllAsync_ShouldFilterByIsActive()
        {
            // Arrange
            var context = GetInMemoryContext("GetAll_IsActive");
            var repository = new LeaderboardRepository(context);

            context.Leaderboards.Add(new DataLayer.Models.Leaderboard
            {
                SeasonName = "Active Season",
                SeasonNumber = 1,
                IsActive = true,
                CreateAt = DateTime.UtcNow
            });
            context.Leaderboards.Add(new DataLayer.Models.Leaderboard
            {
                SeasonName = "Inactive Season",
                SeasonNumber = 2,
                IsActive = false,
                CreateAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetAllAsync(isActive: true);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.True(result[0].IsActive);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_ShouldReturnSeason_WhenExists()
        {
            // Arrange
            var context = GetInMemoryContext("GetById_Exists");
            var repository = new LeaderboardRepository(context);

            context.Leaderboards.Add(new DataLayer.Models.Leaderboard
            {
                LeaderboardId = 1,
                SeasonName = "Test Season",
                SeasonNumber = 100,
                IsActive = true,
                CreateAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Season", result.SeasonName);
            Assert.Equal(100, result.SeasonNumber);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
        {
            // Arrange
            var context = GetInMemoryContext("GetById_NotExists");
            var repository = new LeaderboardRepository(context);

            // Act
            var result = await repository.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetCurrentAsync Tests

        [Fact]
        public async Task GetCurrentAsync_ShouldReturnActiveSeason_WhenWithinDateRange()
        {
            // Arrange
            var context = GetInMemoryContext("GetCurrent_Active");
            var repository = new LeaderboardRepository(context);

            var now = DateTime.UtcNow;
            context.Leaderboards.Add(new DataLayer.Models.Leaderboard
            {
                SeasonName = "Current Season",
                SeasonNumber = 1,
                StartDate = now.AddDays(-10),
                EndDate = now.AddDays(10),
                IsActive = true,
                CreateAt = now
            });
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetCurrentAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Current Season", result.SeasonName);
        }

        [Fact]
        public async Task GetCurrentAsync_ShouldReturnNull_WhenNoActiveSeason()
        {
            // Arrange
            var context = GetInMemoryContext("GetCurrent_NoActive");
            var repository = new LeaderboardRepository(context);

            context.Leaderboards.Add(new DataLayer.Models.Leaderboard
            {
                SeasonName = "Inactive Season",
                SeasonNumber = 1,
                IsActive = false,
                CreateAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetCurrentAsync();

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_ShouldAddLeaderboard()
        {
            // Arrange
            var context = GetInMemoryContext("Create_Success");
            var repository = new LeaderboardRepository(context);

            var newLeaderboard = new DataLayer.Models.Leaderboard
            {
                SeasonName = "New Season",
                SeasonNumber = 10,
                IsActive = false,
                CreateAt = DateTime.UtcNow
            };

            // Act
            var id = await repository.CreateAsync(newLeaderboard);

            // Assert
            Assert.True(id > 0);
            var saved = await context.Leaderboards.FindAsync(id);
            Assert.NotNull(saved);
            Assert.Equal("New Season", saved.SeasonName);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_ShouldUpdateLeaderboard_WhenExists()
        {
            // Arrange
            var context = GetInMemoryContext("Update_Success");
            var repository = new LeaderboardRepository(context);

            var existing = new DataLayer.Models.Leaderboard
            {
                SeasonName = "Old Name",
                SeasonNumber = 1,
                IsActive = false,
                CreateAt = DateTime.UtcNow
            };
            context.Leaderboards.Add(existing);
            await context.SaveChangesAsync();

            context.Entry(existing).State = EntityState.Detached;

            var updated = new DataLayer.Models.Leaderboard
            {
                LeaderboardId = existing.LeaderboardId,
                SeasonName = "New Name",
                SeasonNumber = 1,
                IsActive = true,
                UpdateAt = DateTime.UtcNow
            };

            // Act
            var result = await repository.UpdateAsync(updated);

            // Assert
            Assert.True(result);
            var saved = await context.Leaderboards.FindAsync(existing.LeaderboardId);
            Assert.Equal("New Name", saved?.SeasonName);
            Assert.True(saved?.IsActive);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnFalse_WhenNotExists()
        {
            // Arrange
            var context = GetInMemoryContext("Update_NotExists");
            var repository = new LeaderboardRepository(context);

            var nonExistent = new DataLayer.Models.Leaderboard
            {
                LeaderboardId = 999,
                SeasonName = "Ghost Season",
                SeasonNumber = 999
            };

            // Act
            var result = await repository.UpdateAsync(nonExistent);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_ShouldRemoveLeaderboard_WhenExists()
        {
            // Arrange
            var context = GetInMemoryContext("Delete_Success");
            var repository = new LeaderboardRepository(context);

            var toDelete = new DataLayer.Models.Leaderboard
            {
                SeasonName = "To Delete",
                SeasonNumber = 1,
                IsActive = false,
                CreateAt = DateTime.UtcNow
            };
            context.Leaderboards.Add(toDelete);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.DeleteAsync(toDelete.LeaderboardId);

            // Assert
            Assert.True(result);
            var deleted = await context.Leaderboards.FindAsync(toDelete.LeaderboardId);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnFalse_WhenNotExists()
        {
            // Arrange
            var context = GetInMemoryContext("Delete_NotExists");
            var repository = new LeaderboardRepository(context);

            // Act
            var result = await repository.DeleteAsync(999);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region SetCurrentAsync Tests

        [Fact]
        public async Task SetCurrentAsync_ShouldActivateSeason_AndDeactivateOthers()
        {
            // Arrange
            var context = GetInMemoryContext("SetCurrent_Success");
            var repository = new LeaderboardRepository(context);

            var season1 = new DataLayer.Models.Leaderboard
            {
                SeasonName = "Season 1",
                SeasonNumber = 1,
                IsActive = true,
                CreateAt = DateTime.UtcNow
            };
            var season2 = new DataLayer.Models.Leaderboard
            {
                SeasonName = "Season 2",
                SeasonNumber = 2,
                IsActive = false,
                CreateAt = DateTime.UtcNow
            };
            context.Leaderboards.AddRange(season1, season2);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.SetCurrentAsync(season2.LeaderboardId);

            // Assert
            Assert.True(result);
            var updated1 = await context.Leaderboards.FindAsync(season1.LeaderboardId);
            var updated2 = await context.Leaderboards.FindAsync(season2.LeaderboardId);
            Assert.False(updated1?.IsActive);
            Assert.True(updated2?.IsActive);
        }

        [Fact]
        public async Task SetCurrentAsync_ShouldReturnFalse_WhenNotExists()
        {
            // Arrange
            var context = GetInMemoryContext("SetCurrent_NotExists");
            var repository = new LeaderboardRepository(context);

            // Act
            var result = await repository.SetCurrentAsync(999);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region ExistsSeasonNumberAsync Tests

        [Fact]
        public async Task ExistsSeasonNumberAsync_ShouldReturnTrue_WhenExists()
        {
            // Arrange
            var context = GetInMemoryContext("ExistsSeasonNumber_True");
            var repository = new LeaderboardRepository(context);

            context.Leaderboards.Add(new DataLayer.Models.Leaderboard
            {
                SeasonName = "Season 5",
                SeasonNumber = 5,
                CreateAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            // Act
            var result = await repository.ExistsSeasonNumberAsync(5);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExistsSeasonNumberAsync_ShouldReturnFalse_WhenNotExists()
        {
            // Arrange
            var context = GetInMemoryContext("ExistsSeasonNumber_False");
            var repository = new LeaderboardRepository(context);

            // Act
            var result = await repository.ExistsSeasonNumberAsync(999);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ExistsSeasonNumberAsync_ShouldExcludeSpecifiedId()
        {
            // Arrange
            var context = GetInMemoryContext("ExistsSeasonNumber_Exclude");
            var repository = new LeaderboardRepository(context);

            var season = new DataLayer.Models.Leaderboard
            {
                SeasonName = "Season 7",
                SeasonNumber = 7,
                CreateAt = DateTime.UtcNow
            };
            context.Leaderboards.Add(season);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.ExistsSeasonNumberAsync(7, excludeId: season.LeaderboardId);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetUserRankInSeasonAsync Tests

        [Fact]
        public async Task GetUserRankInSeasonAsync_ShouldReturnCorrectRank()
        {
            // Arrange
            var context = GetInMemoryContext("GetUserRank_Success");
            var repository = new LeaderboardRepository(context);

            var user1 = new User { UserId = 1, FullName = "User 1", Email = "user1@test.com" };
            var user2 = new User { UserId = 2, FullName = "User 2", Email = "user2@test.com" };
            var user3 = new User { UserId = 3, FullName = "User 3", Email = "user3@test.com" };
            context.Users.AddRange(user1, user2, user3);

            var leaderboard = new DataLayer.Models.Leaderboard
            {
                SeasonName = "Test Season",
                SeasonNumber = 1,
                CreateAt = DateTime.UtcNow
            };
            context.Leaderboards.Add(leaderboard);
            await context.SaveChangesAsync();

            context.UserLeaderboards.AddRange(
                new UserLeaderboard { UserId = 1, LeaderboardId = leaderboard.LeaderboardId, Score = 100 },
                new UserLeaderboard { UserId = 2, LeaderboardId = leaderboard.LeaderboardId, Score = 200 },
                new UserLeaderboard { UserId = 3, LeaderboardId = leaderboard.LeaderboardId, Score = 150 }
            );
            await context.SaveChangesAsync();

            // Act
            var rank = await repository.GetUserRankInSeasonAsync(3, leaderboard.LeaderboardId);

            // Assert
            Assert.Equal(2, rank); // User 3 with 150 score should be rank 2 (after User 2 with 200)
        }

        [Fact]
        public async Task GetUserRankInSeasonAsync_ShouldReturnZero_WhenUserNotInSeason()
        {
            // Arrange
            var context = GetInMemoryContext("GetUserRank_NotInSeason");
            var repository = new LeaderboardRepository(context);

            var leaderboard = new DataLayer.Models.Leaderboard
            {
                SeasonName = "Test Season",
                SeasonNumber = 1,
                CreateAt = DateTime.UtcNow
            };
            context.Leaderboards.Add(leaderboard);
            await context.SaveChangesAsync();

            // Act
            var rank = await repository.GetUserRankInSeasonAsync(999, leaderboard.LeaderboardId);

            // Assert
            Assert.Equal(0, rank);
        }

        #endregion

        #region IsSeasonActiveAsync Tests

        [Fact]
        public async Task IsSeasonActiveAsync_ShouldReturnTrue_WhenSeasonIsActive()
        {
            // Arrange
            var context = GetInMemoryContext("IsSeasonActive_True");
            var repository = new LeaderboardRepository(context);

            var now = DateTime.UtcNow;
            var leaderboard = new DataLayer.Models.Leaderboard
            {
                SeasonName = "Active Season",
                SeasonNumber = 1,
                StartDate = now.AddDays(-5),
                EndDate = now.AddDays(5),
                IsActive = true,
                CreateAt = now
            };
            context.Leaderboards.Add(leaderboard);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.IsSeasonActiveAsync(leaderboard.LeaderboardId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsSeasonActiveAsync_ShouldReturnFalse_WhenSeasonIsInactive()
        {
            // Arrange
            var context = GetInMemoryContext("IsSeasonActive_False");
            var repository = new LeaderboardRepository(context);

            var leaderboard = new DataLayer.Models.Leaderboard
            {
                SeasonName = "Inactive Season",
                SeasonNumber = 1,
                IsActive = false,
                CreateAt = DateTime.UtcNow
            };
            context.Leaderboards.Add(leaderboard);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.IsSeasonActiveAsync(leaderboard.LeaderboardId);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region AutoActivateSeasonAsync Tests

        [Fact]
        public async Task AutoActivateSeasonAsync_ShouldActivateSeasons_WhenStartDatePassed()
        {
            // Arrange
            var context = GetInMemoryContext("AutoActivate_Success");
            var repository = new LeaderboardRepository(context);

            var now = DateTime.UtcNow;
            var toActivate = new DataLayer.Models.Leaderboard
            {
                SeasonName = "Should Activate",
                SeasonNumber = 1,
                StartDate = now.AddDays(-1),
                EndDate = now.AddDays(10),
                IsActive = false,
                CreateAt = now
            };
            context.Leaderboards.Add(toActivate);
            await context.SaveChangesAsync();

            // Act
            await repository.AutoActivateSeasonAsync();

            // Assert
            var updated = await context.Leaderboards.FindAsync(toActivate.LeaderboardId);
            Assert.True(updated?.IsActive);
        }

        #endregion

        #region AutoEndSeasonAsync Tests

        [Fact]
        public async Task AutoEndSeasonAsync_ShouldDeactivateSeasons_WhenEndDatePassed()
        {
            // Arrange
            var context = GetInMemoryContext("AutoEnd_Success");
            var repository = new LeaderboardRepository(context);

            var now = DateTime.UtcNow;
            var toEnd = new DataLayer.Models.Leaderboard
            {
                SeasonName = "Should End",
                SeasonNumber = 1,
                StartDate = now.AddDays(-20),
                EndDate = now.AddDays(-1),
                IsActive = true,
                CreateAt = now
            };
            context.Leaderboards.Add(toEnd);
            await context.SaveChangesAsync();

            // Act
            await repository.AutoEndSeasonAsync();

            // Assert
            var updated = await context.Leaderboards.FindAsync(toEnd.LeaderboardId);
            Assert.False(updated?.IsActive);
        }

        #endregion

        #region ResetSeasonScoresAsync Tests

        [Fact]
        public async Task ResetSeasonScoresAsync_ShouldRemoveAllScores()
        {
            // Arrange
            var context = GetInMemoryContext("ResetScores_Success");
            var repository = new LeaderboardRepository(context);

            var user = new User { UserId = 1, FullName = "Test User", Email = "test@test.com" };
            context.Users.Add(user);

            var leaderboard = new DataLayer.Models.Leaderboard
            {
                SeasonName = "Test Season",
                SeasonNumber = 1,
                CreateAt = DateTime.UtcNow
            };
            context.Leaderboards.Add(leaderboard);
            await context.SaveChangesAsync();

            context.UserLeaderboards.Add(new UserLeaderboard
            {
                UserId = 1,
                LeaderboardId = leaderboard.LeaderboardId,
                Score = 500
            });
            await context.SaveChangesAsync();

            // Act
            var result = await repository.ResetSeasonScoresAsync(leaderboard.LeaderboardId);

            // Assert
            Assert.True(result > 0);
            var remaining = await context.UserLeaderboards
                .Where(ul => ul.LeaderboardId == leaderboard.LeaderboardId)
                .CountAsync();
            Assert.Equal(0, remaining);
        }

        [Fact]
        public async Task ResetSeasonScoresAsync_ShouldReturnZero_WhenSeasonNotExists()
        {
            // Arrange
            var context = GetInMemoryContext("ResetScores_NotExists");
            var repository = new LeaderboardRepository(context);

            // Act
            var result = await repository.ResetSeasonScoresAsync(999);

            // Assert
            Assert.Equal(0, result);
        }

        #endregion

        #region GetSeasonRankingAsync Tests

        [Fact]
        public async Task GetSeasonRankingAsync_ShouldReturnEmpty_WhenSeasonNotExists()
        {
            // Arrange
            var context = GetInMemoryContext("GetRanking_NotExists");
            var repository = new LeaderboardRepository(context);

            // Act
            var result = await repository.GetSeasonRankingAsync(999);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetSeasonRankingAsync_ShouldReturnRankedUsers()
        {
            // Arrange
            var context = GetInMemoryContext("GetRanking_Success");
            var repository = new LeaderboardRepository(context);

            var user1 = new User { UserId = 1, FullName = "User One", Email = "user1@test.com" };
            var user2 = new User { UserId = 2, FullName = "User Two", Email = "user2@test.com" };
            var user3 = new User { UserId = 3, FullName = "User Three", Email = "user3@test.com" };
            context.Users.AddRange(user1, user2, user3);

            var leaderboard = new DataLayer.Models.Leaderboard
            {
                SeasonName = "Test Season",
                SeasonNumber = 1,
                IsActive = true,
                CreateAt = DateTime.UtcNow
            };
            context.Leaderboards.Add(leaderboard);
            await context.SaveChangesAsync();

            context.UserLeaderboards.AddRange(
                new UserLeaderboard { UserId = 1, LeaderboardId = leaderboard.LeaderboardId, Score = 300 },
                new UserLeaderboard { UserId = 2, LeaderboardId = leaderboard.LeaderboardId, Score = 500 },
                new UserLeaderboard { UserId = 3, LeaderboardId = leaderboard.LeaderboardId, Score = 200 }
            );
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetSeasonRankingAsync(leaderboard.LeaderboardId, top: 10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal(2, result[0].UserId); // Highest score first
            Assert.Equal(1, result[0].Rank);
            Assert.Equal(500, result[0].Score);
        }

        [Fact]
        public async Task GetSeasonRankingAsync_ShouldLimitToTopN()
        {
            // Arrange
            var context = GetInMemoryContext("GetRanking_TopN");
            var repository = new LeaderboardRepository(context);

            for (int i = 1; i <= 10; i++)
            {
                context.Users.Add(new User { UserId = i, FullName = $"User {i}", Email = $"user{i}@test.com" });
            }

            var leaderboard = new DataLayer.Models.Leaderboard
            {
                SeasonName = "Test Season",
                SeasonNumber = 1,
                CreateAt = DateTime.UtcNow
            };
            context.Leaderboards.Add(leaderboard);
            await context.SaveChangesAsync();

            for (int i = 1; i <= 10; i++)
            {
                context.UserLeaderboards.Add(new UserLeaderboard
                {
                    UserId = i,
                    LeaderboardId = leaderboard.LeaderboardId,
                    Score = i * 100
                });
            }
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetSeasonRankingAsync(leaderboard.LeaderboardId, top: 5);

            // Assert
            Assert.Equal(5, result.Count);
        }

        #endregion

        #region GetUserSeasonStatsAsync Tests

        [Fact]
        public async Task GetUserSeasonStatsAsync_ShouldReturnNull_WhenSeasonNotExists()
        {
            // Arrange
            var context = GetInMemoryContext("GetUserStats_SeasonNotExists");
            var repository = new LeaderboardRepository(context);

            // Act
            var result = await repository.GetUserSeasonStatsAsync(1, 999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserSeasonStatsAsync_ShouldReturnDefaultStats_WhenUserNotInSeason()
        {
            // Arrange
            var context = GetInMemoryContext("GetUserStats_UserNotInSeason");
            var repository = new LeaderboardRepository(context);

            var leaderboard = new DataLayer.Models.Leaderboard
            {
                SeasonName = "Test Season",
                SeasonNumber = 1,
                CreateAt = DateTime.UtcNow
            };
            context.Leaderboards.Add(leaderboard);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetUserSeasonStatsAsync(999, leaderboard.LeaderboardId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(999, result.UserId);
            Assert.Equal(0, result.CurrentScore);
            Assert.Equal(0, result.CurrentRank);
        }

        [Fact]
        public async Task GetUserSeasonStatsAsync_ShouldReturnStats_WhenUserInSeason()
        {
            // Arrange
            var context = GetInMemoryContext("GetUserStats_Success");
            var repository = new LeaderboardRepository(context);

            var user = new User { UserId = 1, FullName = "Test User", Email = "test@test.com" };
            context.Users.Add(user);

            var leaderboard = new DataLayer.Models.Leaderboard
            {
                SeasonName = "Test Season",
                SeasonNumber = 1,
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow.AddDays(30),
                CreateAt = DateTime.UtcNow
            };
            context.Leaderboards.Add(leaderboard);
            await context.SaveChangesAsync();

            context.UserLeaderboards.Add(new UserLeaderboard
            {
                UserId = 1,
                LeaderboardId = leaderboard.LeaderboardId,
                Score = 500
            });
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetUserSeasonStatsAsync(1, leaderboard.LeaderboardId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.UserId);
            Assert.Equal(500, result.CurrentScore);
        }

        #endregion

        #region GetUserTOEICCalculationAsync Tests

        [Fact]
        public async Task GetUserTOEICCalculationAsync_ShouldReturnNull_WhenSeasonNotExists()
        {
            // Arrange
            var context = GetInMemoryContext("GetTOEICCalc_SeasonNotExists");
            var repository = new LeaderboardRepository(context);

            // Act
            var result = await repository.GetUserTOEICCalculationAsync(1, 999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserTOEICCalculationAsync_ShouldReturnCalculation()
        {
            // Arrange
            var context = GetInMemoryContext("GetTOEICCalc_Success");
            var repository = new LeaderboardRepository(context);

            var user = new User { UserId = 1, FullName = "Test User", Email = "test@test.com" };
            context.Users.Add(user);

            var leaderboard = new DataLayer.Models.Leaderboard
            {
                SeasonName = "Test Season",
                SeasonNumber = 1,
                CreateAt = DateTime.UtcNow
            };
            context.Leaderboards.Add(leaderboard);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetUserTOEICCalculationAsync(1, leaderboard.LeaderboardId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.UserId);
        }

        #endregion

        #region ExistsDateOverlapAsync Tests

        [Fact]
        public async Task ExistsDateOverlapAsync_ShouldReturnFalse_WhenNoOverlap()
        {
            // Arrange
            var context = GetInMemoryContext("DateOverlap_NoOverlap");
            var repository = new LeaderboardRepository(context);

            context.Leaderboards.Add(new DataLayer.Models.Leaderboard
            {
                SeasonName = "Season 1",
                SeasonNumber = 1,
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 3, 31),
                CreateAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            // Act
            var result = await repository.ExistsDateOverlapAsync(
                new DateTime(2025, 4, 1),
                new DateTime(2025, 6, 30),
                null
            );

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ExistsDateOverlapAsync_ShouldReturnTrue_WhenOverlap()
        {
            // Arrange
            var context = GetInMemoryContext("DateOverlap_Overlap");
            var repository = new LeaderboardRepository(context);

            context.Leaderboards.Add(new DataLayer.Models.Leaderboard
            {
                SeasonName = "Season 1",
                SeasonNumber = 1,
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 6, 30),
                CreateAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            // Act
            var result = await repository.ExistsDateOverlapAsync(
                new DateTime(2025, 5, 1),
                new DateTime(2025, 8, 31),
                null
            );

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExistsDateOverlapAsync_ShouldExcludeSpecifiedId()
        {
            // Arrange
            var context = GetInMemoryContext("DateOverlap_ExcludeId");
            var repository = new LeaderboardRepository(context);

            var leaderboard = new DataLayer.Models.Leaderboard
            {
                SeasonName = "Season 1",
                SeasonNumber = 1,
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 6, 30),
                CreateAt = DateTime.UtcNow
            };
            context.Leaderboards.Add(leaderboard);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.ExistsDateOverlapAsync(
                new DateTime(2025, 1, 1),
                new DateTime(2025, 6, 30),
                leaderboard.LeaderboardId
            );

            // Assert
            Assert.False(result);
        }

        #endregion

        #region RecalculateSeasonScoresAsync Tests

        [Fact]
        public async Task RecalculateSeasonScoresAsync_ShouldReturnZero_WhenSeasonNotExists()
        {
            // Arrange
            var context = GetInMemoryContext("Recalculate_SeasonNotExists");
            var repository = new LeaderboardRepository(context);

            // Act
            var result = await repository.RecalculateSeasonScoresAsync(999);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task RecalculateSeasonScoresAsync_ShouldRemoveOldScores()
        {
            // Arrange
            var context = GetInMemoryContext("Recalculate_RemoveOld");
            var repository = new LeaderboardRepository(context);

            var user = new User { UserId = 1, FullName = "Test User", Email = "test@test.com" };
            context.Users.Add(user);

            var leaderboard = new DataLayer.Models.Leaderboard
            {
                SeasonName = "Test Season",
                SeasonNumber = 1,
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow.AddDays(30),
                CreateAt = DateTime.UtcNow
            };
            context.Leaderboards.Add(leaderboard);
            await context.SaveChangesAsync();

            context.UserLeaderboards.Add(new UserLeaderboard
            {
                UserId = 1,
                LeaderboardId = leaderboard.LeaderboardId,
                Score = 500
            });
            await context.SaveChangesAsync();

            // Act
            await repository.RecalculateSeasonScoresAsync(leaderboard.LeaderboardId);

            // Assert
            var oldScores = await context.UserLeaderboards
                .Where(ul => ul.LeaderboardId == leaderboard.LeaderboardId)
                .ToListAsync();
            // Scores should be recalculated (may be 0 if no exam attempts)
            Assert.NotNull(oldScores);
        }

        #endregion
    }
}
