using DataLayer.DTOs.User;
using FluentAssertions;
using Moq;
using RepositoryLayer.User;
using ServiceLayer.User;

namespace Lumina.Tests.ServiceTests
{
    public class UpdateCurrentUserProfileAsyncUnitTest
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly UserService _service;

        public UpdateCurrentUserProfileAsyncUnitTest()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _service = new UserService(_mockUserRepository.Object);
        }

        #region UpdateCurrentUserProfileAsync

        [Fact]
        public async Task UpdateCurrentUserProfileAsync_WhenFullNameIsWhitespace_ShouldThrowArgumentException()
        {
            // Arrange
            var userId = 1;
            var request = new UpdateUserProfileDTO
            {
                FullName = "   ",
                Phone = "0123456789"
            };

            // Act
            Func<Task> act = async () => await _service.UpdateCurrentUserProfileAsync(userId, request);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Full name is required");
        }

        [Fact]
        public async Task UpdateCurrentUserProfileAsync_WhenFullNameExceedsMaxLength_ShouldTruncateTo50Characters()
        {
            // Arrange
            var userId = 1;
            var longName = new string('A', 60);
            var expectedTruncatedName = new string('A', 50);
            var request = new UpdateUserProfileDTO
            {
                FullName = longName,
                Phone = "0123456789"
            };

            var expectedResult = new UserDto
            {
                UserId = userId,
                FullName = expectedTruncatedName,
                Phone = "0123456789"
            };

            _mockUserRepository.Setup(r => r.UpdateUserProfileAsync(
                userId,
                expectedTruncatedName,
                request.Phone,
                request.Bio,
                request.AvatarUrl))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.UpdateCurrentUserProfileAsync(userId, request);

            // Assert
            result.Should().NotBeNull();
            result!.FullName.Should().Be(expectedTruncatedName);
            result.FullName.Length.Should().Be(50);
            _mockUserRepository.Verify(r => r.UpdateUserProfileAsync(
                userId,
                expectedTruncatedName,
                request.Phone,
                request.Bio,
                request.AvatarUrl), Times.Once);
        }

        [Fact]
        public async Task UpdateCurrentUserProfileAsync_WithValidFullName_ShouldUpdateProfile()
        {
            // Arrange
            var userId = 1;
            var request = new UpdateUserProfileDTO
            {
                FullName = "John Doe",
                Phone = "0123456789",
                Bio = "Test bio",
                AvatarUrl = "https://example.com/avatar.jpg"
            };

            var expectedResult = new UserDto
            {
                UserId = userId,
                Email = "john@example.com",
                FullName = "John Doe",
                Phone = "0123456789",
                Bio = "Test bio",
                AvatarUrl = "https://example.com/avatar.jpg"
            };

            _mockUserRepository.Setup(r => r.UpdateUserProfileAsync(
                userId,
                request.FullName,
                request.Phone,
                request.Bio,
                request.AvatarUrl))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.UpdateCurrentUserProfileAsync(userId, request);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedResult);
            _mockUserRepository.Verify(r => r.UpdateUserProfileAsync(
                userId,
                request.FullName,
                request.Phone,
                request.Bio,
                request.AvatarUrl), Times.Once);
        }

        [Fact]
        public async Task UpdateCurrentUserProfileAsync_WithFullNameExactly50Characters_ShouldNotTruncate()
        {
            // Arrange
            var userId = 1;
            var exactName = new string('B', 50);
            var request = new UpdateUserProfileDTO
            {
                FullName = exactName,
                Phone = "0123456789"
            };

            var expectedResult = new UserDto
            {
                UserId = userId,
                FullName = exactName,
                Phone = "0123456789"
            };

            _mockUserRepository.Setup(r => r.UpdateUserProfileAsync(
                userId,
                exactName,
                request.Phone,
                request.Bio,
                request.AvatarUrl))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.UpdateCurrentUserProfileAsync(userId, request);

            // Assert
            result.Should().NotBeNull();
            result!.FullName.Should().Be(exactName);
            result.FullName.Length.Should().Be(50);
            _mockUserRepository.Verify(r => r.UpdateUserProfileAsync(
                userId,
                exactName,
                request.Phone,
                request.Bio,
                request.AvatarUrl), Times.Once);
        }

        #endregion
    }
}
