using DataLayer.DTOs.Prompt;
using DataLayer.DTOs.Questions;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using RepositoryLayer.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepositoryLayer.Questions
{
    public class QuestionRepository : Repository<Question>, IQuestionRepository
    {

        private readonly LuminaSystemContext _context;

        public QuestionRepository(LuminaSystemContext context) : base(context)
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
       

        public async Task<bool> EditPromptWithQuestionsAsync(PromptEditDto dto)
        {
            var prompt = await _context.Prompts
                .Include(p => p.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(p => p.PromptId == dto.PromptId);

            if (prompt == null)
                return false;

            prompt.Title = dto.Title;
            prompt.ContentText = dto.ContentText;
            prompt.Skill = dto.Skill;
            prompt.ReferenceImageUrl = dto.ReferenceImageUrl;
            prompt.ReferenceAudioUrl = dto.ReferenceAudioUrl;
           

            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<(List<PromptDto> Items, int TotalPages)> GetPromptsPagedAsync(int page, int size, int? partId)
        {
            var query = _context.Prompts.AsQueryable();

            if (partId.HasValue)
            {
                query = query.Where(p => p.Questions.Any(q => q.PartId == partId.Value));
            }

            int totalRecords = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalRecords / (double)size);

            var items = await query.OrderBy(p => p.PromptId)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(pr => new PromptDto
                {
                    PromptId = pr.PromptId,
                    Title = pr.Title,
                    ContentText = pr.ContentText,
                    Skill = pr.Skill,
                    ReferenceImageUrl = pr.ReferenceImageUrl,
                    ReferenceAudioUrl = pr.ReferenceAudioUrl,
                    PartId = pr.Questions.Any() ? pr.Questions.First().PartId : 0,
                    Questions = pr.Questions
                        .Where(q => !partId.HasValue || q.PartId == partId.Value)
                        .Select(q => new QuestionDto
                        {
                            QuestionId = q.QuestionId,
                            StemText = q.StemText,
                            QuestionExplain = q.QuestionExplain,
                            ScoreWeight = q.ScoreWeight,
                            Time = q.Time,
                            SampleAnswer = q.SampleAnswer,
                            Options = q.Options.Select(o => new OptionDto
                            {
                                OptionId = o.OptionId,
                                Content = o.Content,
                                IsCorrect = o.IsCorrect ?? false
                            }).ToList()
                        }).ToList()
                })
                .ToListAsync();

            return (items, totalPages);
        }



        public async Task<int> AddQuestionAsync(QuestionCrudDto dto)
        {
            var part = await _context.ExamParts.FindAsync(dto.PartId);
            if (part == null)
                throw new Exception($"ExamPart với Id={dto.PartId} không tồn tại.");

            int currentCount = await _context.Questions.CountAsync(q => q.PartId == dto.PartId);

            if (currentCount >= part.MaxQuestions)
                throw new Exception($"ExamPart id {dto.PartId} đã đủ {part.MaxQuestions} câu hỏi rồi.");

            var question = new Question
            {
                StemText = dto.StemText,
                QuestionExplain = dto.QuestionExplain,
                ScoreWeight = dto.ScoreWeight,
                Time = dto.Time,
                PartId = dto.PartId,
                PromptId = dto.PromptId,
                QuestionType = dto.QuestionType,
                Options = dto.Options.Select(o => new Option
                {
                    Content = o.Content,
                    IsCorrect = o.IsCorrect
                }).ToList()
            };

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            return question.QuestionId;
        }


        public async Task<bool> UpdateQuestionAsync(QuestionCrudDto dto)
        {
            var question = await _context.Questions.Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.QuestionId == dto.QuestionId);
            if (question == null) return false;

            question.StemText = dto.StemText;
            question.QuestionExplain = dto.QuestionExplain;
            question.SampleAnswer = dto.SampleAnswer;
            question.ScoreWeight = dto.ScoreWeight;
            question.Time = dto.Time;
            if (dto.Options != null)
            {
                foreach (var optionDto in dto.Options)
                {
                    var existingOption = question.Options.FirstOrDefault(o => o.OptionId == optionDto.OptionId);
                    if (existingOption != null)
                    {
                        existingOption.Content = optionDto.Content;
                        existingOption.IsCorrect = optionDto.IsCorrect;
                    }
                    else
                    {
                        var newOption = new Option
                        {
                            Content = optionDto.Content,
                            IsCorrect = optionDto.IsCorrect
                        };
                        question.Options.Add(newOption);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<bool> DeleteQuestionAsync(int questionId)
        {
            var question = await _context.Questions
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.QuestionId == questionId);

            if (question == null)
                return false;

            var examPart = await _context.ExamParts
                .Include(ep => ep.Exam)
                .FirstOrDefaultAsync(ep => ep.PartId == question.PartId);

            if (examPart.Exam != null && examPart.Exam.IsActive)
                throw new Exception("Không thể xóa câu hỏi vì bài thi đang hoạt động.");

            if (question.Options.Any())
            {
                _context.Options.RemoveRange(question.Options);
            }

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();

            return true;
        }



        public async Task<QuestionStatisticDto> GetQuestionStatisticsAsync()
        {
            var total = await _context.Questions.CountAsync();
            
            var used = 0; 
            var unused = total; 

            return new QuestionStatisticDto
            {
                TotalQuestions = total,
                UsedQuestions = used,
                UnusedQuestions = unused
            };
        }



        public async Task<bool> DeletePromptAsync(int promptId)
        {
            var prompt = await _context.Prompts
                .Include(p => p.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(p => p.PromptId == promptId);

            if (prompt == null)
                return false;

            if (!prompt.Questions.Any())
            {
                _context.Prompts.Remove(prompt);
                await _context.SaveChangesAsync();
                return true;
            }

            var firstQuestion = prompt.Questions.First();
            var examPart = await _context.ExamParts
                .Include(ep => ep.Exam)
                .FirstOrDefaultAsync(ep => ep.PartId == firstQuestion.PartId);

            if (examPart?.Exam != null && examPart.Exam.IsActive)
            {
                throw new Exception("Không thể xóa prompt vì bài thi đang hoạt động.");
            }

            foreach (var question in prompt.Questions)
            {
                if (question.Options.Any())
                {
                    _context.Options.RemoveRange(question.Options);
                }
            }

            _context.Questions.RemoveRange(prompt.Questions);
            _context.Prompts.Remove(prompt);

            await _context.SaveChangesAsync();
            return true;
        }

    }
}
