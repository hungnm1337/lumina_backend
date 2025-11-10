using DataLayer.DTOs.Vocabulary;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Lumina.Tests
{
    public class ReviewVocabularyRequestDTOTests
    {
        [Fact]
        public void ReviewVocabularyRequestDTO_WithDefaultValues_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var dto = new ReviewVocabularyRequestDTO();

            // Assert
            Assert.Equal(0, dto.UserSpacedRepetitionId);
            Assert.Equal(0, dto.Quality);
        }

        [Fact]
        public void ReviewVocabularyRequestDTO_WithUserSpacedRepetitionId_ShouldSetAndGet()
        {
            // Arrange
            var dto = new ReviewVocabularyRequestDTO
            {
                UserSpacedRepetitionId = 123
            };

            // Act & Assert
            Assert.Equal(123, dto.UserSpacedRepetitionId);
        }

        [Fact]
        public void ReviewVocabularyRequestDTO_WithQuality_ShouldSetAndGet()
        {
            // Arrange
            var dto = new ReviewVocabularyRequestDTO
            {
                Quality = 5
            };

            // Act & Assert
            Assert.Equal(5, dto.Quality);
        }

        [Fact]
        public void ReviewVocabularyRequestDTO_WithAllProperties_ShouldSetAndGetAll()
        {
            // Arrange
            var dto = new ReviewVocabularyRequestDTO
            {
                UserSpacedRepetitionId = 456,
                Quality = 3
            };

            // Act & Assert
            Assert.Equal(456, dto.UserSpacedRepetitionId);
            Assert.Equal(3, dto.Quality);
        }

        [Fact]
        public void ReviewVocabularyRequestDTO_QualityRange_ShouldAcceptValidRange()
        {
            // Arrange & Act - Test quality range 0-5
            var dto0 = new ReviewVocabularyRequestDTO { Quality = 0 };
            var dto1 = new ReviewVocabularyRequestDTO { Quality = 1 };
            var dto3 = new ReviewVocabularyRequestDTO { Quality = 3 };
            var dto5 = new ReviewVocabularyRequestDTO { Quality = 5 };

            // Assert
            Assert.Equal(0, dto0.Quality);
            Assert.Equal(1, dto1.Quality);
            Assert.Equal(3, dto3.Quality);
            Assert.Equal(5, dto5.Quality);
        }

        [Fact]
        public void ReviewVocabularyRequestDTO_CanChangeProperties_ShouldUpdateCorrectly()
        {
            // Arrange
            var dto = new ReviewVocabularyRequestDTO
            {
                UserSpacedRepetitionId = 100,
                Quality = 2
            };

            // Act
            dto.UserSpacedRepetitionId = 200;
            dto.Quality = 4;

            // Assert
            Assert.Equal(200, dto.UserSpacedRepetitionId);
            Assert.Equal(4, dto.Quality);
        }

        [Fact]
        public void ReviewVocabularyRequestDTO_PropertyAccessMultipleTimes_ShouldCoverProperty()
        {
            // Arrange
            var dto = new ReviewVocabularyRequestDTO();

            // Act - Access properties multiple times
            var id1 = dto.UserSpacedRepetitionId;
            var quality1 = dto.Quality;

            dto.UserSpacedRepetitionId = 10;
            dto.Quality = 1;

            var id2 = dto.UserSpacedRepetitionId;
            var quality2 = dto.Quality;

            dto.UserSpacedRepetitionId = 20;
            dto.Quality = 5;

            var id3 = dto.UserSpacedRepetitionId;
            var quality3 = dto.Quality;

            // Assert
            Assert.Equal(0, id1);
            Assert.Equal(0, quality1);
            Assert.Equal(10, id2);
            Assert.Equal(1, quality2);
            Assert.Equal(20, id3);
            Assert.Equal(5, quality3);
        }

        [Fact]
        public void ReviewVocabularyRequestDTO_UsedInCollection_ShouldWorkCorrectly()
        {
            // Arrange
            var requests = new List<ReviewVocabularyRequestDTO>
            {
                new ReviewVocabularyRequestDTO { UserSpacedRepetitionId = 1, Quality = 0 },
                new ReviewVocabularyRequestDTO { UserSpacedRepetitionId = 2, Quality = 2 },
                new ReviewVocabularyRequestDTO { UserSpacedRepetitionId = 3, Quality = 5 }
            };

            // Act - Use properties in LINQ queries to ensure coverage
            var highQuality = requests.Where(r => r.Quality >= 4).ToList();
            var lowQuality = requests.Where(r => r.Quality <= 2).ToList();
            var allIds = requests.Select(r => r.UserSpacedRepetitionId).ToList();
            var allQualities = requests.Select(r => r.Quality).ToList();

            // Assert
            Assert.Single(highQuality);
            Assert.Equal(2, lowQuality.Count); // Quality 0 and 2 are <= 2
            Assert.Equal(3, allIds.Count);
            Assert.Equal(3, allQualities.Count);
            Assert.Contains(5, allQualities);
            Assert.Contains(0, allQualities);
            Assert.Contains(2, allQualities);
        }
    }
}

