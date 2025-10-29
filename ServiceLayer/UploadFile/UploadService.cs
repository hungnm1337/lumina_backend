﻿using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DataLayer.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ServiceLayer.UploadFile
{
    public class UploadService : IUploadService
    {
        private readonly Cloudinary _cloudinary;

        public UploadService(IConfiguration configuration)
        {
            var account = new Account(
                configuration["CloudinarySettings:CloudName"],
                configuration["CloudinarySettings:ApiKey"],
                configuration["CloudinarySettings:ApiSecret"]);

            _cloudinary = new Cloudinary(account);
        }

        public async Task<UploadResultDTO> UploadFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File không hợp lệ.");
            }

            UploadResult uploadResult;

            if (file.ContentType.StartsWith("audio/"))
            {
                var audioParams = new VideoUploadParams()
                {
                    File = new FileDescription(file.FileName, file.OpenReadStream()),
                    PublicId = $"lumina/audio/{Path.GetFileNameWithoutExtension(file.FileName)}_{Guid.NewGuid()}",
                };

                uploadResult = await _cloudinary.UploadAsync(audioParams);
            }
            else // Mặc định xử lý cho file ảnh
            {
                var imageParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, file.OpenReadStream()),
                    PublicId = $"music_app/images/{Path.GetFileNameWithoutExtension(file.FileName)}_{Guid.NewGuid()}",
                    // Không đặt Transformation để giữ nguyên kích thước gốc ảnh
                };

                var imageUploadResult = await _cloudinary.UploadAsync(imageParams);
                if (imageUploadResult.Error != null)
                {
                    throw new Exception(imageUploadResult.Error.Message);
                }
                return new DataLayer.DTOs.UploadResultDTO { Url = imageUploadResult.SecureUrl.ToString(), PublicId = imageUploadResult.PublicId };
            }

            if (uploadResult.Error != null)
            {
                throw new Exception(uploadResult.Error.Message);
            }

            // Cách trả về kết quả này là hoàn toàn chính xác
            return new UploadResultDTO { Url = uploadResult.SecureUrl.ToString(), PublicId = uploadResult.PublicId };
        }

        // ✅ Hàm mới: Upload từ URL (không cần file)
        public async Task<UploadResultDTO> UploadFromUrlAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
                throw new ArgumentException("URL không hợp lệ.");

            // Kiểm tra đuôi file để xác định loại upload
            bool isAudio = fileUrl.EndsWith(".mp3") || fileUrl.EndsWith(".wav") || fileUrl.EndsWith(".m4a");

            UploadResult uploadResult;

            if (isAudio)
            {
                var audioParams = new VideoUploadParams
                {
                    File = new FileDescription(fileUrl),
                    PublicId = $"lumina/audio_{Guid.NewGuid()}",
                };
                uploadResult = await _cloudinary.UploadAsync(audioParams);
            }
            else
            {
                var imageParams = new ImageUploadParams
                {
                    File = new FileDescription(fileUrl),
                    PublicId = $"music_app/images_{Guid.NewGuid()}",
                };
                uploadResult = await _cloudinary.UploadAsync(imageParams);
            }

            if (uploadResult.Error != null)
                throw new Exception(uploadResult.Error.Message);

            return new UploadResultDTO
            {
                Url = uploadResult.SecureUrl.ToString(),
                PublicId = uploadResult.PublicId
            };
        }
    }
}