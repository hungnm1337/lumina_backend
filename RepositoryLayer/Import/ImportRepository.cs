using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepositoryLayer.Import
{
    public class ImportRepository : IImportRepository
    {
        private readonly LuminaSystemContext _context;
        public ImportRepository(LuminaSystemContext context)
        {
            _context = context;
        }

    

        public async Task<Prompt> AddPromptAsync(Prompt prompt)
        {
            _context.Prompts.Add(prompt);
            await _context.SaveChangesAsync();
            return prompt;
        }

        public async Task<Question> AddQuestionAsync(Question question)
        {
            _context.Questions.Add(question);
            await _context.SaveChangesAsync();
            return question;
        }

        public async Task AddOptionsAsync(IEnumerable<Option> options)
        {
            _context.Options.AddRange(options);
            await _context.SaveChangesAsync();
        }

      

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<ExamPart> GetExamPartByIdAsync(int partId)
        {
            return await _context.ExamParts
                .FirstOrDefaultAsync(p => p.PartId == partId);
        }

        public async Task<List<Question>> GetQuestionsByPartIdAsync(int partId)
        {
            return await _context.Questions
                .Where(q => q.PartId == partId)
                .ToListAsync();
        }

    }

}
