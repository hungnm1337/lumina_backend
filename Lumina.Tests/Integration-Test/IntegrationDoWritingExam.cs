using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using lumina.Controllers;
using ServiceLayer.Exam.ExamAttempt;
using ServiceLayer.Exam.Writting;
using DataLayer.DTOs.Exam.Writting;
using DataLayer.DTOs.UserAnswer;

namespace Lumina.Tests.IntegrationTest
{
    public class IntegrationDoWritingExam
    {
        private readonly Mock<IExamAttemptService> _mockExamAttemptService;
        private readonly Mock<IWritingService> _mockWritingService;
        private readonly Mock<ILogger<ExamAttemptController>> _mockExamAttemptLogger;
        private readonly Mock<ILogger<WritingController>> _mockWritingLogger;
        private readonly ExamAttemptController _examAttemptController;
        private readonly WritingController _writingController;

        public IntegrationDoWritingExam()
        {
            _mockExamAttemptService = new Mock<IExamAttemptService>();
            _mockWritingService = new Mock<IWritingService>();
            _mockExamAttemptLogger = new Mock<ILogger<ExamAttemptController>>();
            _mockWritingLogger = new Mock<ILogger<WritingController>>();
            
            _examAttemptController = new ExamAttemptController(_mockExamAttemptService.Object, _mockExamAttemptLogger.Object);
            _writingController = new WritingController(_mockWritingService.Object, _mockWritingLogger.Object);
        }

        [Fact]
        public async Task IntegrationTest_LuongLamBaiThiWritingP1_ThanhCong()
        {
            // ========== STEP 1: START AN EXAM (Tạo attempt ID) ==========
            int userId = 1;
            int examId = 1;
            int attemptId = 200;

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
            Assert.Equal("In Progress", startResponse.Status);

            // Lưu attemptId vào "local storage"
            int savedAttemptId = startResponse.AttemptID;

            // ========== STEP 2: GET FEEDBACK P1 FROM AI (Người dùng nộp bài để nhận feedback) ==========
            var getFeedbackRequest = new WritingRequestP1DTO
            {
                PictureCaption = "A woman is reading a book in the library",
                UserAnswer = "The woman is reading a book in the library. She is sitting at a table."
            };

            var feedbackResponse = new WritingResponseDTO
            {
                TotalScore = 8,
                GrammarFeedback = "Good job! No major grammar errors detected.",
                VocabularyFeedback = "Appropriate vocabulary for the context.",
                ContentAccuracyFeedback = "Your description is accurate and clear.",
                CorreededAnswerProposal = "The woman is reading a book in the library. She is sitting at a table and appears focused."
            };

            _mockWritingService
                .Setup(s => s.GetFeedbackP1FromAI(It.IsAny<WritingRequestP1DTO>()))
                .ReturnsAsync(feedbackResponse);

            // Act - Step 2: Nhận feedback từ AI
            var feedbackResult = await _writingController.GetFeedbackP1FromAI(getFeedbackRequest);

            // Assert - Step 2: Kiểm tra feedback đã được trả về
            var feedbackOkResult = Assert.IsType<OkObjectResult>(feedbackResult);
            var feedbackData = Assert.IsType<WritingResponseDTO>(feedbackOkResult.Value);
            Assert.Equal(8, feedbackData.TotalScore);
            Assert.Contains("Good job", feedbackData.GrammarFeedback);

            // ========== STEP 3: SAVE WRITING ANSWER (Lưu thông tin bài làm và feedback) ==========
            var saveAnswerRequest = new WritingAnswerRequestDTO
            {
                AttemptID = savedAttemptId,
                QuestionId = 50,
                UserAnswerContent = getFeedbackRequest.UserAnswer,
                FeedbackFromAI = feedbackData.GrammarFeedback + " | " + feedbackData.VocabularyFeedback
            };

            _mockWritingService
                .Setup(s => s.SaveWritingAnswer(It.IsAny<WritingAnswerRequestDTO>()))
                .ReturnsAsync(true);

            // Act - Step 3: Lưu bài làm
            var saveResult = await _writingController.SaveWritingAnswer(saveAnswerRequest);

            // Assert - Step 3: Kiểm tra đã lưu thành công
            var saveOkResult = Assert.IsType<OkObjectResult>(saveResult);
            var saveValue = saveOkResult.Value;
            Assert.NotNull(saveValue);
            var messageProperty = saveValue.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(saveValue)?.ToString();
            Assert.Equal("Writing answer saved successfully.", message);

            // ========== STEP 4: GET EXAM ATTEMPT BY ID (Người dùng xem lại bài làm) ==========
            var examAttemptDetails = new ExamAttemptDetailResponseDTO
            {
                ExamAttemptInfo = new ExamAttemptResponseDTO
                {
                    AttemptID = savedAttemptId,
                    UserName = "Test User",
                    ExamName = "Writing Test",
                    StartTime = startExamResponse.StartTime,
                    Status = "In Progress",
                    Score = feedbackData.TotalScore
                },
                WritingAnswers = new System.Collections.Generic.List<WritingAnswerResponseDTO>
                {
                    new WritingAnswerResponseDTO
                    {
                        UserAnswerWritingId = 1,
                        AttemptID = savedAttemptId,
                        Question = new DataLayer.DTOs.Exam.QuestionDTO 
                        { 
                            QuestionId = 50, 
                            PartId = 1,
                            QuestionType = "Writing",
                            StemText = "Describe the picture",
                            QuestionNumber = 1,
                            ScoreWeight = 10,
                            Time = 300
                        },
                        UserAnswerContent = getFeedbackRequest.UserAnswer,
                        FeedbackFromAI = feedbackData.GrammarFeedback + " | " + feedbackData.VocabularyFeedback
                    }
                }
            };

            _mockExamAttemptService
                .Setup(s => s.GetExamAttemptById(savedAttemptId))
                .ReturnsAsync(examAttemptDetails);

            // Act - Step 4: Xem lại bài làm
            var detailsResult = await _examAttemptController.GetExamAttemptById(savedAttemptId);

            // Assert - Step 4: Kiểm tra thông tin bài làm
            var detailsOkResult = Assert.IsType<OkObjectResult>(detailsResult.Result);
            var detailsResponse = Assert.IsType<ExamAttemptDetailResponseDTO>(detailsOkResult.Value);
            Assert.NotNull(detailsResponse.ExamAttemptInfo);
            Assert.Equal(savedAttemptId, detailsResponse.ExamAttemptInfo.AttemptID);
            Assert.NotNull(detailsResponse.WritingAnswers);
            Assert.Single(detailsResponse.WritingAnswers);
            Assert.Equal(getFeedbackRequest.UserAnswer, detailsResponse.WritingAnswers[0].UserAnswerContent);
            Assert.NotNull(detailsResponse.WritingAnswers[0].FeedbackFromAI);
            Assert.Contains("Good job", detailsResponse.WritingAnswers[0].FeedbackFromAI);

            // Verify tất cả các services đã được gọi đúng số lần
            _mockExamAttemptService.Verify(s => s.StartAnExam(It.IsAny<ExamAttemptRequestDTO>()), Times.Once);
            _mockWritingService.Verify(s => s.GetFeedbackP1FromAI(It.IsAny<WritingRequestP1DTO>()), Times.Once);
            _mockWritingService.Verify(s => s.SaveWritingAnswer(It.IsAny<WritingAnswerRequestDTO>()), Times.Once);
            _mockExamAttemptService.Verify(s => s.GetExamAttemptById(savedAttemptId), Times.Once);
        }

        [Fact]
        public async Task IntegrationTest_LuongLamBaiThiWritingP23_ThanhCong()
        {
            // ========== STEP 1: START AN EXAM ==========
            int userId = 1;
            int examId = 2;
            int attemptId = 201;

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

            // ========== STEP 2: GET FEEDBACK P23 FROM AI ==========
            var getFeedbackRequest = new WritingRequestP23DTO
            {
                Prompt = "Write an essay about the importance of learning English",
                UserAnswer = "Learning English is very important in today's world. It helps people communicate globally and access more opportunities."
            };

            var feedbackResponse = new WritingResponseDTO
            {
                TotalScore = 7,
                GrammarFeedback = "Minor grammar issues detected.",
                VocabularyFeedback = "Good vocabulary range.",
                ContentAccuracyFeedback = "Good essay structure. Ideas are well connected.",
                CorreededAnswerProposal = "Learning English is very important in today's world. It helps people communicate globally and access more opportunities. Furthermore, it opens doors to education and career advancement."
            };

            _mockWritingService
                .Setup(s => s.GetFeedbackP23FromAI(It.IsAny<WritingRequestP23DTO>()))
                .ReturnsAsync(feedbackResponse);

            var feedbackResult = await _writingController.GetFeedbackP23FromAI(getFeedbackRequest);
            var feedbackOkResult = Assert.IsType<OkObjectResult>(feedbackResult);
            var feedbackData = Assert.IsType<WritingResponseDTO>(feedbackOkResult.Value);
            Assert.Equal(7, feedbackData.TotalScore);

            // ========== STEP 3: SAVE WRITING ANSWER ==========
            var saveAnswerRequest = new WritingAnswerRequestDTO
            {
                AttemptID = savedAttemptId,
                QuestionId = 51,
                UserAnswerContent = getFeedbackRequest.UserAnswer,
                FeedbackFromAI = feedbackData.GrammarFeedback + " | " + feedbackData.VocabularyFeedback
            };

            _mockWritingService
                .Setup(s => s.SaveWritingAnswer(It.IsAny<WritingAnswerRequestDTO>()))
                .ReturnsAsync(true);

            var saveResult = await _writingController.SaveWritingAnswer(saveAnswerRequest);
            var saveOkResult = Assert.IsType<OkObjectResult>(saveResult);
            Assert.NotNull(saveOkResult.Value);

            // ========== STEP 4: GET EXAM ATTEMPT BY ID ==========
            var examAttemptDetails = new ExamAttemptDetailResponseDTO
            {
                ExamAttemptInfo = new ExamAttemptResponseDTO
                {
                    AttemptID = savedAttemptId,
                    Status = "In Progress",
                    Score = feedbackData.TotalScore
                },
                WritingAnswers = new System.Collections.Generic.List<WritingAnswerResponseDTO>
                {
                    new WritingAnswerResponseDTO
                    {
                        UserAnswerWritingId = 1,
                        AttemptID = savedAttemptId,
                        Question = new DataLayer.DTOs.Exam.QuestionDTO 
                        { 
                            QuestionId = 51, 
                            PartId = 2,
                            QuestionType = "Writing",
                            StemText = "Write an essay about the importance of learning English",
                            QuestionNumber = 1,
                            ScoreWeight = 15,
                            Time = 600
                        },
                        UserAnswerContent = getFeedbackRequest.UserAnswer,
                        FeedbackFromAI = feedbackData.GrammarFeedback + " | " + feedbackData.VocabularyFeedback
                    }
                }
            };

            _mockExamAttemptService
                .Setup(s => s.GetExamAttemptById(savedAttemptId))
                .ReturnsAsync(examAttemptDetails);

            var detailsResult = await _examAttemptController.GetExamAttemptById(savedAttemptId);
            var detailsOkResult = Assert.IsType<OkObjectResult>(detailsResult.Result);
            var detailsResponse = Assert.IsType<ExamAttemptDetailResponseDTO>(detailsOkResult.Value);
            Assert.NotNull(detailsResponse.WritingAnswers);
            Assert.Single(detailsResponse.WritingAnswers);

            // Verify
            _mockWritingService.Verify(s => s.GetFeedbackP23FromAI(It.IsAny<WritingRequestP23DTO>()), Times.Once);
        }

        [Fact]
        public async Task IntegrationTest_LuongLamBaiThiWriting_GetFeedbackP1Failed_EmptyUserAnswer()
        {
            // Arrange
            var getFeedbackRequest = new WritingRequestP1DTO
            {
                PictureCaption = "A woman is reading a book",
                UserAnswer = "" // Empty
            };

            // Act
            var feedbackResult = await _writingController.GetFeedbackP1FromAI(getFeedbackRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(feedbackResult);
            var value = badRequestResult.Value;
            Assert.NotNull(value);
            var messageProperty = value.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("UserAnswer cannot be empty.", message);

            // Verify service không được gọi
            _mockWritingService.Verify(s => s.GetFeedbackP1FromAI(It.IsAny<WritingRequestP1DTO>()), Times.Never);
        }

        [Fact]
        public async Task IntegrationTest_LuongLamBaiThiWriting_SaveAnswerFailed_InvalidAttemptID()
        {
            // Arrange
            var saveAnswerRequest = new WritingAnswerRequestDTO
            {
                AttemptID = 0, // Invalid
                QuestionId = 50,
                UserAnswerContent = "Test answer",
                FeedbackFromAI = "Test feedback"
            };

            // Act
            var saveResult = await _writingController.SaveWritingAnswer(saveAnswerRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(saveResult);
            var value = badRequestResult.Value;
            Assert.NotNull(value);
            var messageProperty = value.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("Invalid AttemptID.", message);

            // Verify service không được gọi
            _mockWritingService.Verify(s => s.SaveWritingAnswer(It.IsAny<WritingAnswerRequestDTO>()), Times.Never);
        }

        [Fact]
        public async Task IntegrationTest_LuongLamBaiThiWriting_SaveAnswerFailed_EmptyUserAnswerContent()
        {
            // Arrange
            var saveAnswerRequest = new WritingAnswerRequestDTO
            {
                AttemptID = 200,
                QuestionId = 50,
                UserAnswerContent = "", // Empty
                FeedbackFromAI = "Test feedback"
            };

            // Act
            var saveResult = await _writingController.SaveWritingAnswer(saveAnswerRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(saveResult);
            var value = badRequestResult.Value;
            Assert.NotNull(value);
            var messageProperty = value.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("UserAnswerContent cannot be empty.", message);

            // Verify service không được gọi
            _mockWritingService.Verify(s => s.SaveWritingAnswer(It.IsAny<WritingAnswerRequestDTO>()), Times.Never);
        }
    }
}
