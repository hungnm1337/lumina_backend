using DataLayer.DTOs.Vocabulary;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RepositoryLayer.UnitOfWork;
using ServiceLayer.Vocabulary;
using System.Collections.Generic;
using Xunit;

namespace Lumina.Tests
{
    public class GetPublicVocabularyListsVocabularyListsControllerTests
    {
        private readonly Mock<IVocabularyListService> _mockVocabularyListService;
        private readonly Mock<ILogger<VocabularyListsController>> _mockLogger;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly VocabularyListsController _controller;

        public GetPublicVocabularyListsVocabularyListsControllerTests()
        {
            _mockVocabularyListService = new Mock<IVocabularyListService>();
            _mockLogger = new Mock<ILogger<VocabularyListsController>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _controller = new VocabularyListsController(_mockVocabularyListService.Object, _mockLogger.Object, _mockUnitOfWork.Object);
        }

        [Fact]
        public async Task GetPublicVocabularyLists_WithNullSearchTerm_ShouldReturnPublishedLists()
        {
            // Arrange
            var lists = new List<VocabularyListDTO>
            {
                new VocabularyListDTO { VocabularyListId = 1, Name = "Public List", Status = "Published" }
            };

            _mockVocabularyListService
                .Setup(s => s.GetPublishedListsAsync(null))
                .ReturnsAsync(lists);

            // Act
            var result = await _controller.GetPublicVocabularyLists(null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultLists = Assert.IsAssignableFrom<IEnumerable<VocabularyListDTO>>(okResult.Value);
            Assert.Single(resultLists);
            _mockVocabularyListService.Verify(s => s.GetPublishedListsAsync(null), Times.Once);
        }

        [Fact]
        public async Task GetPublicVocabularyLists_WithSearchTerm_ShouldReturnFilteredLists()
        {
            // Arrange
            var searchTerm = "test";
            var lists = new List<VocabularyListDTO>
            {
                new VocabularyListDTO { VocabularyListId = 1, Name = "Test List", Status = "Published" }
            };

            _mockVocabularyListService
                .Setup(s => s.GetPublishedListsAsync(searchTerm))
                .ReturnsAsync(lists);

            // Act
            var result = await _controller.GetPublicVocabularyLists(searchTerm);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            _mockVocabularyListService.Verify(s => s.GetPublishedListsAsync(searchTerm), Times.Once);
        }

        [Fact]
        public async Task GetPublicVocabularyLists_WithException_ShouldReturnInternalServerError()
        {
            // Arrange
            _mockVocabularyListService
                .Setup(s => s.GetPublishedListsAsync(null))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetPublicVocabularyLists(null);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }
    }
}











