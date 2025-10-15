using DataLayer.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Import
{
    public interface IImportService
    {
        Task ImportQuestionsFromExcelAsync(IFormFile file, int partId);

        
    }

}
