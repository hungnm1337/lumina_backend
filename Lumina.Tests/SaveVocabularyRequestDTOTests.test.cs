using DataLayer.DTOs.Chat;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Lumina.Tests
{
    public class SaveVocabularyRequestDTOTests
    {
        [Fact]
        public void SaveVocabularyRequestDTO_WithDefaultValues_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var dto = new SaveVocabularyRequestDTO();

            // Assert
            Assert.Equal(0, dto.UserId);
            Assert.Null(dto.FolderName);
            Assert.NotNull(dto.Vocabularies);
            Assert.Empty(dto.Vocabularies);
        }

        [Fact]
        public void SaveVocabularyRequestDTO_WithUserId_ShouldSetAndGet()
        {
            // Arrange
            var dto = new SaveVocabularyRequestDTO
            {
                UserId = 123
            };

            // Act & Assert
            Assert.Equal(123, dto.UserId);
        }

        [Fact]
        public void SaveVocabularyRequestDTO_WithFolderName_ShouldSetAndGet()
        {
            // Arrange
            var dto = new SaveVocabularyRequestDTO
            {
                FolderName = "My Folder"
            };

            // Act & Assert
            Assert.Equal("My Folder", dto.FolderName);
        }

        [Fact]
        public void SaveVocabularyRequestDTO_WithVocabularies_ShouldSetAndGet()
        {
            // Arrange
            var vocabularies = new List<GeneratedVocabularyDTO>
            {
                new GeneratedVocabularyDTO { Word = "Hello", Definition = "Greeting" },
                new GeneratedVocabularyDTO { Word = "World", Definition = "Planet" }
            };
            var dto = new SaveVocabularyRequestDTO
            {
                Vocabularies = vocabularies
            };

            // Act & Assert
            Assert.NotNull(dto.Vocabularies);
            Assert.Equal(2, dto.Vocabularies.Count);
            Assert.Equal("Hello", dto.Vocabularies[0].Word);
            Assert.Equal("World", dto.Vocabularies[1].Word);
        }

        [Fact]
        public void SaveVocabularyRequestDTO_WithAllProperties_ShouldSetAndGetAll()
        {
            // Arrange
            var vocabularies = new List<GeneratedVocabularyDTO>
            {
                new GeneratedVocabularyDTO { Word = "Test", Definition = "Test Definition" }
            };
            var dto = new SaveVocabularyRequestDTO
            {
                UserId = 456,
                FolderName = "Test Folder",
                Vocabularies = vocabularies
            };

            // Act & Assert
            Assert.Equal(456, dto.UserId);
            Assert.Equal("Test Folder", dto.FolderName);
            Assert.NotNull(dto.Vocabularies);
            Assert.Single(dto.Vocabularies);
        }

        [Fact]
        public void SaveVocabularyRequestDTO_WithEmptyVocabularies_ShouldAllowEmptyList()
        {
            // Arrange
            var dto = new SaveVocabularyRequestDTO
            {
                UserId = 789,
                FolderName = "Empty Folder",
                Vocabularies = new List<GeneratedVocabularyDTO>()
            };

            // Act & Assert
            Assert.NotNull(dto.Vocabularies);
            Assert.Empty(dto.Vocabularies);
        }

        [Fact]
        public void SaveVocabularyRequestDTO_CanChangeProperties_ShouldUpdateCorrectly()
        {
            // Arrange
            var dto = new SaveVocabularyRequestDTO
            {
                UserId = 100,
                FolderName = "Initial Folder",
                Vocabularies = new List<GeneratedVocabularyDTO>()
            };

            // Act
            dto.UserId = 200;
            dto.FolderName = "Updated Folder";
            dto.Vocabularies.Add(new GeneratedVocabularyDTO { Word = "New Word" });

            // Assert
            Assert.Equal(200, dto.UserId);
            Assert.Equal("Updated Folder", dto.FolderName);
            Assert.Single(dto.Vocabularies);
            Assert.Equal("New Word", dto.Vocabularies[0].Word);
        }

        [Fact]
        public void SaveVocabularyRequestDTO_PropertyAccessMultipleTimes_ShouldCoverProperty()
        {
            // Arrange
            var dto = new SaveVocabularyRequestDTO();

            // Act - Access properties multiple times
            var userId1 = dto.UserId;
            var folderName1 = dto.FolderName;
            var vocabularies1 = dto.Vocabularies;

            dto.UserId = 10;
            dto.FolderName = "Folder 1";
            dto.Vocabularies = new List<GeneratedVocabularyDTO> { new GeneratedVocabularyDTO { Word = "Word 1" } };

            var userId2 = dto.UserId;
            var folderName2 = dto.FolderName;
            var vocabularies2 = dto.Vocabularies;

            // Assert
            Assert.Equal(0, userId1);
            Assert.Null(folderName1);
            Assert.NotNull(vocabularies1);

            Assert.Equal(10, userId2);
            Assert.Equal("Folder 1", folderName2);
            Assert.NotNull(vocabularies2);
            Assert.Single(vocabularies2);
        }

        [Fact]
        public void SaveVocabularyRequestDTO_UsedInCollection_ShouldWorkCorrectly()
        {
            // Arrange
            var requests = new List<SaveVocabularyRequestDTO>
            {
                new SaveVocabularyRequestDTO { UserId = 1, FolderName = "Folder 1", Vocabularies = new List<GeneratedVocabularyDTO>() },
                new SaveVocabularyRequestDTO { UserId = 2, FolderName = "Folder 2", Vocabularies = new List<GeneratedVocabularyDTO> { new GeneratedVocabularyDTO { Word = "Test" } } },
                new SaveVocabularyRequestDTO { UserId = 3, FolderName = "Folder 3", Vocabularies = new List<GeneratedVocabularyDTO>() }
            };

            // Act - Use properties in LINQ queries to ensure coverage
            var withVocabularies = requests.Where(r => r.Vocabularies.Any()).ToList();
            var withoutVocabularies = requests.Where(r => !r.Vocabularies.Any()).ToList();
            var allUserIds = requests.Select(r => r.UserId).ToList();
            var allFolderNames = requests.Select(r => r.FolderName).ToList();

            // Assert
            Assert.Single(withVocabularies);
            Assert.Equal(2, withoutVocabularies.Count);
            Assert.Equal(3, allUserIds.Count);
            Assert.Equal(3, allFolderNames.Count);
            Assert.Contains(1, allUserIds);
            Assert.Contains("Folder 1", allFolderNames);
        }

        [Fact]
        public void SaveVocabularyRequestDTO_AllPropertiesAccess_ShouldCoverAllProperties()
        {
            // Arrange & Act
            var vocabularies = new List<GeneratedVocabularyDTO>
            {
                new GeneratedVocabularyDTO { Word = "Test" }
            };
            var dto = new SaveVocabularyRequestDTO
            {
                UserId = 1,
                FolderName = "Test",
                Vocabularies = vocabularies
            };

            // Access all properties multiple times to ensure coverage
            var userId1 = dto.UserId;
            var folderName1 = dto.FolderName;
            var vocabularies1 = dto.Vocabularies;

            var userId2 = dto.UserId;
            var folderName2 = dto.FolderName;
            var vocabularies2 = dto.Vocabularies;

            // Assert
            Assert.Equal(1, userId1);
            Assert.Equal("Test", folderName1);
            Assert.NotNull(vocabularies1);
            Assert.Single(vocabularies1);

            Assert.Equal(1, userId2);
            Assert.Equal("Test", folderName2);
            Assert.NotNull(vocabularies2);
            Assert.Single(vocabularies2);
        }
    }
}

