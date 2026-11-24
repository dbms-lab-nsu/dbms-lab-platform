using DBMSLab.Api.Data;
using DBMSLab.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DBMSLab.Api.Repositories.Persistence;

public sealed class ProgressRepository(AppDbContext db) : IProgressRepository
{
    public Task<LessonProgress?> GetByUserAndLessonAsync(Guid userId, Guid lessonId, CancellationToken ct = default)
        => db.LessonProgresses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.LessonId == lessonId, ct);

    public async Task<IEnumerable<LessonProgress>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => await db.LessonProgresses.AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToListAsync(ct);

    public async Task<IEnumerable<LessonProgress>> GetByCourseAsync(Guid userId, Guid courseId, CancellationToken ct = default)
        => await db.LessonProgresses.AsNoTracking()
            .Where(x => x.UserId == userId && x.Lesson.Topic.Module.CourseId == courseId)
            .ToListAsync(ct);

    public async Task<LessonProgress> AddOrUpdateAsync(LessonProgress progress, CancellationToken ct = default)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO lesson_progress ("Id", "UserId", "LessonId", "Status", "UpdatedAt")
            VALUES ({progress.Id}, {progress.UserId}, {progress.LessonId}, {(int)progress.Status}, {progress.UpdatedAt})
            ON CONFLICT ("UserId", "LessonId") DO UPDATE
            SET "Status" = EXCLUDED."Status", "UpdatedAt" = EXCLUDED."UpdatedAt"
            WHERE lesson_progress."Status" IS DISTINCT FROM EXCLUDED."Status"
            """, ct);
        return (await GetByUserAndLessonAsync(progress.UserId, progress.LessonId, ct))!;
    }
}
