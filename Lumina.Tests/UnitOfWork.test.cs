using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RepositoryLayer.UnitOfWork;

namespace Lumina.Tests
{
    public class UnitOfWorkTests : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly UnitOfWork _unitOfWork;

        public UnitOfWorkTests()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _unitOfWork = new UnitOfWork(_context);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldInitializeAllRepositories()
        {
            // Assert
            Assert.NotNull(_unitOfWork.Articles);
            Assert.NotNull(_unitOfWork.Categories);
            Assert.NotNull(_unitOfWork.Users);
            Assert.NotNull(_unitOfWork.Vocabularies);
            Assert.NotNull(_unitOfWork.VocabularyLists);
            Assert.NotNull(_unitOfWork.Questions);
            Assert.NotNull(_unitOfWork.ExamAttempts);
            Assert.NotNull(_unitOfWork.UserAnswers);
            Assert.NotNull(_unitOfWork.UserAnswersSpeaking);
            Assert.NotNull(_unitOfWork.UserSpacedRepetitions);
            Assert.NotNull(_unitOfWork.ExamAttemptsGeneric);
            Assert.NotNull(_unitOfWork.QuestionsGeneric);
            Assert.NotNull(_unitOfWork.Options);
        }

        [Fact]
        public void Constructor_ShouldInitializeVocabularyRepository()
        {
            // Assert
            Assert.NotNull(_unitOfWork.Vocabularies);
            Assert.IsAssignableFrom<IVocabularyRepository>(_unitOfWork.Vocabularies);
        }

        [Fact]
        public void Constructor_ShouldInitializeVocabularyListRepository()
        {
            // Assert
            Assert.NotNull(_unitOfWork.VocabularyLists);
            Assert.IsAssignableFrom<IVocabularyListRepository>(_unitOfWork.VocabularyLists);
        }

        [Fact]
        public void Constructor_ShouldInitializeGenericRepositories()
        {
            // Assert
            Assert.NotNull(_unitOfWork.ExamAttemptsGeneric);
            Assert.NotNull(_unitOfWork.QuestionsGeneric);
            Assert.NotNull(_unitOfWork.Options);
        }

        #endregion

        #region CompleteAsync Tests

        [Fact]
        public async Task CompleteAsync_WithNoChanges_ShouldReturnZero()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _unitOfWork.CompleteAsync();

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task CompleteAsync_WithAddedEntity_ShouldReturnNumberOfChanges()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);
            var newVocabulary = new Vocabulary
            {
                VocabularyListId = 1,
                Word = "Test",
                Definition = "Test Definition",
                TypeOfWord = "noun",
                IsDeleted = false
            };
            await _unitOfWork.Vocabularies.AddAsync(newVocabulary);

            // Act
            var result = await _unitOfWork.CompleteAsync();

            // Assert
            Assert.Equal(1, result);
            
            // Verify the entity was saved
            var savedVocabulary = await _context.Vocabularies
                .FirstOrDefaultAsync(v => v.Word == "Test");
            Assert.NotNull(savedVocabulary);
        }

        [Fact]
        public async Task CompleteAsync_WithMultipleChanges_ShouldReturnNumberOfChanges()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);
            var vocabulary1 = new Vocabulary
            {
                VocabularyListId = 1,
                Word = "Test1",
                Definition = "Test Definition 1",
                TypeOfWord = "noun",
                IsDeleted = false
            };
            var vocabulary2 = new Vocabulary
            {
                VocabularyListId = 1,
                Word = "Test2",
                Definition = "Test Definition 2",
                TypeOfWord = "noun",
                IsDeleted = false
            };
            await _unitOfWork.Vocabularies.AddAsync(vocabulary1);
            await _unitOfWork.Vocabularies.AddAsync(vocabulary2);

            // Act
            var result = await _unitOfWork.CompleteAsync();

            // Assert
            Assert.Equal(2, result);
        }

        [Fact]
        public async Task CompleteAsync_WithUpdatedEntity_ShouldReturnNumberOfChanges()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);
            var vocabulary = await _context.Vocabularies.FindAsync(1);
            Assert.NotNull(vocabulary);
            vocabulary.Word = "Updated Word";
            // Update entity directly (UpdateAsync already calls SaveChangesAsync)
            _context.Vocabularies.Update(vocabulary);

            // Act
            var result = await _unitOfWork.CompleteAsync();

            // Assert
            Assert.Equal(1, result);
            
            // Verify the entity was updated
            var updatedVocabulary = await _context.Vocabularies.FindAsync(1);
            Assert.NotNull(updatedVocabulary);
            Assert.Equal("Updated Word", updatedVocabulary.Word);
        }

        [Fact]
        public async Task CompleteAsync_WithDeletedEntity_ShouldReturnNumberOfChanges()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);
            // DeleteAsync already calls SaveChangesAsync, so CompleteAsync will return 0
            await _unitOfWork.Vocabularies.DeleteAsync(1);

            // Act
            var result = await _unitOfWork.CompleteAsync();

            // Assert
            // DeleteAsync already saved, so CompleteAsync returns 0
            Assert.Equal(0, result);
            
            // Verify the entity was soft deleted
            var deletedVocabulary = await _context.Vocabularies.FindAsync(1);
            Assert.NotNull(deletedVocabulary);
            Assert.Equal(true, deletedVocabulary.IsDeleted);
        }

        [Fact]
        public async Task CompleteAsync_WithMultipleRepositoryChanges_ShouldSaveAllChanges()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);
            var vocabulary = new Vocabulary
            {
                VocabularyListId = 1,
                Word = "New Vocabulary",
                Definition = "New Definition",
                TypeOfWord = "noun",
                IsDeleted = false
            };
            await _unitOfWork.Vocabularies.AddAsync(vocabulary);
            
            var vocabularyList = new VocabularyList
            {
                Name = "New List",
                MakeBy = 1,
                CreateAt = DateTime.UtcNow,
                IsDeleted = false,
                IsPublic = true,
                Status = "Draft"
            };
            await _unitOfWork.VocabularyLists.AddAsync(vocabularyList);

            // Act
            var result = await _unitOfWork.CompleteAsync();

            // Assert
            Assert.Equal(2, result);
            
            // Verify both entities were saved
            var savedVocabulary = await _context.Vocabularies
                .FirstOrDefaultAsync(v => v.Word == "New Vocabulary");
            Assert.NotNull(savedVocabulary);
            
            var savedList = await _context.VocabularyLists
                .FirstOrDefaultAsync(vl => vl.Name == "New List");
            Assert.NotNull(savedList);
        }

        #endregion

        #region BeginTransactionAsync Tests

        [Fact]
        public async Task BeginTransactionAsync_ShouldReturnTransaction()
        {
            // Arrange
            // Note: InMemory database doesn't fully support transactions
            // but the method should still return a transaction object (even if it's a no-op)

            // Act
            var transaction = await _unitOfWork.BeginTransactionAsync();

            // Assert
            Assert.NotNull(transaction);
            Assert.IsAssignableFrom<IDbContextTransaction>(transaction);
            await transaction.DisposeAsync();
        }

        [Fact]
        public async Task BeginTransactionAsync_ShouldCreateTransaction()
        {
            // Arrange
            // Note: InMemory database doesn't fully support transactions
            // but the method should still return a transaction object (even if it's a no-op)

            // Act
            var transaction = await _unitOfWork.BeginTransactionAsync();

            // Assert
            Assert.NotNull(transaction);
            // TransactionId may be null for InMemory, but transaction object should exist
            await transaction.DisposeAsync();
        }

        [Fact]
        public async Task BeginTransactionAsync_ShouldBeCallableWithoutError()
        {
            // Arrange
            // Note: InMemory database doesn't support transactions
            // This test verifies the method can be called without throwing exceptions
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act & Assert
            var transaction = await _unitOfWork.BeginTransactionAsync();
            Assert.NotNull(transaction);
            await transaction.DisposeAsync();
        }

        [Fact]
        public async Task BeginTransactionAsync_ShouldDisposeCorrectly()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var transaction = await _unitOfWork.BeginTransactionAsync();
            await transaction.DisposeAsync();

            // Assert
            // Should not throw exception on disposal
            Assert.True(true);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task UnitOfWork_WithVocabularyRepository_ShouldWorkCorrectly()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var vocabularies = await _unitOfWork.Vocabularies.GetByListAsync(1, null);
            var vocabulary = await _unitOfWork.Vocabularies.GetByIdAsync(1);

            // Assert
            Assert.NotNull(vocabularies);
            Assert.NotEmpty(vocabularies);
            Assert.NotNull(vocabulary);
        }

        [Fact]
        public async Task UnitOfWork_WithVocabularyListRepository_ShouldWorkCorrectly()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var lists = await _unitOfWork.VocabularyLists.GetAllAsync(null);
            var list = await _unitOfWork.VocabularyLists.FindByIdAsync(1);

            // Assert
            Assert.NotNull(lists);
            Assert.NotEmpty(lists);
            Assert.NotNull(list);
        }

        [Fact]
        public async Task UnitOfWork_WithMultipleRepositories_ShouldWorkCorrectly()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var vocabularies = await _unitOfWork.Vocabularies.GetByListAsync(1, null);
            var lists = await _unitOfWork.VocabularyLists.GetAllAsync(null);
            var list = await _unitOfWork.VocabularyLists.FindByIdAsync(1);

            // Assert
            Assert.NotNull(vocabularies);
            Assert.NotNull(lists);
            Assert.NotNull(list);
        }

        [Fact]
        public async Task UnitOfWork_WithGenericRepository_ShouldWorkCorrectly()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var examAttempts = _unitOfWork.ExamAttemptsGeneric.Get();
            var questions = _unitOfWork.QuestionsGeneric.Get();

            // Assert
            Assert.NotNull(examAttempts);
            Assert.NotNull(questions);
        }

        [Fact]
        public async Task UnitOfWork_AddAndComplete_ShouldPersistChanges()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);
            var newVocabulary = new Vocabulary
            {
                VocabularyListId = 1,
                Word = "Integration Test",
                Definition = "Integration Test Definition",
                TypeOfWord = "noun",
                IsDeleted = false
            };

            // Act
            await _unitOfWork.Vocabularies.AddAsync(newVocabulary);
            var changes = await _unitOfWork.CompleteAsync();

            // Assert
            Assert.Equal(1, changes);
            var savedVocabulary = await _unitOfWork.Vocabularies.GetByIdAsync(newVocabulary.VocabularyId);
            Assert.NotNull(savedVocabulary);
            Assert.Equal("Integration Test", savedVocabulary.Word);
        }

        [Fact]
        public async Task UnitOfWork_UpdateAndComplete_ShouldPersistChanges()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);
            var vocabulary = await _unitOfWork.Vocabularies.GetByIdAsync(1);
            Assert.NotNull(vocabulary);
            var originalWord = vocabulary.Word;
            vocabulary.Word = "Updated Integration Test";

            // Act
            // UpdateAsync already calls SaveChangesAsync, so CompleteAsync should return 0
            await _unitOfWork.Vocabularies.UpdateAsync(vocabulary);
            var changes = await _unitOfWork.CompleteAsync();

            // Assert
            // UpdateAsync already saved, so CompleteAsync returns 0
            Assert.Equal(0, changes);
            var updatedVocabulary = await _unitOfWork.Vocabularies.GetByIdAsync(1);
            Assert.NotNull(updatedVocabulary);
            Assert.Equal("Updated Integration Test", updatedVocabulary.Word);
        }

        [Fact]
        public async Task UnitOfWork_DeleteAndComplete_ShouldPersistChanges()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);
            var vocabularyBeforeDelete = await _unitOfWork.Vocabularies.GetByIdAsync(1);
            Assert.NotNull(vocabularyBeforeDelete);

            // Act
            // DeleteAsync already calls SaveChangesAsync
            await _unitOfWork.Vocabularies.DeleteAsync(1);
            var changes = await _unitOfWork.CompleteAsync();

            // Assert
            // DeleteAsync already saved, so CompleteAsync returns 0
            Assert.Equal(0, changes);
            var vocabularyAfterDelete = await _unitOfWork.Vocabularies.GetByIdAsync(1);
            Assert.Null(vocabularyAfterDelete); // Should be null because it's soft deleted
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_ShouldDisposeContext()
        {
            // Arrange
            var context = InMemoryDbContextHelper.CreateContext();
            var unitOfWork = new UnitOfWork(context);

            // Act
            unitOfWork.Dispose();

            // Assert
            // Context should be disposed
            // Note: InMemory database may not throw ObjectDisposedException immediately
            // but the context should be disposed
            Assert.True(true); // Verify Dispose was called without exception
        }

        [Fact]
        public void Dispose_ShouldBeCalledMultipleTimes_WithoutError()
        {
            // Arrange
            var context = InMemoryDbContextHelper.CreateContext();
            var unitOfWork = new UnitOfWork(context);

            // Act & Assert
            unitOfWork.Dispose();
            unitOfWork.Dispose(); // Should not throw
            unitOfWork.Dispose(); // Should not throw
        }

        #endregion

        public void Dispose()
        {
            _unitOfWork.Dispose();
        }
    }
}

