using Xunit;
using Moq;
using ServiceLayer.Exam.ExamAttempt;
using RepositoryLayer.Exam.ExamAttempt;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer.Generic;
using RepositoryLayer.User;
using ServiceLayer.Streak;
using Microsoft.Extensions.Logging;
using DataLayer.DTOs.Exam;
using DataLayer.DTOs.Streak;
using DataLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class FinalizeAttemptAsyncUnitTest
    {
        private readonly Mock<IExamAttemptRepository> _mockExamAttemptRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IStreakService> _mockStreakService;
        private readonly Mock<ILogger<ExamAttemptService>> _mockLogger;
        private readonly Mock<IRepository<ExamAttempt>> _mockExamAttemptsGeneric;
        private readonly Mock<IRepository<Question>> _mockQuestionsGeneric;
        private readonly Mock<IUserAnswerRepository> _mockUserAnswers;
        private readonly Mock<IUserAnswerSpeakingRepository> _mockUserAnswersSpeaking;
        private readonly ExamAttemptService _service;

        public FinalizeAttemptAsyncUnitTest()
        {
            _mockExamAttemptRepository = new Mock<IExamAttemptRepository>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockStreakService = new Mock<IStreakService>();
            _mockLogger = new Mock<ILogger<ExamAttemptService>>();
            _mockExamAttemptsGeneric = new Mock<IRepository<ExamAttempt>>();
            _mockQuestionsGeneric = new Mock<IRepository<Question>>();
            _mockUserAnswers = new Mock<IUserAnswerRepository>();
            _mockUserAnswersSpeaking = new Mock<IUserAnswerSpeakingRepository>();

            _mockUnitOfWork.Setup(u => u.ExamAttemptsGeneric).Returns(_mockExamAttemptsGeneric.Object);
            _mockUnitOfWork.Setup(u => u.QuestionsGeneric).Returns(_mockQuestionsGeneric.Object);
            _mockUnitOfWork.Setup(u => u.UserAnswers).Returns(_mockUserAnswers.Object);
            _mockUnitOfWork.Setup(u => u.UserAnswersSpeaking).Returns(_mockUserAnswersSpeaking.Object);

            _service = new ExamAttemptService(
                _mockExamAttemptRepository.Object,
                _mockUnitOfWork.Object,
                _mockStreakService.Object,
                _mockLogger.Object
            );
        }

        #region Test Cases for Invalid AttemptId

        [Fact]
        public async Task FinalizeAttemptAsync_WhenAttemptIdIsNegative_ShouldThrowArgumentException()
        {
            // Arrange
            int attemptId = -1;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.FinalizeAttemptAsync(attemptId)
            );

            Assert.Equal("attemptId", exception.ParamName);
            Assert.Contains("Attempt ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockExamAttemptsGeneric.Verify(
                repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()),
                Times.Never
            );
        }

        [Fact]
        public async Task FinalizeAttemptAsync_WhenAttemptIdIsZero_ShouldThrowArgumentException()
        {
            // Arrange
            int attemptId = 0;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.FinalizeAttemptAsync(attemptId)
            );

            Assert.Equal("attemptId", exception.ParamName);
            Assert.Contains("Attempt ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockExamAttemptsGeneric.Verify(
                repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()),
                Times.Never
            );
        }

        #endregion

        #region Test Cases for Attempt Not Found

        [Fact]
        public async Task FinalizeAttemptAsync_WhenAttemptNotFound_ShouldReturnSuccessFalse()
        {
            // Arrange
            int attemptId = 1;
            ExamAttempt? attempt = null;

            _mockExamAttemptsGeneric
                .Setup(repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync(attempt);

            // Act
            var result = await _service.FinalizeAttemptAsync(attemptId);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);

            // Verify repository is called
            _mockExamAttemptsGeneric.Verify(
                repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()),
                Times.Once
            );
        }

        #endregion

        #region Test Cases for Already Completed

        [Fact]
        public async Task FinalizeAttemptAsync_WhenAttemptAlreadyCompleted_ShouldReturnSuccessFalse()
        {
            // Arrange
            int attemptId = 1;
            var attempt = new ExamAttempt
            {
                AttemptID = attemptId,
                UserID = 1,
                ExamID = 1,
                Status = "Completed",
                StartTime = DateTime.UtcNow.AddHours(-1)
            };

            _mockExamAttemptsGeneric
                .Setup(repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync(attempt);

            // Act
            var result = await _service.FinalizeAttemptAsync(attemptId);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);

            // Verify CompleteAsync is never called
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
        }

        #endregion

        #region Test Cases for Valid Finalization

        [Fact]
        public async Task FinalizeAttemptAsync_WhenValid_ShouldReturnSuccessTrue()
        {
            // Arrange
            int attemptId = 1;
            var startTime = DateTime.UtcNow.AddHours(-1);
            var attempt = new ExamAttempt
            {
                AttemptID = attemptId,
                UserID = 1,
                ExamID = 1,
                Status = "InProgress",
                StartTime = startTime
            };

            var userAnswers = new List<UserAnswerMultipleChoice>
            {
                new UserAnswerMultipleChoice
                {
                    UserAnswerID = 1,
                    AttemptID = attemptId,
                    QuestionId = 1,
                    SelectedOptionId = 1,
                    Score = 10,
                    IsCorrect = true
                },
                new UserAnswerMultipleChoice
                {
                    UserAnswerID = 2,
                    AttemptID = attemptId,
                    QuestionId = 2,
                    SelectedOptionId = 2,
                    Score = 0,
                    IsCorrect = false
                }
            };

            var speakingAnswers = new List<UserAnswerSpeaking>
            {
                new UserAnswerSpeaking
                {
                    UserAnswerSpeakingId = 1,
                    AttemptID = attemptId,
                    QuestionId = 3,
                    OverallScore = 80m
                }
            };

            var question = new Question
            {
                QuestionId = 1,
                StemText = "Test Question",
                ScoreWeight = 10,
                QuestionExplain = "Explanation",
                Options = new List<Option>
                {
                    new Option { OptionId = 1, Content = "Option 1", IsCorrect = true },
                    new Option { OptionId = 2, Content = "Option 2", IsCorrect = false }
                }
            };

            var speakingQuestion = new Question
            {
                QuestionId = 3,
                StemText = "Speaking Question",
                ScoreWeight = 20,
                Options = new List<Option>()
            };

            _mockExamAttemptsGeneric
                .Setup(repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync(attempt);

            _mockUserAnswers
                .Setup(repo => repo.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserAnswerMultipleChoice, bool>>>()))
                .ReturnsAsync(userAnswers);

            _mockUserAnswersSpeaking
                .Setup(repo => repo.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserAnswerSpeaking, bool>>>()))
                .ReturnsAsync(speakingAnswers);

            _mockQuestionsGeneric
                .Setup(repo => repo.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Question, bool>>>()))
                .ReturnsAsync(new List<Question> { speakingQuestion });

            _mockQuestionsGeneric
                .Setup(repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Question, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync(question);

            _mockStreakService
                .Setup(s => s.GetTodayGMT7())
                .Returns(DateOnly.FromDateTime(DateTime.UtcNow));

            _mockStreakService
                .Setup(s => s.UpdateOnValidPracticeAsync(It.IsAny<int>(), It.IsAny<DateOnly>()))
                .ReturnsAsync(new StreakUpdateResultDTO
                {
                    Success = true,
                    EventType = StreakEventType.CompleteDay,
                    Summary = new StreakSummaryDTO { CurrentStreak = 5 }
                });

            _mockUnitOfWork
                .Setup(u => u.CompleteAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.FinalizeAttemptAsync(attemptId);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(attemptId, result.ExamAttemptId);
            Assert.True(result.TotalScore > 0);
            Assert.Equal(3, result.TotalQuestions);
            Assert.Equal(1, result.CorrectAnswers);
            Assert.NotNull(result.Answers);
            Assert.True(result.Answers.Count > 0);

            // Verify all methods are called
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
            _mockStreakService.Verify(s => s.UpdateOnValidPracticeAsync(It.IsAny<int>(), It.IsAny<DateOnly>()), Times.Once);
        }

        [Fact]
        public async Task FinalizeAttemptAsync_WhenValidWithEmptyAnswers_ShouldReturnSuccessTrue()
        {
            // Arrange
            int attemptId = 1;
            var startTime = DateTime.UtcNow.AddHours(-1);
            var attempt = new ExamAttempt
            {
                AttemptID = attemptId,
                UserID = 1,
                ExamID = 1,
                Status = "InProgress",
                StartTime = startTime
            };

            _mockExamAttemptsGeneric
                .Setup(repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync(attempt);

            _mockUserAnswers
                .Setup(repo => repo.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserAnswerMultipleChoice, bool>>>()))
                .ReturnsAsync(new List<UserAnswerMultipleChoice>());

            _mockUserAnswersSpeaking
                .Setup(repo => repo.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserAnswerSpeaking, bool>>>()))
                .ReturnsAsync(new List<UserAnswerSpeaking>());

            _mockStreakService
                .Setup(s => s.GetTodayGMT7())
                .Returns(DateOnly.FromDateTime(DateTime.UtcNow));

            _mockStreakService
                .Setup(s => s.UpdateOnValidPracticeAsync(It.IsAny<int>(), It.IsAny<DateOnly>()))
                .ReturnsAsync(new StreakUpdateResultDTO
                {
                    Success = true,
                    EventType = StreakEventType.CompleteDay
                });

            _mockUnitOfWork
                .Setup(u => u.CompleteAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.FinalizeAttemptAsync(attemptId);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(0, result.TotalScore);
            Assert.Equal(0, result.TotalQuestions);
            Assert.Equal(0, result.CorrectAnswers);
            Assert.Equal(0, result.PercentCorrect);
        }

        [Fact]
        public async Task FinalizeAttemptAsync_WhenStreakServiceFails_ShouldStillReturnSuccess()
        {
            // Arrange
            int attemptId = 1;
            var startTime = DateTime.UtcNow.AddHours(-1);
            var attempt = new ExamAttempt
            {
                AttemptID = attemptId,
                UserID = 1,
                ExamID = 1,
                Status = "InProgress",
                StartTime = startTime
            };

            _mockExamAttemptsGeneric
                .Setup(repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync(attempt);

            _mockUserAnswers
                .Setup(repo => repo.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserAnswerMultipleChoice, bool>>>()))
                .ReturnsAsync(new List<UserAnswerMultipleChoice>());

            _mockUserAnswersSpeaking
                .Setup(repo => repo.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserAnswerSpeaking, bool>>>()))
                .ReturnsAsync(new List<UserAnswerSpeaking>());

            _mockStreakService
                .Setup(s => s.GetTodayGMT7())
                .Returns(DateOnly.FromDateTime(DateTime.UtcNow));

            _mockStreakService
                .Setup(s => s.UpdateOnValidPracticeAsync(It.IsAny<int>(), It.IsAny<DateOnly>()))
                .ReturnsAsync(new StreakUpdateResultDTO
                {
                    Success = false,
                    Message = "Error updating streak"
                });

            _mockUnitOfWork
                .Setup(u => u.CompleteAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.FinalizeAttemptAsync(attemptId);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success); // Should still succeed even if streak fails
        }

        [Fact]
        public async Task FinalizeAttemptAsync_WhenStreakServiceThrowsException_ShouldStillReturnSuccess()
        {
            // Arrange
            int attemptId = 1;
            var startTime = DateTime.UtcNow.AddHours(-1);
            var attempt = new ExamAttempt
            {
                AttemptID = attemptId,
                UserID = 1,
                ExamID = 1,
                Status = "InProgress",
                StartTime = startTime
            };

            _mockExamAttemptsGeneric
                .Setup(repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync(attempt);

            _mockUserAnswers
                .Setup(repo => repo.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserAnswerMultipleChoice, bool>>>()))
                .ReturnsAsync(new List<UserAnswerMultipleChoice>());

            _mockUserAnswersSpeaking
                .Setup(repo => repo.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserAnswerSpeaking, bool>>>()))
                .ReturnsAsync(new List<UserAnswerSpeaking>());

            _mockStreakService
                .Setup(s => s.GetTodayGMT7())
                .Returns(DateOnly.FromDateTime(DateTime.UtcNow));

            _mockStreakService
                .Setup(s => s.UpdateOnValidPracticeAsync(It.IsAny<int>(), It.IsAny<DateOnly>()))
                .ThrowsAsync(new Exception("Streak service error"));

            _mockUnitOfWork
                .Setup(u => u.CompleteAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.FinalizeAttemptAsync(attemptId);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success); // Should still succeed even if streak throws exception
        }

        [Fact]
        public async Task FinalizeAttemptAsync_WhenSpeakingAnswerHasNullOverallScore_ShouldHandleGracefully()
        {
            // Arrange
            int attemptId = 1;
            var startTime = DateTime.UtcNow.AddHours(-1);
            var attempt = new ExamAttempt
            {
                AttemptID = attemptId,
                UserID = 1,
                ExamID = 1,
                Status = "InProgress",
                StartTime = startTime
            };

            var speakingAnswers = new List<UserAnswerSpeaking>
            {
                new UserAnswerSpeaking
                {
                    UserAnswerSpeakingId = 1,
                    AttemptID = attemptId,
                    QuestionId = 3,
                    OverallScore = null // Null OverallScore
                }
            };

            var speakingQuestion = new Question
            {
                QuestionId = 3,
                StemText = "Speaking Question",
                ScoreWeight = 20,
                Options = new List<Option>()
            };

            _mockExamAttemptsGeneric
                .Setup(repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync(attempt);

            _mockUserAnswers
                .Setup(repo => repo.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserAnswerMultipleChoice, bool>>>()))
                .ReturnsAsync(new List<UserAnswerMultipleChoice>());

            _mockUserAnswersSpeaking
                .Setup(repo => repo.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserAnswerSpeaking, bool>>>()))
                .ReturnsAsync(speakingAnswers);

            _mockQuestionsGeneric
                .Setup(repo => repo.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Question, bool>>>()))
                .ReturnsAsync(new List<Question> { speakingQuestion });

            _mockStreakService
                .Setup(s => s.GetTodayGMT7())
                .Returns(DateOnly.FromDateTime(DateTime.UtcNow));

            _mockStreakService
                .Setup(s => s.UpdateOnValidPracticeAsync(It.IsAny<int>(), It.IsAny<DateOnly>()))
                .ReturnsAsync(new StreakUpdateResultDTO { Success = true });

            _mockUnitOfWork
                .Setup(u => u.CompleteAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.FinalizeAttemptAsync(attemptId);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(0, result.TotalScore); // Should handle null OverallScore as 0
        }

        #endregion
    }
}

