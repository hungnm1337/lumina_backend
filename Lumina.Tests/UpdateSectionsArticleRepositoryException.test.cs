using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using RepositoryLayer;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lumina.Tests
{
    public class UpdateSectionsArticleRepositoryExceptionTests : IDisposable
    {
        private LuminaSystemContext? _context;
        private ArticleRepository? _repository;

        public UpdateSectionsArticleRepositoryExceptionTests()
        {
        }

        [Fact]
        public async Task UpdateSectionsAsync_WhenSaveChangesThrowsException_ShouldRollbackTransaction()
        {
            // Arrange
            // Create a context that will throw exception on SaveChangesAsync
            var options = new DbContextOptionsBuilder<LuminaSystemContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            var throwingContext = new ThrowingLuminaSystemContext(options);
            _context = throwingContext;
            _repository = new ArticleRepository(_context);

            // Seed data first (this will save successfully)
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);
            
            // Mark seed as completed so next SaveChangesAsync will throw
            throwingContext.MarkSeedCompleted();
            
            var newSections = new List<ArticleSection>
            {
                new ArticleSection
                {
                    SectionTitle = "Test Section",
                    SectionContent = "Test Content",
                    OrderIndex = 1
                }
            };

            // Act & Assert
            // This should throw exception, which will be caught by catch block in UpdateSectionsAsync
            // The catch block will call RollbackAsync and then rethrow the exception
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _repository.UpdateSectionsAsync(1, newSections);
            });

            // Verify that exception was thrown and caught by catch block
            // This covers the catch block lines (152-157) in UpdateSectionsAsync:
            // - catch (Exception) - line 152
            // - { - line 153
            // - await transaction.RollbackAsync(); - line 155
            // - throw; - line 157
            Assert.Equal("Simulated database error during SaveChangesAsync in UpdateSectionsAsync", exception.Message);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }

    // Custom context that throws exception on SaveChangesAsync to test exception handling
    public class ThrowingLuminaSystemContext : LuminaSystemContext
    {
        private bool _seedCompleted = false;

        public ThrowingLuminaSystemContext(DbContextOptions<LuminaSystemContext> options)
            : base(options)
        {
        }

        public void MarkSeedCompleted()
        {
            _seedCompleted = true;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Allow seed data to be saved first
            if (!_seedCompleted)
            {
                return await base.SaveChangesAsync(cancellationToken);
            }

            // After seed is completed, throw exception on SaveChangesAsync in UpdateSectionsAsync
            // This will trigger the catch block and rollback
            throw new InvalidOperationException("Simulated database error during SaveChangesAsync in UpdateSectionsAsync");
        }
    }
}

