using DataLayer.DTOs.AIGeneratedExam;
using DataLayer.DTOs.Exam;
using DataLayer.DTOs.Questions;
using System.Collections.Generic;
using System.Linq;

namespace ServiceLayer.ExamGenerationAI.Mappers
{
    public class AIExamMapper : IAIExamMapper
    {
        public List<CreatePromptWithQuestionsDTO> MapAIGeneratedToCreatePrompts(AIGeneratedExamDTO aiExam)
        {
            var result = new List<CreatePromptWithQuestionsDTO>();

            if (aiExam?.Prompts == null || !aiExam.Prompts.Any())
                return result;

            foreach (var aiPrompt in aiExam.Prompts)
            {
                var promptDto = new CreatePromptWithQuestionsDTO
                {
                    Skill = aiExam.Skill ?? string.Empty,
                    Title = aiPrompt.ExamTitle ?? string.Empty,
                    ContentText = aiPrompt.Description ?? string.Empty,
                    ReferenceImageUrl = aiPrompt.ReferenceImageUrl,
                    ReferenceAudioUrl = aiPrompt.ReferenceAudioUrl,
                    Questions = aiPrompt.Questions?.Select(q => MapQuestion(q)).ToList() ?? new List<QuestionWithOptionsDTO>()
                };

                result.Add(promptDto);
            }

            return result;
        }

        private static QuestionWithOptionsDTO MapQuestion(AIGeneratedQuestionDTO aiQuestion)
        {
            if (aiQuestion == null) return null;

            return new QuestionWithOptionsDTO
            {
                Question = new AddQuestionDTO
                {
                    PartId = aiQuestion.PartId,
                    QuestionType = aiQuestion.QuestionType ?? string.Empty,
                    StemText = aiQuestion.StemText ?? string.Empty,
                    ScoreWeight = aiQuestion.ScoreWeight,
                    QuestionExplain = aiQuestion.Explanation ?? string.Empty,
                    Time = aiQuestion.Time,
                    QuestionNumber = 0, 
                    PromptId = 0, 
                    SampleAnswer = aiQuestion.SampleAnswer
                },
                Options = aiQuestion.Options?.Select(o => new OptionDTO
                {
                    OptionId = null,
                    QuestionId = 0,
                    Content = o.Content ?? string.Empty,
                    IsCorrect = o.IsCorrect ?? false
                }).ToList() ?? new List<OptionDTO>()
            };
        }
    }
}
