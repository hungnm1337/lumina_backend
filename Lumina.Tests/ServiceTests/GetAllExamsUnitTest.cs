using Xunit;
using Moq;
using ServiceLayer.Exam;
using RepositoryLayer.Exam;
using DataLayer.DTOs.Exam;
using DataLayer.Models;
using Lumina.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class GetAllExamsUnitTest
    {
        private readonly Mock<IExamRepository> _mockExamRepository;
        private readonly LuminaSystemContext _context;
        private readonly ExamService _service;

        public GetAllExamsUnitTest()
        {
            _mockExamRepository = new Mock<IExamRepository>();
            _context = InMemoryDbContextHelper.CreateContext();

            _service = new ExamService(
                _mockExamRepository.Object,
                _context
            );
        }

        #region Test Cases - Both Parameters Null (Default)

        [Fact]
        public async Task GetAllExams_WhenBothParametersAreNull_ShouldReturnListFromRepository()
        {
            // Arrange
            string? examType = null;
            string? partCode = null;
            var expectedResult = new List<ExamDTO>
            {
                new ExamDTO
                {
                    ExamId = 1,
                    ExamType = "Reading",
                    Name = "Test Exam 1",
                    Description = "Test Description 1",
                    IsActive = true,
                    CreatedBy = 1,
                    CreatedAt = DateTime.UtcNow
                },
                new ExamDTO
                {
                    ExamId = 2,
                    ExamType = "Writing",
                    Name = "Test Exam 2",
                    Description = "Test Description 2",
                    IsActive = true,
                    CreatedBy = 1,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockExamRepository
                .Setup(repo => repo.GetAllExams(examType, partCode))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllExams(examType, partCode);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(expectedResult[0].ExamId, result[0].ExamId);
            Assert.Equal(expectedResult[0].Name, result[0].Name);
            Assert.Equal(expectedResult[1].ExamId, result[1].ExamId);

            // Verify repository is called exactly once with null parameters
            _mockExamRepository.Verify(
                repo => repo.GetAllExams(null, null),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExams_WhenBothParametersAreNullAndRepositoryReturnsEmpty_ShouldReturnEmptyList()
        {
            // Arrange
            string? examType = null;
            string? partCode = null;
            var expectedResult = new List<ExamDTO>();

            _mockExamRepository
                .Setup(repo => repo.GetAllExams(examType, partCode))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllExams(examType, partCode);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);

            // Verify repository is called exactly once
            _mockExamRepository.Verify(
                repo => repo.GetAllExams(null, null),
                Times.Once
            );
        }

        #endregion

        #region Test Cases - Only examType Provided

        [Fact]
        public async Task GetAllExams_WhenExamTypeProvidedAndPartCodeNull_ShouldReturnFilteredList()
        {
            // Arrange
            string examType = "Reading";
            string? partCode = null;
            var expectedResult = new List<ExamDTO>
            {
                new ExamDTO
                {
                    ExamId = 1,
                    ExamType = "Reading",
                    Name = "Reading Exam",
                    Description = "Reading Description",
                    IsActive = true,
                    CreatedBy = 1,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockExamRepository
                .Setup(repo => repo.GetAllExams(examType, partCode))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllExams(examType, partCode);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(expectedResult[0].ExamId, result[0].ExamId);
            Assert.Equal(expectedResult[0].ExamType, result[0].ExamType);

            // Verify repository is called exactly once with correct parameters
            _mockExamRepository.Verify(
                repo => repo.GetAllExams(examType, null),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExams_WhenExamTypeIsEmptyStringAndPartCodeNull_ShouldReturnList()
        {
            // Arrange
            string examType = string.Empty;
            string? partCode = null;
            var expectedResult = new List<ExamDTO>();

            _mockExamRepository
                .Setup(repo => repo.GetAllExams(examType, partCode))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllExams(examType, partCode);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);

            // Verify repository is called exactly once
            _mockExamRepository.Verify(
                repo => repo.GetAllExams(string.Empty, null),
                Times.Once
            );
        }

        #endregion

        #region Test Cases - Only partCode Provided

        [Fact]
        public async Task GetAllExams_WhenPartCodeProvidedAndExamTypeNull_ShouldReturnFilteredList()
        {
            // Arrange
            string? examType = null;
            string partCode = "P1";
            var expectedResult = new List<ExamDTO>
            {
                new ExamDTO
                {
                    ExamId = 1,
                    ExamType = "Reading",
                    Name = "Part 1 Exam",
                    Description = "Part 1 Description",
                    IsActive = true,
                    CreatedBy = 1,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockExamRepository
                .Setup(repo => repo.GetAllExams(examType, partCode))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllExams(examType, partCode);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(expectedResult[0].ExamId, result[0].ExamId);

            // Verify repository is called exactly once with correct parameters
            _mockExamRepository.Verify(
                repo => repo.GetAllExams(null, partCode),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExams_WhenPartCodeIsEmptyStringAndExamTypeNull_ShouldReturnList()
        {
            // Arrange
            string? examType = null;
            string partCode = string.Empty;
            var expectedResult = new List<ExamDTO>();

            _mockExamRepository
                .Setup(repo => repo.GetAllExams(examType, partCode))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllExams(examType, partCode);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);

            // Verify repository is called exactly once
            _mockExamRepository.Verify(
                repo => repo.GetAllExams(null, string.Empty),
                Times.Once
            );
        }

        #endregion

        #region Test Cases - Both Parameters Provided

        [Fact]
        public async Task GetAllExams_WhenBothParametersProvided_ShouldReturnFilteredList()
        {
            // Arrange
            string examType = "Reading";
            string partCode = "P1";
            var expectedResult = new List<ExamDTO>
            {
                new ExamDTO
                {
                    ExamId = 1,
                    ExamType = "Reading",
                    Name = "Reading Part 1 Exam",
                    Description = "Reading Part 1 Description",
                    IsActive = true,
                    CreatedBy = 1,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockExamRepository
                .Setup(repo => repo.GetAllExams(examType, partCode))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllExams(examType, partCode);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(expectedResult[0].ExamId, result[0].ExamId);
            Assert.Equal(expectedResult[0].ExamType, result[0].ExamType);

            // Verify repository is called exactly once with both parameters
            _mockExamRepository.Verify(
                repo => repo.GetAllExams(examType, partCode),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExams_WhenBothParametersAreEmptyStrings_ShouldReturnList()
        {
            // Arrange
            string examType = string.Empty;
            string partCode = string.Empty;
            var expectedResult = new List<ExamDTO>();

            _mockExamRepository
                .Setup(repo => repo.GetAllExams(examType, partCode))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllExams(examType, partCode);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);

            // Verify repository is called exactly once
            _mockExamRepository.Verify(
                repo => repo.GetAllExams(string.Empty, string.Empty),
                Times.Once
            );
        }

        #endregion

        #region Test Cases - Default Parameters (No Arguments)

        [Fact]
        public async Task GetAllExams_WhenCalledWithNoArguments_ShouldUseDefaultNullValues()
        {
            // Arrange
            var expectedResult = new List<ExamDTO>
            {
                new ExamDTO
                {
                    ExamId = 1,
                    ExamType = "Reading",
                    Name = "Default Exam",
                    Description = "Default Description",
                    IsActive = true,
                    CreatedBy = 1,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockExamRepository
                .Setup(repo => repo.GetAllExams(null, null))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllExams();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(expectedResult[0].ExamId, result[0].ExamId);

            // Verify repository is called exactly once with default null values
            _mockExamRepository.Verify(
                repo => repo.GetAllExams(null, null),
                Times.Once
            );
        }

        #endregion

        #region Test Cases - Repository Exception Handling

        [Fact]
        public async Task GetAllExams_WhenRepositoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            string? examType = null;
            string? partCode = null;
            var exceptionMessage = "Database connection error";

            _mockExamRepository
                .Setup(repo => repo.GetAllExams(examType, partCode))
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                async () => await _service.GetAllExams(examType, partCode)
            );

            Assert.Equal(exceptionMessage, exception.Message);

            // Verify repository is called exactly once
            _mockExamRepository.Verify(
                repo => repo.GetAllExams(null, null),
                Times.Once
            );
        }

        #endregion
    }
}

