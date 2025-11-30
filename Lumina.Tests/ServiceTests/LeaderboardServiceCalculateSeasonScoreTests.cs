using Xunit;
using Moq;
using ServiceLayer.Leaderboard;
using RepositoryLayer.Leaderboard;
using ServiceLayer.Notification;
using DataLayer.DTOs.Leaderboard;
using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class LeaderboardServiceCalculateSeasonScoreTests
    {
        private readonly Mock<ILeaderboardRepository> _mockRepository;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly LuminaSystemContext _context;
        private readonly LeaderboardService _service;

        public LeaderboardServiceCalculateSeasonScoreTests()
        {
            _mockRepository = new Mock<ILeaderboardRepository>();
            _mockNotificationService = new Mock<INotificationService>();
            _context = InMemoryDbContextHelper.CreateContext();
            _service = new LeaderboardService(_mockRepository.Object, _context, _mockNotificationService.Object);
        }

        [Fact]
        public async Task CalculateSeasonScoreAsync_WhenRequestIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            int userId = 1;
            CalculateScoreRequestDTO? request = null;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _service.CalculateSeasonScoreAsync(userId, request!)
            );

            Assert.Equal("request", exception.ParamName);
        }

        [Fact]
        public async Task CalculateSeasonScoreAsync_WhenExamPartIdIsInvalid_ShouldThrowArgumentException()
        {
            // Arrange
            int userId = 1;
            var request = new CalculateScoreRequestDTO
            {
                ExamAttemptId = 1,
                ExamPartId = 3, // Invalid (only 1 and 2 are valid)
                CorrectAnswers = 10,
                TotalQuestions = 20,
                TimeSpentSeconds = 100,
                ExpectedTimeSeconds = 120
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.CalculateSeasonScoreAsync(userId, request)
            );

            Assert.Contains("Chỉ tính điểm cho Listening (ExamPartId=1) và Reading (ExamPartId=2)", exception.Message);
        }

        [Fact]
        public async Task CalculateSeasonScoreAsync_WhenExamAttemptNotFound_ShouldThrowArgumentException()
        {
            // Arrange
            int userId = 1;
            var request = new CalculateScoreRequestDTO
            {
                ExamAttemptId = 999,
                ExamPartId = 1,
                CorrectAnswers = 10,
                TotalQuestions = 20,
                TimeSpentSeconds = 100,
                ExpectedTimeSeconds = 120
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.CalculateSeasonScoreAsync(userId, request)
            );

            Assert.Contains("ExamAttempt không tồn tại", exception.Message);
        }

        [Fact]
        public async Task CalculateSeasonScoreAsync_WhenNoActiveSeason_ShouldThrowInvalidOperationException()
        {
            // Arrange
            int userId = 1;
            int examId = 1;
            int examPartId = 1;
            var request = new CalculateScoreRequestDTO
            {
                ExamAttemptId = 1,
                ExamPartId = 1,
                CorrectAnswers = 10,
                TotalQuestions = 20,
                TimeSpentSeconds = 100,
                ExpectedTimeSeconds = 120
            };

            var examPart = new ExamPart
            {
                PartId = examPartId,
                ExamId = examId,
                PartCode = "LISTENING_P1",
                Title = "Listening Part 1"
            };

            var examAttempt = new ExamAttempt
            {
                AttemptID = request.ExamAttemptId,
                UserID = userId,
                ExamID = examId,
                ExamPartId = examPartId,
                Status = "Completed",
                Score = 10,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                ExamPart = examPart
            };

            _context.ExamParts.Add(examPart);
            _context.Exams.Add(new Exam { ExamId = examId, Name = "Test Exam", ExamType = "TOEIC", Description = "Test Description", ExamSetKey = "TEST_KEY" });
            _context.ExamAttempts.Add(examAttempt);
            await _context.SaveChangesAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _service.CalculateSeasonScoreAsync(userId, request)
            );

            Assert.Contains("Không có season nào đang active", exception.Message);
        }

        [Fact]
        public async Task CalculateSeasonScoreAsync_WhenFirstAttemptWithCorrectAnswers_ShouldAddPointsAndCreateUserLeaderboard()
        {
            // Arrange
            int userId = 1;
            int examId = 1;
            int examPartId = 1;
            var request = new CalculateScoreRequestDTO
            {
                ExamAttemptId = 1,
                ExamPartId = 1,
                CorrectAnswers = 10,
                TotalQuestions = 20,
                TimeSpentSeconds = 100,
                ExpectedTimeSeconds = 120
            };

            var currentSeason = new Leaderboard
            {
                LeaderboardId = 1,
                SeasonNumber = 1,
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(20)
            };

            var examPart = new ExamPart
            {
                PartId = examPartId,
                ExamId = examId,
                PartCode = "LISTENING_P1",
                Title = "Listening Part 1"
            };

            var examAttempt = new ExamAttempt
            {
                AttemptID = request.ExamAttemptId,
                UserID = userId,
                ExamID = examId,
                ExamPartId = examPartId,
                Status = "Completed",
                Score = 10,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                ExamPart = examPart
            };

            _context.Leaderboards.Add(currentSeason);
            _context.ExamParts.Add(examPart);
            _context.Exams.Add(new Exam { ExamId = examId, Name = "Test Exam", ExamType = "TOEIC", Description = "Test Description", ExamSetKey = "TEST_KEY" });
            _context.ExamAttempts.Add(examAttempt);
            await _context.SaveChangesAsync();

            _mockNotificationService
                .Setup(ns => ns.SendPointsNotificationAsync(
                    userId, It.IsAny<int>(), It.IsAny<int>(), 
                    request.CorrectAnswers, request.TotalQuestions,
                    It.IsAny<int>(), It.IsAny<int>(), true))
                .ReturnsAsync(1);

            _mockNotificationService
                .Setup(ns => ns.SendTOEICNotificationAsync(
                    userId, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(2);

            // Act
            var result = await _service.CalculateSeasonScoreAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsFirstAttempt);
            Assert.True(result.SeasonScore > 0);
            Assert.True(result.TotalAccumulatedScore > 0);

            // Verify UserLeaderboard was created
            var userLeaderboard = await _context.UserLeaderboards
                .FirstOrDefaultAsync(ul => ul.UserId == userId && ul.LeaderboardId == currentSeason.LeaderboardId);
            Assert.NotNull(userLeaderboard);
            Assert.True(userLeaderboard!.Score > 0);
            Assert.NotNull(userLeaderboard.FirstAttemptDate);

            // Verify notifications were sent
            _mockNotificationService.Verify(
                ns => ns.SendPointsNotificationAsync(
                    userId, It.IsAny<int>(), It.IsAny<int>(),
                    request.CorrectAnswers, request.TotalQuestions,
                    It.IsAny<int>(), It.IsAny<int>(), true),
                Times.Once
            );

            _mockNotificationService.Verify(
                ns => ns.SendTOEICNotificationAsync(
                    userId, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Once
            );
        }

        [Fact]
        public async Task CalculateSeasonScoreAsync_WhenNotFirstAttempt_ShouldNotAddPoints()
        {
            // Arrange
            int userId = 1;
            int examId = 1;
            int examPartId = 1;
            var request = new CalculateScoreRequestDTO
            {
                ExamAttemptId = 2, // Second attempt
                ExamPartId = 1,
                CorrectAnswers = 10,
                TotalQuestions = 20,
                TimeSpentSeconds = 100,
                ExpectedTimeSeconds = 120
            };

            var currentSeason = new Leaderboard
            {
                LeaderboardId = 1,
                SeasonNumber = 1,
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(20)
            };

            var examPart = new ExamPart
            {
                PartId = examPartId,
                ExamId = examId,
                PartCode = "LISTENING_P1",
                Title = "Listening Part 1"
            };

            // First attempt (already completed)
            var firstAttempt = new ExamAttempt
            {
                AttemptID = 1,
                UserID = userId,
                ExamID = examId,
                ExamPartId = examPartId,
                Status = "Completed",
                Score = 10,
                StartTime = DateTime.UtcNow.AddDays(-1),
                EndTime = DateTime.UtcNow.AddDays(-1),
                ExamPart = examPart
            };

            // Second attempt (current)
            var secondAttempt = new ExamAttempt
            {
                AttemptID = request.ExamAttemptId,
                UserID = userId,
                ExamID = examId,
                ExamPartId = examPartId,
                Status = "Completed",
                Score = 10,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                ExamPart = examPart
            };

            var userLeaderboard = new UserLeaderboard
            {
                UserId = userId,
                LeaderboardId = currentSeason.LeaderboardId,
                Score = 100,
                FirstAttemptDate = DateTime.UtcNow.AddDays(-1)
            };

            _context.Leaderboards.Add(currentSeason);
            _context.ExamParts.Add(examPart);
            _context.Exams.Add(new Exam { ExamId = examId, Name = "Test Exam", ExamType = "TOEIC", Description = "Test Description", ExamSetKey = "TEST_KEY" });
            _context.ExamAttempts.Add(firstAttempt);
            _context.ExamAttempts.Add(secondAttempt);
            _context.UserLeaderboards.Add(userLeaderboard);
            await _context.SaveChangesAsync();

            _mockNotificationService
                .Setup(ns => ns.SendPointsNotificationAsync(
                    userId, 0, It.IsAny<int>(),
                    request.CorrectAnswers, request.TotalQuestions,
                    It.IsAny<int>(), It.IsAny<int>(), false))
                .ReturnsAsync(1);

            // Act
            var result = await _service.CalculateSeasonScoreAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsFirstAttempt);
            Assert.True(result.SeasonScore > 0); // Score is calculated but not added
            Assert.Equal(100, result.TotalAccumulatedScore); // Should remain unchanged

            // Verify UserLeaderboard score was not changed
            var updatedUserLeaderboard = await _context.UserLeaderboards
                .FirstOrDefaultAsync(ul => ul.UserId == userId && ul.LeaderboardId == currentSeason.LeaderboardId);
            Assert.NotNull(updatedUserLeaderboard);
            Assert.Equal(100, updatedUserLeaderboard!.Score); // Should remain 100

            // Verify TOEIC notification was NOT sent (not first attempt)
            _mockNotificationService.Verify(
                ns => ns.SendTOEICNotificationAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never
            );
        }

        [Fact]
        public async Task CalculateSeasonScoreAsync_WhenZeroCorrectAnswers_ShouldNotAddPoints()
        {
            // Arrange
            int userId = 1;
            int examId = 1;
            int examPartId = 1;
            var request = new CalculateScoreRequestDTO
            {
                ExamAttemptId = 1,
                ExamPartId = 1,
                CorrectAnswers = 0, // Zero correct answers
                TotalQuestions = 20,
                TimeSpentSeconds = 100,
                ExpectedTimeSeconds = 120
            };

            var currentSeason = new Leaderboard
            {
                LeaderboardId = 1,
                SeasonNumber = 1,
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(20)
            };

            var examPart = new ExamPart
            {
                PartId = examPartId,
                ExamId = examId,
                PartCode = "LISTENING_P1",
                Title = "Listening Part 1"
            };

            var examAttempt = new ExamAttempt
            {
                AttemptID = request.ExamAttemptId,
                UserID = userId,
                ExamID = examId,
                ExamPartId = examPartId,
                Status = "Completed",
                Score = 0,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                ExamPart = examPart
            };

            _context.Leaderboards.Add(currentSeason);
            _context.ExamParts.Add(examPart);
            _context.Exams.Add(new Exam { ExamId = examId, Name = "Test Exam", ExamType = "TOEIC", Description = "Test Description", ExamSetKey = "TEST_KEY" });
            _context.ExamAttempts.Add(examAttempt);
            await _context.SaveChangesAsync();

            _mockNotificationService
                .Setup(ns => ns.SendPointsNotificationAsync(
                    userId, 0, It.IsAny<int>(),
                    request.CorrectAnswers, request.TotalQuestions,
                    It.IsAny<int>(), It.IsAny<int>(), true))
                .ReturnsAsync(1);

            _mockNotificationService
                .Setup(ns => ns.SendTOEICNotificationAsync(
                    userId, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(2);

            // Act
            var result = await _service.CalculateSeasonScoreAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.SeasonScore); // No points earned
            Assert.Equal(0, result.BasePoints);
            Assert.Equal(0, result.TimeBonus);
            Assert.Equal(0, result.AccuracyBonus);

            // Verify no UserLeaderboard was created
            var userLeaderboard = await _context.UserLeaderboards
                .FirstOrDefaultAsync(ul => ul.UserId == userId && ul.LeaderboardId == currentSeason.LeaderboardId);
            Assert.Null(userLeaderboard);
        }

        [Fact]
        public async Task CalculateSeasonScoreAsync_WhenAccuracyRateAbove80_ShouldAddAccuracyBonus()
        {
            // Arrange
            int userId = 1;
            int examId = 1;
            int examPartId = 1;
            var request = new CalculateScoreRequestDTO
            {
                ExamAttemptId = 1,
                ExamPartId = 1,
                CorrectAnswers = 18, // 90% accuracy (above 80%)
                TotalQuestions = 20,
                TimeSpentSeconds = 100,
                ExpectedTimeSeconds = 120
            };

            var currentSeason = new Leaderboard
            {
                LeaderboardId = 1,
                SeasonNumber = 1,
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(20)
            };

            var examPart = new ExamPart
            {
                PartId = examPartId,
                ExamId = examId,
                PartCode = "LISTENING_P1",
                Title = "Listening Part 1"
            };

            var examAttempt = new ExamAttempt
            {
                AttemptID = request.ExamAttemptId,
                UserID = userId,
                ExamID = examId,
                ExamPartId = examPartId,
                Status = "Completed",
                Score = 18,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                ExamPart = examPart
            };

            _context.Leaderboards.Add(currentSeason);
            _context.ExamParts.Add(examPart);
            _context.Exams.Add(new Exam { ExamId = examId, Name = "Test Exam", ExamType = "TOEIC", Description = "Test Description", ExamSetKey = "TEST_KEY" });
            _context.ExamAttempts.Add(examAttempt);
            await _context.SaveChangesAsync();

            _mockNotificationService
                .Setup(ns => ns.SendPointsNotificationAsync(
                    userId, It.IsAny<int>(), It.IsAny<int>(),
                    request.CorrectAnswers, request.TotalQuestions,
                    It.IsAny<int>(), It.IsAny<int>(), true))
                .ReturnsAsync(1);

            _mockNotificationService
                .Setup(ns => ns.SendTOEICNotificationAsync(
                    userId, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(2);

            // Act
            var result = await _service.CalculateSeasonScoreAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.AccuracyBonus > 0); // Should have accuracy bonus
            Assert.True(result.SeasonScore > result.BasePoints); // Total should be more than base points
        }

        [Fact]
        public async Task CalculateSeasonScoreAsync_WhenAccuracyRateBelow80_ShouldNotAddAccuracyBonus()
        {
            // Arrange
            int userId = 1;
            int examId = 1;
            int examPartId = 1;
            var request = new CalculateScoreRequestDTO
            {
                ExamAttemptId = 1,
                ExamPartId = 1,
                CorrectAnswers = 15, // 75% accuracy (below 80%)
                TotalQuestions = 20,
                TimeSpentSeconds = 100,
                ExpectedTimeSeconds = 120
            };

            var currentSeason = new Leaderboard
            {
                LeaderboardId = 1,
                SeasonNumber = 1,
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(20)
            };

            var examPart = new ExamPart
            {
                PartId = examPartId,
                ExamId = examId,
                PartCode = "LISTENING_P1",
                Title = "Listening Part 1"
            };

            var examAttempt = new ExamAttempt
            {
                AttemptID = request.ExamAttemptId,
                UserID = userId,
                ExamID = examId,
                ExamPartId = examPartId,
                Status = "Completed",
                Score = 15,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                ExamPart = examPart
            };

            _context.Leaderboards.Add(currentSeason);
            _context.ExamParts.Add(examPart);
            _context.Exams.Add(new Exam { ExamId = examId, Name = "Test Exam", ExamType = "TOEIC", Description = "Test Description", ExamSetKey = "TEST_KEY" });
            _context.ExamAttempts.Add(examAttempt);
            await _context.SaveChangesAsync();

            _mockNotificationService
                .Setup(ns => ns.SendPointsNotificationAsync(
                    userId, It.IsAny<int>(), It.IsAny<int>(),
                    request.CorrectAnswers, request.TotalQuestions,
                    It.IsAny<int>(), It.IsAny<int>(), true))
                .ReturnsAsync(1);

            _mockNotificationService
                .Setup(ns => ns.SendTOEICNotificationAsync(
                    userId, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(2);

            // Act
            var result = await _service.CalculateSeasonScoreAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.AccuracyBonus); // Should not have accuracy bonus
        }

        [Fact]
        public async Task CalculateSeasonScoreAsync_WhenExistingUserLeaderboard_ShouldUpdateScore()
        {
            // Arrange
            int userId = 1;
            int examId = 1;
            int examPartId = 1;
            var request = new CalculateScoreRequestDTO
            {
                ExamAttemptId = 1,
                ExamPartId = 1,
                CorrectAnswers = 10,
                TotalQuestions = 20,
                TimeSpentSeconds = 100,
                ExpectedTimeSeconds = 120
            };

            var currentSeason = new Leaderboard
            {
                LeaderboardId = 1,
                SeasonNumber = 1,
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(20)
            };

            var examPart = new ExamPart
            {
                PartId = examPartId,
                ExamId = examId,
                PartCode = "LISTENING_P1",
                Title = "Listening Part 1"
            };

            var examAttempt = new ExamAttempt
            {
                AttemptID = request.ExamAttemptId,
                UserID = userId,
                ExamID = examId,
                ExamPartId = examPartId,
                Status = "Completed",
                Score = 10,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                ExamPart = examPart
            };

            var existingUserLeaderboard = new UserLeaderboard
            {
                UserId = userId,
                LeaderboardId = currentSeason.LeaderboardId,
                Score = 500, // Existing score
                FirstAttemptDate = DateTime.UtcNow.AddDays(-5)
            };

            _context.Leaderboards.Add(currentSeason);
            _context.ExamParts.Add(examPart);
            _context.Exams.Add(new Exam { ExamId = examId, Name = "Test Exam", ExamType = "TOEIC", Description = "Test Description", ExamSetKey = "TEST_KEY" });
            _context.ExamAttempts.Add(examAttempt);
            _context.UserLeaderboards.Add(existingUserLeaderboard);
            await _context.SaveChangesAsync();

            _mockNotificationService
                .Setup(ns => ns.SendPointsNotificationAsync(
                    userId, It.IsAny<int>(), It.IsAny<int>(),
                    request.CorrectAnswers, request.TotalQuestions,
                    It.IsAny<int>(), It.IsAny<int>(), true))
                .ReturnsAsync(1);

            _mockNotificationService
                .Setup(ns => ns.SendTOEICNotificationAsync(
                    userId, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(2);

            // Act
            var result = await _service.CalculateSeasonScoreAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.TotalAccumulatedScore > 500); // Should be more than initial score

            // Verify UserLeaderboard score was updated
            var updatedUserLeaderboard = await _context.UserLeaderboards
                .FirstOrDefaultAsync(ul => ul.UserId == userId && ul.LeaderboardId == currentSeason.LeaderboardId);
            Assert.NotNull(updatedUserLeaderboard);
            Assert.True(updatedUserLeaderboard!.Score > 500); // Should be increased
        }

        [Fact]
        public async Task CalculateSeasonScoreAsync_WhenExamPartIdIs2_ShouldWorkForReading()
        {
            // Arrange
            int userId = 1;
            int examId = 1;
            int examPartId = 2; // Reading
            var request = new CalculateScoreRequestDTO
            {
                ExamAttemptId = 1,
                ExamPartId = 2,
                CorrectAnswers = 10,
                TotalQuestions = 20,
                TimeSpentSeconds = 100,
                ExpectedTimeSeconds = 120
            };

            var currentSeason = new Leaderboard
            {
                LeaderboardId = 1,
                SeasonNumber = 1,
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(20)
            };

            var examPart = new ExamPart
            {
                PartId = examPartId,
                ExamId = examId,
                PartCode = "READING_P5",
                Title = "Reading Part 5"
            };

            var examAttempt = new ExamAttempt
            {
                AttemptID = request.ExamAttemptId,
                UserID = userId,
                ExamID = examId,
                ExamPartId = examPartId,
                Status = "Completed",
                Score = 10,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                ExamPart = examPart
            };

            _context.Leaderboards.Add(currentSeason);
            _context.ExamParts.Add(examPart);
            _context.Exams.Add(new Exam { ExamId = examId, Name = "Test Exam", ExamType = "TOEIC", Description = "Test Description", ExamSetKey = "TEST_KEY" });
            _context.ExamAttempts.Add(examAttempt);
            await _context.SaveChangesAsync();

            _mockNotificationService
                .Setup(ns => ns.SendPointsNotificationAsync(
                    userId, It.IsAny<int>(), It.IsAny<int>(),
                    request.CorrectAnswers, request.TotalQuestions,
                    It.IsAny<int>(), It.IsAny<int>(), true))
                .ReturnsAsync(1);

            _mockNotificationService
                .Setup(ns => ns.SendTOEICNotificationAsync(
                    userId, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(2);

            // Act
            var result = await _service.CalculateSeasonScoreAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.SeasonScore > 0);
        }
        [Fact]
        public async Task CalculateSeasonScoreAsync_WhenCompletedFasterThanExpected_ShouldAddTimeBonus()
        {
            // Arrange
            int userId = 1;
            int examId = 1;
            int examPartId = 1;
            var request = new CalculateScoreRequestDTO
            {
                ExamAttemptId = 1,
                ExamPartId = 1,
                CorrectAnswers = 10,
                TotalQuestions = 20,
                TimeSpentSeconds = 60, // 50% of expected time
                ExpectedTimeSeconds = 120
            };

            var currentSeason = new Leaderboard
            {
                LeaderboardId = 1,
                SeasonNumber = 1,
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(20)
            };

            var examPart = new ExamPart
            {
                PartId = examPartId,
                ExamId = examId,
                PartCode = "LISTENING_P1",
                Title = "Listening Part 1"
            };

            var examAttempt = new ExamAttempt
            {
                AttemptID = request.ExamAttemptId,
                UserID = userId,
                ExamID = examId,
                ExamPartId = examPartId,
                Status = "Completed",
                Score = 10,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                ExamPart = examPart
            };

            _context.Leaderboards.Add(currentSeason);
            _context.ExamParts.Add(examPart);
            _context.Exams.Add(new Exam { ExamId = examId, Name = "Test Exam", ExamType = "TOEIC", Description = "Test Description", ExamSetKey = "TEST_KEY" });
            _context.ExamAttempts.Add(examAttempt);
            await _context.SaveChangesAsync();

            _mockNotificationService
                .Setup(ns => ns.SendPointsNotificationAsync(
                    userId, It.IsAny<int>(), It.IsAny<int>(),
                    request.CorrectAnswers, request.TotalQuestions,
                    It.IsAny<int>(), It.IsAny<int>(), true))
                .ReturnsAsync(1);

            _mockNotificationService
                .Setup(ns => ns.SendTOEICNotificationAsync(
                    userId, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(2);

            // Act
            var result = await _service.CalculateSeasonScoreAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.TimeBonus > 0); // Should have time bonus
            Assert.True(result.SeasonScore > result.BasePoints);
        }
    }
}
