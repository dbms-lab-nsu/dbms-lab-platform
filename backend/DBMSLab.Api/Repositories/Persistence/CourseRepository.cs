using DBMSLab.Api.Application.Content;
using DBMSLab.Api.Data;
using DBMSLab.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DBMSLab.Api.Repositories.Persistence;

public sealed class CourseRepository(AppDbContext db) : ICourseRepository
{
    public Task<int> CountCoursesAsync(CourseStatus status, CancellationToken cancellationToken) =>
        db.Courses.CountAsync(course => course.Status == status, cancellationToken);

    public async Task<IReadOnlyList<CourseReadModel>> GetCoursesAsync(
        CourseStatus status, int offset, int limit, CancellationToken cancellationToken) =>
        await db.Courses.AsNoTracking()
            .Where(course => course.Status == status)
            .OrderBy(course => course.Title)
            .ThenBy(course => course.Id)
            .Skip(offset)
            .Take(limit)
            .Select(course => new CourseReadModel(
                course.Id, course.Title, course.Description, course.Status))
            .ToListAsync(cancellationToken);

    public Task<CourseReadModel?> GetCourseAsync(Guid id, CancellationToken cancellationToken) =>
        db.Courses.AsNoTracking()
            .Where(course => course.Id == id)
            .Select(course => new CourseReadModel(
                course.Id, course.Title, course.Description, course.Status))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<CourseStructureReadModel?> GetStructureAsync(Guid id, CancellationToken cancellationToken) =>
        db.Courses.AsNoTracking()
            .Where(course => course.Id == id)
            .AsSplitQuery()
            .Select(course => new CourseStructureReadModel(
                course.Id,
                course.Title,
                course.Description,
                course.Status,
                course.Modules.OrderBy(module => module.SortOrder)
                    .Select(module => new ModuleReadModel(
                        module.Id,
                        module.Title,
                        module.Description,
                        module.SortOrder,
                        module.Topics.OrderBy(topic => topic.SortOrder)
                            .Select(topic => new TopicReadModel(
                                topic.Id,
                                topic.Title,
                                topic.Description,
                                topic.SortOrder,
                                topic.Lessons
                                    .OrderBy(lesson => lesson.SortOrder)
                                    .Select(lesson => new LessonSummaryReadModel(
                                        lesson.Id, lesson.Title, lesson.SortOrder, lesson.IsPublished))
                                    .ToList()))
                            .ToList()))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<LessonReadModel?> GetLessonAsync(Guid id, CancellationToken cancellationToken) =>
        db.Lessons
            .AsNoTracking()
            .Where(lesson => lesson.Id == id)
            .Select(lesson => new LessonReadModel(
                lesson.Id,
                lesson.Topic.Module.CourseId,
                lesson.Topic.ModuleId,
                lesson.TopicId,
                lesson.Title,
                lesson.SortOrder,
                lesson.Content,
                lesson.IsPublished,
                lesson.Topic.Module.Course.Status))
            .SingleOrDefaultAsync(cancellationToken);
}
