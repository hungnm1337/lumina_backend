// File: GetAllExams.test.cs
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using lumina.Controllers;
using ServiceLayer.Exam;
using DataLayer.DTOs.Exam;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lumina.Tests
{
    public class GetAllExamsTests
    {
        private readonly Mock<IExamService> _mockExamService;
        private readonly ExamController _controller;

        public GetAllExamsTests()
        {
            _mockExamService = new Mock<IExamService>();
            _controller = new ExamController(_mockExamService.Object);
        }

        [Fact]
        public async Task GetAllExams_KhiKhongCoFilter_TraVeOkVaTatCaExam()
        {
            // Arrange
            var expectedExams = new List<ExamDTO>
            {
                new ExamDTO
                {
                    ExamId = 1,
                    ExamType = "FULL_TEST",
                    Name = "TOEIC Test 1",
                    Description = "Full test 1",
                    IsActive = true
                },
                new ExamDTO
                {
                    ExamId = 2,
                    ExamType = "LISTENING",
                    Name = "Listening Test 1",
                    Description = "Listening only",
                    IsActive = true
                }
            };

            _mockExamService
                .Setup(s => s.GetAllExams(null, null))
                .ReturnsAsync(expectedExams);

            // Act
            var result = await _controller.GetAllExams(null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedExams = Assert.IsType<List<ExamDTO>>(okResult.Value);
            Assert.Equal(2, returnedExams.Count);
            Assert.Equal(1, returnedExams[0].ExamId);
            Assert.Equal(2, returnedExams[1].ExamId);
            _mockExamService.Verify(s => s.GetAllExams(null, null), Times.Once);
        }

        [Fact]
        public async Task GetAllExams_KhiFilterTheoExamType_TraVeOkVaExamDaLoc()
        {
            // Arrange
            string examType = "FULL_TEST";
            var expectedExams = new List<ExamDTO>
            {
                new ExamDTO
                {
                    ExamId = 1,
                    ExamType = "FULL_TEST",
                    Name = "TOEIC Test 1",
                    Description = "Full test 1",
                    IsActive = true
                }
            };

            _mockExamService
                .Setup(s => s.GetAllExams(examType, null))
                .ReturnsAsync(expectedExams);

            // Act
            var result = await _controller.GetAllExams(examType, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedExams = Assert.IsType<List<ExamDTO>>(okResult.Value);
            Assert.Single(returnedExams);
            Assert.Equal("FULL_TEST", returnedExams[0].ExamType);
            _mockExamService.Verify(s => s.GetAllExams(examType, null), Times.Once);
        }

        [Fact]
        public async Task GetAllExams_KhiFilterTheoPartCode_TraVeOkVaExamDaLoc()
        {
            // Arrange
            string partCode = "Part1";
            var expectedExams = new List<ExamDTO>
            {
                new ExamDTO
                {
                    ExamId = 1,
                    ExamType = "LISTENING",
                    Name = "Listening Test",
                    Description = "Contains Part 1",
                    IsActive = true
                }
            };

            _mockExamService
                .Setup(s => s.GetAllExams(null, partCode))
                .ReturnsAsync(expectedExams);

            // Act
            var result = await _controller.GetAllExams(null, partCode);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedExams = Assert.IsType<List<ExamDTO>>(okResult.Value);
            Assert.Single(returnedExams);
            _mockExamService.Verify(s => s.GetAllExams(null, partCode), Times.Once);
        }

        [Fact]
        public async Task GetAllExams_KhiFilterCaExamTypeVaPartCode_TraVeOkVaExamDaLoc()
        {
            // Arrange
            string examType = "FULL_TEST";
            string partCode = "Part7";
            var expectedExams = new List<ExamDTO>
            {
                new ExamDTO
                {
                    ExamId = 1,
                    ExamType = "FULL_TEST",
                    Name = "TOEIC Test 1",
                    Description = "Full test with Part 7",
                    IsActive = true
                }
            };

            _mockExamService
                .Setup(s => s.GetAllExams(examType, partCode))
                .ReturnsAsync(expectedExams);

            // Act
            var result = await _controller.GetAllExams(examType, partCode);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedExams = Assert.IsType<List<ExamDTO>>(okResult.Value);
            Assert.Single(returnedExams);
            Assert.Equal("FULL_TEST", returnedExams[0].ExamType);
            _mockExamService.Verify(s => s.GetAllExams(examType, partCode), Times.Once);
        }

        [Fact]
        public async Task GetAllExams_KhiKhongCoExamNao_TraVeOkVaDanhSachRong()
        {
            // Arrange
            var emptyExams = new List<ExamDTO>();

            _mockExamService
                .Setup(s => s.GetAllExams(null, null))
                .ReturnsAsync(emptyExams);

            // Act
            var result = await _controller.GetAllExams(null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedExams = Assert.IsType<List<ExamDTO>>(okResult.Value);
            Assert.Empty(returnedExams);
            _mockExamService.Verify(s => s.GetAllExams(null, null), Times.Once);
        }

        [Fact]
        public async Task GetAllExams_KhiExamTypeRong_TraVeOkVaExam()
        {
            // Arrange
            string examType = string.Empty;
            var expectedExams = new List<ExamDTO>
            {
                new ExamDTO
                {
                    ExamId = 1,
                    ExamType = "FULL_TEST",
                    Name = "Test 1",
                    Description = "Description",
                    IsActive = true
                }
            };

            _mockExamService
                .Setup(s => s.GetAllExams(examType, null))
                .ReturnsAsync(expectedExams);

            // Act
            var result = await _controller.GetAllExams(examType, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedExams = Assert.IsType<List<ExamDTO>>(okResult.Value);
            Assert.Single(returnedExams);
            _mockExamService.Verify(s => s.GetAllExams(examType, null), Times.Once);
        }

        [Fact]
        public async Task GetAllExams_KhiPartCodeRong_TraVeOkVaExam()
        {
            // Arrange
            string partCode = string.Empty;
            var expectedExams = new List<ExamDTO>
            {
                new ExamDTO
                {
                    ExamId = 1,
                    ExamType = "LISTENING",
                    Name = "Test 1",
                    Description = "Description",
                    IsActive = true
                }
            };

            _mockExamService
                .Setup(s => s.GetAllExams(null, partCode))
                .ReturnsAsync(expectedExams);

            // Act
            var result = await _controller.GetAllExams(null, partCode);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedExams = Assert.IsType<List<ExamDTO>>(okResult.Value);
            Assert.Single(returnedExams);
            _mockExamService.Verify(s => s.GetAllExams(null, partCode), Times.Once);
        }

        [Fact]
        public async Task GetAllExams_KhiServiceThrowException_ThrowException()
        {
            // Arrange
            _mockExamService
                .Setup(s => s.GetAllExams(null, null))
                .ThrowsAsync(new System.Exception("Database connection error"));

            // Act & Assert
            await Assert.ThrowsAsync<System.Exception>(async () =>
                await _controller.GetAllExams(null, null));

            _mockExamService.Verify(s => s.GetAllExams(null, null), Times.Once);
        }
    }
}
