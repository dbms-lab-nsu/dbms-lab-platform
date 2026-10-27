namespace DBMSLab.Api.Contracts;

public sealed record EnrollmentResponse(
    Guid CourseId,
    DateTime EnrolledAt);

public sealed record EnrollmentListResponse(
    Guid CourseId,
    string CourseTitle,
    DateTime EnrolledAt);
