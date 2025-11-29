using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using RepositoryLayer.Streak;
using ServiceLayer.Streak;
using Xunit;

namespace Lumina.Tests.ServiceTests
{
    public class GetUsersNeedingAutoProcessAsyncUnitTest
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

        [Fact]
        public async Task GetUsersNeedingAutoProcessAsync_NoUsers_ReturnsEmptyList()
        {
            // Arrange
            var (service, context, loggerMock, repoMock) = CreateService();
            try
            {
                var today = service.GetTodayGMT7();

                // Act
                var result = await service.GetUsersNeedingAutoProcessAsync(today);

                // Assert
                Assert.NotNull(result);
                Assert.Empty(result);
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Fact]
        public async Task GetUsersNeedingAutoProcessAsync_UsersWithActiveStreak_ReturnsUserIds()
        {
            // Arrange
            var (service, context, loggerMock, repoMock) = CreateService();
            try
            {
                var today = service.GetTodayGMT7();
                var threeDaysAgo = today.AddDays(-3);

                // User 1: Có streak > 0 và lastPracticeDate < yesterday
                var user1 = new User
                {
                    UserId = 1,
                    Email = "user1@example.com",
                    FullName = "User 1",
                    RoleId = 4,
                    CurrentStreak = 5,
                    LastPracticeDate = threeDaysAgo.ToDateTime(TimeOnly.MinValue)
                };

                // User 2: Có streak > 0 và lastPracticeDate < yesterday
                var user2 = new User
                {
                    UserId = 2,
                    Email = "user2@example.com",
                    FullName = "User 2",
                    RoleId = 4,
                    CurrentStreak = 10,
                    LastPracticeDate = threeDaysAgo.ToDateTime(TimeOnly.MinValue)
                };

                // User 3: Streak = 0 (không được trả về)
                var user3 = new User
                {
                    UserId = 3,
                    Email = "user3@example.com",
                    FullName = "User 3",
                    RoleId = 4,
                    CurrentStreak = 0,
                    LastPracticeDate = threeDaysAgo.ToDateTime(TimeOnly.MinValue)
                };

                // User 4: Học hôm qua (không được trả về vì lastPracticeDate = yesterday max time)
                var yesterdayMax = today.AddDays(-1).ToDateTime(TimeOnly.MaxValue);
                var user4 = new User
                {
                    UserId = 4,
                    Email = "user4@example.com",
                    FullName = "User 4",
                    RoleId = 4,
                    CurrentStreak = 5,
                    LastPracticeDate = yesterdayMax // Bằng với yesterdayDateTime, không < nên không được trả về
                };

                await context.Users.AddRangeAsync(user1, user2, user3, user4);
                await context.SaveChangesAsync();

                // Act
                var result = await service.GetUsersNeedingAutoProcessAsync(today);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(2, result.Count);
                Assert.Contains(1, result);
                Assert.Contains(2, result);
                Assert.DoesNotContain(3, result); // Streak = 0
                Assert.DoesNotContain(4, result); // Học hôm qua
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Theory]
        [InlineData(null, true)] // NULL streak
        [InlineData(5, false)] // NULL lastPracticeDate
        public async Task GetUsersNeedingAutoProcessAsync_UsersWithNullValues_ReturnsEmptyList(int? currentStreak, bool hasLastPracticeDate)
        {
            // Arrange
            var (service, context, loggerMock, repoMock) = CreateService();
            try
            {
                var today = service.GetTodayGMT7();
                var threeDaysAgo = today.AddDays(-3);

                var user = new User
                {
                    UserId = 1,
                    Email = "user@example.com",
                    FullName = "User",
                    RoleId = 4,
                    CurrentStreak = currentStreak,
                    LastPracticeDate = hasLastPracticeDate ? threeDaysAgo.ToDateTime(TimeOnly.MinValue) : null
                };

                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();

                // Act
                var result = await service.GetUsersNeedingAutoProcessAsync(today);

                // Assert
                Assert.NotNull(result);
                Assert.Empty(result);
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Fact]
        public async Task GetUsersNeedingAutoProcessAsync_ExceptionHandling_ReturnsEmptyList()
        {
            // Arrange
            var context = InMemoryDbContextHelper.CreateContext(Guid.NewGuid().ToString());
            var loggerMock = new Mock<ILogger<StreakService>>();
            var repoMock = new Mock<IStreakRepository>(MockBehavior.Strict);
            
            var service = new StreakService(context, loggerMock.Object, repoMock.Object);
            
            // Manually break the context to simulate exception
            await context.Database.EnsureDeletedAsync();
            await context.DisposeAsync();

            var today = DateOnly.FromDateTime(DateTime.Now);

            // Act
            var result = await service.GetUsersNeedingAutoProcessAsync(today);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result); // Exception được catch và trả về empty list
        }
    }
}

