using DBMSLab.Api.Domain;

namespace DBMSLab.Api.Application.Content;

/// <summary>Reads course and lesson projections and persists author changes.</summary>
/// <remarks>
/// Does not authorize callers or hide unpublished records returned by identifier.
/// The service must check publication state and preview permissions before exposing a read model.
/// </remarks>
public interface ICourseRepository
{
    /// <summary>Counts courses with the specified publication status.</summary>
    /// <param name="status">The stored course status to match.</param>
    /// <param name="cancellationToken">Cancels the database query.</param>
    /// <returns>The number of matching courses before pagination.</returns>
    Task<int> CountCoursesAsync(CourseStatus status, CancellationToken cancellationToken);

    /// <summary>Reads a catalog slice in ascending title order, using the identifier to break ties.</summary>
    /// <param name="status">The stored course status to match.</param>
    /// <param name="offset">The zero-based offset in the filtered, ordered catalog.</param>
    /// <param name="limit">The maximum number of items to read.</param>
    /// <param name="cancellationToken">Cancels the database query.</param>
    /// <returns>The matching slice, or an empty list when no courses fall within it.</returns>
    Task<IReadOnlyList<CourseReadModel>> GetCoursesAsync(
        CourseStatus status, int offset, int limit, CancellationToken cancellationToken);

    /// <summary>Reads a course card together with its stored publication status.</summary>
    /// <param name="id">The course identifier.</param>
    /// <param name="cancellationToken">Cancels the database query.</param>
    /// <returns>The course, including draft or archived records, or <see langword="null"/> if missing.</returns>
    Task<CourseReadModel?> GetCourseAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Reads module, topic, and lesson metadata in ascending sibling SortOrder.</summary>
    /// <param name="id">The course identifier.</param>
    /// <param name="cancellationToken">Cancels the hierarchy queries.</param>
    /// <returns>The hierarchy, or <see langword="null"/> if the course does not exist.</returns>
    /// <remarks>Includes unpublished lesson metadata but never loads lesson content.</remarks>
    Task<CourseStructureReadModel?> GetStructureAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Reads lesson content and the publication states required to determine its visibility.</summary>
    /// <param name="id">The lesson identifier.</param>
    /// <param name="cancellationToken">Cancels the database query.</param>
    /// <returns>The lesson and its parent identifiers, or <see langword="null"/> if missing.</returns>
    Task<LessonReadModel?> GetLessonAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Saves a new course.</summary>
    Task AddCourseAsync(Course course, CancellationToken cancellationToken);

    /// <summary>Reads all courses owned by the author, ordered by title and then identifier.</summary>
    Task<IReadOnlyList<CourseReadModel>> GetCoursesByAuthorAsync(Guid authorId, CancellationToken cancellationToken);

    /// <summary>Locks an existing course, applies the change, and saves it in a single transaction.</summary>
    /// <param name="id">The course to lock before invoking the change.</param>
    /// <param name="change">Applies business rules to the locked course and can update its tracked lessons.</param>
    /// <param name="cancellationToken">Cancels the transaction and its queries.</param>
    /// <returns>The saved course, or null when the course does not exist.</returns>
    Task<Course?> ChangeCourseAsync(Guid id, Func<Course, CancellationToken, Task> change, CancellationToken cancellationToken);

    /// <summary>Loads the tracked lessons of a course during its locked change transaction.</summary>
    Task<IReadOnlyList<Lesson>> GetCourseLessonsForUpdateAsync(Guid courseId, CancellationToken cancellationToken);
}
