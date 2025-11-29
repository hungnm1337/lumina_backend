using System;
using System.Threading.Tasks;
using DataLayer.DTOs.Streak;
using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using RepositoryLayer.Streak;
using ServiceLayer.Streak;
using Xunit;

namespace Lumina.Tests.ServiceTests
{
    public class GetStreakSummaryAsyncUnitTest
    {
        private static (StreakService Service, LuminaSystemContext Context, Mock<ILogger<StreakService>> LoggerMock, Mock<IStreakRepository> RepoMock) CreateService()
        {
            var context = InMemoryDbContextHelper.CreateContext(Guid.NewGuid().ToString());
            var loggerMock = new Mock<ILogger<StreakService>>();
            var repoMock = new Mock<IStreakRepository>(MockBehavior.Strict);
            var service = new StreakService(context, loggerMock.Object, repoMock.Object);

            return (service, context, loggerMock, repoMock);
        }

        private static async Task DisposeContextAsync(LuminaSystemContext context)
        {
            await context.Database.EnsureDeletedAsync();
            await context.DisposeAsync();
        }

        [Theory]
        [InlineData(999, false)] // User not found
        [InlineData(1, true)] // User with null values
        public async Task GetStreakSummaryAsync_UserNotFoundOrNullValues_ReturnsDefaultValues(int userId, bool createUser)
        {
            // Arrange
            var (service, context, loggerMock, repoMock) = CreateService();
            try
            {
                if (createUser)
                {
                    var user = new User
                    {
                        UserId = 1,
                        Email = "test@example.com",
                        FullName = "Test User",
                        RoleId = 4,
                        CurrentStreak = null,
                        LongestStreak = null,
                        LastPracticeDate = null,
                        StreakFreezesAvailable = null
                    };
                    await context.Users.AddAsync(user);
                    await context.SaveChangesAsync();
                }

                // Act
                var result = await service.GetStreakSummaryAsync(userId);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(0, result.CurrentStreak);
                Assert.Equal(0, result.LongestStreak);
                Assert.False(result.TodayCompleted);
                Assert.Equal(0, result.FreezeTokens);
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Fact]
        public async Task GetStreakSummaryAsync_TodayCompleted_ReturnsTrue()
        {
            // Arrange
            var (service, context, loggerMock, repoMock) = CreateService();
            try
            {
                var today = service.GetTodayGMT7();
                var user = new User
                {
                    UserId = 1,
                    Email = "test@example.com",
                    FullName = "Test User",
                    RoleId = 4,
                    CurrentStreak = 5,
                    LongestStreak = 10,
                    LastPracticeDate = today.ToDateTime(TimeOnly.MinValue),
                    StreakFreezesAvailable = 2
                };
                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();

                // Act
                var result = await service.GetStreakSummaryAsync(1);

                // Assert
                Assert.NotNull(result);
                Assert.True(result.TodayCompleted);
                Assert.Equal(5, result.CurrentStreak);
                Assert.Equal(10, result.LongestStreak);
                Assert.Equal(2, result.FreezeTokens);
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Fact]
        public async Task GetStreakSummaryAsync_WithMilestones_ReturnsCorrectMilestones()
        {
            // Arrange
            var (service, context, loggerMock, repoMock) = CreateService();
            try
            {
                var yesterday = service.GetTodayGMT7().AddDays(-1);
                var user = new User
                {
                    UserId = 1,
                    Email = "test@example.com",
                    FullName = "Test User",
                    RoleId = 4,
                    CurrentStreak = 7, // Đã đạt milestone 7, next là 14
                    LongestStreak = 7,
                    LastPracticeDate = yesterday.ToDateTime(TimeOnly.MinValue),
                    StreakFreezesAvailable = 1
                };
                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();

                // Act
                var result = await service.GetStreakSummaryAsync(1);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(7, result.CurrentStreak);
                Assert.Equal(7, result.LastMilestone); // Đã đạt milestone 7
                Assert.Equal(14, result.NextMilestone); // Next milestone là 14
                Assert.Equal(7, result.DaysToNextMilestone); // 14 - 7 = 7
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Fact]
        public async Task GetStreakSummaryAsync_NoNextMilestone_ReturnsNull()
        {
            // Arrange
            var (service, context, loggerMock, repoMock) = CreateService();
            try
            {
                var yesterday = service.GetTodayGMT7().AddDays(-1);
                var user = new User
                {
                    UserId = 1,
                    Email = "test@example.com",
                    FullName = "Test User",
                    RoleId = 4,
                    CurrentStreak = 400, // Vượt quá milestone cuối (365)
                    LongestStreak = 400,
                    LastPracticeDate = yesterday.ToDateTime(TimeOnly.MinValue),
                    StreakFreezesAvailable = 10
                };
                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();

                // Act
                var result = await service.GetStreakSummaryAsync(1);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(365, result.LastMilestone); // Milestone cuối cùng
                Assert.Null(result.NextMilestone); // Không còn milestone nào
                Assert.Null(result.DaysToNextMilestone);
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Fact]
        public async Task GetStreakSummaryAsync_ExceptionHandling_ThrowsException()
        {
            // Arrange
            var context = InMemoryDbContextHelper.CreateContext(Guid.NewGuid().ToString());
            var loggerMock = new Mock<ILogger<StreakService>>();
            var repoMock = new Mock<IStreakRepository>(MockBehavior.Strict);
            
            var service = new StreakService(context, loggerMock.Object, repoMock.Object);
            
            // Manually break the context to simulate exception
            await context.Database.EnsureDeletedAsync();
            await context.DisposeAsync();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                await service.GetStreakSummaryAsync(1));
        }
    }
}

