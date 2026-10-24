using DBMSLab.Api.Application.Content.Contracts;
using DBMSLab.Api.Domain;

namespace DBMSLab.Api.Application.Content;

public sealed class CourseService(ICourseRepository courses)
{
    public async Task<CourseCatalogDto> GetCatalogAsync(
        CourseCatalogQuery query, CancellationToken cancellationToken)
    {
        var totalCount = await courses.CountCoursesAsync(CourseStatus.Published, cancellationToken);
        var offset = ((long)query.Page - 1) * query.PageSize;
        if (offset >= totalCount)
            return new CourseCatalogDto([], query.Page, query.PageSize, totalCount);

        var page = await courses.GetCoursesAsync(
            CourseStatus.Published, (int)offset, query.PageSize, cancellationToken);
        var items = page.Select(course => new CourseDto(
            course.Id, course.Title, course.Description)).ToList();
        return new CourseCatalogDto(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<CourseDto> GetCourseAsync(Guid id, CancellationToken cancellationToken)
    {
        var course = await courses.GetCourseAsync(id, cancellationToken);
        if (course is null || course.Status != CourseStatus.Published)
            throw new ContentNotFoundException("The course was not found.");

        return new CourseDto(course.Id, course.Title, course.Description);
    }

    public async Task<CourseStructureDto> GetStructureAsync(Guid id, CancellationToken cancellationToken)
    {
        var course = await courses.GetStructureAsync(id, cancellationToken);
        if (course is null || course.Status != CourseStatus.Published)
            throw new ContentNotFoundException("The course was not found.");

        return new CourseStructureDto(course.Id, course.Title, course.Description,
            course.Modules.Select(module => new ModuleDto(
                module.Id, module.Title, module.Description, module.SortOrder,
                module.Topics.Select(topic => new TopicDto(
                    topic.Id, topic.Title, topic.Description, topic.SortOrder,
                    topic.Lessons.Where(lesson => lesson.IsPublished)
                        .Select(lesson => new LessonSummaryDto(lesson.Id, lesson.Title, lesson.SortOrder))
                        .ToList())).ToList())).ToList());
    }

    public async Task<LessonDto> GetLessonAsync(Guid id, CancellationToken cancellationToken)
    {
        var lesson = await courses.GetLessonAsync(id, cancellationToken);
        if (lesson is null || !lesson.IsPublished || lesson.CourseStatus != CourseStatus.Published)
            throw new ContentNotFoundException("The lesson was not found.");

        return new LessonDto(lesson.Id, lesson.CourseId, lesson.ModuleId, lesson.TopicId,
            lesson.Title, lesson.SortOrder, lesson.Content);
    }
}
