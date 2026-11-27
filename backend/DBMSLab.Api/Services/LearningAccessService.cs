using System.Security.Claims;
using DBMSLab.Api.Application.Content;
using DBMSLab.Api.Data;
using DBMSLab.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DBMSLab.Api.Services;

/// <summary>Resolves learner accounts and locks content before transactional learning updates.</summary>
public sealed class LearningAccessService(AppDbContext db, UserManager<AppUser> users)
{
    /// <summary>Resolves the current account without accepting user identifiers from the request.</summary>
    public async Task<Guid> RequireUserIdAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true
            || !Guid.TryParse(users.GetUserId(principal), out var id)
            || await users.FindByIdAsync(id.ToString()) is null)
            throw new ContentAccountRequiredException();

        return id;
    }

    /// <summary>Locks the course against archiving until the caller's transaction finishes.</summary>
    public async Task<Course> LockPublishedCourseAsync(Guid id, CancellationToken ct)
    {
        var rows = await db.Courses.FromSql($"""SELECT * FROM courses WHERE "Id" = {id} FOR SHARE""")
            .AsNoTracking().ToListAsync(ct);
        var course = rows.SingleOrDefault() ?? throw new ContentNotFoundException("The course was not found.");
        if (!ContentAccessRules.IsPublishedCourse(course.Status))
            throw new ContentAccessDeniedException("The course is not published.");

        return course;
    }

    /// <summary>Locks course then lesson, and checks publication without applying preview privileges.</summary>
    /// <remarks>The caller must already have started a transaction on the scoped context.</remarks>
    public async Task<Guid> LockPublishedLessonAsync(Guid lessonId, CancellationToken ct)
    {
        var courseId = await db.Lessons.AsNoTracking().Where(l => l.Id == lessonId)
            .Select(l => (Guid?)l.Topic.Module.CourseId).SingleOrDefaultAsync(ct)
            ?? throw new ContentNotFoundException("The lesson was not found.");

        var course = await LockPublishedCourseAsync(courseId, ct);
        var rows = await db.Lessons.FromSql($"""SELECT * FROM lessons WHERE "Id" = {lessonId} FOR SHARE""")
            .AsNoTracking().ToListAsync(ct);
        var lesson = rows.SingleOrDefault() ?? throw new ContentNotFoundException("The lesson was not found.");
        if (!ContentAccessRules.CanReadPublishedLesson(true, lesson.IsPublished, course.Status))
            throw new ContentAccessDeniedException("The lesson is not published.");

        return courseId;
    }
}
