using Microsoft.EntityFrameworkCore.Storage;
using RepositoryLayer.Exam;
using RepositoryLayer.Questions;
using RepositoryLayer.Speaking;
using RepositoryLayer.User;
using RepositoryLayer.UserSpacedRepetition;
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

    // TODO: Uncomment after migration - SpeakingResult and UserAnswer models have been modified
    // ISpeakingResultRepository SpeakingResults { get; }

    RepositoryLayer.Exam.ExamAttempt.IExamAttemptRepository ExamAttempts { get; }

    IUserAnswerRepository UserAnswers { get; }
    IUserAnswerSpeakingRepository UserAnswersSpeaking { get; }
    IUserSpacedRepetitionRepository UserSpacedRepetitions { get; }

    // Generic repositories for direct entity access
    IRepository<ExamAttempt> ExamAttemptsGeneric { get; }
    IRepository<Question> QuestionsGeneric { get; }
    IRepository<Option> Options { get; }

    Task<int> CompleteAsync();
    Task<IDbContextTransaction> BeginTransactionAsync();
}