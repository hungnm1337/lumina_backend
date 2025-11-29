using Xunit;
using Moq;
using ServiceLayer.Exam;
using RepositoryLayer.Exam;
using DataLayer.DTOs.ExamPart;
using Lumina.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataLayer.Models;

namespace Lumina.Test.Services
{
    public class GetExamsGroupedBySetKeyAsyncUnitTest
    {
        private readonly Mock<IExamRepository> _mockExamRepository;
        private readonly LuminaSystemContext _context;
        private readonly ExamService _service;

        public GetExamsGroupedBySetKeyAsyncUnitTest()
        {
            _mockExamRepository = new Mock<IExamRepository>();
            _context = InMemoryDbContextHelper.CreateContext();

            _service = new ExamService(
                _mockExamRepository.Object,
                _context
            );
        }

        #region Test Cases - Empty List

        [Fact]
        public async Task GetExamsGroupedBySetKeyAsync_WhenRepositoryReturnsEmptyList_ShouldReturnEmptyList()
        {
            // Arrange
            var expectedResult = new List<ExamGroupBySetKeyDto>();

            _mockExamRepository
                .Setup(repo => repo.GetExamsGroupedBySetKeyAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetExamsGroupedBySetKeyAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);

            // Verify repository is called exactly once
            _mockExamRepository.Verify(
                repo => repo.GetExamsGroupedBySetKeyAsync(),
                Times.Once
            );
        }

        #endregion

        #region Test Cases - Valid Input (Success)

        [Fact]
        public async Task GetExamsGroupedBySetKeyAsync_WhenRepositoryReturnsValidData_ShouldReturnGroupedExams()
        {
            // Arrange
            var expectedResult = new List<ExamGroupBySetKeyDto>
            {
                new ExamGroupBySetKeyDto
                {
                    ExamSetKey = "2024-01",
                    Exams = new List<ExamWithPartsDto>
                    {
                        new ExamWithPartsDto
                        {
                            ExamId = 1,
                            Name = "Reading Test 2024-01",
                            IsActive = true,
                            Description = "Reading Description",
                            Parts = new List<ExamPartBriefDto>
                            {
                                new ExamPartBriefDto
                                {
                                    PartId = 1,
                                    PartCode = "P1",
                                    Title = "Part 1",
                                    MaxQuestions = 10,
                                    QuestionCount = 10
                                },
                                new ExamPartBriefDto
                                {
                                    PartId = 2,
                                    PartCode = "P2",
                                    Title = "Part 2",
                                    MaxQuestions = 20,
                                    QuestionCount = 20
                                }
                            }
                        },
                        new ExamWithPartsDto
                        {
                            ExamId = 2,
                            Name = "Listening Test 2024-01",
                            IsActive = false,
                            Description = "Listening Description",
                            Parts = new List<ExamPartBriefDto>
                            {
                                new ExamPartBriefDto
                                {
                                    PartId = 3,
                                    PartCode = "P1",
                                    Title = "Part 1",
                                    MaxQuestions = 15,
                                    QuestionCount = 15
                                }
                            }
                        }
                    }
                },
                new ExamGroupBySetKeyDto
                {
                    ExamSetKey = "2024-02",
                    Exams = new List<ExamWithPartsDto>
                    {
                        new ExamWithPartsDto
                        {
                            ExamId = 3,
                            Name = "Writing Test 2024-02",
                            IsActive = true,
                            Description = "Writing Description",
                            Parts = new List<ExamPartBriefDto>()
                        }
                    }
                }
            };

            _mockExamRepository
                .Setup(repo => repo.GetExamsGroupedBySetKeyAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetExamsGroupedBySetKeyAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("2024-01", result[0].ExamSetKey);
            Assert.Equal("2024-02", result[1].ExamSetKey);
            Assert.Equal(2, result[0].Exams.Count);
            Assert.Single(result[1].Exams);
            Assert.Equal(2, result[0].Exams[0].Parts.Count);
            Assert.Single(result[0].Exams[1].Parts);
            Assert.Empty(result[1].Exams[0].Parts);

            // Verify exam details
            Assert.Equal(1, result[0].Exams[0].ExamId);
            Assert.Equal("Reading Test 2024-01", result[0].Exams[0].Name);
            Assert.True(result[0].Exams[0].IsActive);
            Assert.Equal(2, result[0].Exams[1].ExamId);
            Assert.Equal("Listening Test 2024-01", result[0].Exams[1].Name);
            Assert.False(result[0].Exams[1].IsActive);

            // Verify parts details
            Assert.Equal(1, result[0].Exams[0].Parts[0].PartId);
            Assert.Equal("P1", result[0].Exams[0].Parts[0].PartCode);
            Assert.Equal(10, result[0].Exams[0].Parts[0].MaxQuestions);
            Assert.Equal(10, result[0].Exams[0].Parts[0].QuestionCount);

            // Verify repository is called exactly once
            _mockExamRepository.Verify(
                repo => repo.GetExamsGroupedBySetKeyAsync(),
                Times.Once
            );
        }

        #endregion

        #region Test Cases - Repository Exception Handling

        [Fact]
        public async Task GetExamsGroupedBySetKeyAsync_WhenRepositoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            var exceptionMessage = "Database connection error";

            _mockExamRepository
                .Setup(repo => repo.GetExamsGroupedBySetKeyAsync())
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                async () => await _service.GetExamsGroupedBySetKeyAsync()
            );

            Assert.Equal(exceptionMessage, exception.Message);

            // Verify repository is called exactly once
            _mockExamRepository.Verify(
                repo => repo.GetExamsGroupedBySetKeyAsync(),
                Times.Once
            );
        }

        #endregion
    }
}
