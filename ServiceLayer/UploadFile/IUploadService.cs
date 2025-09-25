using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataLayer.DTOs;
namespace Services.Upload
{
    public interface IUploadService
    {
        Task<DataLayer.DTOs.UploadResultDTO> UploadFile(IFormFile file);

    }
}
