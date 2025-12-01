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
    public class SlideUpdateAsyncUnitTest
    {
        private readonly Mock<ISlideRepository> _mockRepository;
        private readonly SlideService _service;

        public SlideUpdateAsyncUnitTest()
        {
            _mockRepository = new Mock<ISlideRepository>();
            _service = new SlideService(_mockRepository.Object);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowArgumentNullException_WhenDtoIsNull()
        {
            // Arrange
            SlideDTO? dto = null;

            // Act
            Func<Task> act = async () => await _service.UpdateAsync(dto!);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("dto");
            _mockRepository.Verify(repo => repo.UpdateAsync(It.IsAny<SlideDTO>()), Times.Never);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task UpdateAsync_ShouldThrowArgumentException_WhenSlideNameIsEmpty(string? slideName)
        {
            // Arrange
            var dto = new SlideDTO { SlideName = slideName!, SlideUrl = "http://valid.com" };

            // Act
            Func<Task> act = async () => await _service.UpdateAsync(dto);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Slide name cannot be empty. (Parameter 'SlideName')");
            _mockRepository.Verify(repo => repo.UpdateAsync(It.IsAny<SlideDTO>()), Times.Never);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task UpdateAsync_ShouldThrowArgumentException_WhenSlideUrlIsEmpty(string? slideUrl)
        {
            // Arrange
            var dto = new SlideDTO { SlideName = "Valid Name", SlideUrl = slideUrl! };

            // Act
            Func<Task> act = async () => await _service.UpdateAsync(dto);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Slide URL cannot be empty. (Parameter 'SlideUrl')");
            _mockRepository.Verify(repo => repo.UpdateAsync(It.IsAny<SlideDTO>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnTrue_WhenDtoIsValid()
        {
            // Arrange
            var dto = new SlideDTO { SlideId = 1, SlideName = "Valid Name", SlideUrl = "http://valid.com" };
            _mockRepository.Setup(repo => repo.UpdateAsync(dto))
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpdateAsync(dto);

            // Assert
            result.Should().BeTrue();
            _mockRepository.Verify(repo => repo.UpdateAsync(dto), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnFalse_WhenUpdateFails()
        {
            // Arrange
            var dto = new SlideDTO { SlideId = 1, SlideName = "Valid Name", SlideUrl = "http://valid.com" };
            _mockRepository.Setup(repo => repo.UpdateAsync(dto))
                .ReturnsAsync(false);

            // Act
            var result = await _service.UpdateAsync(dto);

            // Assert
            result.Should().BeFalse();
            _mockRepository.Verify(repo => repo.UpdateAsync(dto), Times.Once);
        }
    }
}
