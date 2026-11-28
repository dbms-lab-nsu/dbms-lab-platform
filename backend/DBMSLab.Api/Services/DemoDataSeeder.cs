using DBMSLab.Api.Data;
using DBMSLab.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DBMSLab.Api.Services;

/// <summary>
/// Seeds the database with demo data for the "Отказоустойчивость и высокодоступность" course.
/// </summary>
/// <remarks>
/// The service is idempotent: repeated runs do not create duplicate courses,
/// modules, topics, or lessons. It runs only after PostgreSQL migrations have been
/// applied and only in a demo environment (environment variable <c>SEED_DEMO_DATA</c>).
/// </remarks>
public sealed class DemoDataSeeder(AppDbContext db)
{
    private const string DemoCourseTitle = "Отказоустойчивость и высокодоступность";

    /// <summary>
    /// Performs initial demo data seeding. If the demo course already exists, exits without changes.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await db.Courses.AnyAsync(c => c.Title == DemoCourseTitle, cancellationToken))
            return;

        var author = await EnsureDemoAuthorAsync(cancellationToken);

        var course = new Course
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111101"),
            AuthorId = author.Id,
            Title = DemoCourseTitle,
            Description =
                "Практический курс по обеспечению отказоустойчивости и высокой доступности " +
                "в PostgreSQL. Рассматриваются физическая и логическая репликация, их архитектура, " +
                "настройка и области применения.",
            Status = CourseStatus.Published,
            Modules = new List<CourseModule>
            {
                // Module 1 — Physical replication (1 topic, 1 lesson)
                new CourseModule
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    CourseId = Guid.Parse("11111111-1111-1111-1111-111111111101"),
                    Title = "Физическая репликация",
                    Description =
                        "Байтовый уровень репликации: физическая копия кластера, WAL-журнал, " +
                        "первичный и резервный серверы.",
                    SortOrder = 1,
                    Topics = new List<CourseTopic>
                    {
                        new CourseTopic
                        {
                            Id = Guid.Parse("11111111-1111-1111-1111-111111111112"),
                            ModuleId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                            Title = "Основы физической репликации",
                            Description =
                                "Как PostgreSQL копирует кластер на уровне блоков данных " +
                                "и для чего нужен WAL-журнал.",
                            SortOrder = 1,
                            Lessons = new List<Lesson>
                            {
                                new Lesson
                                {
                                    Id = Guid.Parse("11111111-1111-1111-1111-111111111113"),
                                    TopicId = Guid.Parse("11111111-1111-1111-1111-111111111112"),
                                    Title = "Что такое физическая репликация и WAL",
                                    Content =
                                        "<p>Физическая репликация копирует кластер PostgreSQL " +
                                        "на уровне блоков данных (страниц). Все изменения " +
                                        "сначала записываются в WAL-журнал (Write-Ahead Log), " +
                                        "а затем передаются на резервный сервер, где применяются " +
                                        "те же изменения к его копии данных.</p>" +
                                        "<p>Основные преимущества:</p>" +
                                        "<ul>" +
                                        "<li>простота настройки и надёжность;</li>" +
                                        "<li>полная идентичность первичного и резервного серверов;</li>" +
                                        "<li>поддержка точки восстановления (PITR).</li>" +
                                        "</ul>" +
                                        "<p>Основное ограничение: резервный сервер по умолчанию " +
                                        "доступен только для чтения, а все клиенты должны подключаться " +
                                        "к первичному серверу для записи.</p>",
                                    SortOrder = 1,
                                    IsPublished = true,
                                },
                            },
                        },
                    },
                },
                // Module 2 — Logical replication (2 topics, 3 lessons)
                new CourseModule
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111121"),
                    CourseId = Guid.Parse("11111111-1111-1111-1111-111111111101"),
                    Title = "Логическая репликация",
                    Description =
                        "Репликация на уровне SQL-операций: публикации, подписки, " +
                        "выборочная репликация таблиц и разрешение конфликтов.",
                    SortOrder = 2,
                    Topics = new List<CourseTopic>
                    {
                        new CourseTopic
                        {
                            Id = Guid.Parse("11111111-1111-1111-1111-111111111122"),
                            ModuleId = Guid.Parse("11111111-1111-1111-1111-111111111121"),
                            Title = "Введение в логическую репликацию",
                            Description =
                                "Понятие публикации и подписки, репликация изменений на " +
                                "уровне отдельных таблиц.",
                            SortOrder = 1,
                            Lessons = new List<Lesson>
                            {
                                new Lesson
                                {
                                    Id = Guid.Parse("11111111-1111-1111-1111-111111111123"),
                                    TopicId = Guid.Parse("11111111-1111-1111-1111-111111111122"),
                                    Title = "Отличия логической репликации от физической",
                                    Content =
                                        "<p>Логическая репликация работает на уровне SQL-операций: " +
                                        "INSERT, UPDATE, DELETE, TRUNCATE. В отличие от физической " +
                                        "репликации, которая просто копирует байты страниц и передаёт " +
                                        "WAL-журнал на другой узел, логическая репликация разбирает " +
                                        "содержимое WAL, восстанавливает изменения на уровне строк " +
                                        "и применяет их на подписчике.</p>" +
                                        "<p>Ключевые различия:</p>" +
                                        "<ul>" +
                                        "<li>можно реплицировать только отдельные таблицы, а не весь кластер целиком;</li>" +
                                        "<li>подписчик может вносить свои изменения в реплицируемые таблицы;</li>" +
                                        "<li>возможна репликация между разными версиями PostgreSQL.</li>" +
                                        "</ul>" +
                                        "<p>Пример создания публикации:</p>" +
                                        "<pre>CREATE PUBLICATION pub_replica FOR TABLE accounts, payments;</pre>" +
                                        "<p>Пример создания подписки:</p>" +
                                        "<pre>CREATE SUBSCRIPTION sub_replica CONNECTION 'host=primary dbname=app user=replicator' " +
                                        "PUBLICATION pub_replica;</pre>",
                                    SortOrder = 1,
                                    IsPublished = true,
                                },
                            },
                        },
                        new CourseTopic
                        {
                            Id = Guid.Parse("11111111-1111-1111-1111-111111111125"),
                            ModuleId = Guid.Parse("11111111-1111-1111-1111-111111111121"),
                            Title = "Декодирование WAL",
                            Description =
                                "Как работает декодирование WAL: преобразование записей WAL " +
                                "в SQL-операции и доставка их на подписчика.",
                            SortOrder = 2,
                            Lessons = new List<Lesson>
                            {
                                new Lesson
                                {
                                    Id = Guid.Parse("11111111-1111-1111-1111-111111111124"),
                                    TopicId = Guid.Parse("11111111-1111-1111-1111-111111111125"),
                                    Title = "Декодирование WAL",
                                    Content =
                                        "<p>Логическая репликация использует декодирование WAL: " +
                                        "специальный процесс (logical decoding) читает записи WAL, " +
                                        "преобразует их в понятные SQL-операции и передаёт через " +
                                        "репликационный слот на подписчика.</p>" +
                                        "<p>Как это работает:</p>" +
                                        "<ol>" +
                                        "<li>На первичном сервере устанавливается <code>wal_level = logical</code>.</li>" +
                                        "<li>Создаётся репликационный слот типа <code>logical</code> с помощью плагина декодирования (например, <code>pgoutput</code>).</li>" +
                                        "<li>Плагин читает WAL, извлекает изменения строк и формирует их в виде сообщений репликации.</li>" +
                                        "<li>Подписчик получает эти сообщения и применяет их к своим таблицам.</li>" +
                                        "</ol>" +
                                        "<p>Пример создания слота:</p>" +
                                        "<pre>SELECT pg_create_logical_replication_slot('demo_slot', 'pgoutput');</pre>" +
                                        "<p>Декодирование позволяет фильтровать и преобразовывать изменения " +
                                        "перед их доставкой на подписчика, что делает логическую " +
                                        "репликацию более гибкой, по сравнению с физической.</p>",
                                    SortOrder = 1,
                                    IsPublished = true,
                                },
                            },
                        },
                        new CourseTopic
                        {
                            Id = Guid.Parse("11111111-1111-1111-1111-111111111126"),
                            ModuleId = Guid.Parse("11111111-1111-1111-1111-111111111121"),
                            Title = "Возможности фильтрации данных",
                            Description =
                                "Как фильтровать реплицируемые данные: какие таблицы и строки " +
                                "попадают в публикацию и подписку.",
                            SortOrder = 3,
                            Lessons = new List<Lesson>
                            {
                                new Lesson
                                {
                                    Id = Guid.Parse("11111111-1111-1111-1111-111111111127"),
                                    TopicId = Guid.Parse("11111111-1111-1111-1111-111111111126"),
                                    Title = "Подписки и публикации",
                                    Content =
                                        "<p>Публикация определяет, какие изменения отправляются с первичного сервера, " +
                                        "а подписка — какие из них принимаются на подписчике. С помощью публикации " +
                                        "можно выбрать отдельные таблицы, а с помощью подписки — управлять " +
                                        "подключением и набором реплицируемых объектов.</p>" +
                                        "<p>Пример создания публикации с фильтром по таблицам:</p>" +
                                        "<pre>CREATE PUBLICATION pub_replica FOR TABLE accounts, payments;</pre>" +
                                        "<p>Пример создания подписки:</p>" +
                                        "<pre>CREATE SUBSCRIPTION sub_replica CONNECTION 'host=primary dbname=app user=replicator' " +
                                        "PUBLICATION pub_replica;</pre>" +
                                        "<p>Состояние подписки можно проверить через представление " +
                                        "<code>pg_stat_subscription</code>:</p>" +
                                        "<pre>SELECT subname, subenabled, subconnectstate FROM pg_stat_subscription;</pre>",
                                    SortOrder = 1,
                                    IsPublished = true,
                                },
                            },
                        },
                    },
                },
            },
        };

        db.Courses.Add(course);

        // Tests belong to topics (TopicId foreign key). Add them explicitly since
        // CourseTopic does not expose a Tests navigation property.
        var testTopicId = Guid.Parse("11111111-1111-1111-1111-111111111122");
        db.Tests.Add(new Test
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111131"),
            TopicId = testTopicId,
            Title = "Логическая репликация: проверка знаний",
            Description = "Проверьте понимание различий между физической и логической репликацией.",
            SortOrder = 1,
            Questions = new List<TestQuestion>
            {
                new TestQuestion
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111132"),
                    TestId = Guid.Parse("11111111-1111-1111-1111-111111111131"),
                    Text = "Что логическая репликация делает с WAL-журналом?",
                    SortOrder = 1,
                    AnswerOptions = new List<AnswerOption>
                    {
                        new AnswerOption
                        {
                            Id = Guid.Parse("11111111-1111-1111-1111-111111111133"),
                            QuestionId = Guid.Parse("11111111-1111-1111-1111-111111111132"),
                            Text = "Просто передаёт его на другой узел без изменений.",
                            IsCorrect = false,
                            SortOrder = 1,
                        },
                        new AnswerOption
                        {
                            Id = Guid.Parse("11111111-1111-1111-1111-111111111134"),
                            QuestionId = Guid.Parse("11111111-1111-1111-1111-111111111132"),
                            Text = "Декодирует его, восстанавливая изменения на уровне строк.",
                            IsCorrect = true,
                            SortOrder = 2,
                        },
                        new AnswerOption
                        {
                            Id = Guid.Parse("11111111-1111-1111-1111-111111111135"),
                            QuestionId = Guid.Parse("11111111-1111-1111-1111-111111111132"),
                            Text = "Игнорирует его и копирует файлы данных.",
                            IsCorrect = false,
                            SortOrder = 3,
                        },
                    },
                },
            },
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<AppUser> EnsureDemoAuthorAsync(CancellationToken cancellationToken)
    {
        const string demoAuthorId = "11111111-1111-1111-1111-111111111100";
        const string demoAuthorEmail = "demo-author@dbms-lab.local";

        var author = await db.Users
            .FirstOrDefaultAsync(u => u.Email == demoAuthorEmail, cancellationToken);

        if (author is not null)
            return author;

        author = new AppUser
        {
            Id = Guid.Parse(demoAuthorId),
            UserName = demoAuthorEmail,
            NormalizedUserName = demoAuthorEmail.ToUpperInvariant(),
            Email = demoAuthorEmail,
            NormalizedEmail = demoAuthorEmail.ToUpperInvariant(),
            EmailConfirmed = true,
            FirstName = "Демо",
            LastName = "Автор",
            CreatedAtUtc = DateTime.UtcNow,
            // Password is not set: this record serves only as the owner of demo content
            // and is not intended for signing in.
            PasswordHash = null,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
        };

        db.Users.Add(author);
        await db.SaveChangesAsync(cancellationToken);

        return author;
    }
}
