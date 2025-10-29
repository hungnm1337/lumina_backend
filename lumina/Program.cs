using DataLayer.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RepositoryLayer.Event;
using ServiceLayer.Auth;
using ServiceLayer.Email;
using RepositoryLayer.Leaderboard;
using ServiceLayer.Leaderboard;
using ServiceLayer.Event;
using RepositoryLayer;
using RepositoryLayer.Exam;
using RepositoryLayer.Import;
using RepositoryLayer.Questions;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer.User;
using ServiceLayer.Article;
using ServiceLayer.Configs;
using ServiceLayer.Exam;
using ServiceLayer.Exam.Writting;
using ServiceLayer.Import;
using ServiceLayer.Questions;
using ServiceLayer.Speech;
using ServiceLayer.UploadFile;
using ServiceLayer.User;
using ServiceLayer.Vocabulary;
using System.Text;
using RepositoryLayer.Slide;
using ServiceLayer.Slide;
using OfficeOpenXml;
using DataLayer.DTOs;
using ServiceLayer.ExamGenerationAI;
using ServiceLayer.ExamGenerationAI.Mappers;
using RepositoryLayer.UserNote;
using ServiceLayer.UserNote;
using ServiceLayer.Exam.Speaking;
using ServiceLayer.Exam.Listening;
using ServiceLayer.Exam.Reading;
using RepositoryLayer.Exam.ExamAttempt;
using RepositoryLayer.Exam.Writting;
using ServiceLayer.UserNoteAI;

namespace lumina
{
    public class Program
    {
        public static void Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddDbContext<LuminaSystemContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddHttpClient<ImageCaptioningService>(client =>
            {
                client.BaseAddress = new Uri("http://localhost:5000");

                client.Timeout = TimeSpan.FromSeconds(30);

            });
            builder.Services.Configure<AzureSpeechSettings>(builder.Configuration.GetSection("AzureSpeechSettings"));
            builder.Services.Configure<AzureSpeechSettings>(
    builder.Configuration.GetSection("AzureSpeech"));
            builder.Services.Configure<GeminiOptions>(
    builder.Configuration.GetSection("GeminiAI"));

            builder.Services.AddScoped<IAzureSpeechService, AzureSpeechService>();

            builder.Services.AddScoped<ISpeakingScoringService, SpeakingScoringService>();
            builder.Services.AddScoped<IListeningService, ListeningService>();
            builder.Services.AddScoped<IReadingService, ReadingService>();
            builder.Services.AddScoped<IRoleRepository, RoleRepository>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IRoleService, RoleService>();
            builder.Services.AddScoped<IPackageRepository, PackageRepository>();
            builder.Services.AddScoped<IPackageService, PackageService>();

            builder.Services.AddScoped<RepositoryLayer.Exam.ExamAttempt.IExamAttemptRepository,RepositoryLayer.Exam.ExamAttempt.ExamAttemptRepository > ();
            builder.Services.AddScoped<ServiceLayer.Exam.ExamAttempt.IExamAttemptService, ServiceLayer.Exam.ExamAttempt.ExamAttemptService>();

            builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
            
            // Auth services - Tuân thủ SOLID principles
            builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>(); // Service xử lý Google authentication
            builder.Services.AddScoped<IAuthService, AuthService>(); // Service xử lý authentication chính
            builder.Services.AddScoped<IPasswordResetService, PasswordResetService>(); // Service xử lý reset password
            
            builder.Services.AddScoped<IUploadService, UploadService>();
            builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
            builder.Services.AddScoped<IEventService,EventService>();
            builder.Services.AddScoped<IEventRepository, EventRepository>();
            builder.Services.AddScoped<ISlideService, SlideService>();
            builder.Services.AddScoped<ISlideRepository, SlideRepository>();
            builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
            builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();

            builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
            builder.Services.AddScoped<IQuestionService, QuestionService>();

            builder.Services.AddScoped<IImportRepository, ImportRepository>();
            builder.Services.AddScoped<IImportService, ImportService>();
            builder.Services.AddScoped<IExamRepository, ExamRepository>();
            builder.Services.AddScoped<IExamService, ExamService>();
            builder.Services.AddScoped<ILeaderboardRepository, LeaderboardRepository>();
            builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
            builder.Services.AddScoped<IExamPartRepository, ExamPartRepository>();
            builder.Services.AddScoped<IExamPartService, ExamPartService>();
            
            builder.Services.AddScoped<IUserNoteRepository,UserNoteRepository>();
            builder.Services.AddScoped<IUserNoteService, UserNoteService>();

            builder.Services.AddScoped<IVocabularyListRepository, VocabularyListRepository>();
            builder.Services.AddScoped<IVocabularyListService, VocabularyListService>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IArticleService, ArticleService>();

            builder.Services.AddScoped<IExamPartService, ExamPartService>();
            builder.Services.AddScoped<IExamPartRepository, ExamPartRepository>();
            builder.Services.AddScoped<IWrittingRepository, WrittingRepository>();
            builder.Services.AddScoped<IWritingService, WritingService>();
            builder.Services.AddScoped<IAIChatService, AIChatService>();
            builder.Services.AddScoped<IAIExamMapper, AIExamMapper>();

            builder.Services.AddHttpClient<IExamGenerationAIService, ExamGenerationAIService>("GeminiAI", c =>
            {
                c.Timeout = TimeSpan.FromMinutes(180);
            });

            builder.Services.AddScoped<ServiceLayer.TextToSpeech.ITextToSpeechService, ServiceLayer.TextToSpeech.TextToSpeechService>();



            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                        //.AllowCredentials();
                });
            });



            var jwtSection = builder.Configuration.GetSection("Jwt");
            var jwtSecret = jwtSection["SecretKey"] ?? throw new InvalidOperationException("JWT secret key is not configured.");
            var jwtIssuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("JWT issuer is not configured.");
            var jwtAudience = jwtSection["Audience"] ?? throw new InvalidOperationException("JWT audience is not configured.");

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtIssuer,
                        ValidAudience = jwtAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
                    };
                });
            builder.Services.AddHttpClient();
            builder.Services.AddAuthorization();


            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            
            // ✅ SWAGGER WITH JWT AUTHENTICATION
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Lumina TOEIC API",
                    Version = "v1",
                    Description = "API for Lumina TOEIC Learning Platform"
                });

                // Define JWT security scheme
                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n" +
                                  "Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\n" +
                                  "Example: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\""
                });

                // Require JWT for all endpoints
                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });




            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseCors();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}


