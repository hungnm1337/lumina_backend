using DataLayer.DTOs.Chat;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Lumina.Tests
{
    public class SaveVocabularyResponseDTOTests
    {
        [Fact]
        public void SaveVocabularyResponseDTO_WithDefaultValues_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var dto = new SaveVocabularyResponseDTO();

            // Assert
            Assert.False(dto.Success);
            Assert.Null(dto.Message);
            Assert.Equal(0, dto.VocabularyListId);
            Assert.Equal(0, dto.VocabularyCount);
        }

        [Fact]
        public void SaveVocabularyResponseDTO_WithSuccess_ShouldSetAndGet()
        {
            // Arrange
            var dto = new SaveVocabularyResponseDTO
            {
                Success = true
            };

            // Act & Assert
            Assert.True(dto.Success);
        }

        [Fact]
        public void SaveVocabularyResponseDTO_WithMessage_ShouldSetAndGet()
        {
            // Arrange
            var dto = new SaveVocabularyResponseDTO
            {
                Message = "Vocabulary saved successfully"
            };

            // Act & Assert
            Assert.Equal("Vocabulary saved successfully", dto.Message);
        }

        [Fact]
        public void SaveVocabularyResponseDTO_WithVocabularyListId_ShouldSetAndGet()
        {
            // Arrange
            var dto = new SaveVocabularyResponseDTO
            {
                VocabularyListId = 123
            };

            // Act & Assert
            Assert.Equal(123, dto.VocabularyListId);
        }

        [Fact]
        public void SaveVocabularyResponseDTO_WithVocabularyCount_ShouldSetAndGet()
        {
            // Arrange
            var dto = new SaveVocabularyResponseDTO
            {
                VocabularyCount = 10
            };

            // Act & Assert
            Assert.Equal(10, dto.VocabularyCount);
        }

        [Fact]
        public void SaveVocabularyResponseDTO_WithAllProperties_ShouldSetAndGetAll()
        {
            // Arrange
            var dto = new SaveVocabularyResponseDTO
            {
                Success = true,
                Message = "Success",
                VocabularyListId = 456,
                VocabularyCount = 25
            };

            // Act & Assert
            Assert.True(dto.Success);
            Assert.Equal("Success", dto.Message);
            Assert.Equal(456, dto.VocabularyListId);
            Assert.Equal(25, dto.VocabularyCount);
        }

        [Fact]
        public void SaveVocabularyResponseDTO_WithFailure_ShouldSetFailureProperties()
        {
            // Arrange
            var dto = new SaveVocabularyResponseDTO
            {
                Success = false,
                Message = "Failed to save vocabulary",
                VocabularyListId = 0,
                VocabularyCount = 0
            };

            // Act & Assert
            Assert.False(dto.Success);
            Assert.Equal("Failed to save vocabulary", dto.Message);
            Assert.Equal(0, dto.VocabularyListId);
            Assert.Equal(0, dto.VocabularyCount);
        }

        [Fact]
        public void SaveVocabularyResponseDTO_CanChangeProperties_ShouldUpdateCorrectly()
        {
            // Arrange
            var dto = new SaveVocabularyResponseDTO
            {
                Success = false,
                Message = "Initial",
                VocabularyListId = 100,
                VocabularyCount = 5
            };

            // Act
            dto.Success = true;
            dto.Message = "Updated";
            dto.VocabularyListId = 200;
            dto.VocabularyCount = 10;

            // Assert
            Assert.True(dto.Success);
            Assert.Equal("Updated", dto.Message);
            Assert.Equal(200, dto.VocabularyListId);
            Assert.Equal(10, dto.VocabularyCount);
        }

        [Fact]
        public void SaveVocabularyResponseDTO_PropertyAccessMultipleTimes_ShouldCoverProperty()
        {
            // Arrange
            var dto = new SaveVocabularyResponseDTO();

            // Act - Access properties multiple times
            var success1 = dto.Success;
            var message1 = dto.Message;
            var listId1 = dto.VocabularyListId;
            var count1 = dto.VocabularyCount;

            dto.Success = true;
            dto.Message = "Test";
            dto.VocabularyListId = 50;
            dto.VocabularyCount = 15;

            var success2 = dto.Success;
            var message2 = dto.Message;
            var listId2 = dto.VocabularyListId;
            var count2 = dto.VocabularyCount;

            // Assert
            Assert.False(success1);
            Assert.Null(message1);
            Assert.Equal(0, listId1);
            Assert.Equal(0, count1);

            Assert.True(success2);
            Assert.Equal("Test", message2);
            Assert.Equal(50, listId2);
            Assert.Equal(15, count2);
        }

        [Fact]
        public void SaveVocabularyResponseDTO_UsedInCondition_ShouldEvaluateCorrectly()
        {
            // Arrange
            var dto = new SaveVocabularyResponseDTO
            {
                Success = true,
                Message = "Success"
            };

            // Act - Use Success in condition to ensure coverage
            string result;
            if (dto.Success)
            {
                result = dto.Message;
            }
            else
            {
                result = "Failed";
            }

            // Assert
            Assert.Equal("Success", result);
        }

        [Fact]
        public void SaveVocabularyResponseDTO_UsedInCollection_ShouldWorkCorrectly()
        {
            // Arrange
            var responses = new List<SaveVocabularyResponseDTO>
            {
                new SaveVocabularyResponseDTO { Success = true, Message = "Success 1", VocabularyListId = 1, VocabularyCount = 5 },
                new SaveVocabularyResponseDTO { Success = false, Message = "Failed", VocabularyListId = 0, VocabularyCount = 0 },
                new SaveVocabularyResponseDTO { Success = true, Message = "Success 2", VocabularyListId = 2, VocabularyCount = 10 }
            };

            // Act - Use properties in LINQ queries to ensure coverage
            var successful = responses.Where(r => r.Success).ToList();
            var failed = responses.Where(r => !r.Success).ToList();
            var allListIds = responses.Select(r => r.VocabularyListId).ToList();
            var allCounts = responses.Select(r => r.VocabularyCount).ToList();
            var allMessages = responses.Select(r => r.Message).ToList();

            // Assert
            Assert.Equal(2, successful.Count);
            Assert.Single(failed);
            Assert.Equal(3, allListIds.Count);
            Assert.Equal(3, allCounts.Count);
            Assert.Equal(3, allMessages.Count);
            Assert.Contains(1, allListIds);
            Assert.Contains(5, allCounts);
            Assert.Contains(10, allCounts);
        }

        [Fact]
        public void SaveVocabularyResponseDTO_AllPropertiesAccess_ShouldCoverAllProperties()
        {
            // Arrange & Act
            var dto = new SaveVocabularyResponseDTO
            {
                Success = true,
                Message = "Test",
                VocabularyListId = 1,
                VocabularyCount = 5
            };

            // Access all properties multiple times to ensure coverage
            var success1 = dto.Success;
            var message1 = dto.Message;
            var listId1 = dto.VocabularyListId;
            var count1 = dto.VocabularyCount;

            var success2 = dto.Success;
            var message2 = dto.Message;
            var listId2 = dto.VocabularyListId;
            var count2 = dto.VocabularyCount;

            // Assert
            Assert.True(success1);
            Assert.Equal("Test", message1);
            Assert.Equal(1, listId1);
            Assert.Equal(5, count1);

            Assert.True(success2);
            Assert.Equal("Test", message2);
            Assert.Equal(1, listId2);
            Assert.Equal(5, count2);
        }
    }
}

