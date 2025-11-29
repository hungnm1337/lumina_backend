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
    public class GetListsByUserAsyncUnitTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IVocabularyListRepository> _mockVocabularyListRepository;
        private readonly VocabularyListService _service;

        public GetListsByUserAsyncUnitTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockVocabularyListRepository = new Mock<IVocabularyListRepository>();

            _mockUnitOfWork.Setup(u => u.VocabularyLists).Returns(_mockVocabularyListRepository.Object);

            _service = new VocabularyListService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task GetListsByUserAsync_WhenInputIsValid_ShouldReturnUserLists()
        {
            // Arrange
            var expectedLists = new List<VocabularyListDTO>
            {
                new VocabularyListDTO { VocabularyListId = 1, Name = "User List 1" },
                new VocabularyListDTO { VocabularyListId = 2, Name = "User List 2" }
            };

            _mockVocabularyListRepository
                .Setup(repo => repo.GetByUserAsync(1, "test"))
                .ReturnsAsync(expectedLists);

            // Act
            var result = await _service.GetListsByUserAsync(1, "test");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }
    }
}

