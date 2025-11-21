using DataLayer.DTOs.Exam;
using DataLayer.DTOs.ExamPart;
using DataLayer.Models;
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

        public ExamService(IExamRepository examRepository)
        {
            _examRepository = examRepository;
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
        // Lấy tháng-năm hiện tại
        // Kiểm tra xem đã tồn tại ExamSetKey này chưa
        if (await _examRepository.ExamSetKeyExistsAsync(toSetKey))
            return false; // Đã tạo rồi trong tháng này

        // Tiếp tục clone như bình thường
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
}

