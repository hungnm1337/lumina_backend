using Xunit;
using Moq;
using ServiceLayer.Exam;
using RepositoryLayer.Exam;
using DataLayer.Models;
using Lumina.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Lumina.Test.Services
{
    public class CreateExamFormatAsyncUnitTest
    {
        private readonly Mock<IExamRepository> _mockExamRepository;
        private readonly LuminaSystemContext _context;
        private readonly ExamService _service;

        public CreateExamFormatAsyncUnitTest()
        {
            _mockExamRepository = new Mock<IExamRepository>();
            _context = InMemoryDbContextHelper.CreateContext();

            _service = new ExamService(
                _mockExamRepository.Object,
                _context
            );
        }

        #region Test Cases - String Parameters Validation

        [Theory]
        [InlineData(null, "2024-01", "fromSetKey")]
        [InlineData("", "2024-01", "fromSetKey")]
        [InlineData("   ", "2024-01", "fromSetKey")]
        [InlineData("2024-01", null, "toSetKey")]
        [InlineData("2024-01", "", "toSetKey")]
        [InlineData("2024-01", "   ", "toSetKey")]
        public async Task CreateExamFormatAsync_WhenStringParameterIsInvalid_ShouldThrowArgumentException(
            string? fromSetKey, string? toSetKey, string expectedParamName)
        {
            // Arrange
            int createdBy = 1;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.CreateExamFormatAsync(fromSetKey!, toSetKey!, createdBy)
            );

            Assert.Equal(expectedParamName, exception.ParamName);
            Assert.Contains("cannot be null or empty", exception.Message);

            // Verify repository is not called
            _mockExamRepository.Verify(
                repo => repo.ExamSetKeyExistsAsync(It.IsAny<string>()),
                Times.Never
            );
        }

        #endregion

        #region Test Cases - Business Logic (Early Returns)

        [Theory]
        [InlineData(true, false, false)] // toSetKey exists
        [InlineData(false, true, false)] // fromSetKey has no exams
        public async Task CreateExamFormatAsync_WhenEarlyReturnCondition_ShouldReturnFalse(
            bool toSetKeyExists, bool hasNoExams, bool expectedResult)
        {
            // Arrange
            string fromSetKey = "2024-01";
            string toSetKey = "2024-02";
            int createdBy = 1;

            _mockExamRepository
                .Setup(repo => repo.ExamSetKeyExistsAsync(toSetKey))
                .ReturnsAsync(toSetKeyExists);

            _mockExamRepository
                .Setup(repo => repo.GetExamsBySetKeyAsync(fromSetKey))
                .ReturnsAsync(hasNoExams ? new List<Exam>() : new List<Exam> { new Exam { ExamId = 1, ExamSetKey = fromSetKey } });

            // Act
            var result = await _service.CreateExamFormatAsync(fromSetKey, toSetKey, createdBy);

            // Assert
            Assert.Equal(expectedResult, result);

            _mockExamRepository.Verify(
                repo => repo.ExamSetKeyExistsAsync(toSetKey),
                Times.Once
            );

            if (toSetKeyExists)
            {
                _mockExamRepository.Verify(
                    repo => repo.GetExamsBySetKeyAsync(It.IsAny<string>()),
                    Times.Never
                );
            }
            else
            {
                _mockExamRepository.Verify(
                    repo => repo.GetExamsBySetKeyAsync(fromSetKey),
                    Times.Once
                );
            }

            _mockExamRepository.Verify(
                repo => repo.InsertExamsAsync(It.IsAny<List<Exam>>()),
                Times.Never
            );
        }

        #endregion

        #region Test Cases - Valid Input (Success)

        [Theory]
        [InlineData(1, true)] // Normal createdBy with parts
        [InlineData(0, true)] // CreatedBy = 0 with parts
        [InlineData(-1, true)] // CreatedBy negative with parts
        [InlineData(1, false)] // Normal createdBy without parts
        public async Task CreateExamFormatAsync_WhenValidInput_ShouldReturnTrue(int createdBy, bool hasParts)
        {
            // Arrange
            string fromSetKey = "2024-01";
            string toSetKey = "2024-02";

            var sourceExams = new List<Exam>
            {
                new Exam
                {
                    ExamId = 1,
                    ExamType = "Reading",
                    Name = "Reading Test",
                    Description = "Reading Description",
                    ExamSetKey = fromSetKey
                },
                new Exam
                {
                    ExamId = 2,
                    ExamType = "Listening",
                    Name = "Listening Test",
                    Description = "Listening Description",
                    ExamSetKey = fromSetKey
                }
            };

            var sourceParts = hasParts ? new List<ExamPart>
            {
                new ExamPart
                {
                    PartId = 1,
                    ExamId = 1,
                    PartCode = "P1",
                    Title = "Part 1",
                    OrderIndex = 1,
                    MaxQuestions = 10
                },
                new ExamPart
                {
                    PartId = 2,
                    ExamId = 1,
                    PartCode = "P2",
                    Title = "Part 2",
                    OrderIndex = 2,
                    MaxQuestions = 20
                },
                new ExamPart
                {
                    PartId = 3,
                    ExamId = 2,
                    PartCode = "P1",
                    Title = "Part 1",
                    OrderIndex = 1,
                    MaxQuestions = 15
                }
            } : new List<ExamPart>();

            _mockExamRepository
                .Setup(repo => repo.ExamSetKeyExistsAsync(toSetKey))
                .ReturnsAsync(false);

            _mockExamRepository
                .Setup(repo => repo.GetExamsBySetKeyAsync(fromSetKey))
                .ReturnsAsync(sourceExams);

            _mockExamRepository
                .Setup(repo => repo.GetExamPartsByExamIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(sourceParts);

            List<Exam> insertedExams = null;
            _mockExamRepository
                .Setup(repo => repo.InsertExamsAsync(It.IsAny<List<Exam>>()))
                .Callback<List<Exam>>(exams =>
                {
                    insertedExams = exams;
                    // Simulate setting ExamId after insert
                    for (int i = 0; i < exams.Count; i++)
                    {
                        exams[i].ExamId = 100 + i;
                    }
                })
                .Returns(Task.CompletedTask);

            List<ExamPart> insertedParts = null;
            _mockExamRepository
                .Setup(repo => repo.InsertExamPartsAsync(It.IsAny<List<ExamPart>>()))
                .Callback<List<ExamPart>>(parts => insertedParts = parts)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateExamFormatAsync(fromSetKey, toSetKey, createdBy);

            // Assert
            Assert.True(result);
            Assert.NotNull(insertedExams);
            Assert.Equal(2, insertedExams.Count);
            Assert.NotNull(insertedParts);
            Assert.Equal(sourceParts.Count, insertedParts.Count);

            // Verify exam properties
            Assert.Equal("Reading Test TOEIC 2024-02", insertedExams[0].Name);
            Assert.Equal("Listening Test TOEIC 2024-02", insertedExams[1].Name);
            Assert.Equal(toSetKey, insertedExams[0].ExamSetKey);
            Assert.Equal(toSetKey, insertedExams[1].ExamSetKey);
            Assert.Equal(createdBy, insertedExams[0].CreatedBy);
            Assert.False(insertedExams[0].IsActive);
            Assert.False(insertedExams[1].IsActive);

            if (hasParts)
            {
                // Verify exam parts are mapped correctly
                Assert.Equal(100, insertedParts[0].ExamId); // First new exam
                Assert.Equal(100, insertedParts[1].ExamId); // First new exam
                Assert.Equal(101, insertedParts[2].ExamId); // Second new exam
            }

            // Verify all repository methods are called
            _mockExamRepository.Verify(
                repo => repo.ExamSetKeyExistsAsync(toSetKey),
                Times.Once
            );
            _mockExamRepository.Verify(
                repo => repo.GetExamsBySetKeyAsync(fromSetKey),
                Times.Once
            );
            _mockExamRepository.Verify(
                repo => repo.GetExamPartsByExamIdsAsync(It.IsAny<List<int>>()),
                Times.Once
            );
            _mockExamRepository.Verify(
                repo => repo.InsertExamsAsync(It.IsAny<List<Exam>>()),
                Times.Once
            );
            _mockExamRepository.Verify(
                repo => repo.InsertExamPartsAsync(It.IsAny<List<ExamPart>>()),
                Times.Once
            );
        }

        #endregion

        #region Test Cases - Repository Exception Handling

        [Fact]
        public async Task CreateExamFormatAsync_WhenRepositoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            string fromSetKey = "2024-01";
            string toSetKey = "2024-02";
            int createdBy = 1;
            var exceptionMessage = "Database connection error";

            _mockExamRepository
                .Setup(repo => repo.ExamSetKeyExistsAsync(toSetKey))
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                async () => await _service.CreateExamFormatAsync(fromSetKey, toSetKey, createdBy)
            );

            Assert.Equal(exceptionMessage, exception.Message);

            _mockExamRepository.Verify(
                repo => repo.ExamSetKeyExistsAsync(toSetKey),
                Times.Once
            );
        }

        #endregion
    }
}
