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
                    UserId = attempt.UserID,
                    UserName = attempt.User != null ? attempt.User.FullName : string.Empty,
                    ExamName = attempt.Exam != null ? attempt.Exam.Name : string.Empty,
                    ExamPartName = attempt.ExamPart != null ? attempt.ExamPart.PartCode : string.Empty,
                    StartTime = attempt.StartTime,
                    EndTime = attempt.EndTime,
                    Score = attempt.Score,
                    Status = attempt.Status,
                    IsMocktest = attempt.ExamPartId != null
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
                    UserId = attempt.UserID,
                    UserName = attempt.User != null ? attempt.User.FullName : string.Empty,
                    ExamName = attempt.Exam != null ? attempt.Exam.Name : string.Empty,
                    ExamPartName = attempt.ExamPart != null ? attempt.ExamPart.PartCode : string.Empty,
                    StartTime = attempt.StartTime,
                    EndTime = attempt.EndTime,
                    Score = attempt.Score,
                    Status = attempt.Status
                })
                .FirstOrDefaultAsync();

            if (attemptsInfo == null)
            {
                return null!;
            }

            var writingAnswers = await this.GetWritingAnswerByAttemptId(attemptId);
            var readingAnswers = await this.GetReadingAnswerByAttemptId(attemptId);
            var listeningAnswers = await this.GetListeningAnswerByAttemptId(attemptId);
            var speakingAnswers = await this.GetSpeakingAnswerByAttemptId(attemptId);

            ExamAttemptDetailResponseDTO data = new ExamAttemptDetailResponseDTO()
            {
                ExamAttemptInfo = attemptsInfo!,
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
                        Options = null!,
                        PromptId = answer.Question.PromptId,
                        PartCode = answer.Question.Part != null ? answer.Question.Part.PartCode : string.Empty,
                        QuestionId = answer.Question.QuestionId,
                        Prompt = answer.Question.Prompt != null ? new DataLayer.DTOs.Exam.PromptDTO()
                        {
                            PromptId = answer.Question.Prompt.PromptId,
                            ContentText = answer.Question.Prompt.ContentText,
                            Skill = answer.Question.Prompt.Skill,
                            Title = answer.Question.Prompt.Title,
                            ReferenceAudioUrl = answer.Question.Prompt.ReferenceAudioUrl,
                            ReferenceImageUrl = answer.Question.Prompt.ReferenceImageUrl
                        } : null!,
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

                        Options = answer.Question.Options
                            .Select(option => new DataLayer.DTOs.Exam.OptionDTO()
                            {
                                OptionId = option.OptionId,
                                Content = option.Content ?? string.Empty,
                                IsCorrect = option.IsCorrect,
                                QuestionId = option.QuestionId
                            })
                            .ToList(),
                        PromptId = answer.Question.PromptId,
                        QuestionId = answer.Question.QuestionId,
                        PartCode = answer.Question.Part != null ? answer.Question.Part.PartCode : string.Empty,
                        Prompt = answer.Question.Prompt != null ? new DataLayer.DTOs.Exam.PromptDTO()
                        {
                            PromptId = answer.Question.Prompt.PromptId,
                            ContentText = answer.Question.Prompt.ContentText,
                            Skill = answer.Question.Prompt.Skill,
                            Title = answer.Question.Prompt.Title,
                            ReferenceAudioUrl = answer.Question.Prompt.ReferenceAudioUrl,
                            ReferenceImageUrl = answer.Question.Prompt.ReferenceImageUrl
                        } : null!,

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
                        Content = answer.SelectedOption.Content ?? string.Empty,
                        IsCorrect = answer.SelectedOption.IsCorrect,
                        QuestionId = answer.SelectedOption.QuestionId
                    } : null!
                })
                .ToListAsync();

            return readingAnswers;
        }

        private async Task<List<ListeningAnswerResponseDTO>> GetListeningAnswerByAttemptId(int attemptId)
        {
            var listeningAnswers = await _context.UserAnswerMultipleChoices
                .AsNoTracking()
                .Where(answer => answer.AttemptID == attemptId
                    && (answer.Question.QuestionType == "Listening" ||
                        answer.Question.PartId >= 1 && answer.Question.PartId <= 4))
                .Select(answer => new ListeningAnswerResponseDTO()
                {
                    AttemptID = answer.AttemptID,
                    Question = new DataLayer.DTOs.Exam.QuestionDTO()
                    {
                        Options = null!,
                        PromptId = answer.Question.PromptId,
                        QuestionId = answer.Question.QuestionId,
                        PartCode = answer.Question.Part != null ? answer.Question.Part.PartCode : string.Empty,

                        Prompt = answer.Question.Prompt != null ? new DataLayer.DTOs.Exam.PromptDTO()
                        {
                            PromptId = answer.Question.Prompt.PromptId,
                            ContentText = answer.Question.Prompt.ContentText,
                            Skill = answer.Question.Prompt.Skill,
                            Title = answer.Question.Prompt.Title,
                            ReferenceAudioUrl = answer.Question.Prompt.ReferenceAudioUrl,
                            ReferenceImageUrl = answer.Question.Prompt.ReferenceImageUrl
                        } : null!,
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
                        Content = answer.SelectedOption.Content ?? string.Empty,
                        IsCorrect = answer.SelectedOption.IsCorrect,
                        QuestionId = answer.SelectedOption.QuestionId
                    } : null!
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
                        Options = null!,
                        PromptId = answer.Question.PromptId,
                        QuestionId = answer.Question.QuestionId,
                        PartCode = answer.Question.Part != null ? answer.Question.Part.PartCode : string.Empty,

                        Prompt = answer.Question.Prompt != null ? new DataLayer.DTOs.Exam.PromptDTO()
                        {
                            PromptId = answer.Question.Prompt.PromptId,
                            ContentText = answer.Question.Prompt.ContentText,
                            Skill = answer.Question.Prompt.Skill,
                            Title = answer.Question.Prompt.Title,
                            ReferenceAudioUrl = answer.Question.Prompt.ReferenceAudioUrl,
                            ReferenceImageUrl = answer.Question.Prompt.ReferenceImageUrl
                        } : null!,
                        PartId = answer.Question.PartId,
                        QuestionExplain = answer.Question.QuestionExplain,
                        QuestionType = answer.Question.QuestionType,
                        QuestionNumber = answer.Question.QuestionNumber,
                        ScoreWeight = answer.Question.ScoreWeight,
                        StemText = answer.Question.StemText,
                        Time = answer.Question.Time,
                        SampleAnswer = answer.Question.SampleAnswer
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
                    OverallScore = answer.OverallScore
                })
                .ToListAsync();

            foreach (var answer in speakingAnswers)
            {
                Console.WriteLine($"  - Question {answer.Question.QuestionNumber}: P={answer.PronunciationScore}, A={answer.AccuracyScore}, F={answer.FluencyScore}, C={answer.CompletenessScore}, G={answer.GrammarScore}, V={answer.VocabularyScore}, Ct={answer.ContentScore}, Overall={answer.OverallScore}");
            }

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

        public async Task<bool> SaveReadingAnswer(ReadingAnswerRequestDTO model)
        {
            try
            {
                var option = await _context.Options
                    .Include(o => o.Question)
                    .FirstOrDefaultAsync(o => o.OptionId == model.SelectedOptionId);
                if (option == null || model.ExamAttemptId <= 0 || model.QuestionId <= 0 || option.QuestionId != model.QuestionId)
                {
                    throw new KeyNotFoundException($"Model invalid");
                }
                var answer = new DataLayer.Models.UserAnswerMultipleChoice()
                {
                    AttemptID = model.ExamAttemptId,
                    QuestionId = model.QuestionId,
                    SelectedOptionId = model.SelectedOptionId,
                    IsCorrect = option.IsCorrect ?? false,
                    Score = (option.IsCorrect ?? false) ? (option.Question?.ScoreWeight ?? 0) : 0
                };
                await _context.UserAnswerMultipleChoices.AddAsync(answer);
                return await _context.SaveChangesAsync() > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SaveWritingAnswer(WritingAnswerRequestDTO model)
        {
            try
            {
                var answer = new DataLayer.Models.UserAnswerWriting()
                {
                    AttemptID = model.AttemptID,
                    QuestionId = model.QuestionId,
                    UserAnswerContent = model.UserAnswerContent,
                    FeedbackFromAI = model.FeedbackFromAI
                };
                await _context.UserAnswerWritings.AddAsync(answer);
                return await _context.SaveChangesAsync() > 0;
            }
            catch
            {
                return false;
            }
        }

    }
}