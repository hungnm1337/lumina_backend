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
    public class GetExamDetailAndExamPartByExamIDUnitTest
    {
        private readonly Mock<IExamRepository> _mockExamRepository;
        private readonly LuminaSystemContext _context;
        private readonly ExamService _service;

        public GetExamDetailAndExamPartByExamIDUnitTest()
        {
            _mockExamRepository = new Mock<IExamRepository>();
            _context = InMemoryDbContextHelper.CreateContext();

            _service = new ExamService(
                _mockExamRepository.Object,
                _context
            );
        }

        #region Test Cases for Invalid ExamId

        [Fact]
        public async Task GetExamDetailAndExamPartByExamID_WhenExamIdIsNegative_ShouldThrowArgumentException()
        {
            // Arrange
            int examId = -1;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.GetExamDetailAndExamPartByExamID(examId)
            );

            Assert.Equal("examId", exception.ParamName);
            Assert.Contains("Exam ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockExamRepository.Verify(
                repo => repo.GetExamDetailAndExamPartByExamID(It.IsAny<int>()),
                Times.Never
            );
        }

        [Fact]
        public async Task GetExamDetailAndExamPartByExamID_WhenExamIdIsZero_ShouldThrowArgumentException()
        {
            // Arrange
            int examId = 0;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.GetExamDetailAndExamPartByExamID(examId)
            );

            Assert.Equal("examId", exception.ParamName);
            Assert.Contains("Exam ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockExamRepository.Verify(
                repo => repo.GetExamDetailAndExamPartByExamID(It.IsAny<int>()),
                Times.Never
            );
        }

        #endregion

        #region Test Cases for Valid ExamId

        [Fact]
        public async Task GetExamDetailAndExamPartByExamID_WhenExamIdIsValidAndExists_ShouldReturnExamDTO()
        {
            // Arrange
            int examId = 1;
            var expectedResult = new ExamDTO
            {
                ExamId = 1,
                ExamType = "Reading",
                Name = "Test Exam",
                Description = "Test Description",
                IsActive = true,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow,
                ExamParts = new List<ExamPartDTO>
                {
                    new ExamPartDTO
                    {
                        PartId = 1,
                        ExamId = 1,
                        PartCode = "P1",
                        Title = "Part 1",
                        OrderIndex = 1,
                        Questions = new List<QuestionDTO>()
                    }
                }
            };

            _mockExamRepository
                .Setup(repo => repo.GetExamDetailAndExamPartByExamID(examId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetExamDetailAndExamPartByExamID(examId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult.ExamId, result.ExamId);
            Assert.Equal(expectedResult.Name, result.Name);
            Assert.Equal(expectedResult.ExamType, result.ExamType);
            Assert.NotNull(result.ExamParts);
            Assert.Single(result.ExamParts);
            Assert.Equal(expectedResult.ExamParts[0].PartId, result.ExamParts[0].PartId);

            // Verify repository is called exactly once with correct examId
            _mockExamRepository.Verify(
                repo => repo.GetExamDetailAndExamPartByExamID(examId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetExamDetailAndExamPartByExamID_WhenExamIdIsValidButNotFound_ShouldReturnNull()
        {
            // Arrange
            int examId = 999;

            _mockExamRepository
                .Setup(repo => repo.GetExamDetailAndExamPartByExamID(examId))
                .ReturnsAsync((ExamDTO?)null);

            // Act
            var result = await _service.GetExamDetailAndExamPartByExamID(examId);

            // Assert
            Assert.Null(result);

            // Verify repository is called exactly once
            _mockExamRepository.Verify(
                repo => repo.GetExamDetailAndExamPartByExamID(examId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetExamDetailAndExamPartByExamID_WhenExamIdIsLarge_ShouldReturnExamDTO()
        {
            // Arrange
            int examId = int.MaxValue;
            var expectedResult = new ExamDTO
            {
                ExamId = int.MaxValue,
                ExamType = "Writing",
                Name = "Large ID Exam",
                Description = "Large ID Description",
                IsActive = false,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow,
                ExamParts = null
            };

            _mockExamRepository
                .Setup(repo => repo.GetExamDetailAndExamPartByExamID(examId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetExamDetailAndExamPartByExamID(examId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult.ExamId, result.ExamId);
            Assert.Equal(expectedResult.Name, result.Name);

            // Verify repository is called exactly once
            _mockExamRepository.Verify(
                repo => repo.GetExamDetailAndExamPartByExamID(examId),
                Times.Once
            );
        }

        #endregion

        #region Test Cases - Repository Exception Handling

        [Fact]
        public async Task GetExamDetailAndExamPartByExamID_WhenRepositoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            int examId = 1;
            var exceptionMessage = "Database connection error";

            _mockExamRepository
                .Setup(repo => repo.GetExamDetailAndExamPartByExamID(examId))
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                async () => await _service.GetExamDetailAndExamPartByExamID(examId)
            );

            Assert.Equal(exceptionMessage, exception.Message);

            // Verify repository is called exactly once
            _mockExamRepository.Verify(
                repo => repo.GetExamDetailAndExamPartByExamID(examId),
                Times.Once
            );
        }

        #endregion
    }
}

