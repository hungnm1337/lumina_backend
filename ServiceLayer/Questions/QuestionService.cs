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

        public async Task<List<int>> SavePromptsWithQuestionsAndOptionsAsync(
    List<CreatePromptWithQuestionsDTO> promptDtos, int partId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            var savedPromptIds = new List<int>();

            try
            {
                // Kiểm tra PartId tồn tại
                var part = await _dbContext.ExamParts.FirstOrDefaultAsync(p => p.PartId == partId);
                if (part == null)
                    throw new Exception($"PartId {partId} không tồn tại");

                int maxQuestions = part.MaxQuestions;

                // Tổng số lượng câu hỏi hiện có ở part này
                int currentCount = await _dbContext.Questions.CountAsync(q => q.PartId == partId);

                int assignNumber = currentCount + 1; // Đánh số tiếp theo

                foreach (var promptDto in promptDtos)
                {
                    // 1. Lưu Prompt
                    var prompt = new Prompt
                    {
                        Title = promptDto.Title,
                        ContentText = promptDto.ContentText,
                        Skill = promptDto.Skill,
                        ReferenceImageUrl = promptDto.ReferenceImageUrl,
                        ReferenceAudioUrl = promptDto.ReferenceAudioUrl
                    };
                    _dbContext.Prompts.Add(prompt);
                    await _dbContext.SaveChangesAsync();
                    int promptId = prompt.PromptId;
                    savedPromptIds.Add(promptId);

                    // 2. Lưu Question cho Prompt này (tất cả sẽ gán partId truyền vào)
                    foreach (var q in promptDto.Questions)
                    {
                        if (currentCount + 1 > maxQuestions)
                            throw new Exception($"ExamPart id {partId} chỉ được phép có tối đa {maxQuestions} câu hỏi");

                        var question = new Question
                        {
                            PartId = partId,
                            QuestionType = q.Question.QuestionType,
                            StemText = q.Question.StemText,
                            ScoreWeight = q.Question.ScoreWeight,
                            QuestionExplain = q.Question.QuestionExplain,
                            Time = q.Question.Time,
                            QuestionNumber = assignNumber++,
                            PromptId = promptId,
                            SampleAnswer = q.Question.SampleAnswer
                        };

                        _dbContext.Questions.Add(question);
                        await _dbContext.SaveChangesAsync();
                        int questionId = question.QuestionId;
                        currentCount++;

                        // 3. Lưu Option nếu có
                        if (q.Options != null && q.Options.Any())
                        {
                            var options = q.Options.Select(op => new Option
                            {
                                QuestionId = questionId,
                                Content = op.Content,
                                IsCorrect = op.IsCorrect
                            });
                            _dbContext.Options.AddRange(options);
                            await _dbContext.SaveChangesAsync();
                        }
                    }
                }

                await transaction.CommitAsync();
                return savedPromptIds;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<int> GetAvailableSlots(int partId, int requestedCount)
        {
            var part = await _dbContext.ExamParts.FirstOrDefaultAsync(p => p.PartId == partId);
            if (part == null)
                throw new Exception($"PartId {partId} không tồn tại");

            int currentCount = await _dbContext.Questions.CountAsync(q => q.PartId == partId);
            int available = part.MaxQuestions - currentCount;

            if (requestedCount > available)
                throw new Exception($"ExamPart id {partId} chỉ còn {available} slot, không đủ cho {requestedCount} câu hỏi bạn cần thêm");

            return available;
        }

        public async Task<bool> DeletePromptAsync(int promptId)
{
    try
    {
        return await _questionRepository.DeletePromptAsync(promptId);
    }
    catch (Exception ex)
    {
        // Log error nếu cần
        throw new Exception($"Lỗi khi xóa prompt: {ex.Message}");
    }
}
    }
}
