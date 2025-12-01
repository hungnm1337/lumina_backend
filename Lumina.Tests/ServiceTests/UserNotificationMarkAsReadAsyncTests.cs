using Xunit;
using Moq;
using FluentAssertions;
using ServiceLayer.Notification;
using RepositoryLayer.Notification;
using DataLayer.DTOs.Notification;
using System.Threading.Tasks;

namespace Lumina.Test.Services.UserNotificationServiceTests
{
    public class UserNotificationMarkAsReadAsyncTests
    {
        private readonly Mock<IUserNotificationRepository> _mockRepository;
        private readonly UserNotificationService _service;

        public UserNotificationMarkAsReadAsyncTests()
        {
            _mockRepository = new Mock<IUserNotificationRepository>();
            _service = new UserNotificationService(_mockRepository.Object);
        }

        [Fact]
        public async Task MarkAsReadAsync_WhenNotificationNotFound_ShouldReturnFalse()
        {
            // Arrange
            int uniqueId = 1;
            int userId = 1;

            _mockRepository.Setup(repo => repo.GetByIdAsync(uniqueId))
                .ReturnsAsync((UserNotificationDTO?)null);

            // Act
            var result = await _service.MarkAsReadAsync(uniqueId, userId);

            // Assert
            result.Should().BeFalse();
            _mockRepository.Verify(repo => repo.GetByIdAsync(uniqueId), Times.Once);
            _mockRepository.Verify(repo => repo.MarkAsReadAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task MarkAsReadAsync_WhenNotificationBelongsToAnotherUser_ShouldReturnFalse()
        {
            // Arrange
            int uniqueId = 1;
            int userId = 1;
            int otherUserId = 2;
            var notification = new UserNotificationDTO { NotificationId = 1, UserId = otherUserId };

            _mockRepository.Setup(repo => repo.GetByIdAsync(uniqueId))
                .ReturnsAsync(notification);

            // Act
            var result = await _service.MarkAsReadAsync(uniqueId, userId);

            // Assert
            result.Should().BeFalse();
            _mockRepository.Verify(repo => repo.GetByIdAsync(uniqueId), Times.Once);
            _mockRepository.Verify(repo => repo.MarkAsReadAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task MarkAsReadAsync_WhenValid_ShouldCallRepositoryAndReturnTrue()
        {
            // Arrange
            int uniqueId = 1;
            int userId = 1;
            var notification = new UserNotificationDTO { NotificationId = 1, UserId = userId };

            _mockRepository.Setup(repo => repo.GetByIdAsync(uniqueId))
                .ReturnsAsync(notification);
            _mockRepository.Setup(repo => repo.MarkAsReadAsync(uniqueId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.MarkAsReadAsync(uniqueId, userId);

            // Assert
            result.Should().BeTrue();
            _mockRepository.Verify(repo => repo.GetByIdAsync(uniqueId), Times.Once);
            _mockRepository.Verify(repo => repo.MarkAsReadAsync(uniqueId), Times.Once);
        }
    }
}
