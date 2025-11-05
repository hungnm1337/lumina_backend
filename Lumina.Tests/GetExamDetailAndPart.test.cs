using DataLayer.DTOs.Exam;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.Exam;

namespace Lumina.Tests
{
    public class GetExamDetailAndPartTests
    {
        private readonly Mock<IExamService> _mockExamService;
        private readonly ExamController _controller;

        public GetExamDetailAndPartTests()
        {
            _mockExamService = new Mock<IExamService>();
            _controller = new ExamController(_mockExamService.Object);
        }

        [Fact]
        public async Task GetExamDetailAndPart_WithValidExamId_Returns200OKWithExamDTO()
        {
            // Arrange
            int examId = 1;
            var expectedExam = new ExamDTO
            {
                ExamId = examId,
                ExamType = "Full Test",
                Name = "TOEIC Practice Test 1",
                Description = "Complete TOEIC practice test",
                IsActive = true,
                CreatedBy = 1,
                CreatedByName = "Admin"
            };

            _mockExamService
                .Setup(s => s.GetExamDetailAndExamPartByExamID(examId))
                .ReturnsAsync(expectedExam);

            // Act
            var result = await _controller.GetExamDetailAndPart(examId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var exam = Assert.IsType<ExamDTO>(okResult.Value);
            Assert.Equal(examId, exam.ExamId);
            Assert.Equal("TOEIC Practice Test 1", exam.Name);
            Assert.Equal("Full Test", exam.ExamType);
            _mockExamService.Verify(s => s.GetExamDetailAndExamPartByExamID(examId), Times.Once);
        }

        [Fact]
        public async Task GetExamDetailAndPart_WithNonExistentExamId_Returns404NotFound()
        {
            // Arrange
            int examId = 999;

            _mockExamService
                .Setup(s => s.GetExamDetailAndExamPartByExamID(examId))
                .ReturnsAsync((ExamDTO)null!);

            // Act
            var result = await _controller.GetExamDetailAndPart(examId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
            Assert.Equal($"Exam with ID {examId} not found.", notFoundResult.Value);
            _mockExamService.Verify(s => s.GetExamDetailAndExamPartByExamID(examId), Times.Once);
        }

        [Fact]
        public async Task GetExamDetailAndPart_WithZeroExamId_Returns404NotFound()
        {
            // Arrange
            int examId = 0;

            _mockExamService
                .Setup(s => s.GetExamDetailAndExamPartByExamID(examId))
                .ReturnsAsync((ExamDTO)null!);

            // Act
            var result = await _controller.GetExamDetailAndPart(examId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
            Assert.Equal($"Exam with ID {examId} not found.", notFoundResult.Value);
        }

        [Fact]
        public async Task GetExamDetailAndPart_WithNegativeExamId_Returns404NotFound()
        {
            // Arrange
            int examId = -1;

            _mockExamService
                .Setup(s => s.GetExamDetailAndExamPartByExamID(examId))
                .ReturnsAsync((ExamDTO)null!);

            // Act
            var result = await _controller.GetExamDetailAndPart(examId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task GetExamDetailAndPart_WithLargeExamId_Returns200OKIfExists()
        {
            // Arrange
            int examId = int.MaxValue;
            var expectedExam = new ExamDTO
            {
                ExamId = examId,
                ExamType = "Special Test",
                Name = "Special Exam",
                Description = "Test with max ID",
                IsActive = true
            };

            _mockExamService
                .Setup(s => s.GetExamDetailAndExamPartByExamID(examId))
                .ReturnsAsync(expectedExam);

            // Act
            var result = await _controller.GetExamDetailAndPart(examId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var exam = Assert.IsType<ExamDTO>(okResult.Value);
            Assert.Equal(examId, exam.ExamId);
        }
    }
}
