using Xunit;
using Moq;
using FluentAssertions;
using ServiceLayer.Exam.Listening;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer.Generic;
using RepositoryLayer.User;
using DataLayer.DTOs.UserAnswer;
using DataLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class SubmitAnswerAsyncListeningUnitTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IRepository<ExamAttempt>> _mockExamAttemptsGeneric;
        private readonly Mock<IRepository<Question>> _mockQuestionsGeneric;
        private readonly Mock<IUserAnswerRepository> _mockUserAnswers;
        private readonly ListeningService _service;

        public SubmitAnswerAsyncListeningUnitTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockExamAttemptsGeneric = new Mock<IRepository<ExamAttempt>>();
            _mockQuestionsGeneric = new Mock<IRepository<Question>>();
            _mockUserAnswers = new Mock<IUserAnswerRepository>();

            _mockUnitOfWork.Setup(u => u.ExamAttemptsGeneric).Returns(_mockExamAttemptsGeneric.Object);
            _mockUnitOfWork.Setup(u => u.QuestionsGeneric).Returns(_mockQuestionsGeneric.Object);
            _mockUnitOfWork.Setup(u => u.UserAnswers).Returns(_mockUserAnswers.Object);

            _service = new ListeningService(_mockUnitOfWork.Object);
        }

        #region Validation Failures

        [Fact]
        public async Task SubmitAnswerAsync_WhenExamAttemptNotFound_ShouldReturnFailureResponse()
        {
            // Arrange
            var request = new SubmitAnswerRequestDTO
            {
                ExamAttemptId = 999,
                QuestionId = 1,
                SelectedOptionId = 1
            };

            _mockExamAttemptsGeneric.Setup(repo => repo.GetAsync(
                It.IsAny<Expression<Func<ExamAttempt, bool>>>(),
                It.IsAny<string>()
            )).ReturnsAsync((ExamAttempt)null);

            // Act
            var result = await _service.SubmitAnswerAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Exam attempt not found");
            result.IsCorrect.Should().BeFalse();
            result.Score.Should().Be(0);
        }

        [Fact]
        public async Task SubmitAnswerAsync_WhenExamAlreadyCompleted_ShouldReturnFailureResponse()
        {
            // Arrange
            var request = new SubmitAnswerRequestDTO
            {
                ExamAttemptId = 1,
                QuestionId = 1,
                SelectedOptionId = 1
            };

            var completedAttempt = new ExamAttempt
            {
                AttemptID = 1,
                UserID = 1,
                ExamID = 1,
                Status = "Completed",
                StartTime = DateTime.UtcNow.AddHours(-1)
            };

            _mockExamAttemptsGeneric.Setup(repo => repo.GetAsync(
                It.IsAny<Expression<Func<ExamAttempt, bool>>>(),
                It.IsAny<string>()
            )).ReturnsAsync(completedAttempt);

            // Act
            var result = await _service.SubmitAnswerAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Exam already completed");
        }

        [Fact]
        public async Task SubmitAnswerAsync_WhenQuestionNotFound_ShouldReturnFailureResponse()
        {
            // Arrange
            var request = new SubmitAnswerRequestDTO
            {
                ExamAttemptId = 1,
                QuestionId = 999,
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

            _mockExamAttemptsGeneric.Setup(repo => repo.GetAsync(
                It.IsAny<Expression<Func<ExamAttempt, bool>>>(),
                It.IsAny<string>()
            )).ReturnsAsync(attempt);

            _mockQuestionsGeneric.Setup(repo => repo.GetAsync(
                It.IsAny<Expression<Func<Question, bool>>>(),
                It.IsAny<string>()
            )).ReturnsAsync((Question)null);

            // Act
            var result = await _service.SubmitAnswerAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Question not found");
        }

        [Fact]
        public async Task SubmitAnswerAsync_WhenInvalidOptionSelected_ShouldReturnFailureResponse()
        {
            // Arrange
            var request = new SubmitAnswerRequestDTO
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
                PartId = 1,
                QuestionType = "MultipleChoice",
                StemText = "Test question",
                ScoreWeight = 5,
                Time = 60,
                QuestionNumber = 1,
                Options = new List<Option>
                {
                    new Option { OptionId = 1, QuestionId = 1, Content = "Option A", IsCorrect = true }
                }
            };

            _mockExamAttemptsGeneric.Setup(repo => repo.GetAsync(
                It.IsAny<Expression<Func<ExamAttempt, bool>>>(),
                It.IsAny<string>()
            )).ReturnsAsync(attempt);

            _mockQuestionsGeneric.Setup(repo => repo.GetAsync(
                It.IsAny<Expression<Func<Question, bool>>>(),
                It.IsAny<string>()
            )).ReturnsAsync(question);

            // Act
            var result = await _service.SubmitAnswerAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Invalid option selected");
        }

        #endregion

        #region Submit New Answer

        [Fact]
        public async Task SubmitAnswerAsync_WhenCorrectAnswerSubmittedFirstTime_ShouldReturnSuccessWithScore()
        {
            // Arrange
            var request = new SubmitAnswerRequestDTO
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
                PartId = 1,
                QuestionType = "MultipleChoice",
                StemText = "Test question",
                ScoreWeight = 5,
                Time = 60,
                QuestionNumber = 1,
                Options = new List<Option>
                {
                    new Option { OptionId = 1, QuestionId = 1, Content = "Correct Option", IsCorrect = true },
                    new Option { OptionId = 2, QuestionId = 1, Content = "Wrong Option", IsCorrect = false }
                }
            };

            _mockExamAttemptsGeneric.Setup(repo => repo.GetAsync(
                It.IsAny<Expression<Func<ExamAttempt, bool>>>(),
                It.IsAny<string>()
            )).ReturnsAsync(attempt);

            _mockQuestionsGeneric.Setup(repo => repo.GetAsync(
                It.IsAny<Expression<Func<Question, bool>>>(),
                It.IsAny<string>()
            )).ReturnsAsync(question);

            _mockUserAnswers.Setup(repo => repo.GetAsync(
                It.IsAny<Expression<Func<UserAnswerMultipleChoice, bool>>>(),
                It.IsAny<string>()
            )).ReturnsAsync((UserAnswerMultipleChoice)null);

            _mockUserAnswers.Setup(repo => repo.AddAsync(It.IsAny<UserAnswerMultipleChoice>()))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.SubmitAnswerAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.IsCorrect.Should().BeTrue();
            result.Score.Should().Be(5);
            result.Message.Should().Be("Answer submitted successfully");

            _mockUserAnswers.Verify(
                repo => repo.AddAsync(It.Is<UserAnswerMultipleChoice>(ua =>
                    ua.AttemptID == 1 &&
                    ua.QuestionId == 1 &&
                    ua.SelectedOptionId == 1 &&
                    ua.IsCorrect == true &&
                    ua.Score == 5
                )),
                Times.Once
            );

            _mockUserAnswers.Verify(repo => repo.Update(It.IsAny<UserAnswerMultipleChoice>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task SubmitAnswerAsync_WhenIncorrectAnswerSubmittedFirstTime_ShouldReturnSuccessWithZeroScore()
        {
            // Arrange
            var request = new SubmitAnswerRequestDTO
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
                PartId = 1,
                QuestionType = "MultipleChoice",
                StemText = "Test question",
                ScoreWeight = 5,
                Time = 60,
                QuestionNumber = 1,
                Options = new List<Option>
                {
                    new Option { OptionId = 1, QuestionId = 1, Content = "Correct Option", IsCorrect = true },
                    new Option { OptionId = 2, QuestionId = 1, Content = "Wrong Option", IsCorrect = false }
                }
            };

            _mockExamAttemptsGeneric.Setup(repo => repo.GetAsync(
                It.IsAny<Expression<Func<ExamAttempt, bool>>>(),
                It.IsAny<string>()
            )).ReturnsAsync(attempt);

            _mockQuestionsGeneric.Setup(repo => repo.GetAsync(
                It.IsAny<Expression<Func<Question, bool>>>(),
                It.IsAny<string>()
            )).ReturnsAsync(question);

            _mockUserAnswers.Setup(repo => repo.GetAsync(
                It.IsAny<Expression<Func<UserAnswerMultipleChoice, bool>>>(),
                It.IsAny<string>()
            )).ReturnsAsync((UserAnswerMultipleChoice)null);

            _mockUserAnswers.Setup(repo => repo.AddAsync(It.IsAny<UserAnswerMultipleChoice>()))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.SubmitAnswerAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.IsCorrect.Should().BeFalse();
            result.Score.Should().Be(0);
            result.Message.Should().Be("Answer submitted successfully");

            _mockUserAnswers.Verify(
                repo => repo.AddAsync(It.Is<UserAnswerMultipleChoice>(ua =>
                    ua.IsCorrect == false &&
                    ua.Score == 0
                )),
                Times.Once
            );
        }

        #endregion

        #region Update Existing Answer

        [Fact]
        public async Task SubmitAnswerAsync_WhenUpdatingExistingAnswer_ShouldUpdateAndReturnSuccess()
        {
            // Arrange
            var request = new SubmitAnswerRequestDTO
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
                PartId = 1,
                QuestionType = "MultipleChoice",
                StemText = "Test question",
                ScoreWeight = 5,
                Time = 60,
                QuestionNumber = 1,
                Options = new List<Option>
                {
                    new Option { OptionId = 1, QuestionId = 1, Content = "Correct Option", IsCorrect = true },
                    new Option { OptionId = 2, QuestionId = 1, Content = "Wrong Option", IsCorrect = false }
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

            _mockExamAttemptsGeneric.Setup(repo => repo.GetAsync(
                It.IsAny<Expression<Func<ExamAttempt, bool>>>(),
                It.IsAny<string>()
            )).ReturnsAsync(attempt);

            _mockQuestionsGeneric.Setup(repo => repo.GetAsync(
                It.IsAny<Expression<Func<Question, bool>>>(),
                It.IsAny<string>()
            )).ReturnsAsync(question);

            _mockUserAnswers.Setup(repo => repo.GetAsync(
                It.IsAny<Expression<Func<UserAnswerMultipleChoice, bool>>>(),
                It.IsAny<string>()
            )).ReturnsAsync(existingAnswer);

            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.SubmitAnswerAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.IsCorrect.Should().BeTrue();
            result.Score.Should().Be(5);
            result.Message.Should().Be("Answer submitted successfully");

            existingAnswer.SelectedOptionId.Should().Be(1);
            existingAnswer.IsCorrect.Should().BeTrue();
            existingAnswer.Score.Should().Be(5);

            _mockUserAnswers.Verify(repo => repo.Update(existingAnswer), Times.Once);
            _mockUserAnswers.Verify(repo => repo.AddAsync(It.IsAny<UserAnswerMultipleChoice>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task SubmitAnswerAsync_WhenOptionIsCorrectIsNull_ShouldTreatAsFalse()
        {
            // Arrange
            var request = new SubmitAnswerRequestDTO
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
                PartId = 1,
                QuestionType = "MultipleChoice",
                StemText = "Test question",
                ScoreWeight = 5,
                Time = 60,
                QuestionNumber = 1,
                Options = new List<Option>
                {
                    new Option { OptionId = 1, QuestionId = 1, Content = "Correct Option", IsCorrect = true },
                    new Option { OptionId = 2, QuestionId = 1, Content = "Null Option", IsCorrect = null }
                }
            };

            _mockExamAttemptsGeneric.Setup(repo => repo.GetAsync(
                It.IsAny<Expression<Func<ExamAttempt, bool>>>(),
                It.IsAny<string>()
            )).ReturnsAsync(attempt);

            _mockQuestionsGeneric.Setup(repo => repo.GetAsync(
                It.IsAny<Expression<Func<Question, bool>>>(),
                It.IsAny<string>()
            )).ReturnsAsync(question);

            _mockUserAnswers.Setup(repo => repo.GetAsync(
                It.IsAny<Expression<Func<UserAnswerMultipleChoice, bool>>>(),
                It.IsAny<string>()
            )).ReturnsAsync((UserAnswerMultipleChoice)null);

            _mockUserAnswers.Setup(repo => repo.AddAsync(It.IsAny<UserAnswerMultipleChoice>()))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.SubmitAnswerAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.IsCorrect.Should().BeFalse();
            result.Score.Should().Be(0);
            result.Message.Should().Be("Answer submitted successfully");

            _mockUserAnswers.Verify(
                repo => repo.AddAsync(It.Is<UserAnswerMultipleChoice>(ua =>
                    ua.IsCorrect == false &&
                    ua.Score == 0
                )),
                Times.Once
            );
        }

        [Fact]
        public async Task SubmitAnswerAsync_WhenQuestionHasNoOptions_ShouldReturnInvalidOptionFailure()
        {
            // Arrange
            var request = new SubmitAnswerRequestDTO
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
                PartId = 1,
                QuestionType = "MultipleChoice",
                StemText = "Test question",
                ScoreWeight = 5,
                Time = 60,
                QuestionNumber = 1,
                Options = new List<Option>()
            };

            _mockExamAttemptsGeneric.Setup(repo => repo.GetAsync(
                It.IsAny<Expression<Func<ExamAttempt, bool>>>(),
                It.IsAny<string>()
            )).ReturnsAsync(attempt);

            _mockQuestionsGeneric.Setup(repo => repo.GetAsync(
                It.IsAny<Expression<Func<Question, bool>>>(),
                It.IsAny<string>()
            )).ReturnsAsync(question);

            // Act
            var result = await _service.SubmitAnswerAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Invalid option selected");
        }

        #endregion
    }
}
