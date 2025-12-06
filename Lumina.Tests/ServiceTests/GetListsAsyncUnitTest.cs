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
    public class GetListsAsyncUnitTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IVocabularyListRepository> _mockVocabularyListRepository;
        private readonly VocabularyListService _service;

        public GetListsAsyncUnitTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockVocabularyListRepository = new Mock<IVocabularyListRepository>();

            _mockUnitOfWork.Setup(u => u.VocabularyLists).Returns(_mockVocabularyListRepository.Object);

            _service = new VocabularyListService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task GetListsAsync_WhenSearchTermIsNull_ShouldReturnAllLists()
        {
            // Arrange
            var expectedLists = new List<VocabularyListDTO>
            {
                new VocabularyListDTO { VocabularyListId = 1, Name = "List 1" },
                new VocabularyListDTO { VocabularyListId = 2, Name = "List 2" }
            };

            _mockVocabularyListRepository
                .Setup(repo => repo.GetAllAsync(null))
                .ReturnsAsync(expectedLists);

            // Act
            var result = await _service.GetListsAsync(null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetListsAsync_WhenSearchTermHasValue_ShouldReturnFilteredLists()
        {
            // Arrange
            var expectedLists = new List<VocabularyListDTO>
            {
                new VocabularyListDTO { VocabularyListId = 1, Name = "Test List" }
            };

            _mockVocabularyListRepository
                .Setup(repo => repo.GetAllAsync("Test"))
                .ReturnsAsync(expectedLists);

            // Act
            var result = await _service.GetListsAsync("Test");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }
    }
}













