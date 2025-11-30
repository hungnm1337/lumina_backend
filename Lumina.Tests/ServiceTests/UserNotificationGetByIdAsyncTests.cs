using Xunit;
using Moq;
using FluentAssertions;
using ServiceLayer.Notification;
using RepositoryLayer.Notification;
using DataLayer.DTOs.Notification;
using System.Threading.Tasks;

namespace Lumina.Test.Services.UserNotificationServiceTests
{
    public class UserNotificationGetByIdAsyncTests
    {
        private readonly Mock<IUserNotificationRepository> _mockRepository;
        private readonly UserNotificationService _service;

        public UserNotificationGetByIdAsyncTests()
        {
            _mockRepository = new Mock<IUserNotificationRepository>();
            _service = new UserNotificationService(_mockRepository.Object);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldCallRepositoryAndReturnItem()
        {
            // Arrange
            int uniqueId = 1;
            var expectedItem = new UserNotificationDTO { NotificationId = 1, UserId = 1, Content = "Test" };

            _mockRepository.Setup(repo => repo.GetByIdAsync(uniqueId))
                .ReturnsAsync(expectedItem);

            // Act
            var result = await _service.GetByIdAsync(uniqueId);

            // Assert
            result.Should().BeEquivalentTo(expectedItem);
            _mockRepository.Verify(repo => repo.GetByIdAsync(uniqueId), Times.Once);
        }
    }
}
