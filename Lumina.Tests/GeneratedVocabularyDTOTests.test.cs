using DataLayer.DTOs.Chat;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Lumina.Tests
{
    public class GeneratedVocabularyDTOTests
    {
        [Fact]
        public void GeneratedVocabularyDTO_WithDefaultValues_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var dto = new GeneratedVocabularyDTO();

            // Assert
            Assert.Null(dto.Word);
            Assert.Null(dto.Definition);
            Assert.Null(dto.Example);
            Assert.Null(dto.TypeOfWord);
            Assert.Null(dto.Category);
        }

        [Fact]
        public void GeneratedVocabularyDTO_WithWord_ShouldSetAndGet()
        {
            // Arrange
            var dto = new GeneratedVocabularyDTO
            {
                Word = "Hello"
            };

            // Act & Assert
            Assert.Equal("Hello", dto.Word);
        }

        [Fact]
        public void GeneratedVocabularyDTO_WithDefinition_ShouldSetAndGet()
        {
            // Arrange
            var dto = new GeneratedVocabularyDTO
            {
                Definition = "A greeting"
            };

            // Act & Assert
            Assert.Equal("A greeting", dto.Definition);
        }

        [Fact]
        public void GeneratedVocabularyDTO_WithExample_ShouldSetAndGet()
        {
            // Arrange
            var dto = new GeneratedVocabularyDTO
            {
                Example = "Hello, how are you?"
            };

            // Act & Assert
            Assert.Equal("Hello, how are you?", dto.Example);
        }

        [Fact]
        public void GeneratedVocabularyDTO_WithTypeOfWord_ShouldSetAndGet()
        {
            // Arrange
            var dto = new GeneratedVocabularyDTO
            {
                TypeOfWord = "noun"
            };

            // Act & Assert
            Assert.Equal("noun", dto.TypeOfWord);
        }

        [Fact]
        public void GeneratedVocabularyDTO_WithCategory_ShouldSetAndGet()
        {
            // Arrange
            var dto = new GeneratedVocabularyDTO
            {
                Category = "greeting"
            };

            // Act & Assert
            Assert.Equal("greeting", dto.Category);
        }

        [Fact]
        public void GeneratedVocabularyDTO_WithAllProperties_ShouldSetAndGetAll()
        {
            // Arrange
            var dto = new GeneratedVocabularyDTO
            {
                Word = "Beautiful",
                Definition = "Pleasing to the senses",
                Example = "She has a beautiful smile",
                TypeOfWord = "adjective",
                Category = "appearance"
            };

            // Act & Assert
            Assert.Equal("Beautiful", dto.Word);
            Assert.Equal("Pleasing to the senses", dto.Definition);
            Assert.Equal("She has a beautiful smile", dto.Example);
            Assert.Equal("adjective", dto.TypeOfWord);
            Assert.Equal("appearance", dto.Category);
        }

        [Fact]
        public void GeneratedVocabularyDTO_CanChangeProperties_ShouldUpdateCorrectly()
        {
            // Arrange
            var dto = new GeneratedVocabularyDTO
            {
                Word = "Initial",
                Definition = "Initial definition",
                Example = "Initial example",
                TypeOfWord = "noun",
                Category = "general"
            };

            // Act
            dto.Word = "Updated";
            dto.Definition = "Updated definition";
            dto.Example = "Updated example";
            dto.TypeOfWord = "verb";
            dto.Category = "action";

            // Assert
            Assert.Equal("Updated", dto.Word);
            Assert.Equal("Updated definition", dto.Definition);
            Assert.Equal("Updated example", dto.Example);
            Assert.Equal("verb", dto.TypeOfWord);
            Assert.Equal("action", dto.Category);
        }

        [Fact]
        public void GeneratedVocabularyDTO_PropertyAccessMultipleTimes_ShouldCoverProperty()
        {
            // Arrange
            var dto = new GeneratedVocabularyDTO();

            // Act - Access all properties multiple times
            var word1 = dto.Word;
            var definition1 = dto.Definition;
            var example1 = dto.Example;
            var type1 = dto.TypeOfWord;
            var category1 = dto.Category;

            dto.Word = "Test";
            dto.Definition = "Test definition";
            dto.Example = "Test example";
            dto.TypeOfWord = "test";
            dto.Category = "test";

            var word2 = dto.Word;
            var definition2 = dto.Definition;
            var example2 = dto.Example;
            var type2 = dto.TypeOfWord;
            var category2 = dto.Category;

            // Assert
            Assert.Null(word1);
            Assert.Null(definition1);
            Assert.Null(example1);
            Assert.Null(type1);
            Assert.Null(category1);

            Assert.Equal("Test", word2);
            Assert.Equal("Test definition", definition2);
            Assert.Equal("Test example", example2);
            Assert.Equal("test", type2);
            Assert.Equal("test", category2);
        }

        [Fact]
        public void GeneratedVocabularyDTO_UsedInCollection_ShouldWorkCorrectly()
        {
            // Arrange
            var vocabularies = new List<GeneratedVocabularyDTO>
            {
                new GeneratedVocabularyDTO { Word = "Hello", TypeOfWord = "noun", Category = "greeting" },
                new GeneratedVocabularyDTO { Word = "Run", TypeOfWord = "verb", Category = "action" },
                new GeneratedVocabularyDTO { Word = "Beautiful", TypeOfWord = "adjective", Category = "appearance" }
            };

            // Act - Use properties in LINQ queries to ensure coverage
            var nouns = vocabularies.Where(v => v.TypeOfWord == "noun").ToList();
            var verbs = vocabularies.Where(v => v.TypeOfWord == "verb").ToList();
            var allWords = vocabularies.Select(v => v.Word).ToList();
            var allCategories = vocabularies.Select(v => v.Category).ToList();
            var allDefinitions = vocabularies.Select(v => v.Definition).ToList();

            // Assert
            Assert.Single(nouns);
            Assert.Single(verbs);
            Assert.Equal(3, allWords.Count);
            Assert.Equal(3, allCategories.Count);
            Assert.Equal(3, allDefinitions.Count);
            Assert.Contains("Hello", allWords);
            Assert.Contains("greeting", allCategories);
        }

        [Fact]
        public void GeneratedVocabularyDTO_AllPropertiesAccess_ShouldCoverAllProperties()
        {
            // Arrange & Act
            var dto = new GeneratedVocabularyDTO
            {
                Word = "Test",
                Definition = "Test Definition",
                Example = "Test Example",
                TypeOfWord = "Test Type",
                Category = "Test Category"
            };

            // Access all properties multiple times to ensure coverage
            var word1 = dto.Word;
            var definition1 = dto.Definition;
            var example1 = dto.Example;
            var type1 = dto.TypeOfWord;
            var category1 = dto.Category;

            var word2 = dto.Word;
            var definition2 = dto.Definition;
            var example2 = dto.Example;
            var type2 = dto.TypeOfWord;
            var category2 = dto.Category;

            // Assert
            Assert.Equal("Test", word1);
            Assert.Equal("Test Definition", definition1);
            Assert.Equal("Test Example", example1);
            Assert.Equal("Test Type", type1);
            Assert.Equal("Test Category", category1);

            Assert.Equal("Test", word2);
            Assert.Equal("Test Definition", definition2);
            Assert.Equal("Test Example", example2);
            Assert.Equal("Test Type", type2);
            Assert.Equal("Test Category", category2);
        }
    }
}

