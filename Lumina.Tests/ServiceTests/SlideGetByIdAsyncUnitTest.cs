using Xunit;
using Moq;
using FluentAssertions;
using ServiceLayer.Slide;
using RepositoryLayer.Slide;
using DataLayer.DTOs;
using System;
using System.Threading.Tasks;

namespace Lumina.Test.Services.SlideServiceTests
{
    public class SlideGetByIdAsyncUnitTest
    {
        private readonly Mock<ISlideRepository> _mockRepository;
        private readonly SlideService _service;

        public SlideGetByIdAsyncUnitTest()
        {
            _mockRepository = new Mock<ISlideRepository>();
            _service = new SlideService(_mockRepository.Object);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrowArgumentException_WhenIdIsNegative()
        {
            // Arrange
            int invalidId = -1;

            // Act
            Func<Task> act = async () => await _service.GetByIdAsync(invalidId);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Slide ID cannot be negative. (Parameter 'slideId')");
            _mockRepository.Verify(repo => repo.GetByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenIdDoesNotExist()
        {
            // Arrange
            int nonExistentId = 999;
            _mockRepository.Setup(repo => repo.GetByIdAsync(nonExistentId))
                .ReturnsAsync((SlideDTO?)null);

            // Act
            var result = await _service.GetByIdAsync(nonExistentId);

            // Assert
            result.Should().BeNull();
            _mockRepository.Verify(repo => repo.GetByIdAsync(nonExistentId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnSlide_WhenIdExists()
        {
            // Arrange
            int validId = 1;
            var expectedSlide = new SlideDTO { SlideId = validId, SlideName = "Slide 1", SlideUrl = "http://url1.com" };
            _mockRepository.Setup(repo => repo.GetByIdAsync(validId))
                .ReturnsAsync(expectedSlide);

            // Act
            var result = await _service.GetByIdAsync(validId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedSlide);
            _mockRepository.Verify(repo => repo.GetByIdAsync(validId), Times.Once);
        }
    }
}
