using DataLayer.DTOs.User;
using FluentAssertions;
using Moq;
using RepositoryLayer.User;
using ServiceLayer.User;

namespace Lumina.Tests.ServiceTests
{
    public class GetCurrentUserProfileAsyncUnitTest
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly UserService _service;

        public GetCurrentUserProfileAsyncUnitTest()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _service = new UserService(_mockUserRepository.Object);
        }

        #region GetCurrentUserProfileAsync

        [Fact]
        public async Task GetCurrentUserProfileAsync_WhenUserExists_ShouldReturnUserDto()
        {
            // Arrange
            var userId = 1;
            var expectedUserDto = new UserDto
            {
                UserId = userId,
                Email = "test@example.com",
                FullName = "Test User",
                RoleId = 2,
                RoleName = "Student",
                UserName = "testuser",
                AvatarUrl = "https://example.com/avatar.jpg",
                Bio = "Test bio",
                Phone = "0123456789",
                IsActive = true,
                JoinDate = "2024-01-01"
            };

            _mockUserRepository.Setup(r => r.GetCurrentUserProfileAsync(userId))
                .ReturnsAsync(expectedUserDto);

            // Act
            var result = await _service.GetCurrentUserProfileAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedUserDto);
            _mockUserRepository.Verify(r => r.GetCurrentUserProfileAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetCurrentUserProfileAsync_WhenUserNotFound_ShouldReturnNull()
        {
            // Arrange
            var userId = 999;
            _mockUserRepository.Setup(r => r.GetCurrentUserProfileAsync(userId))
                .ReturnsAsync((UserDto?)null);

            // Act
            var result = await _service.GetCurrentUserProfileAsync(userId);

            // Assert
            result.Should().BeNull();
            _mockUserRepository.Verify(r => r.GetCurrentUserProfileAsync(userId), Times.Once);
        }

        #endregion
    }
}
