using DataLayer.DTOs.User;
using FluentAssertions;
using Moq;
using RepositoryLayer.User;
using ServiceLayer.User;

namespace Lumina.Tests.ServiceTests
{
    public class ChangePasswordAsyncUnitTest
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly UserService _service;

        public ChangePasswordAsyncUnitTest()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _service = new UserService(_mockUserRepository.Object);
        }

        #region ChangePasswordAsync

        [Fact]
        public async Task ChangePasswordAsync_WhenNewPasswordIsTooShort_ShouldThrowArgumentException()
        {
            // Arrange
            var userId = 1;
            var request = new ChangePasswordDTO
            {
                CurrentPassword = "CurrentPass123",
                NewPassword = "Short1"
            };

            // Act
            Func<Task> act = async () => await _service.ChangePasswordAsync(userId, request);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("New password must be at least 8 characters");
        }

        [Fact]
        public async Task ChangePasswordAsync_WhenNewPasswordSameAsCurrentPassword_ShouldThrowArgumentException()
        {
            // Arrange
            var userId = 1;
            var request = new ChangePasswordDTO
            {
                CurrentPassword = "SamePassword123",
                NewPassword = "SamePassword123"
            };

            // Act
            Func<Task> act = async () => await _service.ChangePasswordAsync(userId, request);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("New password must be different from current password");
        }

        [Fact]
        public async Task ChangePasswordAsync_WhenRepositoryReturnsFalse_ShouldThrowArgumentException()
        {
            // Arrange
            var userId = 1;
            var request = new ChangePasswordDTO
            {
                CurrentPassword = "WrongPassword123",
                NewPassword = "NewPassword123"
            };

            _mockUserRepository.Setup(r => r.ChangePasswordAsync(
                userId,
                request.CurrentPassword.Trim(),
                request.NewPassword.Trim()))
                .ReturnsAsync(false);

            // Act
            Func<Task> act = async () => await _service.ChangePasswordAsync(userId, request);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Current password is incorrect or account type does not support password change");
            _mockUserRepository.Verify(r => r.ChangePasswordAsync(
                userId,
                request.CurrentPassword.Trim(),
                request.NewPassword.Trim()), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_WhenRepositoryReturnsTrue_ShouldReturnTrue()
        {
            // Arrange
            var userId = 1;
            var request = new ChangePasswordDTO
            {
                CurrentPassword = "CurrentPassword123",
                NewPassword = "NewPassword123"
            };

            _mockUserRepository.Setup(r => r.ChangePasswordAsync(
                userId,
                request.CurrentPassword,
                request.NewPassword))
                .ReturnsAsync(true);

            // Act
            var result = await _service.ChangePasswordAsync(userId, request);

            // Assert
            result.Should().BeTrue();
            _mockUserRepository.Verify(r => r.ChangePasswordAsync(
                userId,
                request.CurrentPassword,
                request.NewPassword), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_WhenPasswordsHaveWhitespace_ShouldTrimBeforeProcessing()
        {
            // Arrange
            var userId = 1;
            var request = new ChangePasswordDTO
            {
                CurrentPassword = "  CurrentPassword123  ",
                NewPassword = "  NewPassword123  "
            };

            _mockUserRepository.Setup(r => r.ChangePasswordAsync(
                userId,
                "CurrentPassword123",
                "NewPassword123"))
                .ReturnsAsync(true);

            // Act
            var result = await _service.ChangePasswordAsync(userId, request);

            // Assert
            result.Should().BeTrue();
            _mockUserRepository.Verify(r => r.ChangePasswordAsync(
                userId,
                "CurrentPassword123",
                "NewPassword123"), Times.Once);
        }

        #endregion
    }
}
