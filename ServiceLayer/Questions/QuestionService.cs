using DataLayer.DTOs.Passage;
using DataLayer.DTOs.Questions;
using DataLayer.Models;
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
                int? passageId = null;
                if (dto.Passage != null)
                {
                    var passage = new Passage
                    {
                        ContentText = dto.Passage.ContentText,
                        Title = dto.Passage.Title
                    };
                    passage = await _questionRepository.AddPassageAsync(passage);
                    passageId = passage.PassageId;
                }

                var prompt = new Prompt
                {
                    PassageId = passageId,
                    Skill = dto.Prompt.Skill,
                    PromptText = dto.Prompt.PromptText,
                    ReferenceImageUrl = dto.Prompt.ReferenceImageUrl,
                    ReferenceAudioUrl = dto.Prompt.ReferenceAudioUrl
                };
                prompt = await _questionRepository.AddPromptAsync(prompt);

                foreach (var grouped in dto.Questions.GroupBy(q => q.Question.PartId))
                {
                    var partId = grouped.Key;

                    // Lấy số lượng câu hỏi hiện tại của part
                    var part = await _dbContext.ExamParts.FindAsync(partId);
                    if (part == null)
                        throw new Exception($"ExamPart {partId} không tồn tại");
                    int maxQuestions = part.MaxQuestions;

                    // Lấy các question hiện tại đã dùng với part này
                    var currentQuestionNumbers = await _dbContext.Questions
                        .Where(q => q.PartId == partId)
                        .Select(q => q.QuestionNumber)
                        .ToListAsync();

                    // Nếu đã đầy thì không cho thêm nữa
                    if (currentQuestionNumbers.Count >= maxQuestions)
                        throw new Exception($"ExamPart id {partId} đã đủ {maxQuestions} câu hỏi rồi");

                    // Tìm các số thứ tự chưa dùng (từ 1 đến maxQuestions)
                    var availableNumbers = Enumerable.Range(1, maxQuestions)
                        .Except(currentQuestionNumbers)
                        .ToList();

                    int idx = 0;
                    foreach (var q in grouped)
                    {
                        int assignNumber;
                        if (idx < availableNumbers.Count)
                            assignNumber = availableNumbers[idx];
                        else
                            throw new Exception($"Không còn slot QuestionNumber cho PartId {partId}");

                        var question = new Question
                        {
                            PartId = partId,
                            QuestionType = q.Question.QuestionType,
                            StemText = q.Question.StemText,
                            ScoreWeight = q.Question.ScoreWeight,
                            QuestionExplain = q.Question.QuestionExplain,
                            Time = q.Question.Time,                            
                            QuestionNumber = assignNumber,
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
                        idx++;
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

        public Task<bool> EditPassageWithPromptAsync(PassageEditDto dto)
        => _questionRepository.EditPassageWithPromptAsync(dto);

        public Task<(List<PassageDto> Items, int TotalPages)> GetPassagePromptQuestionsPagedAsync(int page, int size, int? partId)
        => _questionRepository.GetPassagePromptQuestionsPagedAsync(page, size, partId);


        public Task<int> AddQuestionAsync(QuestionCrudDto dto) => _questionRepository.AddQuestionAsync(dto);
        public Task<bool> UpdateQuestionAsync(QuestionCrudDto dto) => _questionRepository.UpdateQuestionAsync(dto);
        public Task<bool> DeleteQuestionAsync(int questionId) => _questionRepository.DeleteQuestionAsync(questionId);
    }
}
