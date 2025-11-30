using ServiceLayer.Streak;
using Xunit;

namespace Lumina.Tests.ServiceTests
{
    public class GenerateReminderMessageUnitTest
    {
        private readonly StreakService _service;

        public GenerateReminderMessageUnitTest()
        {
            // Create a minimal service instance just to test the public method
            var context = Lumina.Tests.Helpers.InMemoryDbContextHelper.CreateContext();
            var loggerMock = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<StreakService>>();
            var repoMock = new Moq.Mock<RepositoryLayer.Streak.IStreakRepository>(Moq.MockBehavior.Strict);
            _service = new StreakService(context, loggerMock.Object, repoMock.Object);
        }

        [Theory]
        [InlineData(30, 2, "🔥")] // High streak
        [InlineData(7, 1, "⚡")] // Medium streak
        [InlineData(3, 0, "💪")] // Low streak
        [InlineData(1, 0, "🌟")] // New streak
        [InlineData(0, 0, "🌟")] // Zero streak (same as new)
        public void GenerateReminderMessage_AllStreakLevels_ReturnsCorrectMessage(int currentStreak, int freezeTokens, string expectedEmoji)
        {
            // Act
            var result = _service.GenerateReminderMessage(currentStreak, freezeTokens);

            // Assert
            Assert.Contains(expectedEmoji, result);
            Assert.Contains($"{currentStreak} ngày", result);
            Assert.Contains($"{freezeTokens} freeze token còn lại", result);
        }
    }
}

