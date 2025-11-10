using DataLayer.DTOs.Exam;
using DataLayer.DTOs.ExamPart;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.Exam;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Lumina.Tests
{
    public class ExamControllerTests
    {
        private readonly Mock<IExamService> _mockExamService;
        private readonly ExamController _controller;

        public ExamControllerTests()
        {
            _mockExamService = new Mock<IExamService>();
            _controller = new ExamController(_mockExamService.Object);
        }

        #region GetAllExams Tests

        [Fact]
        public async Task GetAllExams_NoFilters_ReturnsOkWithExamList()
        {
            // Arrange
            var expectedExams = new List<ExamDTO>
            {
                new ExamDTO { ExamId = 1, Name = "Exam 1", ExamType = "Practice" },
                new ExamDTO { ExamId = 2, Name = "Exam 2", ExamType = "Mock" }
            };
            _mockExamService.Setup(s => s.GetAllExams(null, null))
                .ReturnsAsync(expectedExams);

            // Act
            var result = await _controller.GetAllExams(null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedExams = Assert.IsAssignableFrom<List<ExamDTO>>(okResult.Value);
            Assert.Equal(2, returnedExams.Count);
            _mockExamService.Verify(s => s.GetAllExams(null, null), Times.Once);
        }

        [Fact]
        public async Task GetAllExams_WithExamTypeFilter_ReturnsFilteredExams()
        {
            // Arrange
            var expectedExams = new List<ExamDTO>
            {
                new ExamDTO { ExamId = 1, Name = "Exam 1", ExamType = "Practice" }
            };
            _mockExamService.Setup(s => s.GetAllExams("Practice", null))
                .ReturnsAsync(expectedExams);

            // Act
            var result = await _controller.GetAllExams("Practice", null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedExams = Assert.IsAssignableFrom<List<ExamDTO>>(okResult.Value);
            Assert.Single(returnedExams);
            Assert.Equal("Practice", returnedExams[0].ExamType);
        }

        [Fact]
        public async Task GetAllExams_WithPartCodeFilter_ReturnsFilteredExams()
        {
            // Arrange
            var expectedExams = new List<ExamDTO>
            {
                new ExamDTO { ExamId = 1, Name = "Exam 1" }
            };
            _mockExamService.Setup(s => s.GetAllExams(null, "Part1"))
                .ReturnsAsync(expectedExams);

            // Act
            var result = await _controller.GetAllExams(null, "Part1");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedExams = Assert.IsAssignableFrom<List<ExamDTO>>(okResult.Value);
            Assert.Single(returnedExams);
        }

        [Fact]
        public async Task GetAllExams_WithBothFilters_ReturnsFilteredExams()
        {
            // Arrange
            var expectedExams = new List<ExamDTO>();
            _mockExamService.Setup(s => s.GetAllExams("Mock", "Part2"))
                .ReturnsAsync(expectedExams);

            // Act
            var result = await _controller.GetAllExams("Mock", "Part2");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedExams = Assert.IsAssignableFrom<List<ExamDTO>>(okResult.Value);
            Assert.Empty(returnedExams);
        }

        #endregion

        #region GetExamDetailAndPart Tests

        [Fact]
        public async Task GetExamDetailAndPart_ValidExamId_ReturnsOkWithExamDetail()
        {
            // Arrange
            int examId = 1;
            var expectedExam = new ExamDTO
            {
                ExamId = examId,
                Name = "Test Exam",
                ExamParts = new List<ExamPartDTO>
                {
                    new ExamPartDTO { PartId = 1, PartCode = "Part1" }
                }
            };
            _mockExamService.Setup(s => s.GetExamDetailAndExamPartByExamID(examId))
                .ReturnsAsync(expectedExam);

            // Act
            var result = await _controller.GetExamDetailAndPart(examId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedExam = Assert.IsType<ExamDTO>(okResult.Value);
            Assert.Equal(examId, returnedExam.ExamId);
            Assert.Single(returnedExam.ExamParts);
        }

        [Fact]
        public async Task GetExamDetailAndPart_ExamNotFound_ReturnsNotFound()
        {
            // Arrange
            int examId = 999;
            _mockExamService.Setup(s => s.GetExamDetailAndExamPartByExamID(examId))
                .ReturnsAsync((ExamDTO)null);

            // Act
            var result = await _controller.GetExamDetailAndPart(examId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal($"Exam with ID {examId} not found.", notFoundResult.Value);
        }

        [Fact]
        public async Task GetExamDetailAndPart_ZeroId_ReturnsNotFound()
        {
            // Arrange
            int examId = 0;
            _mockExamService.Setup(s => s.GetExamDetailAndExamPartByExamID(examId))
                .ReturnsAsync((ExamDTO)null);

            // Act
            var result = await _controller.GetExamDetailAndPart(examId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        #endregion

        #region GetExamPartDetailAndQuestion Tests

        [Fact]
        public async Task GetExamPartDetailAndQuestion_ValidPartId_ReturnsOkWithPartDetail()
        {
            // Arrange
            int partId = 1;
            var expectedPart = new ExamPartDTO
            {
                PartId = partId,
                PartCode = "Part1",
                Questions = new List<QuestionDTO>
                {
                    new QuestionDTO { QuestionId = 1, StemText = "Question 1" }
                }
            };
            _mockExamService.Setup(s => s.GetExamPartDetailAndQuestionByExamPartID(partId))
                .ReturnsAsync(expectedPart);

            // Act
            var result = await _controller.GetExamPartDetailAndQuestion(partId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedPart = Assert.IsType<ExamPartDTO>(okResult.Value);
            Assert.Equal(partId, returnedPart.PartId);
            Assert.Single(returnedPart.Questions);
        }

        [Fact]
        public async Task GetExamPartDetailAndQuestion_PartNotFound_ReturnsNotFound()
        {
            // Arrange
            int partId = 999;
            _mockExamService.Setup(s => s.GetExamPartDetailAndQuestionByExamPartID(partId))
                .ReturnsAsync((ExamPartDTO)null);

            // Act
            var result = await _controller.GetExamPartDetailAndQuestion(partId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal($"Exam part with ID {partId} not found.", notFoundResult.Value);
        }

        #endregion

        #region CreateExams Tests

        [Fact]
        public async Task CreateExams_ValidToken_ReturnsOkWithSuccessMessage()
        {
            // Arrange
            string toExamSetKey = "11-2025";
            int userId = 1;
            
            var claims = new List<Claim>
            {
                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", userId.ToString())
            };
            var identity = new ClaimsIdentity(claims);
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            _mockExamService.Setup(s => s.CreateExamFormatAsync("10-2025", toExamSetKey, userId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.CreateExams(toExamSetKey);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            var messageProperty = response.GetType().GetProperty("message");
            Assert.Equal("Tạo bài Exam thành công!", messageProperty.GetValue(response));
        }

        [Fact]
        public async Task CreateExams_NoUserIdInToken_ReturnsUnauthorized()
        {
            // Arrange
            string toExamSetKey = "11-2025";
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            // Act
            var result = await _controller.CreateExams(toExamSetKey);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = unauthorizedResult.Value;
            var messageProperty = response.GetType().GetProperty("message");
            Assert.Equal("Không tìm thấy thông tin người dùng trong token", messageProperty.GetValue(response));
        }

        [Fact]
        public async Task CreateExams_ExamAlreadyExists_ReturnsBadRequest()
        {
            // Arrange
            string toExamSetKey = "11-2025";
            int userId = 1;
            
            var claims = new List<Claim>
            {
                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", userId.ToString())
            };
            var identity = new ClaimsIdentity(claims);
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            _mockExamService.Setup(s => s.CreateExamFormatAsync("10-2025", toExamSetKey, userId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.CreateExams(toExamSetKey);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;
            var messageProperty = response.GetType().GetProperty("message");
            Assert.Equal($"Bài Exams tháng {toExamSetKey} đã tồn tại rồi", messageProperty.GetValue(response));
        }

        #endregion

        #region GetAllExamsWithParts Tests

        [Fact]
        public async Task GetAllExamsWithParts_ReturnsOkWithGroupedData()
        {
            // Arrange
            var expectedData = new List<ExamGroupBySetKeyDto>
            {
                new ExamGroupBySetKeyDto
                {
                    ExamSetKey = "10-2025",
                    Exams = new List<ExamWithPartsDto>
                    {
                        new ExamWithPartsDto { ExamId = 1, Name = "Exam 1" }
                    }
                }
            };
            _mockExamService.Setup(s => s.GetExamsGroupedBySetKeyAsync())
                .ReturnsAsync(expectedData);

            // Act
            var result = await _controller.GetAllExamsWithParts();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedData = Assert.IsAssignableFrom<List<ExamGroupBySetKeyDto>>(okResult.Value);
            Assert.Single(returnedData);
        }

        #endregion

        #region ToggleExamStatus Tests

        [Fact]
        public async Task ToggleExamStatus_Success_ReturnsOkWithSuccessMessage()
        {
            // Arrange
            int examId = 1;
            _mockExamService.Setup(s => s.ToggleExamStatusAsync(examId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ToggleExamStatus(examId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            var messageProperty = response.GetType().GetProperty("message");
            Assert.Equal("Đổi trạng thái thành công.", messageProperty.GetValue(response));
        }

        [Fact]
        public async Task ToggleExamStatus_NotEnoughQuestions_ReturnsBadRequest()
        {
            // Arrange
            int examId = 1;
            _mockExamService.Setup(s => s.ToggleExamStatusAsync(examId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.ToggleExamStatus(examId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;
            var messageProperty = response.GetType().GetProperty("message");
            Assert.Equal("Không đủ câu hỏi. Không thể mở khóa bài thi!", messageProperty.GetValue(response));
        }

        [Fact]
        public async Task ToggleExamStatus_InvalidExamId_ReturnsBadRequest()
        {
            // Arrange
            int examId = 0;
            _mockExamService.Setup(s => s.ToggleExamStatusAsync(examId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.ToggleExamStatus(examId);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion
    }
}