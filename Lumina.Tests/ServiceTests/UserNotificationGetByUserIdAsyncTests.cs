using Xunit;
using Moq;
using FluentAssertions;
using ServiceLayer.Notification;
using RepositoryLayer.Notification;
using DataLayer.DTOs.Notification;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lumina.Test.Services.UserNotificationServiceTests
{
    public class UserNotificationGetByUserIdAsyncTests
    {
        private readonly Mock<IUserNotificationRepository> _mockRepository;
        private readonly UserNotificationService _service;

        public UserNotificationGetByUserIdAsyncTests()
        {
            _mockRepository = new Mock<IUserNotificationRepository>();
            _service = new UserNotificationService(_mockRepository.Object);
        }

        [Fact]
        public async Task GetByUserIdAsync_ShouldCallRepositoryAndReturnList()
        {
            // Arrange
            int userId = 1;
            var expectedList = new List<UserNotificationDTO>
            {
                new UserNotificationDTO { NotificationId = 1, UserId = userId, Content = "Test" }
            };

            _mockRepository.Setup(repo => repo.GetByUserIdAsync(userId))
                .ReturnsAsync(expectedList);

            // Act
            var result = await _service.GetByUserIdAsync(userId);

            // Assert
            result.Should().BeEquivalentTo(expectedList);
            _mockRepository.Verify(repo => repo.GetByUserIdAsync(userId), Times.Once);
        }
    }
}
