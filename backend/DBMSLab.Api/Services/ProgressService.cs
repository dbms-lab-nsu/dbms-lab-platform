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
    AppDbContext db) : IProgressService
{
    /// <summary>
    /// Updates the lesson progress status for the current user.
    /// Checks that the user is enrolled in the course the lesson belongs to.
    /// </summary>
    public async Task<LessonProgress> UpdateLessonProgressAsync(
        Guid userId,
        Guid lessonId,
        LessonProgressStatus status,
        CancellationToken ct = default)
    {
        var lesson = await db.Lessons
            .Include(x => x.Topic).ThenInclude(x => x.Module)
            .FirstOrDefaultAsync(x => x.Id == lessonId, ct);
        if (lesson is null)
            throw new KeyNotFoundException("Lesson not found.");

        var isEnrolled = await enrollments.ExistsAsync(userId, lesson.Topic.Module.CourseId, ct);
        if (!isEnrolled)
            throw new AccessDeniedException("You are not enrolled in the course this lesson belongs to.");

        var entity = await progress.GetByUserAndLessonAsync(userId, lessonId, ct);
        if (entity is null)
        {
            entity = new LessonProgress
            {
                UserId = userId,
                LessonId = lessonId,
                Status = status,
                UpdatedAt = DateTime.UtcNow
            };
        }
        else
        {
            // A completed lesson cannot be moved back to "Not started".
            if (entity.Status == LessonProgressStatus.Completed &&
                status == LessonProgressStatus.NotStarted)
            {
                throw new InvalidOperationException(
                    "Cannot move a completed lesson back to the 'Not started' state.");
            }

            entity.Status = status;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        await progress.AddOrUpdateAsync(entity, ct);
        return entity;
    }

    /// <summary>
    /// Returns the course progress for the user.
    /// Available only if the user is enrolled in the course.
    /// Percentage is calculated only from published lessons.
    /// </summary>
    public async Task<CourseProgressDto> GetCourseProgressAsync(Guid userId, Guid courseId, CancellationToken ct = default)
    {
        var course = await db.Courses.FirstOrDefaultAsync(x => x.Id == courseId, ct);
        if (course is null)
            throw new KeyNotFoundException("Course not found.");

        var isEnrolled = await enrollments.ExistsAsync(userId, courseId, ct);
        if (!isEnrolled)
            throw new AccessDeniedException("You are not enrolled in this course.");

        var publishedLessons = await db.Lessons
            .Include(x => x.Topic).ThenInclude(x => x.Module)
            .Where(x => x.Topic.Module.CourseId == courseId && x.IsPublished)
            .ToListAsync(ct);

        var userProgress = await progress.GetByCourseAsync(userId, courseId, ct);
        var progressByLesson = userProgress.ToDictionary(x => x.LessonId);

        int completed = 0;
        foreach (var lesson in publishedLessons)
        {
            if (progressByLesson.TryGetValue(lesson.Id, out var p) && p.Status == LessonProgressStatus.Completed)
                completed++;
        }

        int total = publishedLessons.Count;
        int percent = total == 0 ? 0 : (int)Math.Round((double)completed / total * 100);

        return new CourseProgressDto(courseId, completed, total, percent);
    }
}
