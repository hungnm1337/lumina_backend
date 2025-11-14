using DataLayer.DTOs.Vocabulary;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Lumina.Tests
{
    public class VocabularyListCreateDTOTests
    {
        [Fact]
        public void VocabularyListCreateDTO_WithDefaultValues_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var dto = new VocabularyListCreateDTO();

            // Assert
            Assert.Equal(string.Empty, dto.Name);
            Assert.False(dto.IsPublic);
        }

        [Fact]
        public void VocabularyListCreateDTO_WithName_ShouldSetAndGet()
        {
            // Arrange
            var dto = new VocabularyListCreateDTO
            {
                Name = "My Vocabulary List"
            };

            // Act & Assert
            Assert.Equal("My Vocabulary List", dto.Name);
        }

        [Fact]
        public void VocabularyListCreateDTO_WithIsPublicTrue_ShouldSetAndGet()
        {
            // Arrange
            var dto = new VocabularyListCreateDTO
            {
                IsPublic = true
            };

            // Act & Assert
            Assert.True(dto.IsPublic);
        }

        [Fact]
        public void VocabularyListCreateDTO_WithIsPublicFalse_ShouldSetAndGet()
        {
            // Arrange
            var dto = new VocabularyListCreateDTO
            {
                IsPublic = false
            };

            // Act & Assert
            Assert.False(dto.IsPublic);
        }

        [Fact]
        public void VocabularyListCreateDTO_WithAllProperties_ShouldSetAndGetAll()
        {
            // Arrange
            var dto = new VocabularyListCreateDTO
            {
                Name = "Test List",
                IsPublic = true
            };

            // Act & Assert
            Assert.Equal("Test List", dto.Name);
            Assert.True(dto.IsPublic);
        }

        [Fact]
        public void VocabularyListCreateDTO_CanChangeProperties_ShouldUpdateCorrectly()
        {
            // Arrange
            var dto = new VocabularyListCreateDTO
            {
                Name = "Initial Name",
                IsPublic = false
            };

            // Act
            dto.Name = "Updated Name";
            dto.IsPublic = true;

            // Assert
            Assert.Equal("Updated Name", dto.Name);
            Assert.True(dto.IsPublic);
        }

        [Fact]
        public void VocabularyListCreateDTO_PropertyAccessMultipleTimes_ShouldCoverProperty()
        {
            // Arrange
            var dto = new VocabularyListCreateDTO();

            // Act - Access properties multiple times
            var name1 = dto.Name;
            var isPublic1 = dto.IsPublic;

            dto.Name = "Name 1";
            dto.IsPublic = true;

            var name2 = dto.Name;
            var isPublic2 = dto.IsPublic;

            dto.Name = "Name 2";
            dto.IsPublic = false;

            var name3 = dto.Name;
            var isPublic3 = dto.IsPublic;

            // Assert
            Assert.Equal(string.Empty, name1);
            Assert.False(isPublic1);
            Assert.Equal("Name 1", name2);
            Assert.True(isPublic2);
            Assert.Equal("Name 2", name3);
            Assert.False(isPublic3);
        }

        [Fact]
        public void VocabularyListCreateDTO_UsedInCondition_ShouldEvaluateCorrectly()
        {
            // Arrange
            var dto = new VocabularyListCreateDTO
            {
                IsPublic = true,
                Name = "Public List"
            };

            // Act - Use IsPublic in condition to ensure coverage
            string result;
            if (dto.IsPublic)
            {
                result = $"Public: {dto.Name}";
            }
            else
            {
                result = $"Private: {dto.Name}";
            }

            // Assert
            Assert.Equal("Public: Public List", result);
        }

        [Fact]
        public void VocabularyListCreateDTO_UsedInCollection_ShouldWorkCorrectly()
        {
            // Arrange
            var dtos = new List<VocabularyListCreateDTO>
            {
                new VocabularyListCreateDTO { Name = "List 1", IsPublic = true },
                new VocabularyListCreateDTO { Name = "List 2", IsPublic = false },
                new VocabularyListCreateDTO { Name = "List 3", IsPublic = true }
            };

            // Act - Use properties in LINQ queries to ensure coverage
            var publicLists = dtos.Where(d => d.IsPublic).ToList();
            var privateLists = dtos.Where(d => !d.IsPublic).ToList();
            var allNames = dtos.Select(d => d.Name).ToList();
            var allIsPublic = dtos.Select(d => d.IsPublic).ToList();

            // Assert
            Assert.Equal(2, publicLists.Count);
            Assert.Single(privateLists);
            Assert.Equal(3, allNames.Count);
            Assert.Equal(3, allIsPublic.Count);
            Assert.Contains(true, allIsPublic);
            Assert.Contains(false, allIsPublic);
        }

        [Fact]
        public void VocabularyListCreateDTO_WithLongName_ShouldHandleLongName()
        {
            // Arrange
            var longName = new string('A', 255); // MaxLength is 255
            var dto = new VocabularyListCreateDTO
            {
                Name = longName,
                IsPublic = false
            };

            // Act & Assert
            Assert.Equal(longName, dto.Name);
            Assert.Equal(255, dto.Name.Length);
        }

        [Fact]
        public void VocabularyListCreateDTO_WithEmptyName_ShouldAllowEmptyName()
        {
            // Arrange
            var dto = new VocabularyListCreateDTO
            {
                Name = string.Empty,
                IsPublic = true
            };

            // Act & Assert
            Assert.Equal(string.Empty, dto.Name);
            Assert.True(dto.IsPublic);
        }

        [Fact]
        public void VocabularyListCreateDTO_AllPropertiesAccess_ShouldCoverAllProperties()
        {
            // Arrange & Act
            var dto = new VocabularyListCreateDTO
            {
                Name = "Test",
                IsPublic = true
            };

            // Access all properties multiple times to ensure coverage
            var name1 = dto.Name;
            var isPublic1 = dto.IsPublic;
            var name2 = dto.Name;
            var isPublic2 = dto.IsPublic;

            // Assert
            Assert.Equal("Test", name1);
            Assert.True(isPublic1);
            Assert.Equal("Test", name2);
            Assert.True(isPublic2);
        }
    }
}


