using DataLayer.DTOs.Questions;
using lumina.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.Questions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Lumina.Tests
{
    public class EditQuestionTest
    {
        private readonly Mock<IQuestionService> _mockQuestionService;
        private readonly Mock<ServiceLayer.Import.IImportService> _mockImportService;
        private readonly QuestionController _controller;

        public EditQuestionTest()
        {
            _mockQuestionService = new Mock<IQuestionService>();
            _mockImportService = new Mock<ServiceLayer.Import.IImportService>();
            _controller = new QuestionController(_mockQuestionService.Object, _mockImportService.Object);
        }

        [Fact]
        public async Task Update_ValidDtoWithAllFields_ReturnsOkWithSuccessMessage()
        {
            // Arrange
            var dto = new QuestionCrudDto
            {
                QuestionId = 1,
                PartId = 1,
                PromptId = 1,
                QuestionType = "MultipleChoice",
                StemText = "What is the capital of France?",
                QuestionExplain = "This is a geography question",
                ScoreWeight = 5,
                Time = 60,
                Options = new List<OptionDto>
                {
                    new OptionDto { Content = "Paris", IsCorrect = true },
                    new OptionDto { Content = "London", IsCorrect = false },
                    new OptionDto { Content = "Berlin", IsCorrect = false }
                }
            };
            _mockQuestionService.Setup(s => s.UpdateQuestionAsync(dto))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Update(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            var messageProperty = response.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            Assert.Equal("Đã cập nhật!", messageProperty.GetValue(response));
            _mockQuestionService.Verify(s => s.UpdateQuestionAsync(It.Is<QuestionCrudDto>(d =>
                d.QuestionId == 1 &&
                d.PartId == 1 &&
                d.PromptId == 1 &&
                d.QuestionType == "MultipleChoice" &&
                d.StemText == "What is the capital of France?" &&
                d.QuestionExplain == "This is a geography question" &&
                d.ScoreWeight == 5 &&
                d.Time == 60 &&
                d.Options.Count == 3
            )), Times.Once);
        }

        [Fact]
        public async Task Update_QuestionNotExists_ReturnsNotFound()
        {
            // Arrange
            var dto = new QuestionCrudDto
            {
                QuestionId = 999,
                PartId = 1,
                QuestionType = "MultipleChoice",
                StemText = "Non-existent question",
                ScoreWeight = 1,
                Time = 30
            };
            _mockQuestionService.Setup(s => s.UpdateQuestionAsync(dto))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Update(dto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Không tồn tại question!", notFoundResult.Value);
        }

        [Fact]
        public async Task Update_ValidDtoWithoutPromptId_ReturnsOk()
        {
            // Arrange
            var dto = new QuestionCrudDto
            {
                QuestionId = 2,
                PartId = 2,
                PromptId = null,
                QuestionType = "Speaking",
                StemText = "Describe your hometown",
                QuestionExplain = null,
                ScoreWeight = 10,
                Time = 120,
                Options = null
            };
            _mockQuestionService.Setup(s => s.UpdateQuestionAsync(dto))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Update(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            _mockQuestionService.Verify(s => s.UpdateQuestionAsync(It.Is<QuestionCrudDto>(d =>
                d.QuestionId == 2 &&
                d.PromptId == null &&
                d.QuestionExplain == null &&
                d.Options == null
            )), Times.Once);
        }

        [Fact]
        public async Task Update_ValidDtoWithEmptyStrings_ReturnsOk()
        {
            // Arrange
            var dto = new QuestionCrudDto
            {
                QuestionId = 3,
                PartId = 3,
                QuestionType = "",
                StemText = "",
                QuestionExplain = "",
                ScoreWeight = 1,
                Time = 30,
                Options = new List<OptionDto>()
            };
            _mockQuestionService.Setup(s => s.UpdateQuestionAsync(dto))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Update(dto);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockQuestionService.Verify(s => s.UpdateQuestionAsync(It.Is<QuestionCrudDto>(d =>
                d.QuestionType == "" &&
                d.StemText == "" &&
                d.QuestionExplain == "" &&
                d.Options.Count == 0
            )), Times.Once);
        }

        [Fact]
        public async Task Update_ValidDtoWithBoundaryValues_ReturnsOk()
        {
            // Arrange
            var dto = new QuestionCrudDto
            {
                QuestionId = int.MaxValue,
                PartId = int.MaxValue,
                PromptId = int.MaxValue,
                QuestionType = "Boundary",
                StemText = new string('A', 10000),
                QuestionExplain = new string('B', 10000),
                ScoreWeight = int.MaxValue,
                Time = int.MaxValue,
                Options = new List<OptionDto>
                {
                    new OptionDto { Content = new string('C', 1000), IsCorrect = true }
                }
            };
            _mockQuestionService.Setup(s => s.UpdateQuestionAsync(dto))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Update(dto);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockQuestionService.Verify(s => s.UpdateQuestionAsync(It.Is<QuestionCrudDto>(d =>
                d.QuestionId == int.MaxValue &&
                d.PartId == int.MaxValue &&
                d.ScoreWeight == int.MaxValue &&
                d.Time == int.MaxValue &&
                d.StemText.Length == 10000
            )), Times.Once);
        }

        [Fact]
        public async Task Update_ValidDtoWithMinimumValues_ReturnsOk()
        {
            // Arrange
            var dto = new QuestionCrudDto
            {
                QuestionId = 1,
                PartId = 1,
                PromptId = 0,
                QuestionType = "A",
                StemText = "Q",
                QuestionExplain = "E",
                ScoreWeight = 0,
                Time = 0,
                Options = new List<OptionDto>
                {
                    new OptionDto { Content = "O", IsCorrect = false }
                }
            };
            _mockQuestionService.Setup(s => s.UpdateQuestionAsync(dto))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Update(dto);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockQuestionService.Verify(s => s.UpdateQuestionAsync(It.Is<QuestionCrudDto>(d =>
                d.ScoreWeight == 0 &&
                d.Time == 0 &&
                d.Options.Count == 1
            )), Times.Once);
        }

        [Fact]
        public async Task Update_ValidDtoWithMultipleOptions_ReturnsOk()
        {
            // Arrange
            var options = new List<OptionDto>();
            for (int i = 0; i < 100; i++)
            {
                options.Add(new OptionDto { Content = $"Option {i}", IsCorrect = i == 0 });
            }

            var dto = new QuestionCrudDto
            {
                QuestionId = 5,
                PartId = 1,
                QuestionType = "MultipleChoice",
                StemText = "Question with many options",
                ScoreWeight = 1,
                Time = 30,
                Options = options
            };
            _mockQuestionService.Setup(s => s.UpdateQuestionAsync(dto))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Update(dto);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockQuestionService.Verify(s => s.UpdateQuestionAsync(It.Is<QuestionCrudDto>(d =>
                d.Options.Count == 100
            )), Times.Once);
        }
    }
}