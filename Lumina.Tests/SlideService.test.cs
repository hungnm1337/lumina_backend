using Xunit;
using Moq;
using ServiceLayer.Slide;
using RepositoryLayer.Slide;
using DataLayer.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lumina.Tests
{
    public class SlideServiceTests
    {
        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_ShouldCallRepository_WithNoFilters()
        {
            // Arrange
            var mockRepo = new Mock<ISlideRepository>();
            mockRepo.Setup(r => r.GetAllAsync(null, null))
                .ReturnsAsync(new List<SlideDTO>());
            var service = new SlideService(mockRepo.Object);

            // Act
            await service.GetAllAsync();

            // Assert
            mockRepo.Verify(r => r.GetAllAsync(null, null), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_ShouldCallRepository_WithKeyword()
        {
            // Arrange
            var mockRepo = new Mock<ISlideRepository>();
            mockRepo.Setup(r => r.GetAllAsync("banner", null))
                .ReturnsAsync(new List<SlideDTO>());
            var service = new SlideService(mockRepo.Object);

            // Act
            await service.GetAllAsync(keyword: "banner");

            // Assert
            mockRepo.Verify(r => r.GetAllAsync("banner", null), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_ShouldCallRepository_WithIsActive()
        {
            // Arrange
            var mockRepo = new Mock<ISlideRepository>();
            mockRepo.Setup(r => r.GetAllAsync(null, true))
                .ReturnsAsync(new List<SlideDTO>());
            var service = new SlideService(mockRepo.Object);

            // Act
            await service.GetAllAsync(isActive: true);

            // Assert
            mockRepo.Verify(r => r.GetAllAsync(null, true), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_ShouldCallRepository_WithAllFilters()
        {
            // Arrange
            var mockRepo = new Mock<ISlideRepository>();
            mockRepo.Setup(r => r.GetAllAsync("promo", false))
                .ReturnsAsync(new List<SlideDTO>());
            var service = new SlideService(mockRepo.Object);

            // Act
            await service.GetAllAsync(keyword: "promo", isActive: false);

            // Assert
            mockRepo.Verify(r => r.GetAllAsync("promo", false), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnSlides_FromRepository()
        {
            // Arrange
            var mockRepo = new Mock<ISlideRepository>();
            var expectedSlides = new List<SlideDTO>
            {
                new SlideDTO { SlideId = 1, SlideName = "Slide 1" },
                new SlideDTO { SlideId = 2, SlideName = "Slide 2" }
            };
            mockRepo.Setup(r => r.GetAllAsync(null, null))
                .ReturnsAsync(expectedSlides);
            var service = new SlideService(mockRepo.Object);

            // Act
            var result = await service.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Slide 1", result[0].SlideName);
            Assert.Equal("Slide 2", result[1].SlideName);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_ShouldCallRepository_WithCorrectId()
        {
            // Arrange
            var mockRepo = new Mock<ISlideRepository>();
            mockRepo.Setup(r => r.GetByIdAsync(5))
                .ReturnsAsync(new SlideDTO { SlideId = 5, SlideName = "Test Slide" });
            var service = new SlideService(mockRepo.Object);

            // Act
            await service.GetByIdAsync(5);

            // Assert
            mockRepo.Verify(r => r.GetByIdAsync(5), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnSlide_WhenExists()
        {
            // Arrange
            var mockRepo = new Mock<ISlideRepository>();
            var expectedSlide = new SlideDTO
            {
                SlideId = 10,
                SlideName = "Banner Slide",
                SlideUrl = "http://example.com/banner.jpg",
                IsActive = true
            };
            mockRepo.Setup(r => r.GetByIdAsync(10))
                .ReturnsAsync(expectedSlide);
            var service = new SlideService(mockRepo.Object);

            // Act
            var result = await service.GetByIdAsync(10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.SlideId);
            Assert.Equal("Banner Slide", result.SlideName);
            Assert.Equal("http://example.com/banner.jpg", result.SlideUrl);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
        {
            // Arrange
            var mockRepo = new Mock<ISlideRepository>();
            mockRepo.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((SlideDTO?)null);
            var service = new SlideService(mockRepo.Object);

            // Act
            var result = await service.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_ShouldCallRepository_WithCorrectData()
        {
            // Arrange
            var mockRepo = new Mock<ISlideRepository>();
            mockRepo.Setup(r => r.CreateAsync(It.IsAny<SlideDTO>()))
                .ReturnsAsync(1);
            var service = new SlideService(mockRepo.Object);

            var dto = new SlideDTO
            {
                SlideName = "New Slide",
                SlideUrl = "http://example.com/new.jpg",
                IsActive = true,
                CreateBy = 10
            };

            // Act
            await service.CreateAsync(dto);

            // Assert
            mockRepo.Verify(r => r.CreateAsync(It.Is<SlideDTO>(s =>
                s.SlideName == "New Slide" &&
                s.SlideUrl == "http://example.com/new.jpg" &&
                s.IsActive == true &&
                s.CreateBy == 10
            )), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnGeneratedId()
        {
            // Arrange
            var mockRepo = new Mock<ISlideRepository>();
            mockRepo.Setup(r => r.CreateAsync(It.IsAny<SlideDTO>()))
                .ReturnsAsync(42);
            var service = new SlideService(mockRepo.Object);

            var dto = new SlideDTO
            {
                SlideName = "Test Slide",
                SlideUrl = "http://example.com/test.jpg",
                IsActive = true,
                CreateBy = 1
            };

            // Act
            var result = await service.CreateAsync(dto);

            // Assert
            Assert.Equal(42, result);
        }

        [Fact]
        public async Task CreateAsync_ShouldHandleInactiveSlide()
        {
            // Arrange
            var mockRepo = new Mock<ISlideRepository>();
            mockRepo.Setup(r => r.CreateAsync(It.IsAny<SlideDTO>()))
                .ReturnsAsync(1);
            var service = new SlideService(mockRepo.Object);

            var dto = new SlideDTO
            {
                SlideName = "Inactive Slide",
                SlideUrl = "http://example.com/inactive.jpg",
                IsActive = false,
                CreateBy = 1
            };

            // Act
            await service.CreateAsync(dto);

            // Assert
            mockRepo.Verify(r => r.CreateAsync(It.Is<SlideDTO>(s => s.IsActive == false)), Times.Once);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_ShouldCallRepository_WithCorrectData()
        {
            // Arrange
            var mockRepo = new Mock<ISlideRepository>();
            mockRepo.Setup(r => r.UpdateAsync(It.IsAny<SlideDTO>()))
                .ReturnsAsync(true);
            var service = new SlideService(mockRepo.Object);

            var dto = new SlideDTO
            {
                SlideId = 5,
                SlideName = "Updated Slide",
                SlideUrl = "http://example.com/updated.jpg",
                IsActive = false,
                UpdateBy = 20
            };

            // Act
            await service.UpdateAsync(dto);

            // Assert
            mockRepo.Verify(r => r.UpdateAsync(It.Is<SlideDTO>(s =>
                s.SlideId == 5 &&
                s.SlideName == "Updated Slide" &&
                s.SlideUrl == "http://example.com/updated.jpg" &&
                s.IsActive == false &&
                s.UpdateBy == 20
            )), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnTrue_WhenSuccessful()
        {
            // Arrange
            var mockRepo = new Mock<ISlideRepository>();
            mockRepo.Setup(r => r.UpdateAsync(It.IsAny<SlideDTO>()))
                .ReturnsAsync(true);
            var service = new SlideService(mockRepo.Object);

            var dto = new SlideDTO
            {
                SlideId = 1,
                SlideName = "Updated",
                SlideUrl = "http://example.com/updated.jpg",
                IsActive = true
            };

            // Act
            var result = await service.UpdateAsync(dto);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnFalse_WhenFailed()
        {
            // Arrange
            var mockRepo = new Mock<ISlideRepository>();
            mockRepo.Setup(r => r.UpdateAsync(It.IsAny<SlideDTO>()))
                .ReturnsAsync(false);
            var service = new SlideService(mockRepo.Object);

            var dto = new SlideDTO
            {
                SlideId = 999,
                SlideName = "Non-existent",
                SlideUrl = "http://example.com/ghost.jpg",
                IsActive = true
            };

            // Act
            var result = await service.UpdateAsync(dto);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateAsync_ShouldHandleToggleIsActive()
        {
            // Arrange
            var mockRepo = new Mock<ISlideRepository>();
            mockRepo.Setup(r => r.UpdateAsync(It.IsAny<SlideDTO>()))
                .ReturnsAsync(true);
            var service = new SlideService(mockRepo.Object);

            var dto = new SlideDTO
            {
                SlideId = 1,
                SlideName = "Slide",
                SlideUrl = "http://example.com/slide.jpg",
                IsActive = false
            };

            // Act
            await service.UpdateAsync(dto);

            // Assert
            mockRepo.Verify(r => r.UpdateAsync(It.Is<SlideDTO>(s => s.IsActive == false)), Times.Once);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_ShouldCallRepository_WithCorrectId()
        {
            // Arrange
            var mockRepo = new Mock<ISlideRepository>();
            mockRepo.Setup(r => r.DeleteAsync(3))
                .ReturnsAsync(true);
            var service = new SlideService(mockRepo.Object);

            // Act
            await service.DeleteAsync(3);

            // Assert
            mockRepo.Verify(r => r.DeleteAsync(3), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnTrue_WhenSuccessful()
        {
            // Arrange
            var mockRepo = new Mock<ISlideRepository>();
            mockRepo.Setup(r => r.DeleteAsync(1))
                .ReturnsAsync(true);
            var service = new SlideService(mockRepo.Object);

            // Act
            var result = await service.DeleteAsync(1);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnFalse_WhenFailed()
        {
            // Arrange
            var mockRepo = new Mock<ISlideRepository>();
            mockRepo.Setup(r => r.DeleteAsync(999))
                .ReturnsAsync(false);
            var service = new SlideService(mockRepo.Object);

            // Act
            var result = await service.DeleteAsync(999);

            // Assert
            Assert.False(result);
        }

        #endregion
    }
}
