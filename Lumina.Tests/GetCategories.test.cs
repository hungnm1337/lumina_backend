using DataLayer.Models;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RepositoryLayer;
using RepositoryLayer.UnitOfWork;
using ServiceLayer.Article;

namespace Lumina.Tests
{
    public class GetCategoriesTests
    {
        private readonly Mock<IArticleService> _mockArticleService;
        private readonly Mock<ILogger<ArticlesController>> _mockLogger;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ICategoryRepository> _mockCategoryRepository;
        private readonly ArticlesController _controller;

        public GetCategoriesTests()
        {
            _mockArticleService = new Mock<IArticleService>();
            _mockLogger = new Mock<ILogger<ArticlesController>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCategoryRepository = new Mock<ICategoryRepository>();
            _mockUnitOfWork.Setup(u => u.Categories).Returns(_mockCategoryRepository.Object);
            _controller = new ArticlesController(_mockArticleService.Object, _mockLogger.Object, _mockUnitOfWork.Object);
        }

        #region Happy Path Tests

        [Fact]
        public async Task GetCategories_ShouldReturn200OkWithCategories()
        {
            // Arrange
            var expectedCategories = new List<ArticleCategory>
            {
                new ArticleCategory
                {
                    CategoryId = 1,
                    CategoryName = "Technology",
                    Description = "Technology articles"
                },
                new ArticleCategory
                {
                    CategoryId = 2,
                    CategoryName = "Education",
                    Description = "Education articles"
                }
            };

            _mockCategoryRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(expectedCategories);

            // Act
            var result = await _controller.GetCategories();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.NotNull(okResult.Value);
            
            // Check that the result is an enumerable of anonymous objects with id and name properties
            var resultType = okResult.Value.GetType();
            Assert.True(resultType.IsGenericType);
            var enumerable = okResult.Value as System.Collections.IEnumerable;
            Assert.NotNull(enumerable);
            
            var categoriesList = new List<object>();
            foreach (var item in enumerable)
            {
                categoriesList.Add(item);
                // Verify each item has id and name properties
                var itemType = item.GetType();
                var idProperty = itemType.GetProperty("id");
                var nameProperty = itemType.GetProperty("name");
                Assert.NotNull(idProperty);
                Assert.NotNull(nameProperty);
            }
            
            Assert.Equal(2, categoriesList.Count);
            _mockCategoryRepository.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetCategories_WithNoCategories_ShouldReturn200OkWithEmptyList()
        {
            // Arrange
            var emptyCategories = new List<ArticleCategory>();
            _mockCategoryRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(emptyCategories);

            // Act
            var result = await _controller.GetCategories();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.NotNull(okResult.Value);
            
            var enumerable = okResult.Value as System.Collections.IEnumerable;
            Assert.NotNull(enumerable);
            
            var categoriesList = new List<object>();
            foreach (var item in enumerable)
            {
                categoriesList.Add(item);
            }
            
            Assert.Empty(categoriesList);
            _mockCategoryRepository.Verify(r => r.GetAllAsync(), Times.Once);
        }

        #endregion
    }
}
