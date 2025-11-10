using DataLayer.DTOs.Prompt;
using DataLayer.DTOs.Questions;
using DataLayer.DTOs.Exam; // ← THÊM DÒNG NÀY
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.Import;
using ServiceLayer.Questions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Lumina.Tests
{
    public class QuestionControllerTests
    {
        private readonly Mock<IQuestionService> _mockQuestionService;
        private readonly Mock<IImportService> _mockImportService;
        private readonly QuestionController _controller;

        public QuestionControllerTests()
        {
            _mockQuestionService = new Mock<IQuestionService>();
            _mockImportService = new Mock<IImportService>();
            _controller = new QuestionController(_mockQuestionService.Object, _mockImportService.Object);
        }

        #region CreatePromptWithQuestions Tests (5 test cases)

        [Fact]
        public async Task CreatePromptWithQuestions_ValidDto_ReturnsOkWithPromptId()
        {
            // Arrange
            var dto = new CreatePromptWithQuestionsDTO
            {
                Title = "Test Prompt",
                ContentText = "Test Content",
                Skill = "Reading",
                Questions = new List<QuestionWithOptionsDTO>
                {
                    new QuestionWithOptionsDTO
                    {
                        Question = new AddQuestionDTO
                        {
                            PartId = 1,
                            QuestionType = "Multiple Choice",
                            StemText = "Question 1",
                            ScoreWeight = 1,
                            Time = 30,
                            QuestionNumber = 1,
                            PromptId = 1
                        },
                        Options = new List<OptionDTO> // ← SỬA TỪ OptionDto → OptionDTO
                        {
                            new OptionDTO { Content = "A", IsCorrect = true },
                            new OptionDTO { Content = "B", IsCorrect = false }
                        }
                    }
                }
            };

            _mockQuestionService.Setup(s => s.CreatePromptWithQuestionsAsync(dto))
                .ReturnsAsync(1);

            // Act
            var result = await _controller.CreatePromptWithQuestions(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            var promptIdProperty = response.GetType().GetProperty("PromptId");
            Assert.Equal(1, promptIdProperty.GetValue(response));
        }

        [Fact]
        public async Task CreatePromptWithQuestions_NullDto_ReturnsBadRequest()
        {
            // Arrange
            CreatePromptWithQuestionsDTO dto = null;

            // Act
            var result = await _controller.CreatePromptWithQuestions(dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Dữ liệu không hợp lệ.", badRequestResult.Value);
        }

        [Fact]
        public async Task CreatePromptWithQuestions_MaxQuestionsExceeded_ReturnsBadRequest()
        {
            // Arrange
            var dto = new CreatePromptWithQuestionsDTO
            {
                Title = "Test",
                ContentText = "Content",
                Skill = "Reading",
                Questions = new List<QuestionWithOptionsDTO>()
            };

            _mockQuestionService.Setup(s => s.CreatePromptWithQuestionsAsync(dto))
                .ThrowsAsync(new Exception("ExamPart id 1 đã đủ 10 câu hỏi rồi."));

            // Act
            var result = await _controller.CreatePromptWithQuestions(dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;
            var errorProperty = response.GetType().GetProperty("error");
            Assert.Contains("đã đủ", errorProperty.GetValue(response).ToString());
        }

        [Fact]
        public async Task CreatePromptWithQuestions_NoSlotsAvailable_ReturnsBadRequest()
        {
            // Arrange
            var dto = new CreatePromptWithQuestionsDTO
            {
                Title = "Test",
                ContentText = "Content",
                Skill = "Reading",
                Questions = new List<QuestionWithOptionsDTO>()
            };

            _mockQuestionService.Setup(s => s.CreatePromptWithQuestionsAsync(dto))
                .ThrowsAsync(new Exception("Không còn slot"));

            // Act
            var result = await _controller.CreatePromptWithQuestions(dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;
            var errorProperty = response.GetType().GetProperty("error");
            Assert.Contains("Không còn slot", errorProperty.GetValue(response).ToString());
        }

        [Fact]
        public async Task CreatePromptWithQuestions_ServiceThrowsGeneralException_ReturnsInternalServerError()
        {
            // Arrange
            var dto = new CreatePromptWithQuestionsDTO
            {
                Title = "Test",
                ContentText = "Content",
                Skill = "Reading",
                Questions = new List<QuestionWithOptionsDTO>()
            };

            _mockQuestionService.Setup(s => s.CreatePromptWithQuestionsAsync(dto))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _controller.CreatePromptWithQuestions(dto);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        #endregion

        #region UploadExcel Tests (4 test cases)

        [Fact]
        public async Task UploadExcel_ValidFile_ReturnsOkWithSuccessMessage()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            var content = "fake excel content";
            var fileName = "test.xlsx";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;

            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(ms.Length);

            _mockImportService.Setup(s => s.ImportQuestionsFromExcelAsync(It.IsAny<IFormFile>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UploadExcel(fileMock.Object, 1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Import câu hỏi thành công!", okResult.Value);
        }

        [Fact]
        public async Task UploadExcel_NullFile_ReturnsBadRequest()
        {
            // Arrange
            IFormFile file = null;

            // Act
            var result = await _controller.UploadExcel(file, 1);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Chưa chọn file.", badRequestResult.Value);
        }

        [Fact]
        public async Task UploadExcel_EmptyFile_ReturnsBadRequest()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(0);

            // Act
            var result = await _controller.UploadExcel(fileMock.Object, 1);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Chưa chọn file.", badRequestResult.Value);
        }

        [Fact]
        public async Task UploadExcel_ImportServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(100);
            _mockImportService.Setup(s => s.ImportQuestionsFromExcelAsync(It.IsAny<IFormFile>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("Invalid Excel format"));

            // Act
            var result = await _controller.UploadExcel(fileMock.Object, 1);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        #endregion

        #region GetPaged Tests (5 test cases)

        [Fact]
        public async Task GetPaged_ValidParametersWithoutFilter_ReturnsOkWithData()
        {
            // Arrange
            var prompts = new List<PromptDto>
            {
                new PromptDto { PromptId = 1, Title = "Prompt 1", ContentText = "Content 1", Skill = "Reading" },
                new PromptDto { PromptId = 2, Title = "Prompt 2", ContentText = "Content 2", Skill = "Listening" }
            };

            _mockQuestionService.Setup(s => s.GetPromptsPagedAsync(1, 10, null))
                .ReturnsAsync((prompts, 1));

            // Act
            var result = await _controller.GetPaged(1, 10, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            var itemsProperty = response.GetType().GetProperty("Items");
            var totalPagesProperty = response.GetType().GetProperty("TotalPages");
            
            Assert.NotNull(itemsProperty);
            Assert.NotNull(totalPagesProperty);
            Assert.Equal(1, totalPagesProperty.GetValue(response));
        }

        [Fact]
        public async Task GetPaged_WithPartIdFilter_ReturnsFilteredData()
        {
            // Arrange
            var prompts = new List<PromptDto>
            {
                new PromptDto { PromptId = 1, Title = "Prompt 1", PartId = 1 }
            };

            _mockQuestionService.Setup(s => s.GetPromptsPagedAsync(1, 10, 1))
                .ReturnsAsync((prompts, 1));

            // Act
            var result = await _controller.GetPaged(1, 10, 1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetPaged_PageNumberZero_ReturnsOkWithData()
        {
            // Arrange
            var prompts = new List<PromptDto>();
            _mockQuestionService.Setup(s => s.GetPromptsPagedAsync(0, 10, null))
                .ReturnsAsync((prompts, 0));

            // Act
            var result = await _controller.GetPaged(0, 10, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetPaged_CustomPageSize_ReturnsOkWithData()
        {
            // Arrange
            var prompts = new List<PromptDto>();
            _mockQuestionService.Setup(s => s.GetPromptsPagedAsync(1, 20, null))
                .ReturnsAsync((prompts, 1));

            // Act
            var result = await _controller.GetPaged(1, 20, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetPaged_ServiceThrowsException_ThrowsException()
        {
            // Arrange
            _mockQuestionService.Setup(s => s.GetPromptsPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _controller.GetPaged(1, 10, null));
        }

        #endregion

        #region EditPassage Tests (4 test cases)

        [Fact]
        public async Task EditPassage_ValidDto_ReturnsOkWithSuccessMessage()
        {
            // Arrange
            var dto = new PromptEditDto
            {
                PromptId = 1,
                Title = "Updated Title",
                ContentText = "Updated Content",
                Skill = "Reading"
            };

            _mockQuestionService.Setup(s => s.EditPromptWithQuestionsAsync(dto))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.EditPassage(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            var messageProperty = response.GetType().GetProperty("message");
            Assert.Equal("Cập nhật thành công", messageProperty.GetValue(response));
        }

        [Fact]
        public async Task EditPassage_NullDto_ReturnsBadRequest()
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
        public async Task EditPassage_InvalidPromptId_ReturnsBadRequest()
        {
            // Arrange
            var dto = new PromptEditDto { PromptId = 0 };

            // Act
            var result = await _controller.EditPassage(dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Dữ liệu không hợp lệ", badRequestResult.Value);
        }

        [Fact]
        public async Task EditPassage_PromptNotFound_ReturnsNotFound()
        {
            // Arrange
            var dto = new PromptEditDto
            {
                PromptId = 999,
                Title = "Test",
                ContentText = "Content",
                Skill = "Reading"
            };

            _mockQuestionService.Setup(s => s.EditPromptWithQuestionsAsync(dto))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.EditPassage(dto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Prompt không tồn tại", notFoundResult.Value);
        }

        #endregion

        #region Update Tests (4 test cases)

        [Fact]
        public async Task Update_ValidDto_ReturnsOkWithSuccessMessage()
        {
            // Arrange
            var dto = new QuestionCrudDto
            {
                QuestionId = 1,
                PartId = 1,
                QuestionType = "Multiple Choice",
                StemText = "Updated Question",
                ScoreWeight = 1,
                Time = 30
            };

            _mockQuestionService.Setup(s => s.UpdateQuestionAsync(dto))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Update(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            var messageProperty = response.GetType().GetProperty("message");
            Assert.Equal("Đã cập nhật!", messageProperty.GetValue(response));
        }

        [Fact]
        public async Task Update_QuestionNotFound_ReturnsNotFound()
        {
            // Arrange
            var dto = new QuestionCrudDto
            {
                QuestionId = 999,
                PartId = 1,
                QuestionType = "Multiple Choice",
                StemText = "Question",
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
        public async Task Update_NullQuestionId_ReturnsNotFound()
        {
            // Arrange
            var dto = new QuestionCrudDto
            {
                QuestionId = null,
                PartId = 1,
                QuestionType = "Multiple Choice",
                StemText = "Question",
                ScoreWeight = 1,
                Time = 30
            };

            _mockQuestionService.Setup(s => s.UpdateQuestionAsync(dto))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Update(dto);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Update_ServiceThrowsException_ThrowsException()
        {
            // Arrange
            var dto = new QuestionCrudDto { QuestionId = 1 };
            _mockQuestionService.Setup(s => s.UpdateQuestionAsync(dto))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _controller.Update(dto));
        }

        #endregion

        #region CheckAvailableSlots Tests (4 test cases)

        [Fact]
        public async Task CheckAvailableSlots_ValidRequest_ReturnsOkWithAvailableSlots()
        {
            // Arrange
            _mockQuestionService.Setup(s => s.GetAvailableSlots(1, 5))
                .ReturnsAsync(10);

            // Act
            var result = await _controller.CheckAvailableSlots(1, 5);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            var availableProperty = response.GetType().GetProperty("available");
            var canAddProperty = response.GetType().GetProperty("canAdd");
            
            Assert.Equal(10, availableProperty.GetValue(response));
            Assert.Equal(true, canAddProperty.GetValue(response));
        }

        [Fact]
        public async Task CheckAvailableSlots_InvalidPartId_ReturnsBadRequest()
        {
            // Arrange
            _mockQuestionService.Setup(s => s.GetAvailableSlots(999, 5))
                .ThrowsAsync(new Exception("PartId 999 không tồn tại"));

            // Act
            var result = await _controller.CheckAvailableSlots(999, 5);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;
            var errorProperty = response.GetType().GetProperty("error");
            var canAddProperty = response.GetType().GetProperty("canAdd");
            
            Assert.Contains("không tồn tại", errorProperty.GetValue(response).ToString());
            Assert.Equal(false, canAddProperty.GetValue(response));
        }

        [Fact]
        public async Task CheckAvailableSlots_ExceedsMaxQuestions_ReturnsBadRequest()
        {
            // Arrange
            _mockQuestionService.Setup(s => s.GetAvailableSlots(1, 100))
                .ThrowsAsync(new Exception("ExamPart id 1 chỉ còn 5 slot, không đủ cho 100 câu hỏi"));

            // Act
            var result = await _controller.CheckAvailableSlots(1, 100);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;
            var canAddProperty = response.GetType().GetProperty("canAdd");
            Assert.Equal(false, canAddProperty.GetValue(response));
        }

        [Fact]
        public async Task CheckAvailableSlots_ZeroCount_ReturnsOk()
        {
            // Arrange
            _mockQuestionService.Setup(s => s.GetAvailableSlots(1, 0))
                .ReturnsAsync(10);

            // Act
            var result = await _controller.CheckAvailableSlots(1, 0);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        #endregion

        #region DeletePrompt Tests (5 test cases)

        [Fact]
        public async Task DeletePrompt_ValidPromptId_ReturnsOkWithSuccessMessage()
        {
            // Arrange
            _mockQuestionService.Setup(s => s.DeletePromptAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeletePrompt(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            var messageProperty = response.GetType().GetProperty("message");
            Assert.Equal("Xóa prompt thành công.", messageProperty.GetValue(response));
        }

        [Fact]
        public async Task DeletePrompt_PromptNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockQuestionService.Setup(s => s.DeletePromptAsync(999))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeletePrompt(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = notFoundResult.Value;
            var messageProperty = response.GetType().GetProperty("message");
            Assert.Equal("Không tìm thấy prompt.", messageProperty.GetValue(response));
        }

        [Fact]
        public async Task DeletePrompt_ExamIsActive_ReturnsBadRequest()
        {
            // Arrange
            _mockQuestionService.Setup(s => s.DeletePromptAsync(1))
                .ThrowsAsync(new Exception("Không thể xóa prompt vì bài thi đang hoạt động."));

            // Act
            var result = await _controller.DeletePrompt(1);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;
            var messageProperty = response.GetType().GetProperty("message");
            Assert.Contains("bài thi đang hoạt động", messageProperty.GetValue(response).ToString());
        }

        [Fact]
        public async Task DeletePrompt_ZeroPromptId_ReturnsNotFound()
        {
            // Arrange
            _mockQuestionService.Setup(s => s.DeletePromptAsync(0))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeletePrompt(0);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeletePrompt_NegativePromptId_ReturnsNotFound()
        {
            // Arrange
            _mockQuestionService.Setup(s => s.DeletePromptAsync(-1))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeletePrompt(-1);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion
    }
}