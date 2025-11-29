using DataLayer.DTOs.Exam;
using DataLayer.DTOs.ExamPart;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


    public class ExamRepository : IExamRepository
    {
        private readonly LuminaSystemContext _luminaSystemContext;

        public ExamRepository(LuminaSystemContext luminaSystemContext)
        {
            _luminaSystemContext = luminaSystemContext;
        }

       
        public async Task<List<ExamDTO>> GetAllExams(string? examType = null, string? partCode = null)
        {
            var query = _luminaSystemContext.Exams.Where(e => e.IsActive == true);
            
            if (!string.IsNullOrEmpty(examType))
            {
                query = query.Where(e => e.ExamType == examType);
            }
            
            if (!string.IsNullOrEmpty(partCode))
            {
                query = query.Where(e => e.ExamParts.Any(ep => ep.PartCode == partCode));
            }
            
            var exams = await query.ToListAsync();

            var userIds = exams.Select(e => e.CreatedBy).Distinct();
            var users = await _luminaSystemContext.Users
                          .Where(u => userIds.Contains(u.UserId))
                          .ToDictionaryAsync(u => u.UserId, u => u.FullName);

            var examDtos = exams.Select(e => new ExamDTO
            {
                ExamId = e.ExamId,
                ExamType = e.ExamType,
                Name = e.Name,
                Description = e.Description,
                IsActive = e.IsActive,
                CreatedBy = e.CreatedBy,
                CreatedByName = users.ContainsKey(e.CreatedBy) ? users[e.CreatedBy] : "Unknown",
                UpdateBy = e.UpdateBy,
                UpdateByName = e.UpdateBy.HasValue && users.ContainsKey(e.UpdateBy.Value) ? users[e.UpdateBy.Value] : null,
                CreatedAt = e.CreatedAt,
                UpdateAt = e.UpdateAt,
                ExamParts = null
            }).ToList();

            return examDtos;
        }


        
        public async Task<ExamDTO> GetExamDetailAndExamPartByExamID(int examId)
        {
            var exam = await _luminaSystemContext.Exams
                .Where(e => e.ExamId == examId && e.IsActive == true)
                .Include(e => e.ExamParts)
                .FirstOrDefaultAsync();

            if (exam == null) return null;

            var userIds = new List<int> { exam.CreatedBy };
            if (exam.UpdateBy.HasValue)
                userIds.Add(exam.UpdateBy.Value);

            var users = await _luminaSystemContext.Users
                .Where(u => userIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, u => u.FullName);

            var examDetail = new ExamDTO
            {
                ExamId = exam.ExamId,
                ExamType = exam.ExamType,
                Name = exam.Name,
                Description = exam.Description,
                IsActive = exam.IsActive,
                CreatedBy = exam.CreatedBy,
                CreatedByName = users.ContainsKey(exam.CreatedBy) ? users[exam.CreatedBy] : "Unknown",
                UpdateBy = exam.UpdateBy,
                UpdateByName = exam.UpdateBy.HasValue && users.ContainsKey(exam.UpdateBy.Value) ? users[exam.UpdateBy.Value] : null,
                CreatedAt = exam.CreatedAt,
                UpdateAt = exam.UpdateAt,
                ExamParts = exam.ExamParts.Select(ep => new ExamPartDTO
                {
                    PartId = ep.PartId,
                    ExamId = ep.ExamId,
                    PartCode = ep.PartCode,
                    Title = ep.Title,
                    OrderIndex = ep.OrderIndex,
                    Questions = null 
                }).ToList()
            };

            return examDetail;
        }





    public async Task<ExamPartDTO> GetExamPartDetailAndQuestionByExamPartID(int partId)
    {
        var examPartDetail = await _luminaSystemContext.ExamParts
            .Where(ep => ep.PartId == partId)
            .Include(ep => ep.Questions)
                .ThenInclude(q => q.Options)
            .Include(ep => ep.Questions)
                .ThenInclude(q => q.Prompt)
            .Select(ep => new ExamPartDTO
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
                        IsCorrect = o.IsCorrect
                    }).ToList(), 
                    Prompt = q.Prompt == null ? null : new PromptDTO
                    {
                        PromptId = q.Prompt.PromptId,
                        Skill = q.Prompt.Skill,
                        ReferenceImageUrl = q.Prompt.ReferenceImageUrl,
                        ReferenceAudioUrl = q.Prompt.ReferenceAudioUrl,
                        ContentText = q.Prompt.ContentText,
                        Title = q.Prompt.Title
                    }
                }).ToList() 
            })
            .FirstOrDefaultAsync();

        return examPartDetail;
    }


    public async Task<bool> ExamSetKeyExistsAsync(string setKey)
    {
        return await _luminaSystemContext.Exams.AnyAsync(e => e.ExamSetKey == setKey);
    }

    public async Task<List<Exam>> GetExamsBySetKeyAsync(string examSetKey)
        {
            return await _luminaSystemContext.Exams.Where(x => x.ExamSetKey == examSetKey).ToListAsync();
        }

        public async Task<List<ExamPart>> GetExamPartsByExamIdsAsync(List<int> examIds)
        {
            return await _luminaSystemContext.ExamParts.Where(p => examIds.Contains(p.ExamId)).ToListAsync();
        }

        public async Task InsertExamsAsync(List<Exam> exams)
        {
        _luminaSystemContext.Exams.AddRange(exams);
            await _luminaSystemContext.SaveChangesAsync();
        }

        public async Task InsertExamPartsAsync(List<ExamPart> parts)
        {
            _luminaSystemContext.ExamParts.AddRange(parts);
            await _luminaSystemContext.SaveChangesAsync();
        }

    public async Task<List<ExamGroupBySetKeyDto>> GetExamsGroupedBySetKeyAsync()
    {
        var exams = await _luminaSystemContext.Exams
            .Include(e => e.ExamParts)
                .ThenInclude(p => p.Questions)
            .Select(e => new
            {
                e.ExamId,
                e.Name,
                e.IsActive,
                e.Description,
                e.ExamSetKey,
                Parts = e.ExamParts.Select(p => new ExamPartBriefDto
                {
                    PartId = p.PartId,
                    PartCode = p.PartCode,
                    Title = p.Title,
                    MaxQuestions = p.MaxQuestions,
                    QuestionCount = p.Questions.Count()
                }).ToList()
            }).ToListAsync();

        // Group by ExamSetKey
        var result = exams
            .GroupBy(x => x.ExamSetKey)
            .Select(g => new ExamGroupBySetKeyDto
            {
                ExamSetKey = g.Key,
               
                Exams = g.Select(e => new ExamWithPartsDto
                {
                    ExamId = e.ExamId,
                    Name = e.Name,
                    IsActive = e.IsActive,
                    Description = e.Description,
                    Parts = e.Parts,
                }).ToList()
            }).OrderByDescending(g => g.ExamSetKey).ToList();

        return result;
    }
    public async Task<bool> ToggleExamStatusAsync(int examId)
    {
        var exam = await _luminaSystemContext.Exams
            .Include(e => e.ExamParts)
            .ThenInclude(p => p.Questions)
            .FirstOrDefaultAsync(e => e.ExamId == examId);

        if (exam == null) return false;

        if (exam.IsActive == false)
        {
            bool allPartsEnough = exam.ExamParts.All(p => p.Questions.Count() >= p.MaxQuestions);
            if (!allPartsEnough) return false;
            exam.IsActive = true;
        }
        else
        {
            exam.IsActive = false;
        }

        await _luminaSystemContext.SaveChangesAsync();
        return true;

    }

        public async Task<ExamPartDTO> GetExamPartWithQuestionsAsync(int partId)
        {
            var part = await _luminaSystemContext.ExamParts
                .Include(p => p.Questions)
                    .ThenInclude(q => q.Options)
                .Include(p => p.Questions)
                    .ThenInclude(q => q.Prompt) 
                .FirstOrDefaultAsync(p => p.PartId == partId);

            if (part == null) return null;

            return new ExamPartDTO
            {
                PartId = part.PartId,
                ExamId = part.ExamId,
                PartCode = part.PartCode,
                Title = part.Title,
                OrderIndex = part.OrderIndex,
                Questions = part.Questions.Select(q => new QuestionDTO
                {
                    QuestionId = q.QuestionId,
                    StemText = q.StemText,
                    QuestionType = q.QuestionType,
                    QuestionExplain = q.QuestionExplain,
                    ScoreWeight = q.ScoreWeight,
                    Time = q.Time,
                    Prompt = q.Prompt != null ? new PromptDTO
                    {
                        PromptId = q.Prompt.PromptId,
                        Skill = q.Prompt.Skill,
                        Title = q.Prompt.Title, 
                        ContentText = q.Prompt.ContentText, 
                        ReferenceImageUrl = q.Prompt.ReferenceImageUrl,
                        ReferenceAudioUrl = q.Prompt.ReferenceAudioUrl,
                    } : null,
                    Options = q.Options?.Select(o => new OptionDTO
                    {
                        OptionId = o.OptionId,
                        QuestionId = o.QuestionId,
                        Content = o.Content,
                        IsCorrect = o.IsCorrect
                    }).ToList() ?? new List<OptionDTO>()
                }).ToList()
            };
        }
    }


