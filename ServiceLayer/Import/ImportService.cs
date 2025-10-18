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
           /* if (file == null || file.Length == 0)
                throw new ArgumentException("File không được để trống.");

            using var package = new ExcelPackage(file.OpenReadStream());

            var passageSheet = package.Workbook.Worksheets["Passages_Prompts"];
            var questionSheet = package.Workbook.Worksheets["Questions_Options"];

            if (passageSheet == null)
                throw new Exception("File Excel thiếu sheet 'Passages_Prompts'.");

            if (questionSheet == null)
                throw new Exception("File Excel thiếu sheet 'Questions_Options'.");

            await using var transaction = await _repo.BeginTransactionAsync();

            try
            {
                var part = await _repo.GetExamPartByIdAsync(partId);
                if (part == null)
                    throw new Exception($"ExamPart id {partId} không tồn tại.");
                var maxQuestions = part.MaxQuestions;

                // Lấy các QuestionNumber đã dùng từ DB cho partId
                var existingQuestionNumbers = (await _repo.GetQuestionsByPartIdAsync(partId))
                    .Select(q => q.QuestionNumber).ToHashSet();

                int importCount = questionSheet.Dimension.End.Row - 1; // trừ header
                if (existingQuestionNumbers.Count + importCount > maxQuestions)
                    throw new Exception($"ExamPart id {partId} chỉ cho phép tối đa {maxQuestions} câu hỏi. Hiện đã có {existingQuestionNumbers.Count}, bạn đang định import thêm {importCount}.");

                var availableNumbers = Enumerable.Range(1, maxQuestions).Except(existingQuestionNumbers).ToList();

                var passagePromptCache = new Dictionary<string, Prompt>();

                // Đọc Passages_Prompts sheet
                for (int row = 2; row <= passageSheet.Dimension.End.Row; row++)
                {
                    var passageNumber = passageSheet.Cells[row, 1].Text.Trim();
                    var title = passageSheet.Cells[row, 2].Text.Trim();
                    var content = passageSheet.Cells[row, 3].Text.Trim();
                    var promptText = passageSheet.Cells[row, 4].Text.Trim();
                    var skill = passageSheet.Cells[row, 5].Text.Trim();
                    var referenceImageUrl = passageSheet.Cells[row, 6].Text.Trim();
                    var referenceAudioUrl = passageSheet.Cells[row, 7].Text.Trim();

                    if (!passagePromptCache.ContainsKey(passageNumber))
                    {
                        var passage = new Passage
                        {
                            Title = title,
                            ContentText = content
                        };
                        passage = await _repo.AddPassageAsync(passage);

                        var prompt = new Prompt
                        {
                            PassageId = passage.PassageId,
                            PromptText = promptText,
                            Skill = skill,
                            ReferenceImageUrl = referenceImageUrl,
                            ReferenceAudioUrl = referenceAudioUrl
                        };
                        prompt = await _repo.AddPromptAsync(prompt);

                        passagePromptCache[passageNumber] = prompt;
                    }
                }

                int idx = 0; // để lấy slot QuestionNumber còn trống
                for (int row = 2; row <= questionSheet.Dimension.End.Row; row++)
                {
                    var passageNumber = questionSheet.Cells[row, 1].Text.Trim();
                    if (!passagePromptCache.TryGetValue(passageNumber, out var prompt))
                        throw new Exception($"Không tìm thấy Prompt tương ứng PassageNumber {passageNumber} ở dòng {row}");

                    var questionType = questionSheet.Cells[row, 2].Text.Trim();
                    var stemText = questionSheet.Cells[row, 3].Text.Trim();

                    if (!int.TryParse(questionSheet.Cells[row, 4].Text.Trim(), out int scoreWeight))
                        scoreWeight = 1;

                    var questionExplain = questionSheet.Cells[row, 5].Text.Trim();

                    if (!int.TryParse(questionSheet.Cells[row, 6].Text.Trim(), out int time))
                        time = 30;

                    if (idx >= availableNumbers.Count)
                        throw new Exception($"ExamPart id {partId} đã đầy không còn slot QuestionNumber để thêm câu hỏi mới ở dòng {row}");

                    var questionNumber = availableNumbers[idx];
                    idx++;

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

                    if (prompt.Skill != "Listening" && prompt.Skill != "Speaking")
                    {
                        var options = new List<Option>();
                        for (int col = 8; col <= 15; col += 2)
                        {
                            var optionContent = questionSheet.Cells[row, col].Text.Trim();
                            var isCorrectStr = questionSheet.Cells[row, col + 1].Text.Trim();
                            if (!string.IsNullOrEmpty(optionContent) && bool.TryParse(isCorrectStr, out bool isCorrect))
                            {
                                options.Add(new Option
                                {
                                    QuestionId = question.QuestionId,
                                    Content = optionContent,
                                    IsCorrect = isCorrect
                                });
                            }
                        }
                        if (options.Any())
                            await _repo.AddOptionsAsync(options);
                    }
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }*/
        }

    }
}
