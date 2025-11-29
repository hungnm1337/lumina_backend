using Xunit;
using Moq;
using ServiceLayer.Chat;
using DataLayer.DTOs.Chat;
using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class SaveGeneratedVocabulariesUnitTest
    {
        private readonly LuminaSystemContext _context;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<ServiceLayer.UploadFile.IUploadService> _mockUploadService;
        private readonly ChatService _service;

        public SaveGeneratedVocabulariesUnitTest()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockUploadService = new Mock<ServiceLayer.UploadFile.IUploadService>();

            // Setup configuration
            _mockConfiguration.Setup(c => c["GeminiStudent:ApiKey"]).Returns("test-api-key");
            _mockConfiguration.Setup(c => c["GeminiStudent:BaseUrl"]).Returns("https://test.com");
            _mockConfiguration.Setup(c => c["GeminiStudent:Model"]).Returns("test-model");

            _service = new ChatService(
                _context,
                _mockConfiguration.Object,
                _mockHttpClientFactory.Object,
                _mockUploadService.Object
            );
        }

        [Fact]
        public async Task SaveGeneratedVocabularies_WhenRequestIsValid_ShouldCreateAllEntitiesAndReturnSuccess()
        {
            // Arrange
            var request = new SaveVocabularyRequestDTO
            {
                UserId = 1,
                FolderName = "Business Vocabulary",
                Vocabularies = new List<GeneratedVocabularyDTO>
                {
                    new GeneratedVocabularyDTO
                    {
                        Word = "acquire",
                        Definition = "đạt được, thu được",
                        Example = "The company acquired a new building.",
                        TypeOfWord = "Verb",
                        Category = "Business",
                        ImageUrl = "http://cloudinary.com/image1.jpg"
                    },
                    new GeneratedVocabularyDTO
                    {
                        Word = "benefit",
                        Definition = "lợi ích",
                        Example = "The benefits are great.",
                        TypeOfWord = "Noun",
                        Category = "Business",
                        ImageUrl = "http://cloudinary.com/image2.jpg"
                    }
                }
            };

            // Act
            var result = await _service.SaveGeneratedVocabularies(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("Đã tạo folder 'Business Vocabulary' và lưu 2 từ vựng!", result.Message);
            Assert.Equal(2, result.VocabularyCount);
            Assert.True(result.VocabularyListId > 0);

            // Verify database entities
            var vocabularyList = await _context.VocabularyLists.FindAsync(result.VocabularyListId);
            Assert.NotNull(vocabularyList);
            Assert.Equal("Business Vocabulary", vocabularyList.Name);
            Assert.Equal(1, vocabularyList.MakeBy);
            Assert.True(vocabularyList.IsPublic);
            Assert.Equal("Published", vocabularyList.Status);
            Assert.False(vocabularyList.IsDeleted);

            var vocabularies = _context.Vocabularies.Where(v => v.VocabularyListId == result.VocabularyListId).ToList();
            Assert.Equal(2, vocabularies.Count);
            Assert.Contains(vocabularies, v => v.Word == "acquire");
            Assert.Contains(vocabularies, v => v.Word == "benefit");

            var spacedRepetitions = _context.UserSpacedRepetitions
                .Where(sr => sr.VocabularyListId == result.VocabularyListId)
                .ToList();
            Assert.Equal(2, spacedRepetitions.Count);
            Assert.All(spacedRepetitions, sr =>
            {
                Assert.Equal(1, sr.UserId);
                Assert.Equal("New", sr.Status);
                Assert.Equal(0, sr.ReviewCount);
                Assert.Equal(1, sr.Intervals);
            });
        }

        [Fact]
        public async Task SaveGeneratedVocabularies_WhenSingleVocabulary_ShouldSaveCorrectly()
        {
            // Arrange
            var request = new SaveVocabularyRequestDTO
            {
                UserId = 2,
                FolderName = "Test Folder",
                Vocabularies = new List<GeneratedVocabularyDTO>
                {
                    new GeneratedVocabularyDTO
                    {
                        Word = "test",
                        Definition = "thử nghiệm",
                        Example = "This is a test.",
                        TypeOfWord = "Noun",
                        Category = "General",
                        ImageUrl = "http://test.jpg"
                    }
                }
            };

            // Act
            var result = await _service.SaveGeneratedVocabularies(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(1, result.VocabularyCount);
            Assert.Contains("1 từ vựng", result.Message);
        }


        [Fact]
        public async Task SaveGeneratedVocabularies_WhenMultipleVocabularies_ShouldCreateAllSpacedRepetitions()
        {
            // Arrange
            var vocabularies = new List<GeneratedVocabularyDTO>();
            for (int i = 1; i <= 10; i++)
            {
                vocabularies.Add(new GeneratedVocabularyDTO
                {
                    Word = $"word{i}",
                    Definition = $"definition{i}",
                    Example = $"example{i}",
                    TypeOfWord = "Noun",
                    Category = "Test",
                    ImageUrl = $"http://image{i}.jpg"
                });
            }

            var request = new SaveVocabularyRequestDTO
            {
                UserId = 3,
                FolderName = "Large Folder",
                Vocabularies = vocabularies
            };

            // Act
            var result = await _service.SaveGeneratedVocabularies(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(10, result.VocabularyCount);

            var savedVocabs = _context.Vocabularies.Where(v => v.VocabularyListId == result.VocabularyListId).ToList();
            Assert.Equal(10, savedVocabs.Count);

            var spacedReps = _context.UserSpacedRepetitions
                .Where(sr => sr.VocabularyListId == result.VocabularyListId)
                .ToList();
            Assert.Equal(10, spacedReps.Count);
        }

        [Fact]
        public async Task SaveGeneratedVocabularies_WhenCalled_ShouldSetCorrectVocabularyListProperties()
        {
            // Arrange
            var request = new SaveVocabularyRequestDTO
            {
                UserId = 5,
                FolderName = "Property Test Folder",
                Vocabularies = new List<GeneratedVocabularyDTO>
                {
                    new GeneratedVocabularyDTO
                    {
                        Word = "test",
                        Definition = "test",
                        Example = "test",
                        TypeOfWord = "Noun",
                        Category = "Test",
                        ImageUrl = "http://test.jpg"
                    }
                }
            };

            // Act
            var result = await _service.SaveGeneratedVocabularies(request);

            // Assert
            var list = await _context.VocabularyLists.FindAsync(result.VocabularyListId);
            Assert.NotNull(list);
            Assert.Equal(5, list.MakeBy);
            Assert.Equal("Property Test Folder", list.Name);
            Assert.True(list.IsPublic); // Auto public
            Assert.Equal("Published", list.Status); // Auto published
            Assert.False(list.IsDeleted);
            Assert.NotEqual(default(DateTime), list.CreateAt);
        }
    }
}
