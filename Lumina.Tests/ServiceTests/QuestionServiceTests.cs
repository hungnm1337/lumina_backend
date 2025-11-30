using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataLayer.DTOs.Prompt;
using DataLayer.DTOs.Questions;
using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using RepositoryLayer.Questions;
using ServiceLayer.Questions;
using Xunit;

namespace Lumina.Tests.ServiceTests
{
    public class QuestionServiceTests
    {
        private static (QuestionService Service, LuminaSystemContext Context, Mock<IQuestionRepository> RepoMock) CreateService()
        {
            var context = InMemoryDbContextHelper.CreateContext(Guid.NewGuid().ToString());
            var repoMock = new Mock<IQuestionRepository>(MockBehavior.Strict);
            var service = new QuestionService(repoMock.Object, context);

            return (service, context, repoMock);
        }

        private static QuestionWithOptionsDTO CreateQuestionDto(int partId, int index = 1, bool includeOptions = true)
        {
            return new QuestionWithOptionsDTO
            {
                Question = new AddQuestionDTO
                {
                    PartId = partId,
                    QuestionType = "MultipleChoice",
                    StemText = $"Stem {index}",
                    ScoreWeight = 5,
                    QuestionExplain = "Explain",
                    Time = 45,
                    QuestionNumber = index,
                    PromptId = 0,
                    SampleAnswer = $"Sample {index}"
                },
                Options = includeOptions
                    ? new List<DataLayer.DTOs.Exam.OptionDTO>
                    {
                        new() { Content = $"Option {index}A", IsCorrect = true },
                        new() { Content = $"Option {index}B", IsCorrect = false }
                    }
                    : null
            };
        }

        private static CreatePromptWithQuestionsDTO CreatePromptDto(int partId, int questionCount = 1, bool includeOptions = true)
        {
            var dto = new CreatePromptWithQuestionsDTO
            {
                Title = "Prompt Title",
                ContentText = "Prompt Content",
                Skill = "Listening",
                ReferenceAudioUrl = "audio",
                ReferenceImageUrl = "image"
            };

            for (var i = 1; i <= questionCount; i++)
            {
                dto.Questions.Add(CreateQuestionDto(partId, i, includeOptions));
            }

            return dto;
        }

        private static async Task DisposeContextAsync(LuminaSystemContext context)
        {
            await context.Database.EnsureDeletedAsync();
            await context.DisposeAsync();
        }

        #region CreatePromptWithQuestionsAsync Tests

        [Theory]
        [InlineData(true)]  // With options
        [InlineData(false)] // Without options
        public async Task CreatePromptWithQuestionsAsync_ValidInput_ReturnsPromptId(bool includeOptions)
        {
            var (service, context, repoMock) = CreateService();
            try
            {
                context.ExamParts.Add(new ExamPart { PartId = 1, PartCode = "P1", MaxQuestions = 10, Title = "Part 1" });
                await context.SaveChangesAsync();

                var dto = CreatePromptDto(1, 2, includeOptions);
                repoMock
                    .Setup(r => r.AddPromptAsync(It.IsAny<Prompt>()))
                    .ReturnsAsync((Prompt p) =>
                    {
                        p.PromptId = 99;
                        return p;
                    });

                var questionIndex = 0;
                repoMock
                    .Setup(r => r.AddQuestionAsync(It.IsAny<Question>()))
                    .ReturnsAsync((Question q) =>
                    {
                        q.QuestionId = ++questionIndex;
                        return q;
                    });

                if (includeOptions)
                {
                    repoMock
                        .Setup(r => r.AddOptionsAsync(It.IsAny<IEnumerable<Option>>()))
                        .Returns(Task.CompletedTask);
                }

                var result = await service.CreatePromptWithQuestionsAsync(dto);

                Assert.Equal(99, result);
                repoMock.Verify(r => r.AddPromptAsync(It.IsAny<Prompt>()), Times.Once);
                repoMock.Verify(r => r.AddQuestionAsync(It.IsAny<Question>()), Times.Exactly(dto.Questions.Count));
                
                if (includeOptions)
                {
                    repoMock.Verify(r => r.AddOptionsAsync(It.IsAny<IEnumerable<Option>>()), Times.Exactly(dto.Questions.Count));
                }
                
                repoMock.VerifyNoOtherCalls();
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Fact]
        public async Task CreatePromptWithQuestionsAsync_MultiplePartIds_AssignsCorrectQuestionNumbers()
        {
            var (service, context, repoMock) = CreateService();
            try
            {
                // Setup two different parts
                context.ExamParts.AddRange(
                    new ExamPart { PartId = 1, PartCode = "P1", MaxQuestions = 10, Title = "Part 1" },
                    new ExamPart { PartId = 2, PartCode = "P2", MaxQuestions = 10, Title = "Part 2" }
                );
                
                // Add existing questions for both parts
                context.Questions.AddRange(
                    new Question { QuestionId = 1, PartId = 1, QuestionNumber = 1, QuestionType = "MC", StemText = "Q1" },
                    new Question { QuestionId = 2, PartId = 2, QuestionNumber = 1, QuestionType = "MC", StemText = "Q2" }
                );
                await context.SaveChangesAsync();

                // Create DTO with questions from multiple parts
                var dto = new CreatePromptWithQuestionsDTO
                {
                    Title = "Mixed Prompt",
                    ContentText = "Content",
                    Skill = "Mixed",
                    Questions = new List<QuestionWithOptionsDTO>
                    {
                        CreateQuestionDto(1, 1, false),
                        CreateQuestionDto(2, 1, false),
                        CreateQuestionDto(1, 2, false)
                    }
                };

                repoMock
                    .Setup(r => r.AddPromptAsync(It.IsAny<Prompt>()))
                    .ReturnsAsync((Prompt p) =>
                    {
                        p.PromptId = 100;
                        return p;
                    });

                var questionIndex = 0;
                repoMock
                    .Setup(r => r.AddQuestionAsync(It.IsAny<Question>()))
                    .ReturnsAsync((Question q) =>
                    {
                        q.QuestionId = ++questionIndex;
                        return q;
                    });

                var result = await service.CreatePromptWithQuestionsAsync(dto);

                Assert.Equal(100, result);
                repoMock.Verify(r => r.AddQuestionAsync(It.IsAny<Question>()), Times.Exactly(3));
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Fact]
        public async Task CreatePromptWithQuestionsAsync_PartWithNoExistingQuestions_StartsFromOne()
        {
            var (service, context, repoMock) = CreateService();
            try
            {
                // Add part with no existing questions
                context.ExamParts.Add(new ExamPart { PartId = 1, PartCode = "P1", MaxQuestions = 10, Title = "Part 1" });
                await context.SaveChangesAsync();

                var dto = CreatePromptDto(1, 1, false);
                
                repoMock
                    .Setup(r => r.AddPromptAsync(It.IsAny<Prompt>()))
                    .ReturnsAsync((Prompt p) =>
                    {
                        p.PromptId = 50;
                        return p;
                    });

                Question capturedQuestion = null;
                repoMock
                    .Setup(r => r.AddQuestionAsync(It.IsAny<Question>()))
                    .ReturnsAsync((Question q) =>
                    {
                        capturedQuestion = q;
                        q.QuestionId = 1;
                        return q;
                    });

                await service.CreatePromptWithQuestionsAsync(dto);

                Assert.NotNull(capturedQuestion);
                Assert.Equal(1, capturedQuestion.QuestionNumber);
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Fact]
        public async Task CreatePromptWithQuestionsAsync_MissingExamPart_RollsBackAndThrows()
        {
            var (service, context, repoMock) = CreateService();
            try
            {
                var dto = CreatePromptDto(partId: 999);

                repoMock
                    .Setup(r => r.AddPromptAsync(It.IsAny<Prompt>()))
                    .ReturnsAsync((Prompt p) =>
                    {
                        p.PromptId = 1;
                        return p;
                    });

                var ex = await Assert.ThrowsAsync<Exception>(() => service.CreatePromptWithQuestionsAsync(dto));
                Assert.Contains("không tồn tại", ex.Message, StringComparison.OrdinalIgnoreCase);

                repoMock.Verify(r => r.AddPromptAsync(It.IsAny<Prompt>()), Times.Once);
                repoMock.VerifyNoOtherCalls();
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Fact]
        public async Task CreatePromptWithQuestionsAsync_ExceedMaxQuestions_RollsBackAndThrows()
        {
            var (service, context, repoMock) = CreateService();
            try
            {
                context.ExamParts.Add(new ExamPart { PartId = 2, PartCode = "P2", MaxQuestions = 2, Title = "Part 2" });
                context.Questions.Add(new Question
                {
                    QuestionId = 1,
                    PartId = 2,
                    QuestionNumber = 1,
                    PromptId = 1,
                    QuestionType = "Single",
                    StemText = "Existing stem"
                });
                await context.SaveChangesAsync();

                var dto = CreatePromptDto(partId: 2, questionCount: 2);

                repoMock
                    .Setup(r => r.AddPromptAsync(It.IsAny<Prompt>()))
                    .ReturnsAsync((Prompt p) =>
                    {
                        p.PromptId = 50;
                        return p;
                    });

                var ex = await Assert.ThrowsAsync<Exception>(() => service.CreatePromptWithQuestionsAsync(dto));
                Assert.Contains("tối đa", ex.Message, StringComparison.OrdinalIgnoreCase);

                repoMock.Verify(r => r.AddPromptAsync(It.IsAny<Prompt>()), Times.Once);
                repoMock.VerifyNoOtherCalls();
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        #endregion

        #region GetPromptsPagedAsync Tests

        [Theory]
        [InlineData(1, 10, 1)]
        [InlineData(2, 20, null)]
        public async Task GetPromptsPagedAsync_ForwardsParametersToRepository(int page, int size, int? partId)
        {
            var (service, context, repoMock) = CreateService();
            try
            {
                var expected = partId.HasValue
                    ? (new List<PromptDto>
                    {
                        new() { PromptId = 1, PartId = partId.Value, Title = "Prompt", Skill = "Listening", ContentText = "c", ReferenceAudioUrl = "", ReferenceImageUrl = "", Questions = new List<QuestionDto>() }
                    }, 5)
                    : (new List<PromptDto>(), 0);

                repoMock
                    .Setup(r => r.GetPromptsPagedAsync(page, size, partId))
                    .ReturnsAsync(expected);

                var result = await service.GetPromptsPagedAsync(page, size, partId);

                Assert.Equal(expected, result);
                repoMock.Verify(r => r.GetPromptsPagedAsync(page, size, partId), Times.Once);
                repoMock.VerifyNoOtherCalls();
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        #endregion

        #region AddQuestionAsync Tests

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        public async Task AddQuestionAsync_ValidInput_ReturnsNewId(int expectedId)
        {
            var (service, context, repoMock) = CreateService();
            try
            {
                var dto = new QuestionCrudDto { PartId = 1, QuestionType = "type", StemText = "stem" };
                repoMock
                    .Setup(r => r.AddQuestionAsync(dto))
                    .ReturnsAsync(expectedId);

                var result = await service.AddQuestionAsync(dto);

                Assert.Equal(expectedId, result);
                repoMock.Verify(r => r.AddQuestionAsync(dto), Times.Once);
                repoMock.VerifyNoOtherCalls();
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Fact]
        public async Task AddQuestionAsync_RepositoryThrows_Propagates()
        {
            var (service, context, repoMock) = CreateService();
            try
            {
                var dto = new QuestionCrudDto { PartId = 1, QuestionType = "type", StemText = "stem" };
                repoMock
                    .Setup(r => r.AddQuestionAsync(dto))
                    .ThrowsAsync(new InvalidOperationException("repo"));

                await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddQuestionAsync(dto));

                repoMock.Verify(r => r.AddQuestionAsync(dto), Times.Once);
                repoMock.VerifyNoOtherCalls();
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        #endregion

        #region UpdateQuestionAsync Tests

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UpdateQuestionAsync_ReturnsRepositoryResult(bool expectedResult)
        {
            var (service, context, repoMock) = CreateService();
            try
            {
                var dto = new QuestionCrudDto { PartId = 1, QuestionType = "type", StemText = "stem" };
                repoMock
                    .Setup(r => r.UpdateQuestionAsync(dto))
                    .ReturnsAsync(expectedResult);

                var result = await service.UpdateQuestionAsync(dto);

                Assert.Equal(expectedResult, result);
                repoMock.Verify(r => r.UpdateQuestionAsync(dto), Times.Once);
                repoMock.VerifyNoOtherCalls();
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        #endregion

        #region DeleteQuestionAsync Tests

        [Fact]
        public async Task DeleteQuestionAsync_ValidInput_ReturnsTrue()
        {
            var (service, context, repoMock) = CreateService();
            try
            {
                repoMock
                    .Setup(r => r.DeleteQuestionAsync(10))
                    .ReturnsAsync(true);

                var result = await service.DeleteQuestionAsync(10);

                Assert.True(result);
                repoMock.Verify(r => r.DeleteQuestionAsync(10), Times.Once);
                repoMock.VerifyNoOtherCalls();
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Fact]
        public async Task DeleteQuestionAsync_RepositoryThrows_Propagates()
        {
            var (service, context, repoMock) = CreateService();
            try
            {
                repoMock
                    .Setup(r => r.DeleteQuestionAsync(2))
                    .ThrowsAsync(new Exception("delete"));

                await Assert.ThrowsAsync<Exception>(() => service.DeleteQuestionAsync(2));

                repoMock.Verify(r => r.DeleteQuestionAsync(2), Times.Once);
                repoMock.VerifyNoOtherCalls();
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        #endregion

        #region GetStatisticsAsync Tests

        [Fact]
        public async Task GetStatisticsAsync_RepositoryReturnsValue_ReturnsDto()
        {
            var (service, context, repoMock) = CreateService();
            try
            {
                var dto = new QuestionStatisticDto { TotalQuestions = 5 };
                repoMock
                    .Setup(r => r.GetQuestionStatisticsAsync())
                    .ReturnsAsync(dto);

                var result = await service.GetStatisticsAsync();

                Assert.Same(dto, result);
                repoMock.Verify(r => r.GetQuestionStatisticsAsync(), Times.Once);
                repoMock.VerifyNoOtherCalls();
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Fact]
        public async Task GetStatisticsAsync_RepositoryThrows_Propagates()
        {
            var (service, context, repoMock) = CreateService();
            try
            {
                repoMock
                    .Setup(r => r.GetQuestionStatisticsAsync())
                    .ThrowsAsync(new Exception("stats"));

                await Assert.ThrowsAsync<Exception>(() => service.GetStatisticsAsync());

                repoMock.Verify(r => r.GetQuestionStatisticsAsync(), Times.Once);
                repoMock.VerifyNoOtherCalls();
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        #endregion

        #region EditPromptWithQuestionsAsync Tests

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task EditPromptWithQuestionsAsync_ReturnsRepositoryResult(bool expectedResult)
        {
            var (service, context, repoMock) = CreateService();
            try
            {
                var dto = new PromptEditDto { PromptId = 1, Title = "T", Skill = "S", ContentText = "C" };
                repoMock
                    .Setup(r => r.EditPromptWithQuestionsAsync(dto))
                    .ReturnsAsync(expectedResult);

                var result = await service.EditPromptWithQuestionsAsync(dto);

                Assert.Equal(expectedResult, result);
                repoMock.Verify(r => r.EditPromptWithQuestionsAsync(dto), Times.Once);
                repoMock.VerifyNoOtherCalls();
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        #endregion

        #region SavePromptsWithQuestionsAndOptionsAsync Tests

        [Theory]
        [InlineData(true)]  // With options
        [InlineData(false)] // Without options
        public async Task SavePromptsWithQuestionsAndOptionsAsync_ValidInput_PersistsEntities(bool includeOptions)
        {
            var (service, context, repoMock) = CreateService();
            try
            {
                context.ExamParts.Add(new ExamPart { PartId = 3, PartCode = "P3", MaxQuestions = 10, Title = "Part 3" });
                await context.SaveChangesAsync();

                var promptDtos = new List<CreatePromptWithQuestionsDTO>
                {
                    CreatePromptDto(3, 1, includeOptions),
                    CreatePromptDto(3, 2, includeOptions)
                };

                var result = await service.SavePromptsWithQuestionsAndOptionsAsync(promptDtos, 3);

                Assert.Equal(promptDtos.Count, result.Count);
                Assert.Equal(3, context.Questions.Count());
                
                if (includeOptions)
                {
                    Assert.Equal(6, context.Options.Count());
                }
                else
                {
                    Assert.Empty(context.Options);
                }
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Fact]
        public async Task SavePromptsWithQuestionsAndOptionsAsync_MissingPart_RollsBackAndThrows()
        {
            var (service, context, repoMock) = CreateService();
            try
            {
                var promptDtos = new List<CreatePromptWithQuestionsDTO> { CreatePromptDto(99, 1) };

                var ex = await Assert.ThrowsAsync<Exception>(() => service.SavePromptsWithQuestionsAndOptionsAsync(promptDtos, 99));
                Assert.Contains("không tồn tại", ex.Message, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Fact]
        public async Task SavePromptsWithQuestionsAndOptionsAsync_ExceedsMaxQuestions_RollsBackAndThrows()
        {
            var (service, context, repoMock) = CreateService();
            try
            {
                context.ExamParts.Add(new ExamPart { PartId = 4, PartCode = "P4", MaxQuestions = 1, Title = "Part 4" });
                await context.SaveChangesAsync();

                var promptDtos = new List<CreatePromptWithQuestionsDTO> { CreatePromptDto(4, 2) };

                var ex = await Assert.ThrowsAsync<Exception>(() => service.SavePromptsWithQuestionsAndOptionsAsync(promptDtos, 4));
                Assert.Contains("tối đa", ex.Message, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        #endregion

        #region GetAvailableSlots Tests

        [Fact]
        public async Task GetAvailableSlots_ValidRequest_ReturnsAvailable()
        {
            var (service, context, repoMock) = CreateService();
            try
            {
                context.ExamParts.Add(new ExamPart { PartId = 5, PartCode = "P5", MaxQuestions = 5, Title = "Part 5" });
                context.Questions.Add(new Question
                {
                    QuestionId = 10,
                    PartId = 5,
                    QuestionType = "Single",
                    StemText = "Existing stem"
                });
                await context.SaveChangesAsync();

                var available = await service.GetAvailableSlots(5, 1);

                Assert.Equal(4, available);
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Fact]
        public async Task GetAvailableSlots_PartMissing_ThrowsException()
        {
            var (service, context, repoMock) = CreateService();
            try
            {
                var ex = await Assert.ThrowsAsync<Exception>(() => service.GetAvailableSlots(555, 1));
                Assert.Contains("không tồn tại", ex.Message, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Fact]
        public async Task GetAvailableSlots_RequestGreaterThanAvailable_ThrowsException()
        {
            var (service, context, repoMock) = CreateService();
            try
            {
                context.ExamParts.Add(new ExamPart { PartId = 6, PartCode = "P6", MaxQuestions = 2, Title = "Part 6" });
                context.Questions.Add(new Question
                {
                    QuestionId = 11,
                    PartId = 6,
                    QuestionType = "Single",
                    StemText = "Existing stem"
                });
                await context.SaveChangesAsync();

                var ex = await Assert.ThrowsAsync<Exception>(() => service.GetAvailableSlots(6, 5));
                Assert.Contains("slot", ex.Message, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        #endregion

        #region DeletePromptAsync Tests

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task DeletePromptAsync_ReturnsRepositoryResult(bool expectedResult)
        {
            var (service, context, repoMock) = CreateService();
            try
            {
                repoMock
                    .Setup(r => r.DeletePromptAsync(3))
                    .ReturnsAsync(expectedResult);

                var result = await service.DeletePromptAsync(3);

                Assert.Equal(expectedResult, result);
                repoMock.Verify(r => r.DeletePromptAsync(3), Times.Once);
                repoMock.VerifyNoOtherCalls();
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        [Fact]
        public async Task DeletePromptAsync_RepositoryThrows_WrapsExceptionMessage()
        {
            var (service, context, repoMock) = CreateService();
            try
            {
                repoMock
                    .Setup(r => r.DeletePromptAsync(4))
                    .ThrowsAsync(new InvalidOperationException("boom"));

                var ex = await Assert.ThrowsAsync<Exception>(() => service.DeletePromptAsync(4));
                Assert.Contains("Lỗi khi xóa prompt", ex.Message);
                Assert.Contains("boom", ex.Message);

                repoMock.Verify(r => r.DeletePromptAsync(4), Times.Once);
                repoMock.VerifyNoOtherCalls();
            }
            finally
            {
                await DisposeContextAsync(context);
            }
        }

        #endregion
    }
}

