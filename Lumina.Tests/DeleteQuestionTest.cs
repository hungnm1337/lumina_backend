using lumina.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.Questions;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Lumina.Tests
{
    public class DeleteQuestionTest
    {
        private readonly Mock<IQuestionService> _mockQuestionService;
        private readonly Mock<ServiceLayer.Import.IImportService> _mockImportService;
        private readonly QuestionController _controller;

        public DeleteQuestionTest()
        {
            _mockQuestionService = new Mock<IQuestionService>();
            _mockImportService = new Mock<ServiceLayer.Import.IImportService>();
            _controller = new QuestionController(_mockQuestionService.Object, _mockImportService.Object);
        }

        [Fact]
        public async Task Delete_ValidId_ReturnsOkWithSuccessMessage()
        {
            // Arrange
            int questionId = 1;
            _mockQuestionService.Setup(s => s.DeleteQuestionAsync(questionId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(questionId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            var messageProperty = response.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            Assert.Equal("Đã xóa!", messageProperty.GetValue(response));
            _mockQuestionService.Verify(s => s.DeleteQuestionAsync(questionId), Times.Once);
        }

        [Fact]
        public async Task Delete_QuestionNotExists_ReturnsNotFound()
        {
            // Arrange
            int questionId = 999;
            _mockQuestionService.Setup(s => s.DeleteQuestionAsync(questionId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(questionId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = notFoundResult.Value;
            var messageProperty = response.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            Assert.Equal("Không tồn tại question!", messageProperty.GetValue(response));
        }

        [Fact]
        public async Task Delete_ServiceThrowsException_ReturnsBadRequestWithMessage()
        {
            // Arrange
            int questionId = 1;
            string exceptionMessage = "Không thể xóa câu hỏi vì bài thi đang hoạt động.";
            _mockQuestionService.Setup(s => s.DeleteQuestionAsync(questionId))
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act
            var result = await _controller.Delete(questionId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;
            var messageProperty = response.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            Assert.Equal(exceptionMessage, messageProperty.GetValue(response));
        }

        [Fact]
        public async Task Delete_IdZero_ServiceCalledWithZero()
        {
            // Arrange
            int questionId = 0;
            _mockQuestionService.Setup(s => s.DeleteQuestionAsync(questionId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(questionId);

            // Assert
            _mockQuestionService.Verify(s => s.DeleteQuestionAsync(0), Times.Once);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Delete_NegativeId_ServiceCalledWithNegativeValue()
        {
            // Arrange
            int questionId = -1;
            _mockQuestionService.Setup(s => s.DeleteQuestionAsync(questionId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(questionId);

            // Assert
            _mockQuestionService.Verify(s => s.DeleteQuestionAsync(-1), Times.Once);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Delete_MaxIntId_ReturnsOkWhenDeleted()
        {
            // Arrange
            int questionId = int.MaxValue;
            _mockQuestionService.Setup(s => s.DeleteQuestionAsync(questionId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(questionId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            _mockQuestionService.Verify(s => s.DeleteQuestionAsync(int.MaxValue), Times.Once);
        }

        [Fact]
        public async Task Delete_ExceptionWithDifferentMessage_ReturnsBadRequestWithCorrectMessage()
        {
            // Arrange
            int questionId = 5;
            string exceptionMessage = "Database connection failed";
            _mockQuestionService.Setup(s => s.DeleteQuestionAsync(questionId))
                .ThrowsAsync(new InvalidOperationException(exceptionMessage));

            // Act
            var result = await _controller.Delete(questionId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;
            var messageProperty = response.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            Assert.Equal(exceptionMessage, messageProperty.GetValue(response));
        }
    }
}