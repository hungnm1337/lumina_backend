using DataLayer.Models;
using DataLayer.DTOs.MockTest;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataLayer.DTOs.ExamPart;
using DataLayer.DTOs.Exam;

namespace RepositoryLayer.MockTest
{
    public interface IMockTestRepository
    {
        Task<List<ExamPartDTO>> GetMocktestAsync();
       
    }
}
