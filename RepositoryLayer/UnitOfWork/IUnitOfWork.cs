using Microsoft.EntityFrameworkCore.Storage;


namespace RepositoryLayer.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    IArticleRepository Articles { get; }
    ICategoryRepository Categories { get; }
    IUserRepository Users { get; }
    IVocabularyRepository Vocabularies { get; }

    Task<int> CompleteAsync();
    Task<IDbContextTransaction> BeginTransactionAsync();
}