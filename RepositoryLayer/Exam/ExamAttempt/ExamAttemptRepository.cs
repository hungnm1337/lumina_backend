using DataLayer.DTOs.UserAnswer;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RepositoryLayer.Exam.ExamAttempt
{
    public static class ExamAttemptStatus
    {
        public const string Doing = "Doing";
        public const string Completed = "Completed";
    }

    public class ExamAttemptRepository : IExamAttemptRepository
    {
        private readonly LuminaSystemContext _context;

        public ExamAttemptRepository(LuminaSystemContext context)
        {
            _context = context;
        }

        public async Task<ExamAttemptRequestDTO> EndAnExam(ExamAttemptRequestDTO model)
        {
            var attempt = await _context.ExamAttempts.FindAsync(model.AttemptID);

            if (attempt == null)
            {
                throw new KeyNotFoundException($"Exam attempt with ID {model.AttemptID} not found.");
            }

            attempt.EndTime = model.EndTime;
            attempt.Score = model.Score;
            attempt.Status = ExamAttemptStatus.Completed;

            await _context.SaveChangesAsync();
            return new ExamAttemptRequestDTO()
            {
                AttemptID = attempt.AttemptID,
                UserID = attempt.UserID,
                ExamID = attempt.ExamID,
                ExamPartId = attempt.ExamPartId,
                StartTime = attempt.StartTime,
                EndTime = attempt.EndTime,
                Score = attempt.Score,
                Status = attempt.Status
            };
        }

        public async Task<List<ExamAttemptResponseDTO>> GetAllExamAttempts(int userId)
        {
            var attempts = await _context.ExamAttempts
                .AsNoTracking()
                .Where(attempt => attempt.UserID == userId)
                .Select(attempt => new ExamAttemptResponseDTO()
                {
                    AttemptID = attempt.AttemptID,
                    UserName = attempt.User != null ? attempt.User.FullName : null,
                    ExamName = attempt.Exam != null ? attempt.Exam.Name : null,
                    ExamPartName = attempt.ExamPart != null ? attempt.ExamPart.PartCode : null,
                    StartTime = attempt.StartTime,
                    EndTime = attempt.EndTime,
                    Score = attempt.Score,
                    Status = attempt.Status
                })
                .ToListAsync();

            return attempts;
        }
        public async Task<ExamAttemptDetailResponseDTO> GetExamAttemptById(int attemptId)
        {
            var attemptsInfo = await _context.ExamAttempts
                .AsNoTracking()
                .Where(attempt => attempt.AttemptID == attemptId)
                .Select(attempt => new ExamAttemptResponseDTO()
                {
                    AttemptID = attempt.AttemptID,
                    UserName = attempt.User != null ? attempt.User.FullName : null,
                    ExamName = attempt.Exam != null ? attempt.Exam.Name : null,
                    ExamPartName = attempt.ExamPart != null ? attempt.ExamPart.PartCode : null,
                    StartTime = attempt.StartTime,
                    EndTime = attempt.EndTime,
                    Score = attempt.Score,
                    Status = attempt.Status
                })
                .FirstOrDefaultAsync();

            if (attemptsInfo == null)
            {
                return null;
            }

            var writingAnswers = await this.GetWritingAnswerByAttemptId(attemptId);
            var readingAnswers = await this.GetReadingAnswerByAttemptId(attemptId);
            var listeningAnswers = await this.GetListeningAnswerByAttemptId(attemptId);
            var speakingAnswers = await this.GetSpeakingAnswerByAttemptId(attemptId);

            ExamAttemptDetailResponseDTO data = new ExamAttemptDetailResponseDTO()
            {
                ExamAttemptInfo = attemptsInfo,
                WritingAnswers = writingAnswers,
                ReadingAnswers = readingAnswers,
                ListeningAnswers = listeningAnswers,
                SpeakingAnswers = speakingAnswers
            };
            return data;
        }

        private async Task<List<WritingAnswerResponseDTO>> GetWritingAnswerByAttemptId(int attemptId)
        {
            var writingAnswers = await _context.UserAnswerWritings
                .AsNoTracking()
                .Where(answer => answer.AttemptID == attemptId)
                .Select(answer => new WritingAnswerResponseDTO()
                {
                    UserAnswerWritingId = answer.UserAnswerWritingId,
                    AttemptID = answer.AttemptID,
                    Question = new DataLayer.DTOs.Exam.QuestionDTO()
                    {
                        Options = null,
                        PromptId = answer.Question.PromptId,
                        QuestionId = answer.Question.QuestionId,
                        Prompt = answer.Question.Prompt != null ? new DataLayer.DTOs.Exam.PromptDTO()
                        {
                            PromptId = answer.Question.Prompt.PromptId,
                            ContentText = answer.Question.Prompt.ContentText,
                            Skill = answer.Question.Prompt.Skill,
                            Title = answer.Question.Prompt.Title,
                            ReferenceAudioUrl = answer.Question.Prompt.ReferenceAudioUrl,
                            ReferenceImageUrl = answer.Question.Prompt.ReferenceImageUrl
                        } : null,
                        PartId = answer.Question.PartId,
                        QuestionExplain = answer.Question.QuestionExplain,
                        QuestionType = answer.Question.QuestionType,
                        QuestionNumber = answer.Question.QuestionNumber,
                        ScoreWeight = answer.Question.ScoreWeight,
                        StemText = answer.Question.StemText,
                        Time = answer.Question.Time
                    },
                    UserAnswerContent = answer.UserAnswerContent,
                    FeedbackFromAI = answer.FeedbackFromAI
                })
                .ToListAsync();
            return writingAnswers;
        }

        private async Task<List<ReadingAnswerResponseDTO>> GetReadingAnswerByAttemptId(int attemptId)
        {
            var readingAnswers = await _context.UserAnswerMultipleChoices
                .AsNoTracking()
                .Where(answer => answer.AttemptID == attemptId)
                .Select(answer => new ReadingAnswerResponseDTO()
                {
                    AttemptID = answer.AttemptID,
                    Question = new DataLayer.DTOs.Exam.QuestionDTO()
                    {
                        Options = null,
                        PromptId = answer.Question.PromptId,
                        QuestionId = answer.Question.QuestionId,
                        Prompt = answer.Question.Prompt != null ? new DataLayer.DTOs.Exam.PromptDTO()
                        {
                            PromptId = answer.Question.Prompt.PromptId,
                            ContentText = answer.Question.Prompt.ContentText,
                            Skill = answer.Question.Prompt.Skill,
                            Title = answer.Question.Prompt.Title,
                            ReferenceAudioUrl = answer.Question.Prompt.ReferenceAudioUrl,
                            ReferenceImageUrl = answer.Question.Prompt.ReferenceImageUrl
                        } : null,
                        PartId = answer.Question.PartId,
                        QuestionExplain = answer.Question.QuestionExplain,
                        QuestionType = answer.Question.QuestionType,
                        QuestionNumber = answer.Question.QuestionNumber,
                        ScoreWeight = answer.Question.ScoreWeight,
                        StemText = answer.Question.StemText,
                        Time = answer.Question.Time
                    },
                    IsCorrect = answer.IsCorrect,
                    Score = answer.Score,
                    SelectedOption = answer.SelectedOption != null ? new DataLayer.DTOs.Exam.OptionDTO()
                    {
                        OptionId = answer.SelectedOption.OptionId,
                        Content = answer.SelectedOption.Content,
                        IsCorrect = answer.SelectedOption.IsCorrect,
                        QuestionId = answer.SelectedOption.QuestionId
                    } : null
                })
                .ToListAsync();
            return readingAnswers;
        }

        private async Task<List<ListeningAnswerResponseDTO>> GetListeningAnswerByAttemptId(int attemptId)
        {
            // Listening cũng dùng UserAnswerMultipleChoice nhưng phân biệt qua QuestionType hoặc PartId
            // Giả sử Listening có QuestionType = "Listening" hoặc PartId thuộc về Listening parts (1,2,3,4)
            var listeningAnswers = await _context.UserAnswerMultipleChoices
                .AsNoTracking()
                .Where(answer => answer.AttemptID == attemptId 
                    && (answer.Question.QuestionType == "Listening" || 
                        answer.Question.PartId >= 1 && answer.Question.PartId <= 4)) // Adjust based on your Part structure
                .Select(answer => new ListeningAnswerResponseDTO()
                {
                    AttemptID = answer.AttemptID,
                    Question = new DataLayer.DTOs.Exam.QuestionDTO()
                    {
                        Options = null,
                        PromptId = answer.Question.PromptId,
                        QuestionId = answer.Question.QuestionId,
                        Prompt = answer.Question.Prompt != null ? new DataLayer.DTOs.Exam.PromptDTO()
                        {
                            PromptId = answer.Question.Prompt.PromptId,
                            ContentText = answer.Question.Prompt.ContentText,
                            Skill = answer.Question.Prompt.Skill,
                            Title = answer.Question.Prompt.Title,
                            ReferenceAudioUrl = answer.Question.Prompt.ReferenceAudioUrl,
                            ReferenceImageUrl = answer.Question.Prompt.ReferenceImageUrl
                        } : null,
                        PartId = answer.Question.PartId,
                        QuestionExplain = answer.Question.QuestionExplain,
                        QuestionType = answer.Question.QuestionType,
                        QuestionNumber = answer.Question.QuestionNumber,
                        ScoreWeight = answer.Question.ScoreWeight,
                        StemText = answer.Question.StemText,
                        Time = answer.Question.Time
                    },
                    IsCorrect = answer.IsCorrect,
                    Score = answer.Score,
                    SelectedOption = answer.SelectedOption != null ? new DataLayer.DTOs.Exam.OptionDTO()
                    {
                        OptionId = answer.SelectedOption.OptionId,
                        Content = answer.SelectedOption.Content,
                        IsCorrect = answer.SelectedOption.IsCorrect,
                        QuestionId = answer.SelectedOption.QuestionId
                    } : null
                })
                .ToListAsync();
            return listeningAnswers;
        }

        private async Task<List<SpeakingAnswerResponseDTO>> GetSpeakingAnswerByAttemptId(int attemptId)
        {
            var speakingAnswers = await _context.UserAnswerSpeakings
                .AsNoTracking()
                .Where(answer => answer.AttemptID == attemptId)
                .Select(answer => new SpeakingAnswerResponseDTO()
                {
                    UserAnswerSpeakingId = answer.UserAnswerSpeakingId,
                    AttemptID = answer.AttemptID,
                    Question = new DataLayer.DTOs.Exam.QuestionDTO()
                    {
                        Options = null,
                        PromptId = answer.Question.PromptId,
                        QuestionId = answer.Question.QuestionId,
                        Prompt = answer.Question.Prompt != null ? new DataLayer.DTOs.Exam.PromptDTO()
                        {
                            PromptId = answer.Question.Prompt.PromptId,
                            ContentText = answer.Question.Prompt.ContentText,
                            Skill = answer.Question.Prompt.Skill,
                            Title = answer.Question.Prompt.Title,
                            ReferenceAudioUrl = answer.Question.Prompt.ReferenceAudioUrl,
                            ReferenceImageUrl = answer.Question.Prompt.ReferenceImageUrl
                        } : null,
                        PartId = answer.Question.PartId,
                        QuestionExplain = answer.Question.QuestionExplain,
                        QuestionType = answer.Question.QuestionType,
                        QuestionNumber = answer.Question.QuestionNumber,
                        ScoreWeight = answer.Question.ScoreWeight,
                        StemText = answer.Question.StemText,
                        Time = answer.Question.Time
                    },
                    Transcript = answer.Transcript,
                    AudioUrl = answer.AudioUrl,
                    PronunciationScore = answer.PronunciationScore,
                    AccuracyScore = answer.AccuracyScore,
                    FluencyScore = answer.FluencyScore,
                    CompletenessScore = answer.CompletenessScore,
                    GrammarScore = answer.GrammarScore,
                    VocabularyScore = answer.VocabularyScore,
                    ContentScore = answer.ContentScore,
                    OverallScore = (answer.PronunciationScore + answer.AccuracyScore + answer.FluencyScore + 
                                   answer.CompletenessScore + answer.GrammarScore + answer.VocabularyScore + 
                                   answer.ContentScore) / 7
                })
                .ToListAsync();
            return speakingAnswers;
        }

        public async Task<ExamAttemptRequestDTO> StartAnExam(ExamAttemptRequestDTO model)
        {
            DataLayer.Models.ExamAttempt attempt = new DataLayer.Models.ExamAttempt()
            {
                UserID = model.UserID,
                ExamID = model.ExamID,
                ExamPartId = model.ExamPartId,
                StartTime = model.StartTime,
                Status = ExamAttemptStatus.Doing 
            };

            await _context.ExamAttempts.AddAsync(attempt);

            await _context.SaveChangesAsync();        
            return new ExamAttemptRequestDTO()
            {
                AttemptID = attempt.AttemptID, 
                UserID = attempt.UserID,
                ExamID = attempt.ExamID,
                ExamPartId = attempt.ExamPartId,
                StartTime = attempt.StartTime,
                EndTime = attempt.EndTime, 
                Score = attempt.Score,    
                Status = attempt.Status
            };
        }
    }
}