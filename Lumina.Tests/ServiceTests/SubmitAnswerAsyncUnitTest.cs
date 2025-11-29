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

namespace Lumina.Test.Services
{
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

        #region Test Cases for Null Request

        [Fact]
        public async Task SubmitAnswerAsync_WhenRequestIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            ReadingAnswerRequestDTO? request = null;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _service.SubmitAnswerAsync(request!)
            );

            Assert.Equal("request", exception.ParamName);
            Assert.Contains("Request cannot be null", exception.Message);

            // Verify repository is never called
            _mockExamAttemptsGeneric.Verify(
                repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()),
                Times.Never
            );
        }

        #endregion

        #region Test Cases for Invalid Request Fields

        [Fact]
        public async Task SubmitAnswerAsync_WhenExamAttemptIdIsInvalid_ShouldThrowArgumentException()
        {
            // Arrange
            var request = new ReadingAnswerRequestDTO
            {
                ExamAttemptId = 0,
                QuestionId = 1,
                SelectedOptionId = 1
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.SubmitAnswerAsync(request)
            );

            Assert.Equal("ExamAttemptId", exception.ParamName);
            Assert.Contains("Exam Attempt ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockExamAttemptsGeneric.Verify(
                repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()),
                Times.Never
            );
        }

        [Fact]
        public async Task SubmitAnswerAsync_WhenQuestionIdIsInvalid_ShouldThrowArgumentException()
        {
            // Arrange
            var request = new ReadingAnswerRequestDTO
            {
                ExamAttemptId = 1,
                QuestionId = 0,
                SelectedOptionId = 1
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.SubmitAnswerAsync(request)
            );

            Assert.Equal("QuestionId", exception.ParamName);
            Assert.Contains("Question ID must be greater than zero", exception.Message);
        }

        [Fact]
        public async Task SubmitAnswerAsync_WhenSelectedOptionIdIsInvalid_ShouldThrowArgumentException()
        {
            // Arrange
            var request = new ReadingAnswerRequestDTO
            {
                ExamAttemptId = 1,
                QuestionId = 1,
                SelectedOptionId = 0
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.SubmitAnswerAsync(request)
            );

            Assert.Equal("SelectedOptionId", exception.ParamName);
            Assert.Contains("Selected Option ID must be greater than zero", exception.Message);
        }

        #endregion

        #region Test Cases for Attempt Not Found or Completed

        [Fact]
        public async Task SubmitAnswerAsync_WhenAttemptNotFound_ShouldReturnSuccessFalse()
        {
            // Arrange
            var request = new ReadingAnswerRequestDTO
            {
                ExamAttemptId = 1,
                QuestionId = 1,
                SelectedOptionId = 1
            };

            ExamAttempt? attempt = null;

            _mockExamAttemptsGeneric
                .Setup(repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync(attempt);

            // Act
            var result = await _service.SubmitAnswerAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("Exam attempt not found", result.Message);

            // Verify repository is called but CompleteAsync is not called
            _mockExamAttemptsGeneric.Verify(
                repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()),
                Times.Once
            );
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
        }

        [Fact]
        public async Task SubmitAnswerAsync_WhenAttemptAlreadyCompleted_ShouldReturnSuccessFalse()
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
                Status = "Completed",
                StartTime = DateTime.UtcNow
            };

            _mockExamAttemptsGeneric
                .Setup(repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync(attempt);

            // Act
            var result = await _service.SubmitAnswerAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("Exam already completed", result.Message);

            // Verify CompleteAsync is not called
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
        }

        #endregion

        #region Test Cases for Question Not Found

        [Fact]
        public async Task SubmitAnswerAsync_WhenQuestionNotFound_ShouldReturnSuccessFalse()
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

            Question? question = null;

            _mockExamAttemptsGeneric
                .Setup(repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync(attempt);

            _mockQuestionsGeneric
                .Setup(repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Question, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync(question);

            // Act
            var result = await _service.SubmitAnswerAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("Question not found", result.Message);

            // Verify CompleteAsync is not called
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
        }

        #endregion

        #region Test Cases for Invalid Option

        [Fact]
        public async Task SubmitAnswerAsync_WhenSelectedOptionNotFound_ShouldReturnSuccessFalse()
        {
            // Arrange
            var request = new ReadingAnswerRequestDTO
            {
                ExamAttemptId = 1,
                QuestionId = 1,
                SelectedOptionId = 999
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

            _mockExamAttemptsGeneric
                .Setup(repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync(attempt);

            _mockQuestionsGeneric
                .Setup(repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Question, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync(question);

            // Act
            var result = await _service.SubmitAnswerAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("Invalid option selected", result.Message);

            // Verify CompleteAsync is not called
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
        }

        #endregion

        #region Test Cases for Valid Submission - Create New Answer

        [Fact]
        public async Task SubmitAnswerAsync_WhenValidAndCorrectAnswer_ShouldCreateNewAnswerAndReturnSuccess()
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

            // Verify AddAsync is called (new answer created)
            _mockUserAnswers.Verify(
                repo => repo.AddAsync(It.IsAny<UserAnswerMultipleChoice>()),
                Times.Once
            );
            _mockUserAnswers.Verify(repo => repo.Update(It.IsAny<UserAnswerMultipleChoice>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task SubmitAnswerAsync_WhenValidAndIncorrectAnswer_ShouldCreateNewAnswerAndReturnSuccess()
        {
            // Arrange
            var request = new ReadingAnswerRequestDTO
            {
                ExamAttemptId = 1,
                QuestionId = 1,
                SelectedOptionId = 2
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
            Assert.False(result.IsCorrect);
            Assert.Equal(0, result.Score);
            Assert.Equal("Answer submitted successfully", result.Message);

            // Verify AddAsync is called
            _mockUserAnswers.Verify(
                repo => repo.AddAsync(It.IsAny<UserAnswerMultipleChoice>()),
                Times.Once
            );
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task SubmitAnswerAsync_WhenValidWithNullIsCorrect_ShouldTreatAsIncorrect()
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
                    new Option { OptionId = 1, IsCorrect = null },
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
            Assert.False(result.IsCorrect);
            Assert.Equal(0, result.Score);

            // Verify AddAsync is called with IsCorrect = false
            _mockUserAnswers.Verify(
                repo => repo.AddAsync(It.IsAny<UserAnswerMultipleChoice>()),
                Times.Once
            );
        }

        #endregion

        #region Test Cases for Valid Submission - Update Existing Answer

        [Fact]
        public async Task SubmitAnswerAsync_WhenExistingAnswerExists_ShouldUpdateAnswer()
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

            var existingAnswer = new UserAnswerMultipleChoice
            {
                UserAnswerID = 1,
                AttemptID = 1,
                QuestionId = 1,
                SelectedOptionId = 2,
                IsCorrect = false,
                Score = 0
            };

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

            // Verify Update is called (not AddAsync)
            _mockUserAnswers.Verify(repo => repo.AddAsync(It.IsAny<UserAnswerMultipleChoice>()), Times.Never);
            _mockUserAnswers.Verify(
                repo => repo.Update(It.Is<UserAnswerMultipleChoice>(
                    ua => ua.SelectedOptionId == request.SelectedOptionId &&
                          ua.IsCorrect == true &&
                          ua.Score == 10
                )),
                Times.Once
            );
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        #endregion
    }
}

