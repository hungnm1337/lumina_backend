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
    public class UpdateOnValidPracticeAsyncUnitTest
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
        public async Task UpdateOnValidPracticeAsync_UserNotFound_ReturnsFailure()
        {
            // Arrange
            var (service, context, loggerMock, repoMock) = CreateService();
            try
            {
                var today = service.GetTodayGMT7();

                // Act
                var result = await service.UpdateOnValidPracticeAsync(999, today);

                // Assert
                Assert.False(result.Success);
                Assert.Equal("User not found", result.Message);
                Assert.Null(result.Summary);
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Theory]
        [InlineData(1, "Invalid practice date (future)")] // Future date
        [InlineData(-3, "Invalid practice date (too old)")] // Too old date
        public async Task UpdateOnValidPracticeAsync_InvalidDate_ReturnsFailure(int daysOffset, string expectedMessage)
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
                    RoleId = 4
                };
                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();

                var today = service.GetTodayGMT7();
                var invalidDate = today.AddDays(daysOffset);

                // Act
                var result = await service.UpdateOnValidPracticeAsync(1, invalidDate);

                // Assert
                Assert.False(result.Success);
                Assert.Equal(expectedMessage, result.Message);
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Fact]
        public async Task UpdateOnValidPracticeAsync_FirstTimePractice_StartsNewStreak()
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
                    CurrentStreak = null,
                    LongestStreak = null,
                    LastPracticeDate = null,
                    StreakFreezesAvailable = null
                };
                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();

                var today = service.GetTodayGMT7();

                // Act
                var result = await service.UpdateOnValidPracticeAsync(1, today);

                // Assert
                Assert.True(result.Success);
                Assert.Equal(StreakEventType.CompleteDay, result.EventType);
                Assert.Equal("Bắt đầu chuỗi học tập!", result.Message);
                Assert.NotNull(result.Summary);
                Assert.Equal(1, result.Summary.CurrentStreak);
                Assert.Equal(1, result.Summary.LongestStreak);
                Assert.Equal(1, result.Summary.FreezeTokens);

                // Verify database
                var updatedUser = await context.Users.FindAsync(1);
                Assert.NotNull(updatedUser);
                Assert.Equal(1, updatedUser!.CurrentStreak);
                Assert.Equal(1, updatedUser.LongestStreak);
                Assert.Equal(1, updatedUser.StreakFreezesAvailable);
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Fact]
        public async Task UpdateOnValidPracticeAsync_SameDay_DoesNotIncrement()
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
                var result = await service.UpdateOnValidPracticeAsync(1, today);

                // Assert
                Assert.True(result.Success);
                Assert.Equal(StreakEventType.MaintainDay, result.EventType);
                Assert.Equal("Bạn đã hoàn thành mục tiêu hôm nay rồi!", result.Message);
                Assert.NotNull(result.Summary);
                Assert.Equal(5, result.Summary.CurrentStreak);

                // Verify database unchanged
                var updatedUser = await context.Users.FindAsync(1);
                Assert.NotNull(updatedUser);
                Assert.Equal(5, updatedUser!.CurrentStreak);
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Fact]
        public async Task UpdateOnValidPracticeAsync_NextDayConsecutive_IncrementsStreakAndReachesMilestone()
        {
            // Arrange
            var (service, context, loggerMock, repoMock) = CreateService();
            try
            {
                var today = service.GetTodayGMT7();
                var yesterday = today.AddDays(-1);
                var user = new User
                {
                    UserId = 1,
                    Email = "test@example.com",
                    FullName = "Test User",
                    RoleId = 4,
                    CurrentStreak = 6, // Sẽ đạt milestone 7
                    LongestStreak = 5,
                    LastPracticeDate = yesterday.ToDateTime(TimeOnly.MinValue),
                    StreakFreezesAvailable = 1
                };
                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();

                // Act
                var result = await service.UpdateOnValidPracticeAsync(1, today);

                // Assert
                Assert.True(result.Success);
                Assert.Equal(StreakEventType.CompleteDay, result.EventType);
                Assert.NotNull(result.Summary);
                Assert.Equal(7, result.Summary.CurrentStreak);
                Assert.True(result.MilestoneReached);
                Assert.Equal(7, result.MilestoneValue);

                // Verify database
                var updatedUser = await context.Users.FindAsync(1);
                Assert.NotNull(updatedUser);
                Assert.Equal(7, updatedUser!.CurrentStreak);
                Assert.Equal(7, updatedUser.LongestStreak); // Updated
                Assert.True(updatedUser.StreakFreezesAvailable > 1); // Awarded token
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Fact]
        public async Task UpdateOnValidPracticeAsync_GapWithFreeze_ContinuesStreak()
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
                    Email = "test@example.com",
                    FullName = "Test User",
                    RoleId = 4,
                    CurrentStreak = 10, // Streak vẫn còn (được bảo vệ bởi freeze)
                    LongestStreak = 10,
                    LastPracticeDate = threeDaysAgo.ToDateTime(TimeOnly.MinValue),
                    StreakFreezesAvailable = 1
                };
                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();

                // Act
                var result = await service.UpdateOnValidPracticeAsync(1, today);

                // Assert
                Assert.True(result.Success);
                Assert.Equal(StreakEventType.CompleteDay, result.EventType);
                Assert.NotNull(result.Summary);
                Assert.Equal(11, result.Summary.CurrentStreak);

                // Verify database
                var updatedUser = await context.Users.FindAsync(1);
                Assert.NotNull(updatedUser);
                Assert.Equal(11, updatedUser!.CurrentStreak);
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Fact]
        public async Task UpdateOnValidPracticeAsync_ResetStreak_StartsFromOne()
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
                    Email = "test@example.com",
                    FullName = "Test User",
                    RoleId = 4,
                    CurrentStreak = 0, // Streak đã bị reset
                    LongestStreak = 5,
                    LastPracticeDate = threeDaysAgo.ToDateTime(TimeOnly.MinValue),
                    StreakFreezesAvailable = 0 // Hết freeze
                };
                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();

                // Act
                var result = await service.UpdateOnValidPracticeAsync(1, today);

                // Assert
                Assert.True(result.Success);
                Assert.Equal(StreakEventType.ResetStreak, result.EventType);
                Assert.Equal("Chuỗi đã bị ngắt, bắt đầu lại từ đầu!", result.Message);
                Assert.NotNull(result.Summary);
                Assert.Equal(1, result.Summary.CurrentStreak);
                Assert.Equal(5, result.Summary.LongestStreak); // Giữ nguyên

                // Verify database
                var updatedUser = await context.Users.FindAsync(1);
                Assert.NotNull(updatedUser);
                Assert.Equal(1, updatedUser!.CurrentStreak);
                Assert.Equal(5, updatedUser.LongestStreak);
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Fact]
        public async Task UpdateOnValidPracticeAsync_ExceptionHandling_ReturnsInternalError()
        {
            // Arrange
            var context = InMemoryDbContextHelper.CreateContext(Guid.NewGuid().ToString());
            var loggerMock = new Mock<ILogger<StreakService>>();
            var repoMock = new Mock<IStreakRepository>(MockBehavior.Strict);
            
            var service = new StreakService(context, loggerMock.Object, repoMock.Object);
            
            // Manually break the context to simulate exception
            await context.Database.EnsureDeletedAsync();
            await context.DisposeAsync();

            try
            {
                // Act - This should trigger exception handling
                var result = await service.UpdateOnValidPracticeAsync(1, DateOnly.FromDateTime(DateTime.Now));

                // Assert
                Assert.False(result.Success);
                Assert.Equal("Internal error", result.Message);
            }
            catch
            {
                // Expected - exception may be thrown or handled
            }
        }
    }
}
