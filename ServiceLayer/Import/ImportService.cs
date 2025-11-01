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

        // ✅ Dictionary fix cứng cấu hình theo PartCode
        private readonly Dictionary<string, (int RequiredPrompts, int QuestionsPerPrompt)> PartConfigurations = new()
        {
            // LISTENING
            { "LISTENING_PART1", (6, 1) },
            { "LISTENING_PART2", (25, 1) },
            { "LISTENING_PART3", (5, 3) },
            { "LISTENING_PART4", (5, 3) },
            
            // READING
            { "READING_PART5", (30, 1) },
            { "READING_PART6", (4, 4) },
            { "READING_PART7", (5, 3) },
            
            // SPEAKING
            { "SPEAKING_PART1", (2, 1) },
            { "SPEAKING_PART2", (1, 1) },
            { "SPEAKING_PART3", (3, 3) },
            { "SPEAKING_PART4", (4, 4) },
            { "SPEAKING_PART5", (1, 1) },
            
            // WRITING
            { "WRITING_PART1", (5, 1) },
            { "WRITING_PART2", (2, 1) },
            { "WRITING_PART3", (1, 1) }
        };

        public async Task ImportQuestionsFromExcelAsync(IFormFile file, int partId)
        {
            using var package = new ExcelPackage(file.OpenReadStream());
            var passageSheet = package.Workbook.Worksheets["Prompts"];
            var questionSheet = package.Workbook.Worksheets["Questions_Options"];

            if (passageSheet == null) throw new Exception("File Excel thiếu sheet 'Prompts'.");
            if (questionSheet == null) throw new Exception("File Excel thiếu sheet 'Questions_Options'.");

            await using var transaction = await _repo.BeginTransactionAsync();

            try
            {
                // ✅ Lấy ExamPart và PartCode
                var part = await _repo.GetExamPartByIdAsync(partId);
                if (part == null) throw new Exception($"ExamPart id {partId} không tồn tại.");

                string partCode = part.PartCode?.ToUpper().Trim();
                if (string.IsNullOrEmpty(partCode))
                    throw new Exception($"ExamPart id {partId} không có PartCode.");

                // ✅ Kiểm tra PartCode có trong cấu hình không
                if (!PartConfigurations.TryGetValue(partCode, out var config))
                    throw new Exception($"PartCode '{partCode}' không được hỗ trợ import. Vui lòng kiểm tra lại.");

                int requiredPrompts = config.RequiredPrompts;
                int questionsPerPrompt = config.QuestionsPerPrompt;
                int totalRequiredQuestions = requiredPrompts * questionsPerPrompt;

                // ✅ Đếm số Prompts trong file Excel
                int promptCountInExcel = 0;
                for (int row = 2; row <= passageSheet.Dimension.End.Row; row++)
                {
                    var passageNumber = passageSheet.Cells[row, 1].Text.Trim();
                    if (!string.IsNullOrEmpty(passageNumber))
                        promptCountInExcel++;
                }

                // ✅ Validate số lượng Prompts
                if (promptCountInExcel != requiredPrompts)
                    throw new Exception($"❌ PartCode '{partCode}' yêu cầu đúng {requiredPrompts} prompts, nhưng file Excel có {promptCountInExcel} prompts.");

                // ✅ Đếm số Questions trong file Excel
                int questionCountInExcel = questionSheet.Dimension.End.Row - 1;

                // ✅ Validate số lượng Questions
                if (questionCountInExcel != totalRequiredQuestions)
                    throw new Exception($"❌ PartCode '{partCode}' yêu cầu đúng {totalRequiredQuestions} questions ({requiredPrompts} prompts × {questionsPerPrompt} questions/prompt), nhưng file Excel có {questionCountInExcel} questions.");

                // ✅ Kiểm tra số câu hỏi hiện có trong DB
                var existingQuestions = await _repo.GetQuestionsByPartIdAsync(partId);
                if (existingQuestions.Any())
                    throw new Exception($"❌ ExamPart '{partCode}' (id {partId}) đã có {existingQuestions.Count()} câu hỏi. Vui lòng xóa hết trước khi import.");

                var passagePromptCache = new Dictionary<string, Prompt>();
                var validSkills = new HashSet<string> { "listening", "reading", "speaking", "writing" };

                // ✅ Import Prompts
                for (int row = 2; row <= passageSheet.Dimension.End.Row; row++)
                {
                    var passageNumber = passageSheet.Cells[row, 1].Text.Trim();
                    var title = passageSheet.Cells[row, 2].Text.Trim();
                    var content = passageSheet.Cells[row, 3].Text.Trim();
                    var skill = passageSheet.Cells[row, 4].Text.Trim();
                    var referenceImageUrl = passageSheet.Cells[row, 5].Text.Trim();
                    var referenceAudioUrl = passageSheet.Cells[row, 6].Text.Trim();

                    if (string.IsNullOrWhiteSpace(passageNumber))
                        throw new Exception($"Prompts: Dòng {row} thiếu PassageNumber!");
                    if (string.IsNullOrWhiteSpace(title))
                        throw new Exception($"Prompts: Dòng {row} thiếu Title!");
                    if (string.IsNullOrWhiteSpace(content))
                        throw new Exception($"Prompts: Dòng {row} thiếu ContentText!");
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

                // ✅ Validate số câu hỏi cho mỗi Prompt
                var questionsPerPromptCount = new Dictionary<string, int>();

                for (int row = 2; row <= questionSheet.Dimension.End.Row; row++)
                {
                    var passageNumber = questionSheet.Cells[row, 1].Text.Trim();
                    
                    if (!questionsPerPromptCount.ContainsKey(passageNumber))
                        questionsPerPromptCount[passageNumber] = 0;
                    
                    questionsPerPromptCount[passageNumber]++;
                }

                // ✅ Kiểm tra mỗi prompt có đúng số câu hỏi không
                foreach (var kvp in questionsPerPromptCount)
                {
                    if (kvp.Value != questionsPerPrompt)
                        throw new Exception($"❌ Prompt '{kvp.Key}' phải có đúng {questionsPerPrompt} questions, nhưng có {kvp.Value} questions.");
                }

                // ✅ Import Questions với QuestionNumber tuần tự
                int currentQuestionNumber = 1;

                for (int row = 2; row <= questionSheet.Dimension.End.Row; row++)
                {
                    var passageNumber = questionSheet.Cells[row, 1].Text.Trim();
                    if (!passagePromptCache.TryGetValue(passageNumber, out var prompt))
                        throw new Exception($"Không tìm thấy Prompt tương ứng PassageNumber '{passageNumber}' ở dòng {row}");

                    var stemText = questionSheet.Cells[row, 2].Text.Trim();
                    if (string.IsNullOrWhiteSpace(stemText))
                        throw new Exception($"Questions_Options: Dòng {row} thiếu nội dung câu hỏi (StemText)!");

                    if (!int.TryParse(questionSheet.Cells[row, 3].Text.Trim(), out int scoreWeight))
                        scoreWeight = 1;
                    var questionExplain = questionSheet.Cells[row, 4].Text.Trim();
                    if (!int.TryParse(questionSheet.Cells[row, 5].Text.Trim(), out int time))
                        time = 30;

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
                        QuestionNumber = currentQuestionNumber++, // ✅ Tăng dần từ 1
                        PromptId = prompt.PromptId
                    };

                    question = await _repo.AddQuestionAsync(question);

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
