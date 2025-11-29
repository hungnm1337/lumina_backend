using Xunit;
using Moq;
using ServiceLayer.Vocabulary;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer.UserSpacedRepetition;
using DataLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class GetQuizScoresAsyncUnitTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IUserSpacedRepetitionRepository> _mockRepetitionRepository;
        private readonly SpacedRepetitionService _service;

        public GetQuizScoresAsyncUnitTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockRepetitionRepository = new Mock<IUserSpacedRepetitionRepository>();

            _mockUnitOfWork.Setup(u => u.UserSpacedRepetitions).Returns(_mockRepetitionRepository.Object);

            _service = new SpacedRepetitionService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task GetQuizScoresAsync_WhenVocabularyListIdIsNull_ShouldReturnAllScores()
        {
            // Arrange
            var items = new List<UserSpacedRepetition>
            {
                new UserSpacedRepetition
                {
                    VocabularyListId = 1,
                    BestQuizScore = 80,
                    LastQuizScore = 75,
                    VocabularyList = new VocabularyList { Name = "List 1" }
                },
                new UserSpacedRepetition
                {
                    VocabularyListId = 2,
                    BestQuizScore = 90,
                    LastQuizScore = 85,
                    VocabularyList = new VocabularyList { Name = "List 2" }
                }
            };

            _mockRepetitionRepository
                .Setup(repo => repo.GetByUserIdAsync(1))
                .ReturnsAsync(items);

            // Act
            var result = await _service.GetQuizScoresAsync(1, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetQuizScoresAsync_WhenVocabularyListIdHasValue_ShouldReturnSpecificScore()
        {
            // Arrange
            var item = new UserSpacedRepetition
            {
                VocabularyListId = 1,
                BestQuizScore = 80,
                LastQuizScore = 75,
                VocabularyList = new VocabularyList { Name = "List 1" }
            };

            _mockRepetitionRepository
                .Setup(repo => repo.GetByUserAndListAsync(1, 1))
                .ReturnsAsync(item);

            // Act
            var result = await _service.GetQuizScoresAsync(1, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetQuizScoresAsync_WhenItemNotFound_ShouldReturnEmpty()
        {
            // Arrange
            _mockRepetitionRepository
                .Setup(repo => repo.GetByUserAndListAsync(1, 1))
                .ReturnsAsync((UserSpacedRepetition?)null);

            // Act
            var result = await _service.GetQuizScoresAsync(1, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetQuizScoresAsync_WhenNoQuizScores_ShouldFilterOutItems()
        {
            // Arrange
            var items = new List<UserSpacedRepetition>
            {
                new UserSpacedRepetition
                {
                    VocabularyListId = 1,
                    BestQuizScore = null,
                    LastQuizScore = null,
                    VocabularyList = new VocabularyList { Name = "List 1" }
                }
            };

            _mockRepetitionRepository
                .Setup(repo => repo.GetByUserIdAsync(1))
                .ReturnsAsync(items);

            // Act
            var result = await _service.GetQuizScoresAsync(1, null);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}

