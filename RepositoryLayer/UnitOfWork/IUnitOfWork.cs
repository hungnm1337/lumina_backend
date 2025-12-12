using Microsoft.EntityFrameworkCore.Storage;
using RepositoryLayer.Auth;
using RepositoryLayer.Exam;
using RepositoryLayer.Questions;
using RepositoryLayer.Speaking;
using RepositoryLayer.User;
using RepositoryLayer.UserSpacedRepetition;
using RepositoryLayer.UserArticleProgress;
using RepositoryLayer.Generic;
using DataLayer.Models;


namespace RepositoryLayer.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    IArticleRepository Articles { get; }
    ICategoryRepository Categories { get; }
    IUserRepository Users { get; }
    IVocabularyRepository Vocabularies { get; }
    IVocabularyListRepository VocabularyLists { get; }
    IQuestionRepository Questions { get; }
    RepositoryLayer.Exam.ExamAttempt.IExamAttemptRepository ExamAttempts { get; }

    IUserAnswerRepository UserAnswers { get; }
    IUserAnswerSpeakingRepository UserAnswersSpeaking { get; }
    IUserSpacedRepetitionRepository UserSpacedRepetitions { get; }
    IUserArticleProgressRepository UserArticleProgresses { get; }

    IRepository<ExamAttempt> ExamAttemptsGeneric { get; }
    IRepository<Question> QuestionsGeneric { get; }
    IRepository<Option> Options { get; }
    IRepository<Role> Roles { get; }

    // Auth repositories
    IAccountRepository Accounts { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    IPasswordResetTokenRepository PasswordResetTokens { get; }

    Task<int> CompleteAsync();
    Task<IDbContextTransaction> BeginTransactionAsync();
}