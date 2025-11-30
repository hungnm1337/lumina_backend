using Xunit;
using Moq;
using FluentAssertions;
using ServiceLayer.Slide;
using RepositoryLayer.Slide;
using System;
using System.Threading.Tasks;

namespace Lumina.Test.Services.SlideServiceTests
{
    public class SlideDeleteAsyncUnitTest
    {
        private readonly Mock<ISlideRepository> _mockRepository;
        private readonly SlideService _service;

        public SlideDeleteAsyncUnitTest()
        {
            _mockRepository = new Mock<ISlideRepository>();
            _service = new SlideService(_mockRepository.Object);
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowArgumentException_WhenIdIsNegative()
        {
            // Arrange
            int invalidId = -1;

            // Act
            Func<Task> act = async () => await _service.DeleteAsync(invalidId);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Slide ID cannot be negative. (Parameter 'slideId')");
            _mockRepository.Verify(repo => repo.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnTrue_WhenDeletionSuccessful()
        {
            // Arrange
            int validId = 1;
            _mockRepository.Setup(repo => repo.DeleteAsync(validId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteAsync(validId);

            // Assert
            result.Should().BeTrue();
            _mockRepository.Verify(repo => repo.DeleteAsync(validId), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnFalse_WhenDeletionFails()
        {
            // Arrange
            int validId = 1;
            _mockRepository.Setup(repo => repo.DeleteAsync(validId))
                .ReturnsAsync(false);

            // Act
            var result = await _service.DeleteAsync(validId);

            // Assert
            result.Should().BeFalse();
            _mockRepository.Verify(repo => repo.DeleteAsync(validId), Times.Once);
        }
    }
}
