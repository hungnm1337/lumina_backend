using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataLayer.DTOs;
using lumina.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.Slide;
using Xunit;

namespace Lumina.Tests
{
    public class GetAllSlideTests
    {
        private readonly Mock<ISlideService> _mockSlideService;
        private readonly SlideController _controller;

        public GetAllSlideTests()
        {
            _mockSlideService = new Mock<ISlideService>();
            _controller = new SlideController(_mockSlideService.Object);
        }

        [Fact]
        public async Task GetAll_NoParameters_ReturnsOkWithAllSlides()
        {
            // Arrange
            var expectedSlides = new List<SlideDTO>
            {
                new SlideDTO { SlideId = 1, SlideName = "Slide 1", SlideUrl = "url1.jpg", IsActive = true },
                new SlideDTO { SlideId = 2, SlideName = "Slide 2", SlideUrl = "url2.jpg", IsActive = false },
                new SlideDTO { SlideId = 3, SlideName = "Slide 3", SlideUrl = "url3.jpg", IsActive = true }
            };

            _mockSlideService
                .Setup(s => s.GetAllAsync(null, null))
                .ReturnsAsync(expectedSlides);

            // Act
            var result = await _controller.GetAll(null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedSlides = Assert.IsAssignableFrom<List<SlideDTO>>(okResult.Value);
            Assert.Equal(3, returnedSlides.Count);
            _mockSlideService.Verify(s => s.GetAllAsync(null, null), Times.Once);
        }

        [Fact]
        public async Task GetAll_WithKeyword_ReturnsOkWithFilteredSlides()
        {
            // Arrange
            string keyword = "test";
            var expectedSlides = new List<SlideDTO>
            {
                new SlideDTO { SlideId = 1, SlideName = "Test Slide", SlideUrl = "url1.jpg", IsActive = true }
            };

            _mockSlideService
                .Setup(s => s.GetAllAsync(keyword, null))
                .ReturnsAsync(expectedSlides);

            // Act
            var result = await _controller.GetAll(keyword, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedSlides = Assert.IsAssignableFrom<List<SlideDTO>>(okResult.Value);
            Assert.Single(returnedSlides);
            Assert.Contains("Test", returnedSlides[0].SlideName);
            _mockSlideService.Verify(s => s.GetAllAsync(keyword, null), Times.Once);
        }

        [Fact]
        public async Task GetAll_WithIsActiveTrue_ReturnsOkWithActiveSlides()
        {
            // Arrange
            bool? isActive = true;
            var expectedSlides = new List<SlideDTO>
            {
                new SlideDTO { SlideId = 1, SlideName = "Active Slide 1", SlideUrl = "url1.jpg", IsActive = true },
                new SlideDTO { SlideId = 2, SlideName = "Active Slide 2", SlideUrl = "url2.jpg", IsActive = true }
            };

            _mockSlideService
                .Setup(s => s.GetAllAsync(null, isActive))
                .ReturnsAsync(expectedSlides);

            // Act
            var result = await _controller.GetAll(null, isActive);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedSlides = Assert.IsAssignableFrom<List<SlideDTO>>(okResult.Value);
            Assert.Equal(2, returnedSlides.Count);
            Assert.All(returnedSlides, slide => Assert.True(slide.IsActive));
            _mockSlideService.Verify(s => s.GetAllAsync(null, isActive), Times.Once);
        }

        [Fact]
        public async Task GetAll_WithIsActiveFalse_ReturnsOkWithInactiveSlides()
        {
            // Arrange
            bool? isActive = false;
            var expectedSlides = new List<SlideDTO>
            {
                new SlideDTO { SlideId = 1, SlideName = "Inactive Slide", SlideUrl = "url1.jpg", IsActive = false }
            };

            _mockSlideService
                .Setup(s => s.GetAllAsync(null, isActive))
                .ReturnsAsync(expectedSlides);

            // Act
            var result = await _controller.GetAll(null, isActive);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedSlides = Assert.IsAssignableFrom<List<SlideDTO>>(okResult.Value);
            Assert.Single(returnedSlides);
            Assert.False(returnedSlides[0].IsActive);
            _mockSlideService.Verify(s => s.GetAllAsync(null, isActive), Times.Once);
        }

        [Fact]
        public async Task GetAll_WithKeywordAndIsActive_ReturnsOkWithFilteredSlides()
        {
            // Arrange
            string keyword = "promo";
            bool? isActive = true;
            var expectedSlides = new List<SlideDTO>
            {
                new SlideDTO { SlideId = 1, SlideName = "Promo Slide", SlideUrl = "url1.jpg", IsActive = true }
            };

            _mockSlideService
                .Setup(s => s.GetAllAsync(keyword, isActive))
                .ReturnsAsync(expectedSlides);

            // Act
            var result = await _controller.GetAll(keyword, isActive);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedSlides = Assert.IsAssignableFrom<List<SlideDTO>>(okResult.Value);
            Assert.Single(returnedSlides);
            _mockSlideService.Verify(s => s.GetAllAsync(keyword, isActive), Times.Once);
        }

        [Fact]
        public async Task GetAll_EmptyKeyword_ReturnsOkWithAllSlides()
        {
            // Arrange
            string keyword = string.Empty;
            var expectedSlides = new List<SlideDTO>
            {
                new SlideDTO { SlideId = 1, SlideName = "Slide 1", SlideUrl = "url1.jpg", IsActive = true }
            };

            _mockSlideService
                .Setup(s => s.GetAllAsync(keyword, null))
                .ReturnsAsync(expectedSlides);

            // Act
            var result = await _controller.GetAll(keyword, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedSlides = Assert.IsAssignableFrom<List<SlideDTO>>(okResult.Value);
            Assert.Single(returnedSlides);
            _mockSlideService.Verify(s => s.GetAllAsync(keyword, null), Times.Once);
        }

        [Fact]
        public async Task GetAll_NoMatchingSlides_ReturnsOkWithEmptyList()
        {
            // Arrange
            string keyword = "nonexistent";
            var expectedSlides = new List<SlideDTO>();

            _mockSlideService
                .Setup(s => s.GetAllAsync(keyword, null))
                .ReturnsAsync(expectedSlides);

            // Act
            var result = await _controller.GetAll(keyword, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedSlides = Assert.IsAssignableFrom<List<SlideDTO>>(okResult.Value);
            Assert.Empty(returnedSlides);
            _mockSlideService.Verify(s => s.GetAllAsync(keyword, null), Times.Once);
        }

        [Fact]
        public async Task GetAll_ServiceThrowsException_PropagatesException()
        {
            // Arrange
            _mockSlideService
                .Setup(s => s.GetAllAsync(It.IsAny<string>(), It.IsAny<bool?>()))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _controller.GetAll(null, null)
            );
            _mockSlideService.Verify(s => s.GetAllAsync(null, null), Times.Once);
        }
    }
}
