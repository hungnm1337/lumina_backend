using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace lumina.Models;

public partial class LuminaSystemContext : DbContext
{
    public LuminaSystemContext()
    {
    }

    public LuminaSystemContext(DbContextOptions<LuminaSystemContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Article> Articles { get; set; }

    public virtual DbSet<ArticleCategory> ArticleCategories { get; set; }

    public virtual DbSet<ArticleSection> ArticleSections { get; set; }

    public virtual DbSet<Event> Events { get; set; }

    public virtual DbSet<Exam> Exams { get; set; }

    public virtual DbSet<ExamAttempt> ExamAttempts { get; set; }

    public virtual DbSet<ExamPart> ExamParts { get; set; }

    public virtual DbSet<Leaderboard> Leaderboards { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Option> Options { get; set; }

    public virtual DbSet<Package> Packages { get; set; }

    public virtual DbSet<Passage> Passages { get; set; }

    public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Prompt> Prompts { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<Report> Reports { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SectionCompletion> SectionCompletions { get; set; }

    public virtual DbSet<Slide> Slides { get; set; }

    public virtual DbSet<Subscription> Subscriptions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserAnswer> UserAnswers { get; set; }

    public virtual DbSet<UserArticleProgress> UserArticleProgresses { get; set; }

    public virtual DbSet<UserLeaderboard> UserLeaderboards { get; set; }

    public virtual DbSet<UserNote> UserNotes { get; set; }

    public virtual DbSet<UserNotification> UserNotifications { get; set; }

    public virtual DbSet<UserSpacedRepetition> UserSpacedRepetitions { get; set; }

    public virtual DbSet<Vocabulary> Vocabularies { get; set; }

    public virtual DbSet<VocabularyList> VocabularyLists { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=TRUNGTUYEN;Database=LuminaSystem;User Id=sa;Password=123;Encrypt=False;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("PK__Accounts__349DA5A66E07EEBE");

            entity.HasIndex(e => e.UserId, "IX_Accounts_UserId");

            entity.HasIndex(e => new { e.AuthProvider, e.ProviderUserId }, "UQ_Accounts_AuthProvider_ProviderUserId")
                .IsUnique()
                .HasFilter("([AuthProvider] IS NOT NULL AND [ProviderUserId] IS NOT NULL)");

            entity.HasIndex(e => e.Username, "UQ__Accounts__536C85E4C923B0BE").IsUnique();

            entity.Property(e => e.AccessToken).IsUnicode(false);
            entity.Property(e => e.AuthProvider)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreateAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Create_at");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(512)
                .IsUnicode(false);
            entity.Property(e => e.ProviderUserId)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.RefreshToken).IsUnicode(false);
            entity.Property(e => e.TokenExpiresAt).HasPrecision(3);
            entity.Property(e => e.UpdateAt)
                .HasPrecision(3)
                .HasColumnName("Update_at");
            entity.Property(e => e.Username)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.User).WithMany(p => p.Accounts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Accounts_Users");
        });

        modelBuilder.Entity<Article>(entity =>
        {
            entity.HasKey(e => e.ArticleId).HasName("PK__Articles__9C6270C88BD5B813");

            entity.HasIndex(e => e.CategoryId, "IX_Articles_CategoryID");

            entity.HasIndex(e => e.CreatedBy, "IX_Articles_CreatedBy");

            entity.HasIndex(e => e.UpdatedBy, "IX_Articles_UpdatedBy");

            entity.Property(e => e.ArticleId).HasColumnName("ArticleID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Status).HasMaxLength(15);
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt).HasPrecision(3);

            entity.HasOne(d => d.Category).WithMany(p => p.Articles)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Articles_ArticleCategories");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ArticleCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Articles_CreatedBy");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.ArticleUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("FK_Articles_UpdatedBy");
        });

        modelBuilder.Entity<ArticleCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__ArticleC__19093A2B95CED098");

            entity.HasIndex(e => e.CreatedByUserId, "IX_ArticleCategories_CreatedByUserID");

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CategoryName).HasMaxLength(255);
            entity.Property(e => e.CreateAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CreatedByUserId).HasColumnName("CreatedByUserID");
            entity.Property(e => e.UpdateAt).HasPrecision(3);

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.ArticleCategories)
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("FK_ArticleCategories_Users");
        });

        modelBuilder.Entity<ArticleSection>(entity =>
        {
            entity.HasKey(e => e.SectionId).HasName("PK__ArticleS__80EF0892A9E4A151");

            entity.HasIndex(e => e.ArticleId, "IX_ArticleSections_ArticleID");

            entity.Property(e => e.SectionId).HasColumnName("SectionID");
            entity.Property(e => e.ArticleId).HasColumnName("ArticleID");
            entity.Property(e => e.SectionTitle).HasMaxLength(255);

            entity.HasOne(d => d.Article).WithMany(p => p.ArticleSections)
                .HasForeignKey(d => d.ArticleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ArticleSections_Articles");
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.EventId).HasName("PK__Events__7944C8108B79DD0A");

            entity.HasIndex(e => e.CreateBy, "IX_Events_CreateBy");

            entity.HasIndex(e => e.UpdateBy, "IX_Events_UpdateBy");

            entity.Property(e => e.CreateAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EndDate).HasPrecision(3);
            entity.Property(e => e.EventName).HasMaxLength(255);
            entity.Property(e => e.StartDate).HasPrecision(3);
            entity.Property(e => e.UpdateAt).HasPrecision(3);

            entity.HasOne(d => d.CreateByNavigation).WithMany(p => p.EventCreateByNavigations)
                .HasForeignKey(d => d.CreateBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Events_Users_Create");

            entity.HasOne(d => d.UpdateByNavigation).WithMany(p => p.EventUpdateByNavigations)
                .HasForeignKey(d => d.UpdateBy)
                .HasConstraintName("FK_Events_Users_Update");
        });

        modelBuilder.Entity<Exam>(entity =>
        {
            entity.HasKey(e => e.ExamId).HasName("PK__Exams__297521C791F6DF8D");

            entity.HasIndex(e => e.CreatedBy, "IX_Exams_CreatedBy");

            entity.HasIndex(e => e.UpdateBy, "IX_Exams_UpdateBy");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ExamType).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.UpdateAt).HasPrecision(3);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ExamCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Exams_CreatedBy");

            entity.HasOne(d => d.UpdateByNavigation).WithMany(p => p.ExamUpdateByNavigations)
                .HasForeignKey(d => d.UpdateBy)
                .HasConstraintName("FK_Exams_UpdatedBy");
        });

        modelBuilder.Entity<ExamAttempt>(entity =>
        {
            entity.HasKey(e => e.AttemptId).HasName("PK__ExamAtte__891A68864E80FEA1");

            entity.HasIndex(e => e.ExamId, "IX_ExamAttempts_ExamID");

            entity.HasIndex(e => e.UserId, "IX_ExamAttempts_UserID");

            entity.Property(e => e.AttemptId).HasColumnName("AttemptID");
            entity.Property(e => e.EndTime).HasPrecision(3);
            entity.Property(e => e.ExamId).HasColumnName("ExamID");
            entity.Property(e => e.StartTime).HasPrecision(3);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Exam).WithMany(p => p.ExamAttempts)
                .HasForeignKey(d => d.ExamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExamAttempts_Exam");

            entity.HasOne(d => d.User).WithMany(p => p.ExamAttempts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExamAttempts_Users");
        });

        modelBuilder.Entity<ExamPart>(entity =>
        {
            entity.HasKey(e => e.PartId).HasName("PK__ExamPart__7C3F0D501ED38AB6");

            entity.HasIndex(e => e.ExamId, "IX_ExamParts_ExamId");

            entity.Property(e => e.PartCode).HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasOne(d => d.Exam).WithMany(p => p.ExamParts)
                .HasForeignKey(d => d.ExamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExamParts_Exams");
        });

        modelBuilder.Entity<Leaderboard>(entity =>
        {
            entity.HasKey(e => e.LeaderboardId).HasName("PK__Leaderbo__B358A1E60B6C8F5C");

            entity.ToTable("Leaderboard");

            entity.Property(e => e.LeaderboardId).HasColumnName("LeaderboardID");
            entity.Property(e => e.EndDate).HasPrecision(3);
            entity.Property(e => e.StartDate).HasPrecision(3);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E12AAE7DD1E");

            entity.Property(e => e.Title).HasMaxLength(255);
        });

        modelBuilder.Entity<Option>(entity =>
        {
            entity.HasKey(e => e.OptionId).HasName("PK__Options__92C7A1FF9DEA13B7");

            entity.HasIndex(e => e.QuestionId, "IX_Options_QuestionId");

            entity.HasOne(d => d.Question).WithMany(p => p.Options)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Option_Questions");
        });

        modelBuilder.Entity<Package>(entity =>
        {
            entity.HasKey(e => e.PackageId).HasName("PK__Packages__322035ECD9DF94F4");

            entity.Property(e => e.PackageId).HasColumnName("PackageID");
            entity.Property(e => e.PackageName).HasMaxLength(255);
            entity.Property(e => e.Price).HasColumnType("decimal(12, 2)");
        });

        modelBuilder.Entity<Passage>(entity =>
        {
            entity.HasKey(e => e.PassageId).HasName("PK__Passages__CC0F002C41B406F1");

            entity.Property(e => e.Title).HasMaxLength(255);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_PasswordResetTokens_UserId");

            entity.Property(e => e.PasswordResetTokenId).HasColumnName("PasswordResetTokenID");
            entity.Property(e => e.CodeHash)
                .HasMaxLength(512)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ExpiresAt).HasPrecision(3);
            entity.Property(e => e.UsedAt).HasPrecision(3);

            entity.HasOne(d => d.User).WithMany(p => p.PasswordResetTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_PasswordResetTokens_Users");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A58D393F47D");

            entity.HasIndex(e => e.PackageId, "IX_Payments_PackageID");

            entity.HasIndex(e => e.UserId, "IX_Payments_UserID");

            entity.HasIndex(e => e.PaymentGatewayTransactionId, "UQ__Payments__0058A86E2C7EFF16").IsUnique();

            entity.Property(e => e.PaymentId).HasColumnName("PaymentID");
            entity.Property(e => e.Amount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.PackageId).HasColumnName("PackageID");
            entity.Property(e => e.PaymentGatewayTransactionId)
                .HasMaxLength(255)
                .HasColumnName("PaymentGatewayTransactionID");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Package).WithMany(p => p.Payments)
                .HasForeignKey(d => d.PackageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Packages");

            entity.HasOne(d => d.User).WithMany(p => p.Payments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Users");
        });

        modelBuilder.Entity<Prompt>(entity =>
        {
            entity.HasKey(e => e.PromptId).HasName("PK__Prompts__456CA7536D5666A0");

            entity.HasIndex(e => e.PassageId, "IX_Prompts_PassageId");

            entity.Property(e => e.Skill).HasMaxLength(255);

            entity.HasOne(d => d.Passage).WithMany(p => p.Prompts)
                .HasForeignKey(d => d.PassageId)
                .HasConstraintName("FK_Prompts_Passages");
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.QuestionId).HasName("PK__Question__0DC06FACE795341E");

            entity.HasIndex(e => e.PartId, "IX_Questions_PartId");

            entity.HasIndex(e => e.PromptId, "IX_Questions_PromptId");

            entity.Property(e => e.QuestionType).HasMaxLength(50);

            entity.HasOne(d => d.Part).WithMany(p => p.Questions)
                .HasForeignKey(d => d.PartId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Questions_ExamParts");

            entity.HasOne(d => d.Prompt).WithMany(p => p.Questions)
                .HasForeignKey(d => d.PromptId)
                .HasConstraintName("FK_Questions_Prompts");
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("PK__Reports__D5BD48E51D27B3D5");

            entity.HasIndex(e => e.SendBy, "IX_Reports_SendBy");

            entity.Property(e => e.ReportId).HasColumnName("ReportID");
            entity.Property(e => e.ReplyAt).HasPrecision(3);
            entity.Property(e => e.SendAt).HasPrecision(3);
            entity.Property(e => e.Title).HasMaxLength(50);
            entity.Property(e => e.Type).HasMaxLength(50);

            entity.HasOne(d => d.SendByNavigation).WithMany(p => p.Reports)
                .HasForeignKey(d => d.SendBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reports_ReplyBy");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1AA4681B10");

            entity.Property(e => e.RoleName).HasMaxLength(20);
        });

        modelBuilder.Entity<SectionCompletion>(entity =>
        {
            entity.HasKey(e => e.CompletionId).HasName("PK__SectionC__77FA70AF88803DC4");

            entity.HasIndex(e => e.SectionId, "IX_SectionCompletions_SectionID");

            entity.HasIndex(e => e.UserId, "IX_SectionCompletions_UserID");

            entity.Property(e => e.CompletionId).HasColumnName("CompletionID");
            entity.Property(e => e.CompletedAt).HasPrecision(3);
            entity.Property(e => e.SectionId).HasColumnName("SectionID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Section).WithMany(p => p.SectionCompletions)
                .HasForeignKey(d => d.SectionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SectionCompletions_ArticleSections");

            entity.HasOne(d => d.User).WithMany(p => p.SectionCompletions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SectionCompletions_Users");
        });

        modelBuilder.Entity<Slide>(entity =>
        {
            entity.HasKey(e => e.SlideId).HasName("PK__Slides__9E7CB650BEF67F4A");

            entity.HasIndex(e => e.CreateBy, "IX_Slides_CreateBy");

            entity.HasIndex(e => e.UpdateBy, "IX_Slides_UpdateBy");

            entity.Property(e => e.CreateAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.SlideName).HasMaxLength(255);
            entity.Property(e => e.UpdateAt).HasPrecision(3);

            entity.HasOne(d => d.CreateByNavigation).WithMany(p => p.SlideCreateByNavigations)
                .HasForeignKey(d => d.CreateBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Slides_Users_Create");

            entity.HasOne(d => d.UpdateByNavigation).WithMany(p => p.SlideUpdateByNavigations)
                .HasForeignKey(d => d.UpdateBy)
                .HasConstraintName("FK_Slides_Users_Update");
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.SubscriptionId).HasName("PK__Subscrip__9A2B24BDA2D67BCE");

            entity.HasIndex(e => e.PackageId, "IX_Subscriptions_PackageID");

            entity.HasIndex(e => e.PaymentId, "IX_Subscriptions_PaymentID");

            entity.HasIndex(e => e.UserId, "IX_Subscriptions_UserID");

            entity.Property(e => e.SubscriptionId).HasColumnName("SubscriptionID");
            entity.Property(e => e.EndTime).HasPrecision(3);
            entity.Property(e => e.PackageId).HasColumnName("PackageID");
            entity.Property(e => e.PaymentId).HasColumnName("PaymentID");
            entity.Property(e => e.StartTime).HasPrecision(3);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Package).WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.PackageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Subscriptions_Packages");

            entity.HasOne(d => d.Payment).WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.PaymentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Subscriptions_Payments");

            entity.HasOne(d => d.User).WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Subscriptions_Users");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCACF48D0559");

            entity.HasIndex(e => e.RoleId, "IX_Users_RoleID");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D105346CC2643F").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.CurrentStreak).HasDefaultValue(0);
            entity.Property(e => e.Email).HasMaxLength(50);
            entity.Property(e => e.FullName).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastPracticeDate).HasPrecision(3);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.RoleId).HasColumnName("RoleID");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Roles");
        });

        modelBuilder.Entity<UserAnswer>(entity =>
        {
            entity.HasKey(e => e.UserAnswerId).HasName("PK__UserAnsw__47CE235F4AA17153");

            entity.HasIndex(e => e.AttemptId, "IX_UserAnswers_AttemptID");

            entity.HasIndex(e => e.QuestionId, "IX_UserAnswers_QuestionID");

            entity.HasIndex(e => e.SelectedOptionId, "IX_UserAnswers_SelectedOptionID");

            entity.Property(e => e.UserAnswerId).HasColumnName("UserAnswerID");
            entity.Property(e => e.AttemptId).HasColumnName("AttemptID");
            entity.Property(e => e.FeedbackAi).HasColumnName("FeedbackAI");
            entity.Property(e => e.QuestionId).HasColumnName("QuestionID");
            entity.Property(e => e.SelectedOptionId).HasColumnName("SelectedOptionID");

            entity.HasOne(d => d.Attempt).WithMany(p => p.UserAnswers)
                .HasForeignKey(d => d.AttemptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserAnswers_ExamAttempts");

            entity.HasOne(d => d.Question).WithMany(p => p.UserAnswers)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserAnswers_Questions");

            entity.HasOne(d => d.SelectedOption).WithMany(p => p.UserAnswers)
                .HasForeignKey(d => d.SelectedOptionId)
                .HasConstraintName("FK_UserAnswers_Options");
        });

        modelBuilder.Entity<UserArticleProgress>(entity =>
        {
            entity.HasKey(e => e.ProgressId).HasName("PK__UserArti__BAE29C85A832D5F7");

            entity.ToTable("UserArticleProgress");

            entity.HasIndex(e => e.ArticleId, "IX_UserArticleProgress_ArticleID");

            entity.HasIndex(e => e.UserId, "IX_UserArticleProgress_UserID");

            entity.Property(e => e.ProgressId).HasColumnName("ProgressID");
            entity.Property(e => e.ArticleId).HasColumnName("ArticleID");
            entity.Property(e => e.CompletedAt).HasPrecision(3);
            entity.Property(e => e.LastAccessedAt).HasPrecision(3);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Article).WithMany(p => p.UserArticleProgresses)
                .HasForeignKey(d => d.ArticleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserArticleProgress_Articles");

            entity.HasOne(d => d.User).WithMany(p => p.UserArticleProgresses)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserArticleProgress_Users");
        });

        modelBuilder.Entity<UserLeaderboard>(entity =>
        {
            entity.HasKey(e => e.UserLeaderboardId).HasName("PK__UserLead__F2E24551691C1D90");

            entity.ToTable("UserLeaderboard");

            entity.HasIndex(e => e.LeaderboardId, "IX_UserLeaderboard_LeaderboardID");

            entity.HasIndex(e => e.UserId, "IX_UserLeaderboard_UserID");

            entity.Property(e => e.UserLeaderboardId).HasColumnName("UserLeaderboardID");
            entity.Property(e => e.LeaderboardId).HasColumnName("LeaderboardID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Leaderboard).WithMany(p => p.UserLeaderboards)
                .HasForeignKey(d => d.LeaderboardId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserLeaderboard_Leaderboard");

            entity.HasOne(d => d.User).WithMany(p => p.UserLeaderboards)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserLeaderboard_Users");
        });

        modelBuilder.Entity<UserNote>(entity =>
        {
            entity.HasKey(e => e.NoteId).HasName("PK__UserNote__EACE357FD9109DCF");

            entity.HasIndex(e => e.ArticleId, "IX_UserNotes_ArticleID");

            entity.HasIndex(e => e.SectionId, "IX_UserNotes_SectionID");

            entity.HasIndex(e => e.UserId, "IX_UserNotes_UserID");

            entity.Property(e => e.NoteId).HasColumnName("NoteID");
            entity.Property(e => e.ArticleId).HasColumnName("ArticleID");
            entity.Property(e => e.CreateAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.SectionId).HasColumnName("SectionID");
            entity.Property(e => e.UpdateAt).HasPrecision(3);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Article).WithMany(p => p.UserNotes)
                .HasForeignKey(d => d.ArticleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserNotes_Articles");

            entity.HasOne(d => d.Section).WithMany(p => p.UserNotes)
                .HasForeignKey(d => d.SectionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserNotes_ArticleSections");

            entity.HasOne(d => d.User).WithMany(p => p.UserNotes)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserNotes_Users");
        });

        modelBuilder.Entity<UserNotification>(entity =>
        {
            entity.HasKey(e => e.UniqueId).HasName("PK__UserNoti__A2A2BAAAD8BD5A4B");

            entity.HasIndex(e => e.NotificationId, "IX_UserNotifications_NotificationId");

            entity.HasIndex(e => e.UserId, "IX_UserNotifications_UserId");

            entity.Property(e => e.UniqueId).HasColumnName("UniqueID");
            entity.Property(e => e.CreateAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsRead).HasDefaultValue(false);

            entity.HasOne(d => d.Notification).WithMany(p => p.UserNotifications)
                .HasForeignKey(d => d.NotificationId)
                .HasConstraintName("FK_UserNotifications_Notifications");

            entity.HasOne(d => d.User).WithMany(p => p.UserNotifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserNotifications_Users");
        });

        modelBuilder.Entity<UserSpacedRepetition>(entity =>
        {
            entity.HasKey(e => e.UserSpacedRepetitionId).HasName("PK__UserSpac__F141C818F2E5BE1C");

            entity.ToTable("UserSpacedRepetition");

            entity.HasIndex(e => e.UserId, "IX_UserSpacedRepetition_UserId");

            entity.HasIndex(e => e.VocabularyListId, "IX_UserSpacedRepetition_VocabularyListId");

            entity.Property(e => e.LastReviewedAt).HasPrecision(3);
            entity.Property(e => e.NextReviewAt).HasPrecision(3);
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.User).WithMany(p => p.UserSpacedRepetitions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserSpacedRepetition_Users");

            entity.HasOne(d => d.VocabularyList).WithMany(p => p.UserSpacedRepetitions)
                .HasForeignKey(d => d.VocabularyListId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserSpacedRepetition_VocabularyList");
        });

        modelBuilder.Entity<Vocabulary>(entity =>
        {
            entity.HasKey(e => e.VocabularyId).HasName("PK__Vocabula__9274069F7C141637");

            entity.HasIndex(e => e.VocabularyListId, "IX_Vocabularies_VocabularyListId");

            entity.Property(e => e.VocabularyId).HasColumnName("VocabularyID");
            entity.Property(e => e.TypeOfWord).HasMaxLength(50);
            entity.Property(e => e.Word).HasMaxLength(255);

            entity.HasOne(d => d.VocabularyList).WithMany(p => p.Vocabularies)
                .HasForeignKey(d => d.VocabularyListId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Vocabularies_VocabularyList");
        });

        modelBuilder.Entity<VocabularyList>(entity =>
        {
            entity.HasKey(e => e.VocabularyListId).HasName("PK__Vocabula__C2D6E440F67079D4");

            entity.ToTable("VocabularyList");

            entity.HasIndex(e => e.MakeBy, "IX_VocabularyList_MakeBy");

            entity.Property(e => e.CreateAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Status).HasMaxLength(15);
            entity.Property(e => e.UpdateAt).HasPrecision(3);

            entity.HasOne(d => d.MakeByNavigation).WithMany(p => p.VocabularyLists)
                .HasForeignKey(d => d.MakeBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VocabularyList_Users");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
