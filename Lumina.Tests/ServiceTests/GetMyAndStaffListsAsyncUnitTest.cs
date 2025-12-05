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
    public class GetMyAndStaffListsAsyncUnitTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IVocabularyListRepository> _mockVocabularyListRepository;
        private readonly VocabularyListService _service;

        public GetMyAndStaffListsAsyncUnitTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockVocabularyListRepository = new Mock<IVocabularyListRepository>();

            _mockUnitOfWork.Setup(u => u.VocabularyLists).Returns(_mockVocabularyListRepository.Object);

            _service = new VocabularyListService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task GetMyAndStaffListsAsync_WhenInputIsValid_ShouldReturnLists()
        {
            // Arrange
            var expectedLists = new List<VocabularyListDTO>
            {
                new VocabularyListDTO { VocabularyListId = 1, Name = "My List" },
                new VocabularyListDTO { VocabularyListId = 2, Name = "Staff List" }
            };

            _mockVocabularyListRepository
                .Setup(repo => repo.GetMyAndStaffListsAsync(1, "test"))
                .ReturnsAsync(expectedLists);

            // Act
            var result = await _service.GetMyAndStaffListsAsync(1, "test");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }
    }
}













