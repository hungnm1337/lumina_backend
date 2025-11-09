using System;
using System.Threading.Tasks;
using lumina.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.Slide;
using Xunit;

namespace Lumina.Tests
{
    public class DeleteSlideTests
    {
        private readonly Mock<ISlideService> _mockSlideService;
        private readonly SlideController _controller;

        public DeleteSlideTests()
        {
            _mockSlideService = new Mock<ISlideService>();
            _controller = new SlideController(_mockSlideService.Object);
        }

        [Fact]
        public async Task Delete_ExistingSlideId_ReturnsNoContent()
        {
            // Arrange
            int slideId = 123;

            _mockSlideService
                .Setup(s => s.DeleteAsync(slideId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(slideId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockSlideService.Verify(s => s.DeleteAsync(slideId), Times.Once);
        }

        [Fact]
        public async Task Delete_NonExistingSlideId_ReturnsNotFound()
        {
            // Arrange
            int slideId = 999;

            _mockSlideService
                .Setup(s => s.DeleteAsync(slideId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(slideId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockSlideService.Verify(s => s.DeleteAsync(slideId), Times.Once);
        }

        [Fact]
        public async Task Delete_ServiceThrowsException_PropagatesException()
        {
            // Arrange
            int slideId = 123;

            _mockSlideService
                .Setup(s => s.DeleteAsync(slideId))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _controller.Delete(slideId)
            );
            _mockSlideService.Verify(s => s.DeleteAsync(slideId), Times.Once);
        }

        [Fact]
        public async Task Delete_ZeroSlideId_CallsServiceWithZeroId()
        {
            // Arrange
            int slideId = 0;

            _mockSlideService
                .Setup(s => s.DeleteAsync(slideId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(slideId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockSlideService.Verify(s => s.DeleteAsync(slideId), Times.Once);
        }

        [Fact]
        public async Task Delete_NegativeSlideId_CallsServiceWithNegativeId()
        {
            // Arrange
            int slideId = -1;

            _mockSlideService
                .Setup(s => s.DeleteAsync(slideId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(slideId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockSlideService.Verify(s => s.DeleteAsync(slideId), Times.Once);
        }

        [Fact]
        public async Task Delete_MaxIntSlideId_CallsServiceSuccessfully()
        {
            // Arrange
            int slideId = int.MaxValue;

            _mockSlideService
                .Setup(s => s.DeleteAsync(slideId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(slideId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockSlideService.Verify(s => s.DeleteAsync(slideId), Times.Once);
        }
    }
}
