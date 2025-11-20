using DataLayer.Models;
using DataLayer.DTOs.MockTest;
using RepositoryLayer.MockTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataLayer.DTOs.Exam;

namespace ServiceLayer.MockTest
{
    public class MockTestService : IMockTestService
    {
        private readonly IMockTestRepository _mockTestRepository;

        public MockTestService(IMockTestRepository mockTestRepository)
        {
            _mockTestRepository = mockTestRepository;
        }

        public async Task<List<ExamPartDTO>> GetMocktestAsync()
        {
            int[] examPartids = new int[] {1,2,3,4,5,6,7,8,9,10,11,12,13,14,15 };
            return await _mockTestRepository.GetMocktestAsync(examPartids);
        }
    }
}
