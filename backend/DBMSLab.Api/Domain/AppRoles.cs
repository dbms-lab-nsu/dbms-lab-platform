namespace DBMSLab.Api.Domain;

public static class AppRoles
{
    // Guest is not a stored Identity role: it means an anonymous visitor.
    public const string Student = "Student";
    public const string Author = "Author";
    public const string Curator = "Curator";
    public const string Administrator = "Administrator";
}
