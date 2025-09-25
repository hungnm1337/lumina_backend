using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using System.IO;
using Microsoft.Extensions.Configuration;
using DataLayer.DTOs;
using Microsoft.AspNetCore.Http;

namespace Services.Upload
{
    public class UploadService : IUploadService
    {
        private readonly CloudinaryDotNet.Cloudinary _cloudinary;

        public UploadService(IConfiguration configuration)
        {
            var account = new Account(
                configuration["CloudinarySettings:CloudName"],
                configuration["CloudinarySettings:ApiKey"],
                configuration["CloudinarySettings:ApiSecret"]);

            _cloudinary = new CloudinaryDotNet.Cloudinary(account);
        }

        public async Task<DataLayer.DTOs.UploadResultDTO> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File không hợp lệ.");
            }

            // Nếu là file âm thanh (MP3), ta sẽ dùng VideoUploadParams
            if (file.ContentType.Contains("audio"))
            {
                var audioParams = new VideoUploadParams
                {
                    File = new FileDescription(file.FileName, file.OpenReadStream()),
                    PublicId = $"music_app/audio/{Path.GetFileNameWithoutExtension(file.FileName)}_{Guid.NewGuid()}",
                };
                var videoUploadResult = await _cloudinary.UploadAsync(audioParams);
                if (videoUploadResult.Error != null)
                {
                    throw new Exception(videoUploadResult.Error.Message);
                }
                return new DataLayer.DTOs.UploadResultDTO { Url = videoUploadResult.SecureUrl.ToString(), PublicId = videoUploadResult.PublicId };
            }
            else // Mặc định xử lý cho file ảnh
            {
                var imageParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, file.OpenReadStream()),
                    PublicId = $"music_app/images/{Path.GetFileNameWithoutExtension(file.FileName)}_{Guid.NewGuid()}",
                    Transformation = new Transformation().Height(500).Width(500).Crop("fill").Gravity("face")
                };
                var imageUploadResult = await _cloudinary.UploadAsync(imageParams);
                if (imageUploadResult.Error != null)
                {
                    throw new Exception(imageUploadResult.Error.Message);
                }
                return new DataLayer.DTOs.UploadResultDTO { Url = imageUploadResult.SecureUrl.ToString(), PublicId = imageUploadResult.PublicId };
            }
        }
    }
}
