using Xunit;
using Moq;
using ServiceLayer.Exam.Reading;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer.Generic;
using RepositoryLayer.User;
using DataLayer.DTOs.UserAnswer;
using DataLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Lumina.Test.Services
{
    /// <summary>
    /// Unit tests for SubmitAnswerAsync method in ReadingService
    /// Test cases based on test matrix: URC01 - URC05
    /// </summary>
    public class SubmitAnswerAsyncUnitTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IRepository<ExamAttempt>> _mockExamAttemptsGeneric;
        private readonly Mock<IRepository<Question>> _mockQuestionsGeneric;
        private readonly Mock<IUserAnswerRepository> _mockUserAnswers;
        private readonly ReadingService _service;

        public SubmitAnswerAsyncUnitTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockExamAttemptsGeneric = new Mock<IRepository<ExamAttempt>>();
            _mockQuestionsGeneric = new Mock<IRepository<Question>>();
            _mockUserAnswers = new Mock<IUserAnswerRepository>();

            _mockUnitOfWork.Setup(u => u.ExamAttemptsGeneric).Returns(_mockExamAttemptsGeneric.Object);
            _mockUnitOfWork.Setup(u => u.QuestionsGeneric).Returns(_mockQuestionsGeneric.Object);
            _mockUnitOfWork.Setup(u => u.UserAnswers).Returns(_mockUserAnswers.Object);

            _service = new ReadingService(_mockUnitOfWork.Object);
        }

        #region URC01 - Happy Path (Valid Submission with Correct Answer)

        /// <summary>
        /// Test Case: URC01
        /// Precondition: Can connect with server, Database available
        /// Input: ExamAttemptId = 1 (valid), QuestionId = 1 (valid), SelectedOptionId = 1 (valid, correct)
        /// Expected: Success = true, IsCorrect = true, Score = 10
        /// </summary>
        [Fact]
        public async Task URC01_ValidSubmissionWithCorrectAnswer_ShouldReturnSuccess()
        {
            // Arrange
            var request = new ReadingAnswerRequestDTO
            {
                ExamAttemptId = 1,
                QuestionId = 1,
                SelectedOptionId = 1
            };

            var attempt = new ExamAttempt
            {
                AttemptID = 1,
                UserID = 1,
                ExamID = 1,
                Status = "InProgress",
                StartTime = DateTime.UtcNow
            };

            var question = new Question
            {
                QuestionId = 1,
                ScoreWeight = 10,
                Options = new List<Option>
                {
                    new Option { OptionId = 1, IsCorrect = true },
                    new Option { OptionId = 2, IsCorrect = false }
                }
            };

            UserAnswerMultipleChoice? existingAnswer = null;

            _mockExamAttemptsGeneric
                .Setup(repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync(attempt);

            _mockQuestionsGeneric
                .Setup(repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Question, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync(question);

            _mockUserAnswers
                .Setup(repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserAnswerMultipleChoice, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync(existingAnswer);

            _mockUnitOfWork
                .Setup(u => u.CompleteAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.SubmitAnswerAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.True(result.IsCorrect);
            Assert.Equal(10, result.Score);
            Assert.Equal("Answer submitted successfully", result.Message);

            // Verify repository calls
            _mockExamAttemptsGeneric.Verify(
                repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()),
                Times.Once
            );
            _mockQuestionsGeneric.Verify(
                repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Question, bool>>>(), It.IsAny<string>()),
                Times.Once
            );
            _mockUserAnswers.Verify(
                repo => repo.AddAsync(It.IsAny<UserAnswerMultipleChoice>()),
                Times.Once
            );
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        #endregion

        #region URC02 - Invalid Input (Zero Values)

        /// <summary>
        /// Test Case: URC02
        /// Precondition: Can connect with server, Database available
        /// Input: ExamAttemptId = 0 (invalid), QuestionId = 0 (invalid), SelectedOptionId = 0 (invalid)
        /// Expected: ArgumentException thrown, Repository never called
        /// </summary>
        [Fact]
        public async Task URC02_InvalidInput_ZeroValues_ShouldThrowArgumentException()
        {
            // Arrange - Test ExamAttemptId = 0
            var requestWithZeroAttemptId = new ReadingAnswerRequestDTO
            {
                ExamAttemptId = 0,
                QuestionId = 1,
                SelectedOptionId = 1
            };

            // Act & Assert - ExamAttemptId = 0
            var exception1 = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.SubmitAnswerAsync(requestWithZeroAttemptId)
            );
            Assert.Equal("ExamAttemptId", exception1.ParamName);
            Assert.Contains("Exam Attempt ID must be greater than zero", exception1.Message);

            // Arrange - Test QuestionId = 0
            var requestWithZeroQuestionId = new ReadingAnswerRequestDTO
            {
                ExamAttemptId = 1,
                QuestionId = 0,
                SelectedOptionId = 1
            };

            // Act & Assert - QuestionId = 0
            var exception2 = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.SubmitAnswerAsync(requestWithZeroQuestionId)
            );
            Assert.Equal("QuestionId", exception2.ParamName);
            Assert.Contains("Question ID must be greater than zero", exception2.Message);

            // Arrange - Test SelectedOptionId = 0
            var requestWithZeroOptionId = new ReadingAnswerRequestDTO
            {
                ExamAttemptId = 1,
                QuestionId = 1,
                SelectedOptionId = 0
            };

            // Act & Assert - SelectedOptionId = 0
            var exception3 = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.SubmitAnswerAsync(requestWithZeroOptionId)
            );
            Assert.Equal("SelectedOptionId", exception3.ParamName);
            Assert.Contains("Selected Option ID must be greater than zero", exception3.Message);

            // Verify repository never called for all cases
            _mockExamAttemptsGeneric.Verify(
                repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()),
                Times.Never
            );
        }

        #endregion

        #region URC03 - Invalid Input (Negative Values)

        /// <summary>
        /// Test Case: URC03
        /// Precondition: Can connect with server, Database available
        /// Input: ExamAttemptId = -1 (negative), QuestionId = -1 (negative), SelectedOptionId = -1 (negative)
        /// Expected: ArgumentException thrown, Repository never called
        /// </summary>
        [Fact]
        public async Task URC03_InvalidInput_NegativeValues_ShouldThrowArgumentException()
        {
            // Arrange - Test ExamAttemptId = -1
            var requestWithNegativeAttemptId = new ReadingAnswerRequestDTO
            {
                ExamAttemptId = -1,
                QuestionId = 1,
                SelectedOptionId = 1
            };

            // Act & Assert - ExamAttemptId = -1
            var exception1 = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.SubmitAnswerAsync(requestWithNegativeAttemptId)
            );
            Assert.Equal("ExamAttemptId", exception1.ParamName);
            Assert.Contains("Exam Attempt ID must be greater than zero", exception1.Message);

            // Arrange - Test QuestionId = -1
            var requestWithNegativeQuestionId = new ReadingAnswerRequestDTO
            {
                ExamAttemptId = 1,
                QuestionId = -1,
                SelectedOptionId = 1
            };

            // Act & Assert - QuestionId = -1
            var exception2 = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.SubmitAnswerAsync(requestWithNegativeQuestionId)
            );
            Assert.Equal("QuestionId", exception2.ParamName);
            Assert.Contains("Question ID must be greater than zero", exception2.Message);

            // Arrange - Test SelectedOptionId = -1
            var requestWithNegativeOptionId = new ReadingAnswerRequestDTO
            {
                ExamAttemptId = 1,
                QuestionId = 1,
                SelectedOptionId = -1
            };

            // Act & Assert - SelectedOptionId = -1
            var exception3 = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.SubmitAnswerAsync(requestWithNegativeOptionId)
            );
            Assert.Equal("SelectedOptionId", exception3.ParamName);
            Assert.Contains("Selected Option ID must be greater than zero", exception3.Message);

            // Verify repository never called for all cases
            _mockExamAttemptsGeneric.Verify(
                repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()),
                Times.Never
            );
        }

        #endregion

        #region URC04 - Not Found (Non-existent IDs)

        /// <summary>
        /// Test Case: URC04
        /// Precondition: Can connect with server, Database available
        /// Input: ExamAttemptId = 999 (not exists), QuestionId = 999 (not exists), SelectedOptionId = 999 (not exists)
        /// Expected: Success = false, Appropriate error message
        /// </summary>
        [Fact]
        public async Task URC04_NotFound_NonExistentIDs_ShouldReturnSuccessFalse()
        {
            // Test Case 1: ExamAttemptId = 999 (not found)
            var requestWithNonExistentAttempt = new ReadingAnswerRequestDTO
            {
                ExamAttemptId = 999,
                QuestionId = 1,
                SelectedOptionId = 1
            };

            _mockExamAttemptsGeneric
                .Setup(repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync((ExamAttempt?)null);

            var result1 = await _service.SubmitAnswerAsync(requestWithNonExistentAttempt);

            Assert.NotNull(result1);
            Assert.False(result1.Success);
            Assert.Equal("Exam attempt not found", result1.Message);

            // Test Case 2: QuestionId = 999 (not found)
            var requestWithNonExistentQuestion = new ReadingAnswerRequestDTO
            {
                ExamAttemptId = 1,
                QuestionId = 999,
                SelectedOptionId = 1
            };

            var validAttempt = new ExamAttempt
            {
                AttemptID = 1,
                UserID = 1,
                ExamID = 1,
                Status = "InProgress",
                StartTime = DateTime.UtcNow
            };

            _mockExamAttemptsGeneric
                .Setup(repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync(validAttempt);

            _mockQuestionsGeneric
                .Setup(repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Question, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync((Question?)null);

            var result2 = await _service.SubmitAnswerAsync(requestWithNonExistentQuestion);

            Assert.NotNull(result2);
            Assert.False(result2.Success);
            Assert.Equal("Question not found", result2.Message);

            // Test Case 3: SelectedOptionId = 999 (not found)
            var requestWithNonExistentOption = new ReadingAnswerRequestDTO
            {
                ExamAttemptId = 1,
                QuestionId = 1,
                SelectedOptionId = 999
            };

            var validQuestion = new Question
            {
                QuestionId = 1,
                ScoreWeight = 10,
                Options = new List<Option>
                {
                    new Option { OptionId = 1, IsCorrect = true },
                    new Option { OptionId = 2, IsCorrect = false }
                }
            };

            _mockQuestionsGeneric
                .Setup(repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Question, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync(validQuestion);

            var result3 = await _service.SubmitAnswerAsync(requestWithNonExistentOption);

            Assert.NotNull(result3);
            Assert.False(result3.Success);
            Assert.Equal("Invalid option selected", result3.Message);

            // Verify CompleteAsync never called for all error cases
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
        }

        #endregion

        #region URC05 - Service/Database Exception

        /// <summary>
        /// Test Case: URC05
        /// Precondition: Service throws exception OR Database unavailable
        /// Input: ExamAttemptId = 1 (valid), QuestionId = 1 (valid), SelectedOptionId = 1 (valid)
        /// Expected: Exception thrown (DbException, TimeoutException, etc.)
        /// </summary>
        [Fact]
        public async Task URC05_ServiceException_DatabaseError_ShouldPropagateException()
        {
            // Test Case 1: Database connection exception
            var requestValid = new ReadingAnswerRequestDTO
            {
                ExamAttemptId = 1,
                QuestionId = 1,
                SelectedOptionId = 1
            };

            _mockExamAttemptsGeneric
                .Setup(repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()))
                .ThrowsAsync(new DbUpdateException("Database connection failed"));

            // Act & Assert - Database exception
            await Assert.ThrowsAsync<DbUpdateException>(
                async () => await _service.SubmitAnswerAsync(requestValid)
            );

            // Test Case 2: CompleteAsync fails (returns 0)
            var validAttempt = new ExamAttempt
            {
                AttemptID = 1,
                UserID = 1,
                ExamID = 1,
                Status = "InProgress",
                StartTime = DateTime.UtcNow
            };

            var validQuestion = new Question
            {
                QuestionId = 1,
                ScoreWeight = 10,
                Options = new List<Option>
                {
                    new Option { OptionId = 1, IsCorrect = true }
                }
            };

            _mockExamAttemptsGeneric.Reset();
            _mockExamAttemptsGeneric
                .Setup(repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync(validAttempt);

            _mockQuestionsGeneric
                .Setup(repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Question, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync(validQuestion);

            _mockUserAnswers
                .Setup(repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserAnswerMultipleChoice, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync((UserAnswerMultipleChoice?)null);

            _mockUnitOfWork
                .Setup(u => u.CompleteAsync())
                .ThrowsAsync(new Exception("Failed to save changes to database"));

            // Act & Assert - Save exception
            await Assert.ThrowsAsync<Exception>(
                async () => await _service.SubmitAnswerAsync(requestValid)
            );

            // Verify that AddAsync was attempted before exception
            _mockUserAnswers.Verify(
                repo => repo.AddAsync(It.IsAny<UserAnswerMultipleChoice>()),
                Times.Once
            );
        }

        #endregion
    }
}

