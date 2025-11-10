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
    public class CreateSlideTests
    {
        private readonly Mock<ISlideService> _mockSlideService;
        private readonly SlideController _controller;

        public CreateSlideTests()
        {
            _mockSlideService = new Mock<ISlideService>();
            _controller = new SlideController(_mockSlideService.Object);
        }

        [Fact]
        public async Task Create_ValidSlideDTO_ReturnsCreatedAtActionAndSetsCreateBy()
        {
            // Arrange
            var slideDto = new SlideDTO
            {
                SlideName = "Test Slide",
                SlideUrl = "https://example.com/image.jpg",
                IsActive = true
            };
            int expectedSlideId = 123;
            int userId = 1;

            _mockSlideService
                .Setup(s => s.CreateAsync(It.IsAny<SlideDTO>()))
                .ReturnsAsync(expectedSlideId);

            SetupUserClaims(userId);

            // Act
            var result = await _controller.Create(slideDto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(SlideController.GetById), createdAtActionResult.ActionName);
            Assert.Equal(expectedSlideId, createdAtActionResult.Value);
            Assert.Equal(expectedSlideId, createdAtActionResult.RouteValues["slideId"]);
            
            // Verify CreateBy was set
            Assert.Equal(userId, slideDto.CreateBy);
            
            _mockSlideService.Verify(s => s.CreateAsync(It.Is<SlideDTO>(dto => dto.CreateBy == userId)), Times.Once);
        }

        [Fact]
        public async Task Create_ServiceReturnsZero_ReturnsCreatedAtActionWithZero()
        {
            // Arrange
            var slideDto = new SlideDTO
            {
                SlideName = "Test Slide",
                SlideUrl = "https://example.com/image.jpg"
            };
            int expectedSlideId = 0;
            int userId = 5;

            _mockSlideService
                .Setup(s => s.CreateAsync(It.IsAny<SlideDTO>()))
                .ReturnsAsync(expectedSlideId);

            SetupUserClaims(userId);

            // Act
            var result = await _controller.Create(slideDto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(expectedSlideId, createdAtActionResult.Value);
            Assert.Equal(userId, slideDto.CreateBy);
        }

        [Fact]
        public async Task Create_MissingUserIdClaim_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var slideDto = new SlideDTO
            {
                SlideName = "Test Slide",
                SlideUrl = "https://example.com/image.jpg"
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                async () => await _controller.Create(slideDto)
            );
            Assert.Equal("Missing user id claim", exception.Message);
            _mockSlideService.Verify(s => s.CreateAsync(It.IsAny<SlideDTO>()), Times.Never);
        }

        [Fact]
        public async Task Create_EmptyUserIdClaim_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var slideDto = new SlideDTO
            {
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
                async () => await _controller.Create(slideDto)
            );
            Assert.Equal("Missing user id claim", exception.Message);
            _mockSlideService.Verify(s => s.CreateAsync(It.IsAny<SlideDTO>()), Times.Never);
        }

        [Fact]
        public async Task Create_ServiceThrowsException_PropagatesException()
        {
            // Arrange
            var slideDto = new SlideDTO
            {
                SlideName = "Test Slide",
                SlideUrl = "https://example.com/image.jpg"
            };
            int userId = 1;

            _mockSlideService
                .Setup(s => s.CreateAsync(It.IsAny<SlideDTO>()))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            SetupUserClaims(userId);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _controller.Create(slideDto)
            );
            
            // Verify CreateBy was still set before exception
            Assert.Equal(userId, slideDto.CreateBy);
        }

        [Fact]
        public async Task Create_ServiceReturnsNegativeId_ReturnsCreatedAtActionWithNegativeId()
        {
            // Arrange
            var slideDto = new SlideDTO
            {
                SlideName = "Test Slide",
                SlideUrl = "https://example.com/image.jpg"
            };
            int expectedSlideId = -100;
            int userId = 1;

            _mockSlideService
                .Setup(s => s.CreateAsync(It.IsAny<SlideDTO>()))
                .ReturnsAsync(expectedSlideId);

            SetupUserClaims(userId);

            // Act
            var result = await _controller.Create(slideDto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(expectedSlideId, createdAtActionResult.Value);
            Assert.Equal(userId, slideDto.CreateBy);
        }

        [Fact]
        public async Task Create_DtoWithPresetCreateBy_OverridesWithUserId()
        {
            // Arrange
            var slideDto = new SlideDTO
            {
                SlideName = "Test Slide",
                SlideUrl = "https://example.com/image.jpg",
                CreateBy = 999 // Pre-set value that should be overridden
            };
            int expectedSlideId = 123;
            int userId = 1;

            _mockSlideService
                .Setup(s => s.CreateAsync(It.IsAny<SlideDTO>()))
                .ReturnsAsync(expectedSlideId);

            SetupUserClaims(userId);

            // Act
            var result = await _controller.Create(slideDto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(expectedSlideId, createdAtActionResult.Value);
            
            // Verify CreateBy was overridden with actual userId
            Assert.Equal(userId, slideDto.CreateBy);
            Assert.NotEqual(999, slideDto.CreateBy);
            
            _mockSlideService.Verify(s => s.CreateAsync(It.Is<SlideDTO>(dto => dto.CreateBy == userId)), Times.Once);
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
