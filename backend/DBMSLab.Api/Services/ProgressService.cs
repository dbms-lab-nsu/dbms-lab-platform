using DBMSLab.Api.Application.Content;
using DBMSLab.Api.Contracts;
using DBMSLab.Api.Data;
using DBMSLab.Api.Domain;
using DBMSLab.Api.Exceptions;
using DBMSLab.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DBMSLab.Api.Services;

public sealed class ProgressService(
    IProgressRepository progress,
    IEnrollmentRepository enrollments,
    AppDbContext db,
    LearningAccessService access) : IProgressService
{
    /// <summary>
    /// Updates the lesson progress status for the current user.
    /// Requires a published lesson in a published enrolled course, without preview exceptions.
    /// </summary>
    public async Task<LessonProgress> UpdateLessonProgressAsync(
        Guid userId,
        Guid lessonId,
        LessonProgressStatus status,
        CancellationToken ct = default)
    {
        if (!Enum.IsDefined(status))
            throw new InvalidProgressStatusException();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var courseId = await access.LockPublishedLessonAsync(lessonId, ct);
        var isEnrolled = await enrollments.ExistsAsync(userId, courseId, ct);
        if (!isEnrolled)
            throw new ContentAccessDeniedException("You are not enrolled in the course this lesson belongs to.");

        var entity = new LessonProgress
        {
            UserId = userId,
            LessonId = lessonId,
            Status = status,
            UpdatedAt = DateTime.UtcNow
        };
        var saved = await progress.AddOrUpdateAsync(entity, ct);
        await transaction.CommitAsync(ct);
        return saved;
    }

    /// <summary>
    /// Returns the course progress for the user.
    /// Available only if the user is enrolled in the course.
    /// Percentage is calculated only from published lessons.
    /// </summary>
    public async Task<CourseProgressDto> GetCourseProgressAsync(Guid userId, Guid courseId, CancellationToken ct = default)
    {
        if (!await db.Courses.AnyAsync(x => x.Id == courseId, ct))
            throw new ContentNotFoundException("The course was not found.");

        var isEnrolled = await enrollments.ExistsAsync(userId, courseId, ct);
        if (!isEnrolled)
            throw new ContentAccessDeniedException("You are not enrolled in this course.");

        var publishedLessons = await db.Lessons
            .Where(x => x.Topic.Module.CourseId == courseId && x.IsPublished)
            .Select(x => x.Id)
            .ToListAsync(ct);

        var userProgress = await progress.GetByCourseAsync(userId, courseId, ct);
        var progressByLesson = userProgress.ToDictionary(x => x.LessonId);

        int completed = 0;
        foreach (var lesson in publishedLessons)
        {
            if (progressByLesson.TryGetValue(lesson, out var p) && p.Status == LessonProgressStatus.Completed)
                completed++;
        }

        int total = publishedLessons.Count;
        int percent = total == 0 ? 0 : (int)Math.Round((double)completed / total * 100);

        return new CourseProgressDto(courseId, completed, total, percent);
    }
}
