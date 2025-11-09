using DataLayer.DTOs.Prompt;
using lumina.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.Questions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lumina.Tests
{
    public class EditPromptTest
    {
         private readonly Mock<IQuestionService> _mockQuestionService;
        private readonly Mock<ServiceLayer.Import.IImportService> _mockImportService;
        private readonly QuestionController _controller;

        public EditPromptTest()
        {
            _mockQuestionService = new Mock<IQuestionService>();
            _mockImportService = new Mock<ServiceLayer.Import.IImportService>();
            _controller = new QuestionController(_mockQuestionService.Object, _mockImportService.Object);
        }

        [Fact]
        public async Task EditPassage_DtoNull_ReturnsBadRequest()
        {
            // Arrange
            PromptEditDto dto = null;

            // Act
            var result = await _controller.EditPassage(dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Dữ liệu không hợp lệ", badRequestResult.Value);
        }

        [Fact]
        public async Task EditPassage_PromptIdZero_ReturnsBadRequest()
        {
            // Arrange
            var dto = new PromptEditDto 
            { 
                PromptId = 0,
                Skill = "Reading",
                ContentText = "Sample content text",
                Title = "Sample Title",
                ReferenceAudioUrl = "audio.mp3"
            };

            // Act
            var result = await _controller.EditPassage(dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Dữ liệu không hợp lệ", badRequestResult.Value);
        }

        [Fact]
        public async Task EditPassage_PromptIdNegative_ReturnsBadRequest()
        {
            // Arrange
            var dto = new PromptEditDto 
            { 
                PromptId = -1,
                Skill = "Listening",
                ContentText = "Sample content",
                Title = "Test Title",
                ReferenceImageUrl = "image.jpg"
            };

            // Act
            var result = await _controller.EditPassage(dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Dữ liệu không hợp lệ", badRequestResult.Value);
        }

        [Fact]
        public async Task EditPassage_PromptNotExists_ReturnsNotFound()
        {
            // Arrange
            var dto = new PromptEditDto 
            { 
                PromptId = 999,
                Skill = "Speaking",
                ContentText = "Non-existent prompt content",
                Title = "Non-existent Title"
            };
            _mockQuestionService.Setup(s => s.EditPromptWithQuestionsAsync(dto))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.EditPassage(dto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Prompt không tồn tại", notFoundResult.Value);
        }

        [Fact]
        public async Task EditPassage_ValidDtoWithAllFields_ReturnsOkWithSuccessMessage()
        {
            // Arrange
            var dto = new PromptEditDto 
            { 
                PromptId = 1,
                Skill = "Writing",
                ContentText = "Updated content text with full details",
                Title = "Updated Title",
                ReferenceImageUrl = "updated-image.jpg",
                ReferenceAudioUrl = "updated-audio.mp3"
            };
            _mockQuestionService.Setup(s => s.EditPromptWithQuestionsAsync(dto))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.EditPassage(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            var messageProperty = response.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            Assert.Equal("Cập nhật thành công", messageProperty.GetValue(response));
            _mockQuestionService.Verify(s => s.EditPromptWithQuestionsAsync(It.Is<PromptEditDto>(d => 
                d.PromptId == 1 && 
                d.Skill == "Writing" && 
                d.ContentText == "Updated content text with full details" &&
                d.Title == "Updated Title" &&
                d.ReferenceImageUrl == "updated-image.jpg" &&
                d.ReferenceAudioUrl == "updated-audio.mp3"
            )), Times.Once);
        }

        [Fact]
        public async Task EditPassage_ValidDtoWithOnlyRequiredFields_ReturnsOk()
        {
            // Arrange
            var dto = new PromptEditDto 
            { 
                PromptId = 2,
                Skill = "Reading",
                ContentText = "Only required fields content",
                Title = "Required Title Only",
                ReferenceImageUrl = null,
                ReferenceAudioUrl = null
            };
            _mockQuestionService.Setup(s => s.EditPromptWithQuestionsAsync(dto))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.EditPassage(dto);

            // Assert
            _mockQuestionService.Verify(s => s.EditPromptWithQuestionsAsync(It.Is<PromptEditDto>(d => 
                d.PromptId == 2 && 
                d.Skill == "Reading" && 
                d.ContentText == "Only required fields content" &&
                d.Title == "Required Title Only" &&
                d.ReferenceImageUrl == null &&
                d.ReferenceAudioUrl == null
            )), Times.Once);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task EditPassage_ValidDtoWithEmptySkillAndContentAndTitle_ReturnsOk()
        {
            // Arrange
            var dto = new PromptEditDto 
            { 
                PromptId = 5,
                Skill = "",
                ContentText = "",
                Title = "",
                ReferenceImageUrl = "",
                ReferenceAudioUrl = ""
            };
            _mockQuestionService.Setup(s => s.EditPromptWithQuestionsAsync(dto))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.EditPassage(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            _mockQuestionService.Verify(s => s.EditPromptWithQuestionsAsync(It.Is<PromptEditDto>(d => 
                d.Skill == "" && 
                d.ContentText == "" &&
                d.Title == "" &&
                d.ReferenceImageUrl == "" &&
                d.ReferenceAudioUrl == ""
            )), Times.Once);
        }

        [Fact]
        public async Task EditPassage_ValidDtoWithLongStrings_ReturnsOk()
        {
            // Arrange
            var longText = new string('A', 10000);
            var dto = new PromptEditDto 
            { 
                PromptId = 10,
                Skill = longText,
                ContentText = longText,
                Title = longText,
                ReferenceImageUrl = longText,
                ReferenceAudioUrl = longText
            };
            _mockQuestionService.Setup(s => s.EditPromptWithQuestionsAsync(dto))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.EditPassage(dto);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockQuestionService.Verify(s => s.EditPromptWithQuestionsAsync(It.Is<PromptEditDto>(d => 
                d.Skill.Length == 10000 && 
                d.ContentText.Length == 10000 &&
                d.Title.Length == 10000
            )), Times.Once);
        }

        [Fact]
        public async Task EditPassage_ValidDtoWithMaxPromptId_CallsServiceOnce()
        {
            // Arrange
            var dto = new PromptEditDto 
            { 
                PromptId = int.MaxValue,
                Skill = "Boundary",
                ContentText = "Boundary test content",
                Title = "Boundary Title"
            };
            _mockQuestionService.Setup(s => s.EditPromptWithQuestionsAsync(dto))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.EditPassage(dto);

            // Assert
            _mockQuestionService.Verify(s => s.EditPromptWithQuestionsAsync(It.Is<PromptEditDto>(d => 
                d.PromptId == int.MaxValue
            )), Times.Once);
            Assert.IsType<OkObjectResult>(result);
        }
    }
}
