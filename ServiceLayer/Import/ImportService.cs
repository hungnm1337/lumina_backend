using DataLayer.Models;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using RepositoryLayer.Import;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Import
{
    public class ImportService : IImportService
    {
        private readonly IImportRepository _repo;

        public ImportService(IImportRepository repo)
        {
            _repo = repo;
            ExcelPackage.License.SetNonCommercialPersonal("TrungTuyen");
        }

        public async Task ImportQuestionsFromExcelAsync(IFormFile file, int partId)
        {
            using var package = new ExcelPackage(file.OpenReadStream());
            var passageSheet = package.Workbook.Worksheets["Prompts"];
            var questionSheet = package.Workbook.Worksheets["Questions_Options"];

            // Sửa tên sheet trong thông báo lỗi cho khớp với thực tế
            if (passageSheet == null) throw new Exception("File Excel thiếu sheet 'Prompts'.");
            if (questionSheet == null) throw new Exception("File Excel thiếu sheet 'Questions_Options'.");

            await using var transaction = await _repo.BeginTransactionAsync();

            try
            {
                var part = await _repo.GetExamPartByIdAsync(partId);
                if (part == null) throw new Exception($"ExamPart id {partId} không tồn tại.");
                var maxQuestions = part.MaxQuestions;

                var existingQuestionNumbers = (await _repo.GetQuestionsByPartIdAsync(partId))
                                                .Select(q => q.QuestionNumber).ToHashSet();

                int importCount = questionSheet.Dimension.End.Row - 1;
                if (existingQuestionNumbers.Count + importCount > maxQuestions)
                    throw new Exception($"ExamPart id {partId} chỉ cho phép tối đa {maxQuestions} câu hỏi. Hiện có {existingQuestionNumbers.Count}, định import {importCount}.");

                var availableNumbers = Enumerable.Range(1, maxQuestions).Except(existingQuestionNumbers).ToList();

                var passagePromptCache = new Dictionary<string, Prompt>();

                // Cải tiến validation cho Skill
                var validSkills = new HashSet<string> { "listening", "reading", "speaking", "writing" };

                // Đọc sheet Prompts
                for (int row = 2; row <= passageSheet.Dimension.End.Row; row++)
                {
                    var passageNumber = passageSheet.Cells[row, 1].Text.Trim();
                    var title = passageSheet.Cells[row, 2].Text.Trim();
                    var content = passageSheet.Cells[row, 3].Text.Trim();
                    var skill = passageSheet.Cells[row, 4].Text.Trim();
                    var referenceImageUrl = passageSheet.Cells[row, 5].Text.Trim();
                    var referenceAudioUrl = passageSheet.Cells[row, 6].Text.Trim();

                    // Validate các cột bắt buộc
                    if (string.IsNullOrWhiteSpace(passageNumber))
                        throw new Exception($"Prompts: Dòng {row} thiếu PassageNumber!");
                    if (string.IsNullOrWhiteSpace(title))
                        throw new Exception($"Prompts: Dòng {row} thiếu Title!");
                    if (string.IsNullOrWhiteSpace(content))
                        throw new Exception($"Prompts: Dòng {row} thiếu ContentText!");

                    // BỔ SUNG VALIDATE: Kiểm tra skill chính xác hơn
                    if (string.IsNullOrWhiteSpace(skill) || !validSkills.Contains(skill.Trim().ToLower()))
                        throw new Exception($"Prompts: Dòng {row} cột Skill không hợp lệ. Chỉ chấp nhận: listening, reading, speaking, writing.");

                    if (!passagePromptCache.ContainsKey(passageNumber))
                    {
                        var prompt = new Prompt
                        {
                            Title = title,
                            ContentText = content,
                            Skill = skill,
                            ReferenceImageUrl = referenceImageUrl,
                            ReferenceAudioUrl = referenceAudioUrl
                        };
                        prompt = await _repo.AddPromptAsync(prompt);
                        passagePromptCache[passageNumber] = prompt;
                    }
                }

                int idx = 0;
                // Đọc sheet Questions_Options
                for (int row = 2; row <= questionSheet.Dimension.End.Row; row++)
                {
                    var passageNumber = questionSheet.Cells[row, 1].Text.Trim();
                    if (!passagePromptCache.TryGetValue(passageNumber, out var prompt))
                        throw new Exception($"Không tìm thấy Prompt tương ứng PassageNumber '{passageNumber}' ở dòng {row}");

                    var stemText = questionSheet.Cells[row, 2].Text.Trim();
                    // BỔ SUNG VALIDATE: Nội dung câu hỏi không được trống
                    if (string.IsNullOrWhiteSpace(stemText))
                        throw new Exception($"Questions_Options: Dòng {row} thiếu nội dung câu hỏi (StemText)!");

                    if (!int.TryParse(questionSheet.Cells[row, 3].Text.Trim(), out int scoreWeight))
                        scoreWeight = 1;
                    var questionExplain = questionSheet.Cells[row, 4].Text.Trim();
                    if (!int.TryParse(questionSheet.Cells[row, 5].Text.Trim(), out int time))
                        time = 30;

                    if (idx >= availableNumbers.Count)
                        throw new Exception($"ExamPart id {partId} đã đầy không còn slot QuestionNumber để thêm câu hỏi dòng {row}");

                    int questionNumber = availableNumbers[idx++];
                    string skillLower = prompt.Skill?.ToLower() ?? "";

                    var correctOptionsStr = questionSheet.Cells[row, 6].Text.Trim();
                    var correctIndexes = correctOptionsStr?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                             .Select(s => int.TryParse(s.Trim(), out int i) ? i : -1)
                                             .Where(i => i > 0)
                                             .ToList() ?? new List<int>();

                    var options = new List<Option>();
                    for (int optIndex = 1; optIndex <= 4; optIndex++)
                    {
                        int colContent = 6 + optIndex;
                        var optionContent = questionSheet.Cells[row, colContent].Text.Trim();
                        if (!string.IsNullOrEmpty(optionContent))
                        {
                            options.Add(new Option
                            {
                                Content = optionContent,
                                IsCorrect = correctIndexes.Contains(optIndex)
                            });
                        }
                    }

                    // BỔ SUNG VALIDATE: Logic cho câu hỏi trắc nghiệm
                    if (skillLower == "reading" || skillLower == "listening")
                    {
                        if (options.Count < 2)
                            throw new Exception($"Questions_Options: Dòng {row}: Câu hỏi trắc nghiệm phải có ít nhất 2 lựa chọn.");

                        if (!correctIndexes.Any())
                            throw new Exception($"Questions_Options: Dòng {row}: Câu hỏi trắc nghiệm phải có ít nhất 1 đáp án đúng (cột CorrectOptions).");

                        if (correctIndexes.Any(index => index > options.Count))
                            throw new Exception($"Questions_Options: Dòng {row}: Cột CorrectOptions chứa đáp án '{correctIndexes.First(index => index > options.Count)}' không tồn tại (chỉ có {options.Count} lựa chọn được cung cấp).");
                    }

                    string questionType;
                    if (skillLower == "speaking")
                        questionType = "SPEAKING";
                    else if (skillLower == "writing")
                        questionType = "WRITING";
                    else
                        questionType = correctIndexes.Count > 1 ? "MULTIPLE_CHOICE" : "SINGLE_CHOICE";

                    var question = new Question
                    {
                        PartId = partId,
                        QuestionType = questionType,
                        StemText = stemText,
                        ScoreWeight = scoreWeight,
                        QuestionExplain = questionExplain,
                        Time = time,
                        QuestionNumber = questionNumber,
                        PromptId = prompt.PromptId
                    };

                    question = await _repo.AddQuestionAsync(question);

                    // Gán QuestionId cho các options sau khi đã có question
                    options.ForEach(opt => opt.QuestionId = question.QuestionId);

                    if (options.Any() && (skillLower == "reading" || skillLower == "listening"))
                        await _repo.AddOptionsAsync(options);
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


    }
}
