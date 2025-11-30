using Xunit;
using Moq;
using FluentAssertions;
using ServiceLayer.Notification;
using RepositoryLayer.Notification;
using System.Threading.Tasks;

namespace Lumina.Test.Services.UserNotificationServiceTests
{
    public class UserNotificationGetUnreadCountAsyncTests
    {
        private readonly Mock<IUserNotificationRepository> _mockRepository;
        private readonly UserNotificationService _service;

        public UserNotificationGetUnreadCountAsyncTests()
        {
            _mockRepository = new Mock<IUserNotificationRepository>();
            _service = new UserNotificationService(_mockRepository.Object);
        }

        [Fact]
        public async Task GetUnreadCountAsync_ShouldCallRepositoryAndReturnCount()
        {
            // Arrange
            int userId = 1;
            int expectedCount = 5;

            _mockRepository.Setup(repo => repo.GetUnreadCountAsync(userId))
                .ReturnsAsync(expectedCount);

            // Act
            var result = await _service.GetUnreadCountAsync(userId);

            // Assert
            result.Should().Be(expectedCount);
            _mockRepository.Verify(repo => repo.GetUnreadCountAsync(userId), Times.Once);
        }
    }
}
