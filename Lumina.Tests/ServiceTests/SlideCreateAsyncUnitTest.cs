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
    public class SlideCreateAsyncUnitTest
    {
        private readonly Mock<ISlideRepository> _mockRepository;
        private readonly SlideService _service;

        public SlideCreateAsyncUnitTest()
        {
            _mockRepository = new Mock<ISlideRepository>();
            _service = new SlideService(_mockRepository.Object);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowArgumentNullException_WhenDtoIsNull()
        {
            // Arrange
            SlideDTO? dto = null;

            // Act
            Func<Task> act = async () => await _service.CreateAsync(dto!);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("dto");
            _mockRepository.Verify(repo => repo.CreateAsync(It.IsAny<SlideDTO>()), Times.Never);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task CreateAsync_ShouldThrowArgumentException_WhenSlideNameIsEmpty(string? slideName)
        {
            // Arrange
            var dto = new SlideDTO { SlideName = slideName!, SlideUrl = "http://valid.com" };

            // Act
            Func<Task> act = async () => await _service.CreateAsync(dto);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Slide name cannot be empty. (Parameter 'SlideName')");
            _mockRepository.Verify(repo => repo.CreateAsync(It.IsAny<SlideDTO>()), Times.Never);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task CreateAsync_ShouldThrowArgumentException_WhenSlideUrlIsEmpty(string? slideUrl)
        {
            // Arrange
            var dto = new SlideDTO { SlideName = "Valid Name", SlideUrl = slideUrl! };

            // Act
            Func<Task> act = async () => await _service.CreateAsync(dto);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Slide URL cannot be empty. (Parameter 'SlideUrl')");
            _mockRepository.Verify(repo => repo.CreateAsync(It.IsAny<SlideDTO>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnId_WhenDtoIsValid()
        {
            // Arrange
            var dto = new SlideDTO { SlideName = "Valid Name", SlideUrl = "http://valid.com" };
            int expectedId = 1;
            _mockRepository.Setup(repo => repo.CreateAsync(dto))
                .ReturnsAsync(expectedId);

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            result.Should().Be(expectedId);
            _mockRepository.Verify(repo => repo.CreateAsync(dto), Times.Once);
        }
    }
}
