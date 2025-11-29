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
    public class ApplyAutoFreezeOrResetAsyncUnitTest
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
        public async Task ApplyAutoFreezeOrResetAsync_UserNotFound_ReturnsFailure()
        {
            // Arrange
            var (service, context, loggerMock, repoMock) = CreateService();
            try
            {
                var today = service.GetTodayGMT7();

                // Act
                var result = await service.ApplyAutoFreezeOrResetAsync(999, today);

                // Assert
                Assert.False(result.Success);
                Assert.Equal("User not found", result.Message);
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Theory]
        [InlineData(0, null, "No active streak to process")] // No active streak
        [InlineData(5, null, "No practice date")] // No practice date
        public async Task ApplyAutoFreezeOrResetAsync_EarlyReturn_ReturnsSuccess(int currentStreak, DateTime? lastPracticeDate, string expectedMessage)
        {
            // Arrange
            var (service, context, loggerMock, repoMock) = CreateService();
            try
            {
                var user = new User
                {
                    UserId = 1,
                    Email = "test@example.com",
                    FullName = "Test User",
                    RoleId = 4,
                    CurrentStreak = currentStreak,
                    LongestStreak = 5,
                    LastPracticeDate = lastPracticeDate,
                    StreakFreezesAvailable = 0
                };
                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();

                var today = service.GetTodayGMT7();

                // Act
                var result = await service.ApplyAutoFreezeOrResetAsync(1, today);

                // Assert
                Assert.True(result.Success);
                Assert.Equal(expectedMessage, result.Message);
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Fact]
        public async Task ApplyAutoFreezeOrResetAsync_StreakIsSafe_ReturnsSuccess()
        {
            // Arrange
            var (service, context, loggerMock, repoMock) = CreateService();
            try
            {
                var today = service.GetTodayGMT7();
                var yesterday = today.AddDays(-1); // Học hôm qua, hôm nay chưa lỡ ngày
                var user = new User
                {
                    UserId = 1,
                    Email = "test@example.com",
                    FullName = "Test User",
                    RoleId = 4,
                    CurrentStreak = 5,
                    LongestStreak = 5,
                    LastPracticeDate = yesterday.ToDateTime(TimeOnly.MinValue),
                    StreakFreezesAvailable = 1
                };
                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();

                // Act
                var result = await service.ApplyAutoFreezeOrResetAsync(1, today);

                // Assert
                Assert.True(result.Success);
                Assert.Equal("Streak is safe", result.Message);
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Fact]
        public async Task ApplyAutoFreezeOrResetAsync_WithFreezeToken_UsesFreezeToken()
        {
            // Arrange
            var (service, context, loggerMock, repoMock) = CreateService();
            try
            {
                var today = service.GetTodayGMT7();
                var threeDaysAgo = today.AddDays(-3); // Lỡ 2 ngày
                var user = new User
                {
                    UserId = 1,
                    Email = "test@example.com",
                    FullName = "Test User",
                    RoleId = 4,
                    CurrentStreak = 10,
                    LongestStreak = 10,
                    LastPracticeDate = threeDaysAgo.ToDateTime(TimeOnly.MinValue),
                    StreakFreezesAvailable = 2 // Có freeze token
                };
                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();

                // Act
                var result = await service.ApplyAutoFreezeOrResetAsync(1, today);

                // Assert
                Assert.True(result.Success);
                Assert.Equal(StreakEventType.FreezeUsed, result.EventType);
                Assert.Contains("Freeze token đã được sử dụng", result.Message);

                // Verify database
                var updatedUser = await context.Users.FindAsync(1);
                Assert.NotNull(updatedUser);
                Assert.Equal(1, updatedUser!.StreakFreezesAvailable); // Giảm từ 2 xuống 1
                Assert.Equal(10, updatedUser.CurrentStreak); // Streak được bảo vệ
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Fact]
        public async Task ApplyAutoFreezeOrResetAsync_NoFreezeToken_ResetsStreak()
        {
            // Arrange
            var (service, context, loggerMock, repoMock) = CreateService();
            try
            {
                var today = service.GetTodayGMT7();
                var threeDaysAgo = today.AddDays(-3); // Lỡ 2 ngày
                var user = new User
                {
                    UserId = 1,
                    Email = "test@example.com",
                    FullName = "Test User",
                    RoleId = 4,
                    CurrentStreak = 10,
                    LongestStreak = 10,
                    LastPracticeDate = threeDaysAgo.ToDateTime(TimeOnly.MinValue),
                    StreakFreezesAvailable = 0 // Không có freeze token
                };
                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();

                // Act
                var result = await service.ApplyAutoFreezeOrResetAsync(1, today);

                // Assert
                Assert.True(result.Success);
                Assert.Equal(StreakEventType.StreakLost, result.EventType);
                Assert.Contains("Chuỗi học tập đã bị mất", result.Message);

                // Verify database
                var updatedUser = await context.Users.FindAsync(1);
                Assert.NotNull(updatedUser);
                Assert.Equal(0, updatedUser!.CurrentStreak); // Reset về 0
                Assert.Equal(10, updatedUser.LongestStreak); // Giữ nguyên longest streak
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Fact]
        public async Task ApplyAutoFreezeOrResetAsync_ExceptionHandling_ReturnsInternalError()
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
            var result = await service.ApplyAutoFreezeOrResetAsync(1, today);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Internal error", result.Message);
        }
    }
}

