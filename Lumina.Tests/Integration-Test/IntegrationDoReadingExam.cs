using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using lumina.Controllers;
using ServiceLayer.Exam.ExamAttempt;
using ServiceLayer.Exam.Reading;
using DataLayer.DTOs.Exam;
using DataLayer.DTOs.UserAnswer;

namespace Lumina.Tests.IntegrationTest
{
    public class IntegrationDoReadingExam
    {
        private readonly Mock<IExamAttemptService> _mockExamAttemptService;
        private readonly Mock<IReadingService> _mockReadingService;
        private readonly Mock<ILogger<ExamAttemptController>> _mockLogger;
        private readonly ExamAttemptController _examAttemptController;
        private readonly ReadingController _readingController;

        public IntegrationDoReadingExam()
        {
            _mockExamAttemptService = new Mock<IExamAttemptService>();
            _mockReadingService = new Mock<IReadingService>();
            _mockLogger = new Mock<ILogger<ExamAttemptController>>();
            
            _examAttemptController = new ExamAttemptController(_mockExamAttemptService.Object, _mockLogger.Object);
            _readingController = new ReadingController(_mockReadingService.Object);
        }

        [Fact]
        public async Task IntegrationTest_LuongLamBaiThiReading_ThanhCong()
        {
            // ========== STEP 1: START AN EXAM (Tạo attempt ID) ==========
            // Arrange
            int userId = 1;
            int examId = 1;
            int attemptId = 100; // ID được tạo ra từ backend (lưu vào local storage)

            var startExamRequest = new ExamAttemptRequestDTO
            {
                UserID = userId,
                ExamID = examId
            };

            var startExamResponse = new ExamAttemptRequestDTO
            {
                AttemptID = attemptId,
                UserID = userId,
                ExamID = examId,
                StartTime = DateTime.UtcNow,
                Status = "In Progress"
            };

            _mockExamAttemptService
                .Setup(s => s.StartAnExam(It.IsAny<ExamAttemptRequestDTO>()))
                .ReturnsAsync(startExamResponse);

            // Act - Step 1: Bắt đầu làm bài thi
            var startResult = await _examAttemptController.StartAnExam(startExamRequest);

            // Assert - Step 1: Kiểm tra exam đã bắt đầu thành công
            var startOkResult = Assert.IsType<OkObjectResult>(startResult);
            var startResponse = Assert.IsType<ExamAttemptRequestDTO>(startOkResult.Value);
            Assert.Equal(attemptId, startResponse.AttemptID);
            Assert.Equal(userId, startResponse.UserID);
            Assert.Equal(examId, startResponse.ExamID);
            Assert.Equal("In Progress", startResponse.Status);

            // Lưu attemptId vào "local storage" (trong test này là biến)
            int savedAttemptId = startResponse.AttemptID;

            // ========== STEP 2: SUBMIT ANSWER (Người dùng nộp đáp án) ==========
            // Arrange - Người dùng trả lời câu hỏi 1
            var submitAnswerRequest = new ReadingAnswerRequestDTO
            {
                ExamAttemptId = savedAttemptId,
                QuestionId = 10,
                SelectedOptionId = 2
            };

            var submitAnswerResponse = new SubmitAnswerResponseDTO
            {
                Success = true,
                IsCorrect = true,
                Score = 5,
                Message = "Answer submitted successfully"
            };

            _mockReadingService
                .Setup(s => s.SubmitAnswerAsync(It.IsAny<ReadingAnswerRequestDTO>()))
                .ReturnsAsync(submitAnswerResponse);

            // Act - Step 2: Nộp đáp án
            var submitResult = await _readingController.SubmitAnswer(submitAnswerRequest);

            // Assert - Step 2: Kiểm tra đáp án đã được nộp thành công
            var submitOkResult = Assert.IsType<OkObjectResult>(submitResult);
            var submitResponse = Assert.IsType<SubmitAnswerResponseDTO>(submitOkResult.Value);
            Assert.True(submitResponse.Success);
            Assert.True(submitResponse.IsCorrect);
            Assert.Equal(5, submitResponse.Score);

            // ========== STEP 3: END AN EXAM (Người dùng nộp bài thi) ==========
            // Arrange
            var endExamRequest = new ExamAttemptRequestDTO
            {
                AttemptID = savedAttemptId
            };

            var endExamResponse = new ExamAttemptRequestDTO
            {
                AttemptID = savedAttemptId,
                UserID = userId,
                ExamID = examId,
                EndTime = DateTime.UtcNow,
                Status = "Completed",
                Score = 85
            };

            _mockExamAttemptService
                .Setup(s => s.EndAnExam(It.IsAny<ExamAttemptRequestDTO>()))
                .ReturnsAsync(endExamResponse);

            // Act - Step 3: Kết thúc bài thi
            var endResult = await _examAttemptController.EndAnExam(endExamRequest);

            // Assert - Step 3: Kiểm tra bài thi đã kết thúc
            var endOkResult = Assert.IsType<OkObjectResult>(endResult);
            var endResponse = Assert.IsType<ExamAttemptRequestDTO>(endOkResult.Value);
            Assert.Equal(savedAttemptId, endResponse.AttemptID);
            Assert.Equal("Completed", endResponse.Status);
            Assert.Equal(85, endResponse.Score);
            Assert.NotNull(endResponse.EndTime);

            // ========== STEP 4: GET EXAM ATTEMPT BY ID (Người dùng xem kết quả) ==========
            // Arrange
            var examAttemptDetails = new ExamAttemptDetailResponseDTO
            {
                ExamAttemptInfo = new ExamAttemptResponseDTO
                {
                    AttemptID = savedAttemptId,
                    UserName = "Test User",
                    ExamName = "Test Exam",
                    StartTime = startExamResponse.StartTime,
                    EndTime = endExamResponse.EndTime,
                    Status = "Completed",
                    Score = 85
                },
                ReadingAnswers = new System.Collections.Generic.List<ReadingAnswerResponseDTO>()
            };

            _mockExamAttemptService
                .Setup(s => s.GetExamAttemptById(savedAttemptId))
                .ReturnsAsync(examAttemptDetails);

            // Act - Step 4: Xem kết quả bài thi
            var detailsResult = await _examAttemptController.GetExamAttemptById(savedAttemptId);

            // Assert - Step 4: Kiểm tra thông tin kết quả
            var detailsOkResult = Assert.IsType<OkObjectResult>(detailsResult.Result);
            var detailsResponse = Assert.IsType<ExamAttemptDetailResponseDTO>(detailsOkResult.Value);
            Assert.NotNull(detailsResponse.ExamAttemptInfo);
            Assert.Equal(savedAttemptId, detailsResponse.ExamAttemptInfo.AttemptID);
            Assert.Equal("Completed", detailsResponse.ExamAttemptInfo.Status);
            Assert.Equal(85, detailsResponse.ExamAttemptInfo.Score);

            // Verify tất cả các services đã được gọi đúng số lần
            _mockExamAttemptService.Verify(s => s.StartAnExam(It.IsAny<ExamAttemptRequestDTO>()), Times.Once);
            _mockReadingService.Verify(s => s.SubmitAnswerAsync(It.IsAny<ReadingAnswerRequestDTO>()), Times.Once);
            _mockExamAttemptService.Verify(s => s.EndAnExam(It.IsAny<ExamAttemptRequestDTO>()), Times.Once);
            _mockExamAttemptService.Verify(s => s.GetExamAttemptById(savedAttemptId), Times.Once);
        }

        [Fact]
        public async Task IntegrationTest_LuongLamBaiThiReading_NhieuCauTraLoi_ThanhCong()
        {
            // ========== STEP 1: START AN EXAM ==========
            int userId = 1;
            int examId = 1;
            int attemptId = 101;

            var startExamRequest = new ExamAttemptRequestDTO { UserID = userId, ExamID = examId };
            var startExamResponse = new ExamAttemptRequestDTO
            {
                AttemptID = attemptId,
                UserID = userId,
                ExamID = examId,
                StartTime = DateTime.UtcNow,
                Status = "In Progress"
            };

            _mockExamAttemptService
                .Setup(s => s.StartAnExam(It.IsAny<ExamAttemptRequestDTO>()))
                .ReturnsAsync(startExamResponse);

            var startResult = await _examAttemptController.StartAnExam(startExamRequest);
            var startOkResult = Assert.IsType<OkObjectResult>(startResult);
            var startResponse = Assert.IsType<ExamAttemptRequestDTO>(startOkResult.Value);
            int savedAttemptId = startResponse.AttemptID;

            // ========== STEP 2: SUBMIT MULTIPLE ANSWERS ==========
            // Submit Answer 1 - Correct
            var answer1 = new ReadingAnswerRequestDTO { ExamAttemptId = savedAttemptId, QuestionId = 1, SelectedOptionId = 1 };
            var response1 = new SubmitAnswerResponseDTO { Success = true, IsCorrect = true, Score = 5 };
            _mockReadingService.Setup(s => s.SubmitAnswerAsync(It.Is<ReadingAnswerRequestDTO>(r => r.QuestionId == 1)))
                .ReturnsAsync(response1);
            
            var result1 = await _readingController.SubmitAnswer(answer1);
            var okResult1 = Assert.IsType<OkObjectResult>(result1);
            var submitResponse1 = Assert.IsType<SubmitAnswerResponseDTO>(okResult1.Value);
            Assert.True(submitResponse1.IsCorrect);

            // Submit Answer 2 - Incorrect
            var answer2 = new ReadingAnswerRequestDTO { ExamAttemptId = savedAttemptId, QuestionId = 2, SelectedOptionId = 3 };
            var response2 = new SubmitAnswerResponseDTO { Success = true, IsCorrect = false, Score = 0 };
            _mockReadingService.Setup(s => s.SubmitAnswerAsync(It.Is<ReadingAnswerRequestDTO>(r => r.QuestionId == 2)))
                .ReturnsAsync(response2);
            
            var result2 = await _readingController.SubmitAnswer(answer2);
            var okResult2 = Assert.IsType<OkObjectResult>(result2);
            var submitResponse2 = Assert.IsType<SubmitAnswerResponseDTO>(okResult2.Value);
            Assert.False(submitResponse2.IsCorrect);

            // Submit Answer 3 - Correct
            var answer3 = new ReadingAnswerRequestDTO { ExamAttemptId = savedAttemptId, QuestionId = 3, SelectedOptionId = 2 };
            var response3 = new SubmitAnswerResponseDTO { Success = true, IsCorrect = true, Score = 5 };
            _mockReadingService.Setup(s => s.SubmitAnswerAsync(It.Is<ReadingAnswerRequestDTO>(r => r.QuestionId == 3)))
                .ReturnsAsync(response3);
            
            var result3 = await _readingController.SubmitAnswer(answer3);
            var okResult3 = Assert.IsType<OkObjectResult>(result3);
            var submitResponse3 = Assert.IsType<SubmitAnswerResponseDTO>(okResult3.Value);
            Assert.True(submitResponse3.IsCorrect);

            // ========== STEP 3: END EXAM ==========
            var endExamRequest = new ExamAttemptRequestDTO { AttemptID = savedAttemptId };
            var endExamResponse = new ExamAttemptRequestDTO
            {
                AttemptID = savedAttemptId,
                EndTime = DateTime.UtcNow,
                Status = "Completed",
                Score = 67
            };

            _mockExamAttemptService
                .Setup(s => s.EndAnExam(It.IsAny<ExamAttemptRequestDTO>()))
                .ReturnsAsync(endExamResponse);

            var endResult = await _examAttemptController.EndAnExam(endExamRequest);
            var endOkResult = Assert.IsType<OkObjectResult>(endResult);
            var endResponse = Assert.IsType<ExamAttemptRequestDTO>(endOkResult.Value);
            Assert.Equal("Completed", endResponse.Status);

            // ========== STEP 4: GET RESULTS ==========
            var details = new ExamAttemptDetailResponseDTO
            {
                ExamAttemptInfo = new ExamAttemptResponseDTO
                {
                    AttemptID = savedAttemptId,
                    Status = "Completed",
                    Score = 67
                },
                ReadingAnswers = new System.Collections.Generic.List<ReadingAnswerResponseDTO>()
            };

            _mockExamAttemptService
                .Setup(s => s.GetExamAttemptById(savedAttemptId))
                .ReturnsAsync(details);

            var detailsResult = await _examAttemptController.GetExamAttemptById(savedAttemptId);
            var detailsOkResult = Assert.IsType<OkObjectResult>(detailsResult.Result);
            var detailsResponse = Assert.IsType<ExamAttemptDetailResponseDTO>(detailsOkResult.Value);
            Assert.NotNull(detailsResponse.ExamAttemptInfo);
            Assert.Equal(savedAttemptId, detailsResponse.ExamAttemptInfo.AttemptID);
            Assert.NotNull(detailsResponse.ReadingAnswers);
            Assert.Empty(detailsResponse.ReadingAnswers); // No answers in mock for simplicity

            // Verify
            _mockReadingService.Verify(s => s.SubmitAnswerAsync(It.IsAny<ReadingAnswerRequestDTO>()), Times.Exactly(3));
        }

        [Fact]
        public async Task IntegrationTest_LuongLamBaiThiReading_StartExamFailed_InvalidUserId()
        {
            // Arrange
            var startExamRequest = new ExamAttemptRequestDTO
            {
                UserID = 0, // Invalid
                ExamID = 1
            };

            // Act
            var startResult = await _examAttemptController.StartAnExam(startExamRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(startResult);
            var value = badRequestResult.Value;
            Assert.NotNull(value);
            var messageProperty = value.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("Invalid UserID.", message);

            // Verify service không được gọi
            _mockExamAttemptService.Verify(s => s.StartAnExam(It.IsAny<ExamAttemptRequestDTO>()), Times.Never);
        }

        [Fact]
        public async Task IntegrationTest_LuongLamBaiThiReading_EndExamFailed_AttemptNotFound()
        {
            // Arrange
            var endExamRequest = new ExamAttemptRequestDTO
            {
                AttemptID = 999 // Không tồn tại
            };

            _mockExamAttemptService
                .Setup(s => s.EndAnExam(It.IsAny<ExamAttemptRequestDTO>()))
                .ThrowsAsync(new KeyNotFoundException("Exam attempt not found"));

            // Act
            var endResult = await _examAttemptController.EndAnExam(endExamRequest);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(endResult);
            var value = notFoundResult.Value;
            Assert.NotNull(value);
            var messageProperty = value.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("Exam attempt not found", message);
        }
    }
}