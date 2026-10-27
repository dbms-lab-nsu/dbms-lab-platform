using DBMSLab.Api.Data;
using DBMSLab.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DBMSLab.Api.Repositories;

public sealed class ProgressRepository(AppDbContext db) : IProgressRepository
{
    public Task<LessonProgress?> GetByUserAndLessonAsync(Guid userId, Guid lessonId, CancellationToken ct = default)
        => db.LessonProgresses
            .FirstOrDefaultAsync(x => x.UserId == userId && x.LessonId == lessonId, ct);

    public async Task<IEnumerable<LessonProgress>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => await db.LessonProgresses
            .Where(x => x.UserId == userId)
            .ToListAsync(ct);

    public async Task<IEnumerable<LessonProgress>> GetByCourseAsync(Guid userId, Guid courseId, CancellationToken ct = default)
        => await db.LessonProgresses
            .Include(x => x.Lesson).ThenInclude(x => x.Topic).ThenInclude(x => x.Module)
            .Where(x => x.UserId == userId && x.Lesson.Topic.Module.CourseId == courseId)
            .ToListAsync(ct);

    public async Task AddOrUpdateAsync(LessonProgress progress, CancellationToken ct = default)
    {
        var existing = await GetByUserAndLessonAsync(progress.UserId, progress.LessonId, ct);
        if (existing is null)
        {
            await db.LessonProgresses.AddAsync(progress, ct);
        }
        else
        {
            existing.Status = progress.Status;
            existing.UpdatedAt = progress.UpdatedAt;
        }

        await db.SaveChangesAsync(ct);
    }
}
