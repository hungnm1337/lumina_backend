using Xunit;
using Moq;
using ServiceLayer.Exam;
using RepositoryLayer.Exam;
using DataLayer.DTOs.Exam;
using DataLayer.Models;
using Lumina.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class GetExamPartDetailAndQuestionByExamPartIDUnitTest
    {
        private readonly Mock<IExamRepository> _mockExamRepository;
        private readonly LuminaSystemContext _context;
        private readonly ExamService _service;

        public GetExamPartDetailAndQuestionByExamPartIDUnitTest()
        {
            _mockExamRepository = new Mock<IExamRepository>();
            _context = InMemoryDbContextHelper.CreateContext();

            _service = new ExamService(
                _mockExamRepository.Object,
                _context
            );
        }

        #region Test Cases for Invalid PartId

        [Fact]
        public async Task GetExamPartDetailAndQuestionByExamPartID_WhenPartIdIsNegative_ShouldThrowArgumentException()
        {
            // Arrange
            int partId = -1;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.GetExamPartDetailAndQuestionByExamPartID(partId)
            );

            Assert.Equal("partId", exception.ParamName);
            Assert.Contains("Part ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockExamRepository.Verify(
                repo => repo.GetExamPartDetailAndQuestionByExamPartID(It.IsAny<int>()),
                Times.Never
            );
        }

        [Fact]
        public async Task GetExamPartDetailAndQuestionByExamPartID_WhenPartIdIsZero_ShouldThrowArgumentException()
        {
            // Arrange
            int partId = 0;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.GetExamPartDetailAndQuestionByExamPartID(partId)
            );

            Assert.Equal("partId", exception.ParamName);
            Assert.Contains("Part ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockExamRepository.Verify(
                repo => repo.GetExamPartDetailAndQuestionByExamPartID(It.IsAny<int>()),
                Times.Never
            );
        }

        #endregion

        #region Test Cases for Valid PartId

        [Fact]
        public async Task GetExamPartDetailAndQuestionByExamPartID_WhenPartIdIsValidAndExists_ShouldReturnExamPartDTO()
        {
            // Arrange
            int partId = 1;
            var expectedResult = new ExamPartDTO
            {
                PartId = 1,
                ExamId = 1,
                PartCode = "P1",
                Title = "Part 1 - Reading",
                OrderIndex = 1,
                Questions = new List<QuestionDTO>
                {
                    new QuestionDTO
                    {
                        QuestionId = 1,
                        PartId = 1,
                        PartCode = "P1",
                        QuestionType = "MultipleChoice",
                        StemText = "What is the main idea?",
                        ScoreWeight = 1,
                        Time = 60,
                        QuestionNumber = 1,
                        Prompt = new PromptDTO
                        {
                            PromptId = 1,
                            Skill = "Reading",
                            ContentText = "Read the passage",
                            Title = "Reading Passage"
                        },
                        Options = new List<OptionDTO>
                        {
                            new OptionDTO
                            {
                                OptionId = 1,
                                QuestionId = 1,
                                Content = "Option A",
                                IsCorrect = true
                            },
                            new OptionDTO
                            {
                                OptionId = 2,
                                QuestionId = 1,
                                Content = "Option B",
                                IsCorrect = false
                            }
                        }
                    },
                    new QuestionDTO
                    {
                        QuestionId = 2,
                        PartId = 1,
                        PartCode = "P1",
                        QuestionType = "MultipleChoice",
                        StemText = "What is the author's purpose?",
                        ScoreWeight = 1,
                        Time = 60,
                        QuestionNumber = 2,
                        Prompt = new PromptDTO
                        {
                            PromptId = 1,
                            Skill = "Reading",
                            ContentText = "Read the passage",
                            Title = "Reading Passage"
                        },
                        Options = new List<OptionDTO>()
                    }
                }
            };

            _mockExamRepository
                .Setup(repo => repo.GetExamPartDetailAndQuestionByExamPartID(partId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetExamPartDetailAndQuestionByExamPartID(partId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult.PartId, result.PartId);
            Assert.Equal(expectedResult.PartCode, result.PartCode);
            Assert.Equal(expectedResult.Title, result.Title);
            Assert.NotNull(result.Questions);
            Assert.Equal(2, result.Questions.Count);
            Assert.Equal(expectedResult.Questions[0].QuestionId, result.Questions[0].QuestionId);
            Assert.Equal(expectedResult.Questions[0].StemText, result.Questions[0].StemText);
            Assert.NotNull(result.Questions[0].Options);
            Assert.Equal(2, result.Questions[0].Options.Count);

            // Verify repository is called exactly once with correct partId
            _mockExamRepository.Verify(
                repo => repo.GetExamPartDetailAndQuestionByExamPartID(partId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetExamPartDetailAndQuestionByExamPartID_WhenPartIdIsValidButNotFound_ShouldReturnNull()
        {
            // Arrange
            int partId = 999;

            _mockExamRepository
                .Setup(repo => repo.GetExamPartDetailAndQuestionByExamPartID(partId))
                .ReturnsAsync((ExamPartDTO?)null);

            // Act
            var result = await _service.GetExamPartDetailAndQuestionByExamPartID(partId);

            // Assert
            Assert.Null(result);

            // Verify repository is called exactly once
            _mockExamRepository.Verify(
                repo => repo.GetExamPartDetailAndQuestionByExamPartID(partId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetExamPartDetailAndQuestionByExamPartID_WhenPartIdIsValidWithNoQuestions_ShouldReturnExamPartDTO()
        {
            // Arrange
            int partId = 2;
            var expectedResult = new ExamPartDTO
            {
                PartId = 2,
                ExamId = 1,
                PartCode = "P2",
                Title = "Part 2 - Writing",
                OrderIndex = 2,
                Questions = new List<QuestionDTO>()
            };

            _mockExamRepository
                .Setup(repo => repo.GetExamPartDetailAndQuestionByExamPartID(partId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetExamPartDetailAndQuestionByExamPartID(partId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult.PartId, result.PartId);
            Assert.Equal(expectedResult.PartCode, result.PartCode);
            Assert.NotNull(result.Questions);
            Assert.Empty(result.Questions);

            // Verify repository is called exactly once
            _mockExamRepository.Verify(
                repo => repo.GetExamPartDetailAndQuestionByExamPartID(partId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetExamPartDetailAndQuestionByExamPartID_WhenPartIdIsLarge_ShouldReturnExamPartDTO()
        {
            // Arrange
            int partId = int.MaxValue;
            var expectedResult = new ExamPartDTO
            {
                PartId = int.MaxValue,
                ExamId = 1,
                PartCode = "P999",
                Title = "Large ID Part",
                OrderIndex = 999,
                Questions = null
            };

            _mockExamRepository
                .Setup(repo => repo.GetExamPartDetailAndQuestionByExamPartID(partId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetExamPartDetailAndQuestionByExamPartID(partId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult.PartId, result.PartId);
            Assert.Equal(expectedResult.Title, result.Title);

            // Verify repository is called exactly once
            _mockExamRepository.Verify(
                repo => repo.GetExamPartDetailAndQuestionByExamPartID(partId),
                Times.Once
            );
        }

        #endregion

        #region Test Cases - Repository Exception Handling

        [Fact]
        public async Task GetExamPartDetailAndQuestionByExamPartID_WhenRepositoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            int partId = 1;
            var exceptionMessage = "Database connection error";

            _mockExamRepository
                .Setup(repo => repo.GetExamPartDetailAndQuestionByExamPartID(partId))
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                async () => await _service.GetExamPartDetailAndQuestionByExamPartID(partId)
            );

            Assert.Equal(exceptionMessage, exception.Message);

            // Verify repository is called exactly once
            _mockExamRepository.Verify(
                repo => repo.GetExamPartDetailAndQuestionByExamPartID(partId),
                Times.Once
            );
        }

        #endregion
    }
}

