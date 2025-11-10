using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RepositoryLayer.UnitOfWork;
using ServiceLayer.TextToSpeech;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Xunit;

namespace Lumina.Tests
{
    public class GetCategoriesVocabulariesControllerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IVocabularyRepository> _mockVocabularyRepository;
        private readonly Mock<ITextToSpeechService> _mockTtsService;
        private readonly VocabulariesController _controller;

        public GetCategoriesVocabulariesControllerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockVocabularyRepository = new Mock<IVocabularyRepository>();
            _mockTtsService = new Mock<ITextToSpeechService>();
            _mockUnitOfWork.Setup(u => u.Vocabularies).Returns(_mockVocabularyRepository.Object);
            _controller = new VocabulariesController(_mockUnitOfWork.Object, _mockTtsService.Object);
        }

        private void SetupUserClaims(int userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, "Staff")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        [Fact]
        public async Task GetCategories_WithValidStaffUser_ShouldReturnCategories()
        {
            // Arrange
            var userId = 1;
            var categories = new List<string> { "greeting", "action", "general" };

            SetupUserClaims(userId);
            _mockVocabularyRepository
                .Setup(r => r.GetDistinctCategoriesAsync())
                .ReturnsAsync(categories);

            // Act
            var result = await _controller.GetCategories();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            var resultCategories = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value);
            Assert.Equal(3, resultCategories.Count());
            Assert.Contains("greeting", resultCategories);
            Assert.Contains("action", resultCategories);
            Assert.Contains("general", resultCategories);
            _mockVocabularyRepository.Verify(r => r.GetDistinctCategoriesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetCategories_WithEmptyCategories_ShouldReturnEmptyList()
        {
            // Arrange
            var userId = 1;
            var categories = new List<string>();

            SetupUserClaims(userId);
            _mockVocabularyRepository
                .Setup(r => r.GetDistinctCategoriesAsync())
                .ReturnsAsync(categories);

            // Act
            var result = await _controller.GetCategories();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            var resultCategories = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value);
            Assert.Empty(resultCategories);
            _mockVocabularyRepository.Verify(r => r.GetDistinctCategoriesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetCategories_WithMultipleCategories_ShouldReturnAllCategories()
        {
            // Arrange
            var userId = 1;
            var categories = new List<string> 
            { 
                "greeting", 
                "action", 
                "general", 
                "food", 
                "travel" 
            };

            SetupUserClaims(userId);
            _mockVocabularyRepository
                .Setup(r => r.GetDistinctCategoriesAsync())
                .ReturnsAsync(categories);

            // Act
            var result = await _controller.GetCategories();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultCategories = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value);
            Assert.Equal(5, resultCategories.Count());
            Assert.All(categories, cat => Assert.Contains(cat, resultCategories));
        }
    }
}

