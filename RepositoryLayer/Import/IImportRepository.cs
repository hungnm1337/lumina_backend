using DataLayer.Models;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepositoryLayer.Import
{
    public interface IImportRepository
    {
        Task<Prompt> AddPromptAsync(Prompt prompt);
        Task<Question> AddQuestionAsync(Question question);
        Task AddOptionsAsync(IEnumerable<Option> options);

        Task<IDbContextTransaction> BeginTransactionAsync();
        Task SaveChangesAsync();

        Task<List<Question>> GetQuestionsByPartIdAsync(int partId);

        Task<ExamPart> GetExamPartByIdAsync(int partId);
    }

}
