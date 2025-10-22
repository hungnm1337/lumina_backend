using DataLayer.DTOs.Passage;
using DataLayer.DTOs.Prompt;
using DataLayer.DTOs.Questions;
using DataLayer.Models;
using GenerativeAI.Types;
using Microsoft.EntityFrameworkCore;
using RepositoryLayer.Questions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Questions
{
    public class QuestionService : IQuestionService
    {

        private readonly IQuestionRepository _questionRepository;

        private readonly LuminaSystemContext _dbContext;

        public QuestionService(IQuestionRepository questionRepository, LuminaSystemContext systemContext)
        {
            _questionRepository = questionRepository;
            _dbContext = systemContext;
        }


        public async Task<int> CreatePromptWithQuestionsAsync(CreatePromptWithQuestionsDTO dto)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // Tạo Prompt mới (đã gộp Passage)
                var prompt = new Prompt
                {
                    Title = dto.Title,
                    ContentText = dto.ContentText,
                    Skill = dto.Skill,
                    ReferenceImageUrl = dto.ReferenceImageUrl,
                    ReferenceAudioUrl = dto.ReferenceAudioUrl
                };
                prompt = await _questionRepository.AddPromptAsync(prompt);

                // Lấy danh sách PartId trong dto để xử lý 1 lượt
                var partIds = dto.Questions.Select(q => q.Question.PartId).Distinct().ToList();

                // Lấy các ExamParts tương ứng
                var partsDict = await _dbContext.ExamParts
                    .Where(p => partIds.Contains(p.PartId))
                    .ToDictionaryAsync(p => p.PartId);

                // Lấy số câu hỏi hiện có theo từng Part
                var currentCounts = await _dbContext.Questions
                    .Where(q => partIds.Contains(q.PartId))
                    .GroupBy(q => q.PartId)
                    .Select(g => new { PartId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.PartId, x => x.Count);

                // Duyệt theo từng nhóm câu hỏi theo PartId
                foreach (var grouped in dto.Questions.GroupBy(q => q.Question.PartId))
                {
                    var partId = grouped.Key;
                    if (!partsDict.ContainsKey(partId))
                        throw new Exception($"ExamPart {partId} không tồn tại");

                    int maxQuestions = partsDict[partId].MaxQuestions;
                    int currentCount = currentCounts.ContainsKey(partId) ? currentCounts[partId] : 0;

                    // Kiểm tra số lượng câu hỏi tối đa
                    if (currentCount + grouped.Count() > maxQuestions)
                        throw new Exception($"ExamPart id {partId} chỉ được phép có tối đa {maxQuestions} câu hỏi");

                    int assignNumber = currentCount + 1;

                    foreach (var q in grouped)
                    {
                        var question = new Question
                        {
                            PartId = partId,
                            QuestionType = q.Question.QuestionType,
                            StemText = q.Question.StemText,
                            ScoreWeight = q.Question.ScoreWeight,
                            QuestionExplain = q.Question.QuestionExplain,
                            Time = q.Question.Time,
                            QuestionNumber = assignNumber++,
                            PromptId = prompt.PromptId
                        };
                        question = await _questionRepository.AddQuestionAsync(question);

                        if (q.Options != null && q.Options.Any())
                        {
                            var options = q.Options.Select(o => new Option
                            {
                                QuestionId = question.QuestionId,
                                Content = o.Content,
                                IsCorrect = o.IsCorrect
                            });
                            await _questionRepository.AddOptionsAsync(options);
                        }
                    }
                }

                await transaction.CommitAsync();
                return prompt.PromptId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }



       /* public Task<bool> EditPassageWithPromptAsync(PassageEditDto dto)
        => _questionRepository.EditPassageWithPromptAsync(dto);*/

        public Task<(List<PromptDto> Items, int TotalPages)> GetPromptsPagedAsync(int page, int size, int? partId)
        => _questionRepository.GetPromptsPagedAsync(page, size, partId);


        public Task<int> AddQuestionAsync(QuestionCrudDto dto) => _questionRepository.AddQuestionAsync(dto);
        public Task<bool> UpdateQuestionAsync(QuestionCrudDto dto) => _questionRepository.UpdateQuestionAsync(dto);
        public Task<bool> DeleteQuestionAsync(int questionId) => _questionRepository.DeleteQuestionAsync(questionId);

        public async Task<QuestionStatisticDto> GetStatisticsAsync()
        {
            return await _questionRepository.GetQuestionStatisticsAsync();
        }

        public async Task<bool> EditPromptWithQuestionsAsync(PromptEditDto dto)
        {
            return await _questionRepository.EditPromptWithQuestionsAsync(dto);
        }
    }
}
