using DataLayer.DTOs.Exam;
using DataLayer.DTOs.ExamPart;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using RepositoryLayer.Exam;
using ServiceLayer.Exam;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


    public class ExamService : IExamService
    {
        private readonly IExamRepository _examRepository;
        private readonly LuminaSystemContext _context;

        public ExamService(IExamRepository examRepository, LuminaSystemContext context)
        {
            _examRepository = examRepository;
            _context = context;
        }

        public async Task<List<ExamDTO>> GetAllExams(string? examType = null, string? partCode = null)
        {
            return await _examRepository.GetAllExams(examType, partCode);
        }

        public async Task<ExamDTO> GetExamDetailAndExamPartByExamID(int examId)
        {
            return await _examRepository.GetExamDetailAndExamPartByExamID(examId);
        }

        public async Task<ExamPartDTO> GetExamPartDetailAndQuestionByExamPartID(int partId)
        {
            return await _examRepository.GetExamPartDetailAndQuestionByExamPartID(partId);
        }

    public async Task<bool> CreateExamFormatAsync(string fromSetKey, string toSetKey, int createdBy)
    {
        if (await _examRepository.ExamSetKeyExistsAsync(toSetKey))
            return false; // Đã tạo rồi trong tháng này

        var sourceExams = await _examRepository.GetExamsBySetKeyAsync(fromSetKey);
        if (!sourceExams.Any()) return false;

        var newExams = sourceExams.Select(e => new Exam
        {
            ExamType = e.ExamType,
            Name = $"{e.Name} TOEIC {toSetKey}",
            Description = e.Description,
            IsActive = false,
            CreatedBy = createdBy,
            CreatedAt = DateTime.Now,
            ExamSetKey = toSetKey
        }).ToList();

        await _examRepository.InsertExamsAsync(newExams);

        var sourceExamIds = sourceExams.Select(e => e.ExamId).ToList();
        var sourceParts = await _examRepository.GetExamPartsByExamIdsAsync(sourceExamIds);

        var mapExamId = sourceExams
            .Select((e, idx) => new { Old = e.ExamId, New = newExams[idx].ExamId })
            .ToDictionary(x => x.Old, x => x.New);

        var newParts = sourceParts.Select(p => new ExamPart
        {
            ExamId = mapExamId[p.ExamId],
            PartCode = p.PartCode,
            Title = p.Title,
            OrderIndex = p.OrderIndex,
            MaxQuestions = p.MaxQuestions
        }).ToList();

        await _examRepository.InsertExamPartsAsync(newParts);
        return true;
    }

    public async Task<List<ExamGroupBySetKeyDto>> GetExamsGroupedBySetKeyAsync()
    {
        return await _examRepository.GetExamsGroupedBySetKeyAsync();
    }

    public async Task<bool> ToggleExamStatusAsync(int examId)
    {
        return await _examRepository.ToggleExamStatusAsync(examId);
    }

    public async Task<List<ExamCompletionStatusDTO>> GetUserExamCompletionStatusesAsync(int userId)
    {
        var exams = await _context.Exams
            .Include(e => e.ExamParts)
            .Where(e => e.IsActive == true)
            .ToListAsync();

        var completionStatuses = new List<ExamCompletionStatusDTO>();

        foreach (var exam in exams)
        {
            var partStatuses = await GetPartCompletionStatusAsync(userId, exam.ExamId);

            completionStatuses.Add(new ExamCompletionStatusDTO
            {
                ExamId = exam.ExamId,
                TotalPartsCount = exam.ExamParts.Count,
                CompletedPartsCount = partStatuses.Count(p => p.IsCompleted),
                IsCompleted = exam.ExamParts.Count > 0 && partStatuses.All(p => p.IsCompleted),
                Parts = partStatuses
            });
        }

        return completionStatuses;
    }

    public async Task<ExamCompletionStatusDTO> GetExamCompletionStatusAsync(int userId, int examId)
    {
        var exam = await _context.Exams
            .Include(e => e.ExamParts)
            .FirstOrDefaultAsync(e => e.ExamId == examId);

        if (exam == null)
        {
            return new ExamCompletionStatusDTO
            {
                ExamId = examId,
                IsCompleted = false,
                CompletedPartsCount = 0,
                TotalPartsCount = 0,
                Parts = new List<PartCompletionStatusDTO>()
            };
        }

        var partStatuses = await GetPartCompletionStatusAsync(userId, examId);

        return new ExamCompletionStatusDTO
        {
            ExamId = exam.ExamId,
            TotalPartsCount = exam.ExamParts.Count,
            CompletedPartsCount = partStatuses.Count(p => p.IsCompleted),
            IsCompleted = exam.ExamParts.Count > 0 && partStatuses.All(p => p.IsCompleted),
            Parts = partStatuses
        };
    }
    public async Task<List<PartCompletionStatusDTO>> GetPartCompletionStatusAsync(int userId, int examId)
    {
        var parts = await _context.ExamParts
            .Where(p => p.ExamId == examId)
            .OrderBy(p => p.OrderIndex)
            .ToListAsync();

        var partStatuses = new List<PartCompletionStatusDTO>();

        foreach (var part in parts)
        {
            var completedAttempts = await _context.ExamAttempts
                .Where(ea => ea.UserID == userId 
                    && ea.ExamPartId == part.PartId 
                    && ea.Status == "Completed")
                .OrderByDescending(ea => ea.EndTime)
                .ToListAsync();

            var latestAttempt = completedAttempts.FirstOrDefault();

            partStatuses.Add(new PartCompletionStatusDTO
            {
                PartId = part.PartId,
                PartCode = part.PartCode,
                IsCompleted = latestAttempt != null,
                CompletedAt = latestAttempt?.EndTime,
                Score = latestAttempt?.Score,
                AttemptCount = completedAttempts.Count
            });
        }

        return partStatuses;
    }
}

