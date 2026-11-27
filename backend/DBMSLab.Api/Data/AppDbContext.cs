using DBMSLab.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DBMSLab.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseModule> Modules => Set<CourseModule>();
    public DbSet<CourseTopic> Topics => Set<CourseTopic>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<CourseEnrollment> CourseEnrollments => Set<CourseEnrollment>();
    public DbSet<LessonProgress> LessonProgresses => Set<LessonProgress>();
    public DbSet<Test> Tests => Set<Test>();
    public DbSet<TestQuestion> TestQuestions => Set<TestQuestion>();
    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();
    public DbSet<TestAttempt> TestAttempts => Set<TestAttempt>();
    public DbSet<TestAnswer> TestAnswers => Set<TestAnswer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("users");
            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.NormalizedEmail).IsUnique();
        });
        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("roles");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");

        modelBuilder.Entity<IdentityRole<Guid>>().HasData(
            Role("e83dd480-7a85-4cb8-9e18-1743ce72b3a1", AppRoles.Student),
            Role("7e6f3f46-455a-4082-9038-cc9dce59f085", AppRoles.Author),
            Role("a0f486b2-5a52-47da-87fb-e73359972495", AppRoles.Curator),
            Role("2e2a21da-13fd-4053-96ee-ef9ce528895e", AppRoles.Administrator));

        modelBuilder.Entity<Course>(entity =>
        {
            entity.ToTable("courses");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CourseModule>(entity =>
        {
            entity.ToTable("modules");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => new { x.CourseId, x.SortOrder }).IsUnique();
            entity.HasOne(x => x.Course).WithMany(x => x.Modules).HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CourseTopic>(entity =>
        {
            entity.ToTable("topics");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => new { x.ModuleId, x.SortOrder }).IsUnique();
            entity.HasOne(x => x.Module).WithMany(x => x.Topics).HasForeignKey(x => x.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.ToTable("lessons");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Content).IsRequired();
            entity.HasIndex(x => new { x.TopicId, x.SortOrder }).IsUnique();
            entity.HasOne(x => x.Topic).WithMany(x => x.Lessons).HasForeignKey(x => x.TopicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CourseEnrollment>(entity =>
        {
            entity.ToTable("course_enrollments");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UserId, x.CourseId }).IsUnique();
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LessonProgress>(entity =>
        {
            entity.ToTable("lesson_progress");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UserId, x.LessonId }).IsUnique();
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Lesson).WithMany().HasForeignKey(x => x.LessonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Test>(entity =>
        {
            entity.ToTable("tests");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).IsRequired();
            entity.HasIndex(x => new { x.TopicId, x.SortOrder }).IsUnique();
            entity.HasOne(x => x.Topic).WithMany().HasForeignKey(x => x.TopicId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TestQuestion>(entity =>
        {
            entity.ToTable("test_questions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Text).IsRequired();
            entity.HasIndex(x => new { x.TestId, x.SortOrder }).IsUnique();
            entity.HasOne(x => x.Test).WithMany(x => x.Questions).HasForeignKey(x => x.TestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AnswerOption>(entity =>
        {
            entity.ToTable("answer_options");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Text).IsRequired();
            entity.HasIndex(x => new { x.QuestionId, x.SortOrder }).IsUnique();
            entity.HasIndex(x => x.QuestionId)
                .IsUnique()
                .HasFilter("\"IsCorrect\" = true");
            entity.HasOne(x => x.Question).WithMany(x => x.AnswerOptions).HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TestAttempt>(entity =>
        {
            entity.ToTable("test_attempts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ScorePercent).IsRequired();
            entity.HasIndex(x => x.TestId);
            entity.HasIndex(x => x.UserId);
            entity.HasOne(x => x.Test).WithMany(x => x.Attempts).HasForeignKey(x => x.TestId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TestAnswer>(entity =>
        {
            entity.ToTable("test_answers");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.AttemptId);
            entity.HasIndex(x => x.QuestionId);
            entity.HasIndex(x => new { x.AttemptId, x.QuestionId }).IsUnique();
            entity.HasOne(x => x.Attempt).WithMany(x => x.Answers).HasForeignKey(x => x.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Question).WithMany(x => x.UserAnswers).HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AnswerOption).WithMany(x => x.SelectedBy).HasForeignKey(x => x.AnswerOptionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static IdentityRole<Guid> Role(string id, string name) => new()
    {
        Id = Guid.Parse(id),
        Name = name,
        NormalizedName = name.ToUpperInvariant(),
        ConcurrencyStamp = "dbms-lab-role-" + name.ToLowerInvariant() + "-v1"
    };
}
