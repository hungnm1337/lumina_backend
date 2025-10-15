using Microsoft.EntityFrameworkCore.Storage;
using RepositoryLayer.Exam;
using RepositoryLayer.Questions;
using RepositoryLayer.Speaking;
using RepositoryLayer.User;


namespace RepositoryLayer.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    IArticleRepository Articles { get; }
    ICategoryRepository Categories { get; }
    IUserRepository Users { get; }
    IVocabularyRepository Vocabularies { get; }
    IVocabularyListRepository VocabularyLists { get; }
    IQuestionRepository Questions { get; }

    ISpeakingResultRepository SpeakingResults { get; }

    IExamAttemptRepository ExamAttempts { get; }

    IUserAnswerRepository UserAnswers { get; }
    Task<int> CompleteAsync();
    Task<IDbContextTransaction> BeginTransactionAsync();
}