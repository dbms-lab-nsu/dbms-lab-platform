using DBMSLab.Api.Domain;

namespace DBMSLab.Api.Application.Content;

/// <summary>Defines ordinary content access, independently of author and administrator previews.</summary>
public static class ContentAccessRules
{
    /// <summary>Whether a course accepts new enrollments and progress changes.</summary>
    public static bool IsPublishedCourse(CourseStatus status) => status == CourseStatus.Published;

    /// <summary>Whether published lesson content can be read by an authenticated learner.</summary>
    public static bool CanReadPublishedLesson(bool authenticated, bool lessonPublished, CourseStatus courseStatus) =>
        authenticated && lessonPublished && IsPublishedCourse(courseStatus);
}
