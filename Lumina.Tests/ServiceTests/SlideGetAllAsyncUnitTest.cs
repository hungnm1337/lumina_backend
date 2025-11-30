using Xunit;
using Moq;
using FluentAssertions;
using ServiceLayer.Slide;
using RepositoryLayer.Slide;
using DataLayer.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lumina.Test.Services.SlideServiceTests
{
    public class SlideGetAllAsyncUnitTest
    {
        private readonly Mock<ISlideRepository> _mockRepository;
        private readonly SlideService _service;

        public SlideGetAllAsyncUnitTest()
        {
            _mockRepository = new Mock<ISlideRepository>();
            _service = new SlideService(_mockRepository.Object);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnListOfSlides_WhenCalled()
        {
            // Arrange
            var expectedSlides = new List<SlideDTO>
            {
                new SlideDTO { SlideId = 1, SlideName = "Slide 1", SlideUrl = "http://url1.com" },
                new SlideDTO { SlideId = 2, SlideName = "Slide 2", SlideUrl = "http://url2.com" }
            };

            _mockRepository.Setup(repo => repo.GetAllAsync(It.IsAny<string?>(), It.IsAny<bool?>()))
                .ReturnsAsync(expectedSlides);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().BeEquivalentTo(expectedSlides);
            _mockRepository.Verify(repo => repo.GetAllAsync(null, null), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnFilteredSlides_WhenParametersProvided()
        {
            // Arrange
            string keyword = "test";
            bool isActive = true;
            var expectedSlides = new List<SlideDTO>
            {
                new SlideDTO { SlideId = 1, SlideName = "Test Slide", SlideUrl = "http://url1.com", IsActive = true }
            };

            _mockRepository.Setup(repo => repo.GetAllAsync(keyword, isActive))
                .ReturnsAsync(expectedSlides);

            // Act
            var result = await _service.GetAllAsync(keyword, isActive);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.Should().BeEquivalentTo(expectedSlides);
            _mockRepository.Verify(repo => repo.GetAllAsync(keyword, isActive), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoSlidesFound()
        {
            // Arrange
            _mockRepository.Setup(repo => repo.GetAllAsync(It.IsAny<string?>(), It.IsAny<bool?>()))
                .ReturnsAsync(new List<SlideDTO>());

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
            _mockRepository.Verify(repo => repo.GetAllAsync(null, null), Times.Once);
        }
    }
}
