using Xunit;
using Moq;
using ServiceLayer.Vocabulary;
using RepositoryLayer.UnitOfWork;
using DataLayer.DTOs.Vocabulary;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class GetPublishedListsAsyncUnitTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IVocabularyListRepository> _mockVocabularyListRepository;
        private readonly VocabularyListService _service;

        public GetPublishedListsAsyncUnitTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockVocabularyListRepository = new Mock<IVocabularyListRepository>();

            _mockUnitOfWork.Setup(u => u.VocabularyLists).Returns(_mockVocabularyListRepository.Object);

            _service = new VocabularyListService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task GetPublishedListsAsync_WhenSearchTermIsNull_ShouldReturnPublishedLists()
        {
            // Arrange
            var expectedLists = new List<VocabularyListDTO>
            {
                new VocabularyListDTO { VocabularyListId = 1, Name = "Published List", Status = "Published" }
            };

            _mockVocabularyListRepository
                .Setup(repo => repo.GetPublishedAsync(null))
                .ReturnsAsync(expectedLists);

            // Act
            var result = await _service.GetPublishedListsAsync(null);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetPublishedListsAsync_WhenSearchTermHasValue_ShouldReturnFilteredLists()
        {
            // Arrange
            var expectedLists = new List<VocabularyListDTO>
            {
                new VocabularyListDTO { VocabularyListId = 1, Name = "Test Published", Status = "Published" }
            };

            _mockVocabularyListRepository
                .Setup(repo => repo.GetPublishedAsync("Test"))
                .ReturnsAsync(expectedLists);

            // Act
            var result = await _service.GetPublishedListsAsync("Test");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }
    }
}

