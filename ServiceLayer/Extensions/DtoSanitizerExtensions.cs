using DataLayer.DTOs.Exam;
using DataLayer.DTOs.Questions;
using System.Collections.Generic;
using System.Linq;

namespace ServiceLayer.Extensions
{
    /// <summary>
    /// Extension methods to sanitize DTOs by removing sensitive answer data
    /// </summary>
    public static class DtoSanitizerExtensions
    {
        /// <summary>
        /// Removes sensitive answer data from QuestionDTO for regular users
        /// </summary>
        public static QuestionDTO SanitizeForUser(this QuestionDTO question)
        {
            if (question == null) return null;

            question.SampleAnswer = null; // ❌ Remove correct answer
            question.QuestionExplain = null; // ❌ Remove explanation (shows strategy)

            // Sanitize options - remove IsCorrect flag
            if (question.Options != null)
            {
                foreach (var option in question.Options)
                {
                    option.IsCorrect = null; // ❌ Hide which option is correct
                }
            }

            return question;
        }

        /// <summary>
        /// Sanitizes a list of questions
        /// </summary>
        public static List<QuestionDTO> SanitizeForUser(this List<QuestionDTO> questions)
        {
            if (questions == null) return null;

            foreach (var question in questions)
            {
                question.SanitizeForUser();
            }

            return questions;
        }

        /// <summary>
        /// Removes sensitive data from ExamPartDTO for regular users
        /// </summary>
        public static ExamPartDTO SanitizeForUser(this ExamPartDTO examPart)
        {
            if (examPart == null) return null;

            // Sanitize all questions in this part
            examPart.Questions?.SanitizeForUser();

            return examPart;
        }

        /// <summary>
        /// Removes sensitive data from ExamDTO for regular users
        /// </summary>
        public static ExamDTO SanitizeForUser(this ExamDTO exam)
        {
            if (exam == null) return null;

            // Sanitize all parts and their questions
            if (exam.ExamParts != null)
            {
                foreach (var part in exam.ExamParts)
                {
                    part.SanitizeForUser();
                }
            }

            return exam;
        }

        /// <summary>
        /// Removes IsCorrect flag from OptionDto (QuestionView)
        /// </summary>
        public static OptionDto SanitizeForUser(this OptionDto option)
        {
            if (option == null) return null;

            option.IsCorrect = false; // ❌ Always show false for users
            return option;
        }

        /// <summary>
        /// Removes sensitive data from QuestionDto (QuestionView)
        /// </summary>
        public static QuestionDto SanitizeForUser(this QuestionDto question)
        {
            if (question == null) return null;

            question.SampleAnswer = null; // ❌ Remove correct answer
            question.QuestionExplain = null; // ❌ Remove explanation

            // Sanitize options
            if (question.Options != null)
            {
                foreach (var option in question.Options)
                {
                    option.SanitizeForUser();
                }
            }

            return question;
        }

        /// <summary>
        /// Removes sensitive data from PromptDto
        /// </summary>
        public static PromptDto SanitizeForUser(this PromptDto prompt)
        {
            if (prompt == null) return null;

            // Sanitize all questions in the prompt
            if (prompt.Questions != null)
            {
                foreach (var question in prompt.Questions)
                {
                    question.SanitizeForUser();
                }
            }

            return prompt;
        }
    }
}
