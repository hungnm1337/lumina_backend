using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using lumina.Controllers;
using ServiceLayer.Exam;
using DataLayer.DTOs.Exam;

namespace Lumina.Tests
{
    public class GetExamPartDetailAndQuestionTests
    {
        private readonly Mock<IExamService> _mockExamService;
        private readonly ExamController _controller;

        public GetExamPartDetailAndQuestionTests()
        {
            _mockExamService = new Mock<IExamService>();
            _controller = new ExamController(_mockExamService.Object);
        }

        [Fact]
        public async Task GetExamPartDetailAndQuestion_KhiPartIDHopLe_TraVeOkVaExamPartDTO()
        {
            // Arrange
            int partId = 1;
            var expectedPart = new ExamPartDTO
            {
                PartId = partId,
                PartCode = "Part1",
                Title = "Photographs",
                ExamId = 1,
                OrderIndex = 1,
                Questions = new List<QuestionDTO>()
            };

            _mockExamService
                .Setup(s => s.GetExamPartDetailAndQuestionByExamPartID(partId))
                .ReturnsAsync(expectedPart);

            // Act
            var result = await _controller.GetExamPartDetailAndQuestion(partId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedPart = Assert.IsType<ExamPartDTO>(okResult.Value);
            Assert.Equal(partId, returnedPart.PartId);
            Assert.Equal("Part1", returnedPart.PartCode);
            Assert.Equal("Photographs", returnedPart.Title);
            _mockExamService.Verify(s => s.GetExamPartDetailAndQuestionByExamPartID(partId), Times.Once);
        }

        [Fact]
        public async Task GetExamPartDetailAndQuestion_KhiPartIDKhongTonTai_TraVeNotFound()
        {
            // Arrange
            int partId = 999;

            _mockExamService
                .Setup(s => s.GetExamPartDetailAndQuestionByExamPartID(partId))
                .ReturnsAsync((ExamPartDTO)null!);

            // Act
            var result = await _controller.GetExamPartDetailAndQuestion(partId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal($"Exam part with ID {partId} not found.", notFoundResult.Value);
            _mockExamService.Verify(s => s.GetExamPartDetailAndQuestionByExamPartID(partId), Times.Once);
        }

        [Fact]
        public async Task GetExamPartDetailAndQuestion_KhiPartIDBangKhong_TraVeNotFound()
        {
            // Arrange
            int partId = 0;

            _mockExamService
                .Setup(s => s.GetExamPartDetailAndQuestionByExamPartID(partId))
                .ReturnsAsync((ExamPartDTO)null!);

            // Act
            var result = await _controller.GetExamPartDetailAndQuestion(partId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal($"Exam part with ID {partId} not found.", notFoundResult.Value);
        }

        [Fact]
        public async Task GetExamPartDetailAndQuestion_KhiPartIDAm_TraVeNotFound()
        {
            // Arrange
            int partId = -1;

            _mockExamService
                .Setup(s => s.GetExamPartDetailAndQuestionByExamPartID(partId))
                .ReturnsAsync((ExamPartDTO)null!);

            // Act
            var result = await _controller.GetExamPartDetailAndQuestion(partId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal($"Exam part with ID {partId} not found.", notFoundResult.Value);
        }

        [Fact]
        public async Task GetExamPartDetailAndQuestion_KhiPartIDLon_TraVeOkVaExamPartDTO()
        {
            // Arrange
            int partId = int.MaxValue;
            var expectedPart = new ExamPartDTO
            {
                PartId = partId,
                PartCode = "Part7",
                Title = "Reading Comprehension",
                ExamId = 1,
                OrderIndex = 7,
                Questions = new List<QuestionDTO>()
            };

            _mockExamService
                .Setup(s => s.GetExamPartDetailAndQuestionByExamPartID(partId))
                .ReturnsAsync(expectedPart);

            // Act
            var result = await _controller.GetExamPartDetailAndQuestion(partId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedPart = Assert.IsType<ExamPartDTO>(okResult.Value);
            Assert.Equal(partId, returnedPart.PartId);
            _mockExamService.Verify(s => s.GetExamPartDetailAndQuestionByExamPartID(partId), Times.Once);
        }
    }
}
