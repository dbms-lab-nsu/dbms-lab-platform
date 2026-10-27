using DBMSLab.Api.Data;
using DBMSLab.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DBMSLab.Api.Repositories;

public sealed class EnrollmentRepository(AppDbContext db) : IEnrollmentRepository
{
    public Task<CourseEnrollment?> GetByUserAndCourseAsync(Guid userId, Guid courseId, CancellationToken ct = default)
        => db.CourseEnrollments
            .FirstOrDefaultAsync(x => x.UserId == userId && x.CourseId == courseId, ct);

    public async Task<IEnumerable<CourseEnrollment>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => await db.CourseEnrollments
            .Include(x => x.Course)
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Course.Title)
            .ToListAsync(ct);

    public async Task AddAsync(CourseEnrollment enrollment, CancellationToken ct = default)
    {
        await db.CourseEnrollments.AddAsync(enrollment, ct);
        await db.SaveChangesAsync(ct);
    }

    public Task<bool> ExistsAsync(Guid userId, Guid courseId, CancellationToken ct = default)
        => db.CourseEnrollments
            .AnyAsync(x => x.UserId == userId && x.CourseId == courseId, ct);
}
