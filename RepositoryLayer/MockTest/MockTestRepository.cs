using DataLayer;
using DataLayer.Models;
using DataLayer.DTOs.MockTest;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataLayer.DTOs.Exam;

namespace RepositoryLayer.MockTest
{
    public class MockTestRepository : IMockTestRepository
    {
        private readonly LuminaSystemContext _context;

        public MockTestRepository(LuminaSystemContext context)
        {
            _context = context;
        }

        
        public async Task<List<ExamPartDTO>> GetMocktestAsync(int[] examPartIds)
        {
            var examParts = await _context.ExamParts
                .Where(ep => examPartIds.Contains(ep.PartId))
                .Include(ep => ep.Exam) 
                .Include(ep => ep.Questions) 
                    .ThenInclude(q => q.Options) 
                .Include(ep => ep.Questions)
                    .ThenInclude(q => q.Prompt) 
                .AsNoTracking() 
                .ToListAsync();

            // Map sang DTO
            var result = examParts.Select(ep => new ExamPartDTO
            {
                PartId = ep.PartId,
                ExamId = ep.ExamId,
                PartCode = ep.PartCode,
                Title = ep.Title,
                OrderIndex = ep.OrderIndex,
                Questions = ep.Questions.Select(q => new QuestionDTO
                {
                    QuestionId = q.QuestionId,
                    PartId = q.PartId,
                    QuestionType = q.QuestionType,
                    StemText = q.StemText,
                    PromptId = q.PromptId,
                    ScoreWeight = q.ScoreWeight,
                    QuestionExplain = q.QuestionExplain,
                    Time = q.Time,
                    QuestionNumber = q.QuestionNumber,
                    Options = q.Options.Select(o => new OptionDTO
                    {
                        OptionId = o.OptionId,
                        QuestionId = o.QuestionId,
                        Content = o.Content,
                        IsCorrect = o.IsCorrect ?? false
                    }).ToList(),
                    Prompt = q.Prompt != null ? new PromptDTO
                    {
                        PromptId = q.Prompt.PromptId,
                        Skill = q.Prompt.Skill ?? string.Empty,
                        ContentText = q.Prompt.ContentText ?? string.Empty,
                        Title = q.Prompt.Title ?? string.Empty,
                        ReferenceImageUrl = q.Prompt.ReferenceImageUrl,
                        ReferenceAudioUrl = q.Prompt.ReferenceAudioUrl
                    } : null!
                }).OrderBy(q => q.QuestionNumber).ToList()
            }).OrderBy(ep => ep.OrderIndex).ToList();

            return result;
        }

    }
}
