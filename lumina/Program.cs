using DataLayer.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RepositoryLayer.Event;
using ServiceLayer.Auth;
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
using ServiceLayer.Chat;
using ServiceLayer.Email;
using ServiceLayer.Exam.Listening;
using ServiceLayer.Exam.Reading;
using RepositoryLayer.Exam.ExamAttempt;
using RepositoryLayer.Exam.Writting;
using RepositoryLayer.Statistic;
using ServiceLayer.Statistic;
using ServiceLayer.UserNoteAI;
using ServiceLayer.Analytics;
using ServiceLayer.Streak;
using Hangfire;
using Hangfire.SqlServer;
using lumina.Filters;
using RepositoryLayer.Streak;

namespace lumina
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ========================================
            // 1. DATABASE
            // ========================================
            builder.Services.AddDbContext<LuminaSystemContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // ========================================
            // 2. HTTP CLIENTS
            // ========================================
            builder.Services.AddHttpClient<ImageCaptioningService>(client =>
            {
                client.BaseAddress = new Uri("http://localhost:5000");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            builder.Services.AddHttpClient<IExamGenerationAIService, ExamGenerationAIService>("OpenAI", c =>
            {
                c.Timeout = TimeSpan.FromMinutes(10); // ✅ Timeout cho OpenAI
                c.MaxResponseContentBufferSize = 10 * 1024 * 1024; // 10 MB
            });

            builder.Services.AddHttpClient();

            // ========================================
            // 3. CONFIGURATION OPTIONS
            // ========================================
            builder.Services.Configure<AzureSpeechSettings>(builder.Configuration.GetSection("AzureSpeechSettings"));
            builder.Services.Configure<AzureSpeechSettings>(builder.Configuration.GetSection("AzureSpeech"));
            builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAI"));

            // ========================================
            // 4. SERVICES & REPOSITORIES (DI)
            // ========================================
            
            // Azure & Speech Services
            builder.Services.AddScoped<IAzureSpeechService, AzureSpeechService>();
            builder.Services.AddScoped<ServiceLayer.TextToSpeech.ITextToSpeechService, ServiceLayer.TextToSpeech.TextToSpeechService>();

            // Scoring Services
            builder.Services.AddScoped<IScoringWeightService, ScoringWeightService>();
            builder.Services.AddScoped<ISpeakingScoringService, SpeakingScoringService>();
            builder.Services.AddScoped<IListeningService, ListeningService>();
            builder.Services.AddScoped<IReadingService, ReadingService>();

            // User & Role Services
            builder.Services.AddScoped<IRoleRepository, RoleRepository>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IRoleService, RoleService>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();

            // Package Services
            builder.Services.AddScoped<IPackageRepository, PackageRepository>();
            builder.Services.AddScoped<IPackageService, PackageService>();

            // Exam Attempt Services
            builder.Services.AddScoped<RepositoryLayer.Exam.ExamAttempt.IExamAttemptRepository, RepositoryLayer.Exam.ExamAttempt.ExamAttemptRepository>();
            builder.Services.AddScoped<ServiceLayer.Exam.ExamAttempt.IExamAttemptService, ServiceLayer.Exam.ExamAttempt.ExamAttemptService>();

            // Auth Services
            builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
            builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();

            // Upload & Email Services
            builder.Services.AddScoped<IUploadService, UploadService>();
            builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();

            // Event & Slide Services
            builder.Services.AddScoped<IEventService, EventService>();
            builder.Services.AddScoped<IEventRepository, EventRepository>();
            builder.Services.AddScoped<ISlideService, SlideService>();
            builder.Services.AddScoped<ISlideRepository, SlideRepository>();

            // Article Services
            builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
            builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
            builder.Services.AddScoped<IArticleService, ArticleService>();

            // Question Services
            builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
            builder.Services.AddScoped<IQuestionService, QuestionService>();

            // Import Services
            builder.Services.AddScoped<IImportRepository, ImportRepository>();
            builder.Services.AddScoped<IImportService, ImportService>();

            // Exam Services
            builder.Services.AddScoped<IExamRepository, ExamRepository>();
            builder.Services.AddScoped<IExamService, ExamService>();
            builder.Services.AddScoped<IExamPartRepository, ExamPartRepository>();
            builder.Services.AddScoped<IExamPartService, ExamPartService>();

            // Leaderboard Services
            builder.Services.AddScoped<ILeaderboardRepository, LeaderboardRepository>();
            builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();

            // User Note Services
            builder.Services.AddScoped<IUserNoteRepository, UserNoteRepository>();
            builder.Services.AddScoped<IUserNoteService, UserNoteService>();

            // Vocabulary Services
            builder.Services.AddScoped<IVocabularyListRepository, VocabularyListRepository>();
            builder.Services.AddScoped<IVocabularyListService, VocabularyListService>();
            builder.Services.AddScoped<ISpacedRepetitionService, SpacedRepetitionService>();

            // Writing Services
            builder.Services.AddScoped<IWrittingRepository, WrittingRepository>();
            builder.Services.AddScoped<IWritingService, WritingService>();

            // Chat Services
            builder.Services.AddScoped<IAIChatService, AIChatService>();
            builder.Services.AddScoped<IChatService, ChatService>();

            // AI & Mapper Services
            builder.Services.AddScoped<IAIExamMapper, AIExamMapper>();

            // Statistic Services
            builder.Services.AddScoped<IStatisticRepository, StatisticRepository>();
            builder.Services.AddScoped<IStatisticService, StatisticService>();
            builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

            // Unit of Work
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            // ✅ Streak Services
            builder.Services.AddScoped<IStreakRepository, StreakRepository>();
            builder.Services.AddScoped<IStreakService, StreakService>();
            builder.Services.AddScoped<StreakBackgroundJob>();
            builder.Services.AddScoped<StreakReminderJob>();

            // ✅ Quota and Payment services
            builder.Services.AddScoped<RepositoryLayer.Quota.IQuotaRepository, RepositoryLayer.Quota.QuotaRepository>();
            builder.Services.AddScoped<ServiceLayer.Quota.IQuotaService, ServiceLayer.Quota.QuotaService>();
            builder.Services.AddScoped<ServiceLayer.Payment.IPayOSService, ServiceLayer.Payment.PayOSService>();
            builder.Services.AddScoped<ServiceLayer.Subscription.ISubscriptionService, ServiceLayer.Subscription.SubscriptionService>();

            // ========================================
            // 5. HANGFIRE (TRƯỚC builder.Build())
            // ========================================
            builder.Services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    new SqlServerStorageOptions
                    {
                        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                        QueuePollInterval = TimeSpan.Zero,
                        UseRecommendedIsolationLevel = true,
                        DisableGlobalLocks = true,
                        SchemaName = "Hangfire"
                    }
                )
            );

            builder.Services.AddHangfireServer(options =>
            {
                options.WorkerCount = 2;
                options.ServerName = "LuminaStreakServer";
            });

            // ========================================
            // 6. CORS
            // ========================================
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            // ========================================
            // 7. JWT AUTHENTICATION
            // ========================================
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

            builder.Services.AddAuthorization();

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                });
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Lumina TOEIC API",
                    Version = "v1",
                    Description = "API for Lumina TOEIC Learning Platform"
                });

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

                options.CustomSchemaIds(type => type.FullName);
            });

            // ========================================
            // ✅ BUILD APP (ĐIỂM CHIA)
            // ========================================
            var app = builder.Build();

            // ========================================
            // 9. MIDDLEWARE
            // ========================================
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();

            // ========================================
            // 10. HANGFIRE DASHBOARD & RECURRING JOBS
            // ========================================
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[] { new HangfireAuthorizationFilter() },
                DashboardTitle = "Lumina Streak Jobs"
            });

            //  Xóa jobs cũ để tránh catch-up
            if (app.Environment.IsDevelopment())
            {
                RecurringJob.RemoveIfExists("daily-streak-processing");
                RecurringJob.RemoveIfExists("daily-streak-reminder");
                Console.WriteLine("✅ Removed existing Hangfire jobs (Development mode)");
            }

            // Job 1: Auto-freeze/reset lúc 00:05 GMT+7
            RecurringJob.AddOrUpdate<StreakBackgroundJob>(
                "daily-streak-processing",
                job => job.ProcessDailyStreaksAsync(),
                "5 17 * * *", // 00:05 GMT+7 = 17:05 UTC
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"),
                    MisfireHandling = MisfireHandlingMode.Ignorable // ✅ THÊM
                }
            );

            // Job 2: Gửi reminder lúc 21:00 GMT+7
            RecurringJob.AddOrUpdate<StreakReminderJob>(
                "daily-streak-reminder",
                job => job.ProcessDailyRemindersAsync(),
                "0 14 * * *", // 21:00 GMT+7 = 14:00 UTC
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"),
                    MisfireHandling = MisfireHandlingMode.Ignorable // ✅ THÊM
                }
            );

            app.MapControllers();
            app.Run();
        }
    }
}


