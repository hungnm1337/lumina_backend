using Xunit;
using Moq;
using ServiceLayer.Vocabulary;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer.UserSpacedRepetition;
using DataLayer.DTOs.Vocabulary;
using DataLayer.Models;
using System;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class SaveQuizResultAsyncUnitTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IUserSpacedRepetitionRepository> _mockRepetitionRepository;
        private readonly Mock<IVocabularyListRepository> _mockVocabularyListRepository;
        private readonly SpacedRepetitionService _service;

        public SaveQuizResultAsyncUnitTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockRepetitionRepository = new Mock<IUserSpacedRepetitionRepository>();
            _mockVocabularyListRepository = new Mock<IVocabularyListRepository>();

            _mockUnitOfWork.Setup(u => u.UserSpacedRepetitions).Returns(_mockRepetitionRepository.Object);
            _mockUnitOfWork.Setup(u => u.VocabularyLists).Returns(_mockVocabularyListRepository.Object);

            _service = new SpacedRepetitionService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task SaveQuizResultAsync_WhenRepetitionExists_ShouldUpdateAndReturnTrue()
        {
            // Arrange
            var repetition = new UserSpacedRepetition
            {
                UserSpacedRepetitionId = 1,
                UserId = 1,
                VocabularyListId = 1,
                BestQuizScore = 70,
                TotalQuizAttempts = 1
            };

            var request = new SaveQuizResultRequestDTO
            {
                VocabularyListId = 1,
                Score = 80
            };

            _mockRepetitionRepository
                .Setup(repo => repo.GetByUserAndListAsync(1, 1))
                .ReturnsAsync(repetition);

            _mockRepetitionRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<UserSpacedRepetition>()))
                .ReturnsAsync((UserSpacedRepetition r) => r);

            _mockUnitOfWork
                .Setup(u => u.CompleteAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.SaveQuizResultAsync(1, request);

            // Assert
            Assert.True(result);
            Assert.Equal(80, repetition.BestQuizScore);
            Assert.Equal(80, repetition.LastQuizScore);
            Assert.Equal(2, repetition.TotalQuizAttempts);
        }

        [Fact]
        public async Task SaveQuizResultAsync_WhenRepetitionNotExists_ShouldCreateAndReturnTrue()
        {
            // Arrange
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = 1,
                Name = "Test List"
            };

            var request = new SaveQuizResultRequestDTO
            {
                VocabularyListId = 1,
                Score = 75
            };

            _mockRepetitionRepository
                .Setup(repo => repo.GetByUserAndListAsync(1, 1))
                .ReturnsAsync((UserSpacedRepetition?)null);

            _mockVocabularyListRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(vocabularyList);

            _mockRepetitionRepository
                .Setup(repo => repo.AddAsync(It.IsAny<UserSpacedRepetition>()))
                .ReturnsAsync((UserSpacedRepetition r) => r);

            _mockRepetitionRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<UserSpacedRepetition>()))
                .ReturnsAsync((UserSpacedRepetition r) => r);

            _mockUnitOfWork
                .Setup(u => u.CompleteAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.SaveQuizResultAsync(1, request);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task SaveQuizResultAsync_WhenVocabularyListNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var request = new SaveQuizResultRequestDTO
            {
                VocabularyListId = 1,
                Score = 75
            };

            _mockRepetitionRepository
                .Setup(repo => repo.GetByUserAndListAsync(1, 1))
                .ReturnsAsync((UserSpacedRepetition?)null);

            _mockVocabularyListRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync((VocabularyList?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                async () => await _service.SaveQuizResultAsync(1, request)
            );
        }
    }
}

