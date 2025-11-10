using System;
using System.Security.Claims;
using System.Threading.Tasks;
using DataLayer.DTOs;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.Slide;
using Xunit;

namespace Lumina.Tests
{
    public class UpdateSlideTests
    {
        private readonly Mock<ISlideService> _mockSlideService;
        private readonly SlideController _controller;

        public UpdateSlideTests()
        {
            _mockSlideService = new Mock<ISlideService>();
            _controller = new SlideController(_mockSlideService.Object);
        }

        [Fact]
        public async Task Update_ValidSlideDTO_ReturnsNoContentAndSetsUpdateBy()
        {
            // Arrange
            int slideId = 123;
            var slideDto = new SlideDTO
            {
                SlideId = slideId,
                SlideName = "Updated Slide",
                SlideUrl = "https://example.com/updated.jpg",
                IsActive = true
            };
            int userId = 1;

            _mockSlideService
                .Setup(s => s.UpdateAsync(It.IsAny<SlideDTO>()))
                .ReturnsAsync(true);

            SetupUserClaims(userId);

            // Act
            var result = await _controller.Update(slideId, slideDto);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal(userId, slideDto.UpdateBy);
            _mockSlideService.Verify(s => s.UpdateAsync(It.Is<SlideDTO>(dto => dto.UpdateBy == userId)), Times.Once);
        }

        [Fact]
        public async Task Update_SlideIdMismatch_ReturnsBadRequest()
        {
            // Arrange
            int slideId = 123;
            var slideDto = new SlideDTO
            {
                SlideId = 456, // Different from slideId parameter
                SlideName = "Test Slide",
                SlideUrl = "https://example.com/image.jpg"
            };
            int userId = 1;

            SetupUserClaims(userId);

            // Act
            var result = await _controller.Update(slideId, slideDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Slide ID mismatch", badRequestResult.Value);
            _mockSlideService.Verify(s => s.UpdateAsync(It.IsAny<SlideDTO>()), Times.Never);
        }

        [Fact]
        public async Task Update_ServiceReturnsFalse_ReturnsNotFound()
        {
            // Arrange
            int slideId = 999;
            var slideDto = new SlideDTO
            {
                SlideId = slideId,
                SlideName = "Non-existent Slide",
                SlideUrl = "https://example.com/image.jpg"
            };
            int userId = 1;

            _mockSlideService
                .Setup(s => s.UpdateAsync(It.IsAny<SlideDTO>()))
                .ReturnsAsync(false);

            SetupUserClaims(userId);

            // Act
            var result = await _controller.Update(slideId, slideDto);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            Assert.Equal(userId, slideDto.UpdateBy);
            _mockSlideService.Verify(s => s.UpdateAsync(slideDto), Times.Once);
        }

        [Fact]
        public async Task Update_MissingUserIdClaim_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            int slideId = 123;
            var slideDto = new SlideDTO
            {
                SlideId = slideId,
                SlideName = "Test Slide",
                SlideUrl = "https://example.com/image.jpg"
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                async () => await _controller.Update(slideId, slideDto)
            );
            Assert.Equal("Missing user id claim", exception.Message);
            _mockSlideService.Verify(s => s.UpdateAsync(It.IsAny<SlideDTO>()), Times.Never);
        }

        [Fact]
        public async Task Update_EmptyUserIdClaim_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            int slideId = 123;
            var slideDto = new SlideDTO
            {
                SlideId = slideId,
                SlideName = "Test Slide",
                SlideUrl = "https://example.com/image.jpg"
            };

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, string.Empty)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                async () => await _controller.Update(slideId, slideDto)
            );
            Assert.Equal("Missing user id claim", exception.Message);
            _mockSlideService.Verify(s => s.UpdateAsync(It.IsAny<SlideDTO>()), Times.Never);
        }

        [Fact]
        public async Task Update_ServiceThrowsException_PropagatesException()
        {
            // Arrange
            int slideId = 123;
            var slideDto = new SlideDTO
            {
                SlideId = slideId,
                SlideName = "Test Slide",
                SlideUrl = "https://example.com/image.jpg"
            };
            int userId = 1;

            _mockSlideService
                .Setup(s => s.UpdateAsync(It.IsAny<SlideDTO>()))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            SetupUserClaims(userId);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _controller.Update(slideId, slideDto)
            );
            
            // Verify UpdateBy was still set before exception
            Assert.Equal(userId, slideDto.UpdateBy);
        }

        [Fact]
        public async Task Update_DtoWithPresetUpdateBy_OverridesWithUserId()
        {
            // Arrange
            int slideId = 123;
            var slideDto = new SlideDTO
            {
                SlideId = slideId,
                SlideName = "Test Slide",
                SlideUrl = "https://example.com/image.jpg",
                UpdateBy = 999 // Pre-set value that should be overridden
            };
            int userId = 1;

            _mockSlideService
                .Setup(s => s.UpdateAsync(It.IsAny<SlideDTO>()))
                .ReturnsAsync(true);

            SetupUserClaims(userId);

            // Act
            var result = await _controller.Update(slideId, slideDto);

            // Assert
            Assert.IsType<NoContentResult>(result);
            
            // Verify UpdateBy was overridden with actual userId
            Assert.Equal(userId, slideDto.UpdateBy);
            Assert.NotEqual(999, slideDto.UpdateBy);
            
            _mockSlideService.Verify(s => s.UpdateAsync(It.Is<SlideDTO>(dto => dto.UpdateBy == userId)), Times.Once);
        }

        [Fact]
        public async Task Update_ZeroSlideId_ReturnsNoContent()
        {
            // Arrange
            int slideId = 0;
            var slideDto = new SlideDTO
            {
                SlideId = slideId,
                SlideName = "Test Slide",
                SlideUrl = "https://example.com/image.jpg"
            };
            int userId = 1;

            _mockSlideService
                .Setup(s => s.UpdateAsync(It.IsAny<SlideDTO>()))
                .ReturnsAsync(true);

            SetupUserClaims(userId);

            // Act
            var result = await _controller.Update(slideId, slideDto);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockSlideService.Verify(s => s.UpdateAsync(slideDto), Times.Once);
        }

        [Fact]
        public async Task Update_NullSlideIdInDto_ReturnsBadRequest()
        {
            // Arrange
            int slideId = 123;
            var slideDto = new SlideDTO
            {
                SlideId = null, // Null SlideId in DTO
                SlideName = "Test Slide",
                SlideUrl = "https://example.com/image.jpg"
            };
            int userId = 1;

            SetupUserClaims(userId);

            // Act
            var result = await _controller.Update(slideId, slideDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Slide ID mismatch", badRequestResult.Value);
            _mockSlideService.Verify(s => s.UpdateAsync(It.IsAny<SlideDTO>()), Times.Never);
        }

        private void SetupUserClaims(int userId)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };
        }
    }
}
