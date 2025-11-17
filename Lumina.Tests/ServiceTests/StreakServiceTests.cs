using Xunit;
using Moq;
using ServiceLayer.Streak;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DataLayer.DTOs.Streak;

namespace Lumina.Tests.ServiceTests
{
    public class StreakServiceTests
    {
        private readonly Mock<ILogger<StreakService>> _mockLogger;
        private readonly LuminaSystemContext _context;
        private readonly StreakService _service;

        public StreakServiceTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<LuminaSystemContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new LuminaSystemContext(options);
            _mockLogger = new Mock<ILogger<StreakService>>();
            _service = new StreakService(_context, _mockLogger.Object);
        }

        [Fact]
        public async Task UpdateStreak_FirstTime_ShouldSet1()
        {
            // Arrange: User mới, tất cả cột streak = null
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                CurrentStreak = null,  // ⚠️ NULL
                LongestStreak = null,  // ⚠️ NULL
                LastPracticeDate = null,  // ⚠️ NULL
                StreakFreezesAvailable = null  // ⚠️ NULL
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var today = _service.GetTodayGMT7();

            // Act
            var result = await _service.UpdateOnValidPracticeAsync(1, today);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(StreakEventType.CompleteDay, result.EventType);
            Assert.Equal(1, result.Summary?.CurrentStreak);
            Assert.Equal(1, result.Summary?.LongestStreak);
        }

        [Fact]
        public async Task UpdateStreak_SecondDayConsecutive_ShouldIncrement()
        {
            // Arrange: User có streak = 5
            var yesterday = _service.GetTodayGMT7().AddDays(-1);
            var user = new User
            {
                UserId = 2,
                Email = "test2@example.com",
                CurrentStreak = 5,
                LongestStreak = 5,
                LastPracticeDate = yesterday.ToDateTime(TimeOnly.MinValue),
                StreakFreezesAvailable = 1
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var today = _service.GetTodayGMT7();

            // Act
            var result = await _service.UpdateOnValidPracticeAsync(2, today);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(StreakEventType.CompleteDay, result.EventType);
            Assert.Equal(6, result.Summary?.CurrentStreak);
            Assert.Equal(6, result.Summary?.LongestStreak);
        }

        [Fact]
        public async Task UpdateStreak_SameDay_ShouldNotIncrement()
        {
            // Arrange
            var today = _service.GetTodayGMT7();
            var user = new User
            {
                UserId = 3,
                Email = "test3@example.com",
                CurrentStreak = 7,
                LongestStreak = 10,
                LastPracticeDate = today.ToDateTime(TimeOnly.MinValue),
                StreakFreezesAvailable = 2
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act: Làm bài lần 2 trong cùng ngày
            var result = await _service.UpdateOnValidPracticeAsync(3, today);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(StreakEventType.MaintainDay, result.EventType);
            Assert.Equal(7, result.Summary?.CurrentStreak); // Không tăng
        }

        [Fact]
        public async Task UpdateStreak_AfterGap_ShouldResetTo1()
        {
            // Arrange: Bỏ lỡ 2 ngày
            var threeDaysAgo = _service.GetTodayGMT7().AddDays(-3);
            var user = new User
            {
                UserId = 4,
                Email = "test4@example.com",
                CurrentStreak = 15,
                LongestStreak = 20,
                LastPracticeDate = threeDaysAgo.ToDateTime(TimeOnly.MinValue),
                StreakFreezesAvailable = 0  // Hết freeze
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var today = _service.GetTodayGMT7();

            // Act
            var result = await _service.UpdateOnValidPracticeAsync(4, today);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(StreakEventType.ResetStreak, result.EventType);
            Assert.Equal(1, result.Summary?.CurrentStreak); // Reset về 1
            Assert.Equal(20, result.Summary?.LongestStreak); // Giữ nguyên
        }

        [Fact]
        public async Task Milestone_Day7_ShouldTrigger()
        {
            // Arrange: User có streak = 6
            var yesterday = _service.GetTodayGMT7().AddDays(-1);
            var user = new User
            {
                UserId = 5,
                Email = "test5@example.com",
                CurrentStreak = 6,
                LongestStreak = 6,
                LastPracticeDate = yesterday.ToDateTime(TimeOnly.MinValue),
                StreakFreezesAvailable = 1
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var today = _service.GetTodayGMT7();

            // Act: Ngày thứ 7
            var result = await _service.UpdateOnValidPracticeAsync(5, today);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.MilestoneReached);
            Assert.Equal(7, result.MilestoneValue);
            
            // Kiểm tra freeze token được tặng
            var updatedUser = await _context.Users.FindAsync(5);
            Assert.Equal(2, updatedUser.StreakFreezesAvailable); // 1 + 1 (thưởng)
        }
    }
}