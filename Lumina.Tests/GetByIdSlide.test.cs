using System;
using System.Threading.Tasks;
using DataLayer.DTOs;
using lumina.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.Slide;
using Xunit;

namespace Lumina.Tests
{
    public class GetByIdSlideTests
    {
        private readonly Mock<ISlideService> _mockSlideService;
        private readonly SlideController _controller;

        public GetByIdSlideTests()
        {
            _mockSlideService = new Mock<ISlideService>();
            _controller = new SlideController(_mockSlideService.Object);
        }

        [Fact]
        public async Task GetById_ExistingSlideId_ReturnsOkWithSlideDTO()
        {
            // Arrange
            int slideId = 123;
            var expectedSlide = new SlideDTO
            {
                SlideId = slideId,
                SlideName = "Test Slide",
                SlideUrl = "https://example.com/slide.jpg",
                IsActive = true,
                CreateBy = 1,
                CreateAt = DateTime.Now
            };

            _mockSlideService
                .Setup(s => s.GetByIdAsync(slideId))
                .ReturnsAsync(expectedSlide);

            // Act
            var result = await _controller.GetById(slideId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedSlide = Assert.IsType<SlideDTO>(okResult.Value);
            Assert.Equal(expectedSlide.SlideId, returnedSlide.SlideId);
            Assert.Equal(expectedSlide.SlideName, returnedSlide.SlideName);
            Assert.Equal(expectedSlide.SlideUrl, returnedSlide.SlideUrl);
            Assert.Equal(expectedSlide.IsActive, returnedSlide.IsActive);
            _mockSlideService.Verify(s => s.GetByIdAsync(slideId), Times.Once);
        }

        [Fact]
        public async Task GetById_NonExistingSlideId_ReturnsNotFound()
        {
            // Arrange
            int slideId = 999;

            _mockSlideService
                .Setup(s => s.GetByIdAsync(slideId))
                .ReturnsAsync((SlideDTO)null);

            // Act
            var result = await _controller.GetById(slideId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
            _mockSlideService.Verify(s => s.GetByIdAsync(slideId), Times.Once);
        }

        [Fact]
        public async Task GetById_ServiceThrowsException_PropagatesException()
        {
            // Arrange
            int slideId = 123;

            _mockSlideService
                .Setup(s => s.GetByIdAsync(slideId))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _controller.GetById(slideId)
            );
            _mockSlideService.Verify(s => s.GetByIdAsync(slideId), Times.Once);
        }

        [Fact]
        public async Task GetById_ZeroSlideId_ReturnsNotFound()
        {
            // Arrange
            int slideId = 0;

            _mockSlideService
                .Setup(s => s.GetByIdAsync(slideId))
                .ReturnsAsync((SlideDTO)null);

            // Act
            var result = await _controller.GetById(slideId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
            _mockSlideService.Verify(s => s.GetByIdAsync(slideId), Times.Once);
        }

        [Fact]
        public async Task GetById_NegativeSlideId_ReturnsNotFound()
        {
            // Arrange
            int slideId = -1;

            _mockSlideService
                .Setup(s => s.GetByIdAsync(slideId))
                .ReturnsAsync((SlideDTO)null);

            // Act
            var result = await _controller.GetById(slideId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
            _mockSlideService.Verify(s => s.GetByIdAsync(slideId), Times.Once);
        }

        [Fact]
        public async Task GetById_MaxIntSlideId_ReturnsOkWithSlideDTO()
        {
            // Arrange
            int slideId = int.MaxValue;
            var expectedSlide = new SlideDTO
            {
                SlideId = slideId,
                SlideName = "Max ID Slide",
                SlideUrl = "https://example.com/max.jpg",
                IsActive = true,
                CreateBy = 1,
                CreateAt = DateTime.Now
            };

            _mockSlideService
                .Setup(s => s.GetByIdAsync(slideId))
                .ReturnsAsync(expectedSlide);

            // Act
            var result = await _controller.GetById(slideId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedSlide = Assert.IsType<SlideDTO>(okResult.Value);
            Assert.Equal(slideId, returnedSlide.SlideId);
            _mockSlideService.Verify(s => s.GetByIdAsync(slideId), Times.Once);
        }
    }
}
