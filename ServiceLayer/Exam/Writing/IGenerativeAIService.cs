using System.Threading.Tasks;

namespace ServiceLayer.Exam.Writting
{
    public interface IGenerativeAIService
    {
        Task<string> GenerateContentAsync(string prompt);
    }
}

