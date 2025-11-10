using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using RepositoryLayer;

namespace Lumina.Tests
{
    public class UpdateSectionsArticleRepositoryTests : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly ArticleRepository _repository;

        public UpdateSectionsArticleRepositoryTests()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _repository = new ArticleRepository(_context);
        }

        [Fact]
        public async Task UpdateSectionsAsync_WithExistingSections_ShouldReplaceOldSections()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);
            var oldSections = await _context.ArticleSections
                .Where(s => s.ArticleId == 1)
                .ToListAsync();
            Assert.NotEmpty(oldSections);

            var newSections = new List<ArticleSection>
            {
                new ArticleSection
                {
                    SectionTitle = "New Section 1",
                    SectionContent = "New Content 1",
                    OrderIndex = 1
                },
                new ArticleSection
                {
                    SectionTitle = "New Section 2",
                    SectionContent = "New Content 2",
                    OrderIndex = 2
                }
            };

            // Act
            await _repository.UpdateSectionsAsync(1, newSections);

            // Assert
            var updatedSections = await _context.ArticleSections
                .Where(s => s.ArticleId == 1)
                .ToListAsync();
            Assert.Equal(2, updatedSections.Count);
            Assert.Contains(updatedSections, s => s.SectionTitle == "New Section 1");
            Assert.Contains(updatedSections, s => s.SectionTitle == "New Section 2");
            // Verify old sections are removed
            Assert.DoesNotContain(updatedSections, s => oldSections.Any(os => os.SectionId == s.SectionId));
        }

        [Fact]
        public async Task UpdateSectionsAsync_WithNoExistingSections_ShouldAddNewSections()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);
            // Article 3 may have no sections
            var sectionsBefore = await _context.ArticleSections
                .Where(s => s.ArticleId == 3)
                .ToListAsync();

            var newSections = new List<ArticleSection>
            {
                new ArticleSection
                {
                    SectionTitle = "First Section",
                    SectionContent = "First Content",
                    OrderIndex = 1
                }
            };

            // Act
            await _repository.UpdateSectionsAsync(3, newSections);

            // Assert
            var sectionsAfter = await _context.ArticleSections
                .Where(s => s.ArticleId == 3)
                .ToListAsync();
            Assert.Single(sectionsAfter);
            Assert.Equal("First Section", sectionsAfter[0].SectionTitle);
        }

        [Fact]
        public async Task UpdateSectionsAsync_ShouldAssignCorrectArticleId()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);
            var newSections = new List<ArticleSection>
            {
                new ArticleSection
                {
                    SectionTitle = "Test Section",
                    SectionContent = "Test Content",
                    OrderIndex = 1
                }
            };

            // Act
            await _repository.UpdateSectionsAsync(1, newSections);

            // Assert
            var sections = await _context.ArticleSections
                .Where(s => s.ArticleId == 1 && s.SectionTitle == "Test Section")
                .ToListAsync();
            Assert.Single(sections);
            Assert.Equal(1, sections[0].ArticleId);
        }

        [Fact]
        public async Task UpdateSectionsAsync_WithEmptySections_ShouldRemoveAllSections()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);
            var sectionsBefore = await _context.ArticleSections
                .Where(s => s.ArticleId == 1)
                .ToListAsync();
            Assert.NotEmpty(sectionsBefore);

            var emptySections = new List<ArticleSection>();

            // Act
            await _repository.UpdateSectionsAsync(1, emptySections);

            // Assert
            var sectionsAfter = await _context.ArticleSections
                .Where(s => s.ArticleId == 1)
                .ToListAsync();
            Assert.Empty(sectionsAfter);
        }

        [Fact]
        public async Task UpdateSectionsAsync_ShouldPreserveSectionProperties()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);
            var newSections = new List<ArticleSection>
            {
                new ArticleSection
                {
                    SectionTitle = "Preserved Section",
                    SectionContent = "Preserved Content",
                    OrderIndex = 5
                }
            };

            // Act
            await _repository.UpdateSectionsAsync(1, newSections);

            // Assert
            var sections = await _context.ArticleSections
                .Where(s => s.ArticleId == 1)
                .ToListAsync();
            Assert.Single(sections);
            Assert.Equal("Preserved Section", sections[0].SectionTitle);
            Assert.Equal("Preserved Content", sections[0].SectionContent);
            Assert.Equal(5, sections[0].OrderIndex);
        }

        [Fact]
        public async Task UpdateSectionsAsync_ShouldNotAffectOtherArticleSections()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);
            var article2SectionsBefore = await _context.ArticleSections
                .Where(s => s.ArticleId == 2)
                .ToListAsync();

            var newSections = new List<ArticleSection>
            {
                new ArticleSection
                {
                    SectionTitle = "Article 1 Section",
                    SectionContent = "Article 1 Content",
                    OrderIndex = 1
                }
            };

            // Act
            await _repository.UpdateSectionsAsync(1, newSections);

            // Assert
            var article2SectionsAfter = await _context.ArticleSections
                .Where(s => s.ArticleId == 2)
                .ToListAsync();
            Assert.Equal(article2SectionsBefore.Count, article2SectionsAfter.Count);
        }

        [Fact]
        public async Task UpdateSectionsAsync_WithMultipleSections_ShouldAddAllSections()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);
            var newSections = new List<ArticleSection>
            {
                new ArticleSection
                {
                    SectionTitle = "Section 1",
                    SectionContent = "Content 1",
                    OrderIndex = 1
                },
                new ArticleSection
                {
                    SectionTitle = "Section 2",
                    SectionContent = "Content 2",
                    OrderIndex = 2
                },
                new ArticleSection
                {
                    SectionTitle = "Section 3",
                    SectionContent = "Content 3",
                    OrderIndex = 3
                }
            };

            // Act
            await _repository.UpdateSectionsAsync(1, newSections);

            // Assert
            var sections = await _context.ArticleSections
                .Where(s => s.ArticleId == 1)
                .ToListAsync();
            Assert.Equal(3, sections.Count);
            Assert.Contains(sections, s => s.SectionTitle == "Section 1");
            Assert.Contains(sections, s => s.SectionTitle == "Section 2");
            Assert.Contains(sections, s => s.SectionTitle == "Section 3");
        }

        [Fact]
        public async Task UpdateSectionsAsync_ShouldCommitTransactionOnSuccess()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);
            var newSections = new List<ArticleSection>
            {
                new ArticleSection
                {
                    SectionTitle = "Committed Section",
                    SectionContent = "Committed Content",
                    OrderIndex = 1
                }
            };

            // Act
            await _repository.UpdateSectionsAsync(1, newSections);

            // Assert
            // Verify sections are persisted by querying again
            var sections = await _context.ArticleSections
                .Where(s => s.ArticleId == 1 && s.SectionTitle == "Committed Section")
                .ToListAsync();
            Assert.Single(sections);
        }

        [Fact]
        public async Task UpdateSectionsAsync_WithNonExistentArticleId_ShouldStillExecute()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);
            var newSections = new List<ArticleSection>
            {
                new ArticleSection
                {
                    SectionTitle = "New Section",
                    SectionContent = "New Content",
                    OrderIndex = 1
                }
            };

            // Act
            await _repository.UpdateSectionsAsync(999, newSections);

            // Assert
            // Should not throw exception, but sections won't be associated with any article
            var sections = await _context.ArticleSections
                .Where(s => s.ArticleId == 999)
                .ToListAsync();
            // Sections should be added even if article doesn't exist (depending on foreign key constraints)
            // In real scenario, this might fail due to FK constraint, but we test the method behavior
        }

        [Fact]
        public async Task UpdateSectionsAsync_ShouldHandleSectionsWithSameOrderIndex()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);
            var newSections = new List<ArticleSection>
            {
                new ArticleSection
                {
                    SectionTitle = "Section A",
                    SectionContent = "Content A",
                    OrderIndex = 1
                },
                new ArticleSection
                {
                    SectionTitle = "Section B",
                    SectionContent = "Content B",
                    OrderIndex = 1 // Same order index
                }
            };

            // Act
            await _repository.UpdateSectionsAsync(1, newSections);

            // Assert
            var sections = await _context.ArticleSections
                .Where(s => s.ArticleId == 1)
                .ToListAsync();
            Assert.Equal(2, sections.Count);
            Assert.Contains(sections, s => s.SectionTitle == "Section A");
            Assert.Contains(sections, s => s.SectionTitle == "Section B");
        }

        [Fact]
        public async Task UpdateSectionsAsync_WithExistingSectionsAndNewSections_ShouldReplaceCompletely()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);
            // Article 1 has 2 sections initially
            var initialSectionCount = await _context.ArticleSections
                .Where(s => s.ArticleId == 1)
                .CountAsync();
            Assert.True(initialSectionCount >= 2);

            var newSections = new List<ArticleSection>
            {
                new ArticleSection
                {
                    SectionTitle = "Only New Section",
                    SectionContent = "Only New Content",
                    OrderIndex = 1
                }
            };

            // Act
            await _repository.UpdateSectionsAsync(1, newSections);

            // Assert
            var finalSections = await _context.ArticleSections
                .Where(s => s.ArticleId == 1)
                .ToListAsync();
            Assert.Single(finalSections);
            Assert.Equal("Only New Section", finalSections[0].SectionTitle);
        }

        [Fact]
        public async Task UpdateSectionsAsync_WhenNoExistingSections_ShouldStillAddNewSections()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);
            // Ensure article 3 has no sections
            var existingSections = await _context.ArticleSections
                .Where(s => s.ArticleId == 3)
                .ToListAsync();
            if (existingSections.Any())
            {
                _context.ArticleSections.RemoveRange(existingSections);
                await _context.SaveChangesAsync();
            }

            var newSections = new List<ArticleSection>
            {
                new ArticleSection
                {
                    SectionTitle = "New Section for Article 3",
                    SectionContent = "New Content",
                    OrderIndex = 1
                }
            };

            // Act
            await _repository.UpdateSectionsAsync(3, newSections);

            // Assert
            var sectionsAfter = await _context.ArticleSections
                .Where(s => s.ArticleId == 3)
                .ToListAsync();
            Assert.Single(sectionsAfter);
            Assert.Equal("New Section for Article 3", sectionsAfter[0].SectionTitle);
        }

        [Fact]
        public async Task UpdateSectionsAsync_ShouldHandleTransactionCommit()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);
            var newSections = new List<ArticleSection>
            {
                new ArticleSection
                {
                    SectionTitle = "Transaction Test Section",
                    SectionContent = "Transaction Test Content",
                    OrderIndex = 1
                }
            };

            // Act
            await _repository.UpdateSectionsAsync(1, newSections);

            // Assert
            // Verify transaction was committed by checking persistence
            var sections = await _context.ArticleSections
                .Where(s => s.ArticleId == 1 && s.SectionTitle == "Transaction Test Section")
                .ToListAsync();
            Assert.Single(sections);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

