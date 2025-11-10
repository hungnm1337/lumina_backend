using DataLayer.DTOs.Article;
using System;
using System.Collections.Generic;
using Xunit;

namespace Lumina.Tests
{
    public class ArticleSectionUpdateDTOTests
    {
        [Fact]
        public void ArticleSectionUpdateDTO_WithSectionId_ShouldSetAndGetSectionId()
        {
            // Arrange
            var dto = new ArticleSectionUpdateDTO
            {
                SectionId = 1,
                SectionTitle = "Test Section",
                SectionContent = "Test Content",
                OrderIndex = 1
            };

            // Act & Assert
            Assert.Equal(1, dto.SectionId);
            Assert.Equal("Test Section", dto.SectionTitle);
            Assert.Equal("Test Content", dto.SectionContent);
            Assert.Equal(1, dto.OrderIndex);
        }

        [Fact]
        public void ArticleSectionUpdateDTO_WithNullSectionId_ShouldAllowNullSectionId()
        {
            // Arrange
            var dto = new ArticleSectionUpdateDTO
            {
                SectionId = null,
                SectionTitle = "Test Section",
                SectionContent = "Test Content",
                OrderIndex = 1
            };

            // Act & Assert
            Assert.Null(dto.SectionId);
            Assert.Equal("Test Section", dto.SectionTitle);
            Assert.Equal("Test Content", dto.SectionContent);
            Assert.Equal(1, dto.OrderIndex);
        }

        [Fact]
        public void ArticleSectionUpdateDTO_WithAllProperties_ShouldSetAndGetAllProperties()
        {
            // Arrange
            var dto = new ArticleSectionUpdateDTO
            {
                SectionId = 5,
                SectionTitle = "Updated Section Title",
                SectionContent = "Updated Section Content",
                OrderIndex = 10
            };

            // Act & Assert
            Assert.Equal(5, dto.SectionId);
            Assert.Equal("Updated Section Title", dto.SectionTitle);
            Assert.Equal("Updated Section Content", dto.SectionContent);
            Assert.Equal(10, dto.OrderIndex);
        }

        [Fact]
        public void ArticleSectionUpdateDTO_WithDefaultValues_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var dto = new ArticleSectionUpdateDTO();

            // Assert
            Assert.Null(dto.SectionId);
            Assert.Equal(string.Empty, dto.SectionTitle);
            Assert.Equal(string.Empty, dto.SectionContent);
            Assert.Equal(0, dto.OrderIndex);
        }

        [Fact]
        public void ArticleSectionUpdateDTO_UsedInUpdateArticle_ShouldBeUsedCorrectly()
        {
            // Arrange
            var updateDto = new ArticleUpdateDTO
            {
                Title = "Updated Title",
                Summary = "Updated Summary",
                CategoryId = 1,
                Sections = new List<ArticleSectionUpdateDTO>
                {
                    new ArticleSectionUpdateDTO
                    {
                        SectionId = 1, // With SectionId
                        SectionTitle = "Section 1",
                        SectionContent = "Content 1",
                        OrderIndex = 1
                    },
                    new ArticleSectionUpdateDTO
                    {
                        SectionId = null, // Without SectionId
                        SectionTitle = "Section 2",
                        SectionContent = "Content 2",
                        OrderIndex = 2
                    }
                }
            };

            // Act & Assert
            Assert.Equal(2, updateDto.Sections.Count);
            Assert.Equal(1, updateDto.Sections[0].SectionId);
            Assert.Null(updateDto.Sections[1].SectionId);
            Assert.Equal("Section 1", updateDto.Sections[0].SectionTitle);
            Assert.Equal("Section 2", updateDto.Sections[1].SectionTitle);
        }

        [Fact]
        public void ArticleSectionUpdateDTO_WithSectionIdInCondition_ShouldEvaluateCorrectly()
        {
            // Arrange
            var sections = new List<ArticleSectionUpdateDTO>
            {
                new ArticleSectionUpdateDTO
                {
                    SectionId = 1,
                    SectionTitle = "Section 1",
                    SectionContent = "Content 1",
                    OrderIndex = 1
                },
                new ArticleSectionUpdateDTO
                {
                    SectionId = null,
                    SectionTitle = "Section 2",
                    SectionContent = "Content 2",
                    OrderIndex = 2
                },
                new ArticleSectionUpdateDTO
                {
                    SectionId = 5,
                    SectionTitle = "Section 3",
                    SectionContent = "Content 3",
                    OrderIndex = 3
                }
            };

            // Act - Use SectionId in logic to ensure coverage
            var sectionsWithId = sections.Where(s => s.SectionId.HasValue).ToList();
            var sectionsWithoutId = sections.Where(s => !s.SectionId.HasValue).ToList();
            var sectionIds = sections.Select(s => s.SectionId).ToList();

            // Assert
            Assert.Equal(2, sectionsWithId.Count);
            Assert.Single(sectionsWithoutId);
            Assert.Equal(3, sectionIds.Count);
            Assert.Contains(1, sectionIds);
            Assert.Contains(null, sectionIds);
            Assert.Contains(5, sectionIds);
        }

        [Fact]
        public void ArticleSectionUpdateDTO_AllPropertiesAccess_ShouldCoverAllProperties()
        {
            // Arrange & Act
            var dto = new ArticleSectionUpdateDTO
            {
                SectionId = 10,
                SectionTitle = "Test Title",
                SectionContent = "Test Content",
                OrderIndex = 5
            };

            // Access all properties multiple times to ensure coverage
            var id1 = dto.SectionId;
            var title1 = dto.SectionTitle;
            var content1 = dto.SectionContent;
            var index1 = dto.OrderIndex;

            // Modify and access again
            dto.SectionId = 20;
            dto.SectionTitle = "Updated Title";
            dto.SectionContent = "Updated Content";
            dto.OrderIndex = 10;

            var id2 = dto.SectionId;
            var title2 = dto.SectionTitle;
            var content2 = dto.SectionContent;
            var index2 = dto.OrderIndex;

            // Assert
            Assert.Equal(10, id1);
            Assert.Equal("Test Title", title1);
            Assert.Equal("Test Content", content1);
            Assert.Equal(5, index1);

            Assert.Equal(20, id2);
            Assert.Equal("Updated Title", title2);
            Assert.Equal("Updated Content", content2);
            Assert.Equal(10, index2);
        }
    }
}

