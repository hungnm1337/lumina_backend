using DataLayer.Models;
using Microsoft.EntityFrameworkCore.Storage;
using RepositoryLayer.Exam;
using RepositoryLayer.Questions;
using RepositoryLayer.Speaking;
using RepositoryLayer.User;


namespace RepositoryLayer.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly LuminaSystemContext _context;

    public IArticleRepository Articles { get; private set; }
    public ICategoryRepository Categories { get; private set; }
    public IUserRepository Users { get; private set; }
    public IVocabularyRepository Vocabularies { get; private set; }
    public IVocabularyListRepository VocabularyLists { get; private set; }
    public IQuestionRepository Questions { get; private set; }

    public ISpeakingResultRepository SpeakingResults { get; private set; }

    public IExamAttemptRepository ExamAttempts { get; private set; }

    public IUserAnswerRepository UserAnswers { get; private set; }
    public UnitOfWork(LuminaSystemContext context)
    {
        _context = context;
        Articles = new ArticleRepository(_context);
        Categories = new CategoryRepository(_context);
        Users = new UserRepository(_context);
        Vocabularies = new VocabularyRepository(_context);
        VocabularyLists = new VocabularyListRepository(_context);
        Questions = new QuestionRepository(_context);

        SpeakingResults = new SpeakingResultRepository(_context);

        ExamAttempts = new ExamAttemptRepository(_context);

        UserAnswers = new UserAnswerRepository(_context);
    }

    public async Task<int> CompleteAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await _context.Database.BeginTransactionAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}