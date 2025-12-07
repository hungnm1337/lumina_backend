using Xunit;
using Moq;
using ServiceLayer.MockTest;
using RepositoryLayer.MockTest;
using RepositoryLayer.Exam.ExamAttempt;
using RepositoryLayer;
using DataLayer.DTOs.Exam;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class GetMocktestAsyncUnitTest
    {
        private readonly Mock<IMockTestRepository> _mockMockTestRepository;
        private readonly Mock<IExamAttemptRepository> _mockExamAttemptRepository;
        private readonly Mock<IArticleRepository> _mockArticleRepository;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly MockTestService _service;

        public GetMocktestAsyncUnitTest()
        {
            _mockMockTestRepository = new Mock<IMockTestRepository>();
            _mockExamAttemptRepository = new Mock<IExamAttemptRepository>();
            _mockArticleRepository = new Mock<IArticleRepository>();
            _mockConfiguration = new Mock<IConfiguration>();

            // Setup configuration để tránh throw exception khi khởi tạo service
            var configurationSection = new Mock<IConfigurationSection>();
            configurationSection.Setup(x => x.Value).Returns("test-api-key");
            _mockConfiguration.Setup(x => x["GeminiAI:ApiKey"]).Returns("test-api-key");
            _mockConfiguration.Setup(x => x["Gemini:ApiKey"]).Returns("test-api-key");
            _mockConfiguration.Setup(x => x["GeminiAI:Model"]).Returns("gemini-2.5-flash");

            _service = new MockTestService(
                _mockMockTestRepository.Object,
                _mockExamAttemptRepository.Object,
                _mockArticleRepository.Object,
                _mockConfiguration.Object
            );
        }

        #region Test Cases - Repository Returns Data

        [Fact]
        public async Task GetMocktestAsync_WhenRepositoryReturnsData_ShouldReturnList()
        {
            // Arrange
            int[] expectedExamPartIds = new int[] { 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 };
            var expectedResult = new List<ExamPartDTO>
            {
                new ExamPartDTO
                {
                    PartId = 16,
                    ExamId = 1,
                    PartCode = "P1",
                    Title = "Part 1 - Reading",
                    OrderIndex = 1,
                    Questions = new List<QuestionDTO>()
                },
                new ExamPartDTO
                {
                    PartId = 17,
                    ExamId = 1,
                    PartCode = "P2",
                    Title = "Part 2 - Writing",
                    OrderIndex = 2,
                    Questions = new List<QuestionDTO>()
                },
                new ExamPartDTO
                {
                    PartId = 18,
                    ExamId = 1,
                    PartCode = "P3",
                    Title = "Part 3 - Listening",
                    OrderIndex = 3,
                    Questions = new List<QuestionDTO>()
                }
            };

            _mockMockTestRepository
                .Setup(repo => repo.GetMocktestAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetMocktestAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal(expectedResult[0].PartId, result[0].PartId);
            Assert.Equal(expectedResult[0].PartCode, result[0].PartCode);
            Assert.Equal(expectedResult[0].Title, result[0].Title);
            Assert.Equal(expectedResult[1].PartId, result[1].PartId);
            Assert.Equal(expectedResult[2].PartId, result[2].PartId);

            // Verify repository is called exactly once
            _mockMockTestRepository.Verify(
                repo => repo.GetMocktestAsync(),
                Times.Once
            );
        }

        [Fact]
        public async Task GetMocktestAsync_WhenRepositoryReturnsSingleItem_ShouldReturnList()
        {
            // Arrange
            int[] expectedExamPartIds = new int[] { 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 };
            var expectedResult = new List<ExamPartDTO>
            {
                new ExamPartDTO
                {
                    PartId = 16,
                    ExamId = 1,
                    PartCode = "P1",
                    Title = "Part 1",
                    OrderIndex = 1,
                    Questions = new List<QuestionDTO>()
                }
            };

            _mockMockTestRepository
                .Setup(repo => repo.GetMocktestAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetMocktestAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(expectedResult[0].PartId, result[0].PartId);
            Assert.Equal(expectedResult[0].PartCode, result[0].PartCode);

            // Verify repository is called exactly once
            _mockMockTestRepository.Verify(
                repo => repo.GetMocktestAsync(),
                Times.Once
            );
        }

        [Fact]
        public async Task GetMocktestAsync_WhenRepositoryReturnsDataWithQuestions_ShouldReturnList()
        {
            // Arrange
            int[] expectedExamPartIds = new int[] { 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 };
            var expectedResult = new List<ExamPartDTO>
            {
                new ExamPartDTO
                {
                    PartId = 16,
                    ExamId = 1,
                    PartCode = "P1",
                    Title = "Part 1 - Reading",
                    OrderIndex = 1,
                    Questions = new List<QuestionDTO>
                    {
                        new QuestionDTO
                        {
                            QuestionId = 1,
                            PartId = 16,
                            QuestionType = "MultipleChoice",
                            StemText = "Test Question",
                            QuestionNumber = 1,
                            ScoreWeight = 1,
                            Time = 60
                        }
                    }
                }
            };

            _mockMockTestRepository
                .Setup(repo => repo.GetMocktestAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetMocktestAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.NotNull(result[0].Questions);
            Assert.Single(result[0].Questions);
            Assert.Equal(expectedResult[0].Questions[0].QuestionId, result[0].Questions[0].QuestionId);

            // Verify repository is called exactly once
            _mockMockTestRepository.Verify(
                repo => repo.GetMocktestAsync(),
                Times.Once
            );
        }

        #endregion

        #region Test Cases - Repository Returns Empty

        [Fact]
        public async Task GetMocktestAsync_WhenRepositoryReturnsEmpty_ShouldReturnEmptyList()
        {
            // Arrange
            int[] expectedExamPartIds = new int[] { 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 };
            var expectedResult = new List<ExamPartDTO>();

            _mockMockTestRepository
                .Setup(repo => repo.GetMocktestAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetMocktestAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);

            // Verify repository is called exactly once
            _mockMockTestRepository.Verify(
                repo => repo.GetMocktestAsync(),
                Times.Once
            );
        }

        #endregion

        #region Test Cases - Repository Exception Handling

        [Fact]
        public async Task GetMocktestAsync_WhenRepositoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            int[] expectedExamPartIds = new int[] { 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 };
            var exceptionMessage = "Database connection error";

            _mockMockTestRepository
                .Setup(repo => repo.GetMocktestAsync())
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                async () => await _service.GetMocktestAsync()
            );

            Assert.Equal(exceptionMessage, exception.Message);

            // Verify repository is called exactly once
            _mockMockTestRepository.Verify(
                repo => repo.GetMocktestAsync(),
                Times.Once
            );
        }

        #endregion
    }
}

