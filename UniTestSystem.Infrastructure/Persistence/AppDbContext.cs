using Microsoft.EntityFrameworkCore;
using UniTestSystem.Domain;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace UniTestSystem.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<Lecturer> Lecturers { get; set; }
    public DbSet<StudentClass> StudentClasses { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<QuestionBank> QuestionBanks { get; set; }
    public DbSet<Test> Tests { get; set; }
    public DbSet<Assessment> Assessments { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<StudentAnswer> StudentAnswers { get; set; }
    public DbSet<SessionLog> SessionLogs { get; set; }
    public DbSet<DeviceFingerprint> DeviceFingerprints { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }
    public DbSet<RolePermissionMapping> RolePermissions { get; set; }
    public DbSet<Faculty> Faculties { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<PasswordReset> PasswordResets { get; set; }
    public DbSet<SystemSettings> SystemSettings { get; set; }
    public DbSet<AuditEntry> AuditEntries { get; set; }
    public DbSet<Option> Options { get; set; }
    public DbSet<TestQuestion> TestQuestions { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<TestQuestionSnapshot> TestQuestionSnapshots { get; set; }

    // NEW academic entities
    public DbSet<Course> Courses { get; set; }
    public DbSet<Enrollment> Enrollments { get; set; }
    public DbSet<ExamSchedule> ExamSchedules { get; set; }
    public DbSet<Transcript> Transcripts { get; set; }
    public DbSet<Result> Results { get; set; }

    // Question bank normalization
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<Skill> Skills { get; set; }
    public DbSet<DifficultyLevel> DifficultyLevels { get; set; }
    public DbSet<SessionQuestionSnapshot> SessionQuestionSnapshots { get; set; }

    // RBAC
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RolePermission> RolePermissionEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // [OPTIMIZATION] Indexes for high-load queries
        modelBuilder.Entity<Session>(entity =>
        {
            entity.ToTable("Session");
            entity.HasIndex(e => new { e.UserId, e.Status }); // Critical for dashboard/reports
            entity.HasIndex(e => e.EndAt).HasFilter("[EndAt] IS NOT NULL");
            
            entity.Property(e => e.AutoScore).HasColumnType("decimal(5,2)");
            entity.Property(e => e.ManualScore).HasColumnType("decimal(5,2)");
            entity.Property(e => e.TotalScore).HasColumnType("decimal(5,2)");
            entity.Property(e => e.MaxScore).HasColumnType("decimal(5,2)");
            entity.Property(e => e.Percent).HasColumnType("decimal(5,2)");

            entity.HasMany(e => e.StudentAnswers)
                .WithOne(a => a.Session)
                .HasForeignKey(a => a.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Logs)
                .WithOne(l => l.Session)
                .HasForeignKey(l => l.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.DeviceFingerprints)
                .WithOne(f => f.Session)
                .HasForeignKey(f => f.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Test)
                .WithMany(p => p.Sessions)
                .HasForeignKey(d => d.TestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.At).IsRequired();
            entity.HasIndex(e => e.At); // Critical for log readers
            entity.HasIndex(e => e.Actor);
        });

        // Configure Question entity
        modelBuilder.Entity<Question>(entity =>
        {
            entity.ToTable("Question");

            entity.HasMany(e => e.Options)
                .WithOne()
                .HasForeignKey(o => o.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.MatchingPairs)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<MatchPair>>(v, JsonOptions) ?? new List<MatchPair>(),
                    new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<MatchPair>>(
                        (c1, c2) => c1!.SequenceEqual(c2!),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()))
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.DragDrop)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<DragDropConfig>(v, JsonOptions))
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.Tags)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<string>>(v, JsonOptions) ?? new List<string>(),
                    new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                        (c1, c2) => c1!.SequenceEqual(c2!),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()))
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.Media)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<MediaFile>>(v, JsonOptions) ?? new List<MediaFile>(),
                    new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<MediaFile>>(
                        (c1, c2) => c1!.SequenceEqual(c2!),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()))
                .HasColumnType("nvarchar(max)");

            entity.HasOne(e => e.Subject)
                .WithMany()
                .HasForeignKey(e => e.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.DifficultyLevel)
                .WithMany()
                .HasForeignKey(e => e.DifficultyLevelId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Skill)
                .WithMany()
                .HasForeignKey(e => e.SkillId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ParentQuestion)
                .WithMany()
                .HasForeignKey(e => e.ParentQuestionId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.QuestionBank)
                .WithMany(b => b.Questions)
                .HasForeignKey(e => e.QuestionBankId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure Test entity
        modelBuilder.Entity<Test>(entity =>
        {
            entity.ToTable("Test");

            // Test -> Assessment
            entity.HasOne(d => d.Assessment)
                .WithOne()
                .HasForeignKey<Test>(d => d.AssessmentId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            // Test -> Course
            entity.HasOne(d => d.Course)
                .WithMany(p => p.Tests)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasQueryFilter(e => !e.IsDeleted);

            entity.Property(e => e.TotalMaxScore).HasPrecision(18, 2); // Fix warning 30000

            entity.HasMany(e => e.QuestionSnapshots)
                .WithOne(s => s.Test)
                .HasForeignKey(s => s.TestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure TestQuestion many-to-many
        modelBuilder.Entity<TestQuestion>(entity =>
        {
            entity.ToTable("TestQuestion");
            entity.HasKey(tq => new { tq.TestId, tq.QuestionId });

            entity.HasOne(tq => tq.Test)
                .WithMany(t => t.TestQuestions)
                .HasForeignKey(tq => tq.TestId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false); // Fix warning 10622

            entity.HasOne(tq => tq.Question)
                .WithMany(q => q.TestQuestions)
                .HasForeignKey(tq => tq.QuestionId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false); // Fix warning 10622

            entity.Property(e => e.Points).HasPrecision(18, 2); // Fix warning 30000
        });

        modelBuilder.Entity<RolePermissionMapping>(entity =>
        {
            entity.ToTable("RolePermissionMapping");
            entity.Property(e => e.Permissions)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<string>>(v, JsonOptions) ?? new List<string>(),
                    new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                        (c1, c2) => c1!.SequenceEqual(c2!),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()))
                .HasColumnType("nvarchar(max)");
        });

        // --- Relationships & Foreign Keys ---

        // User -> StudentClass (TPT)
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("Student");
            entity.HasIndex(e => e.StudentCode).IsUnique();
            
            entity.HasOne(d => d.StudentClass)
                .WithMany(p => p.Students)
                .HasForeignKey(d => d.StudentClassId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Lecturer>(entity =>
        {
            entity.ToTable("Lecturer");
            entity.HasIndex(e => e.LecturerCode).IsUnique();
            
            entity.HasOne(d => d.Faculty)
                .WithMany(p => p.Lecturers) // Fix warning 10625: use existing collection
                .HasForeignKey(d => d.FacultyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // StudentClass -> Faculty
        modelBuilder.Entity<StudentClass>(entity =>
        {
            entity.ToTable("StudentClass");
            entity.HasOne(d => d.Faculty)
                .WithMany(p => p.StudentClasses)
                .HasForeignKey(d => d.FacultyId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<Faculty>(entity =>
        {
            entity.ToTable("Faculty");
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
        
        modelBuilder.Entity<Assessment>(entity =>
        {
            entity.ToTable("Assessment");
            entity.Property(e => e.Weight).HasColumnType("decimal(5,2)");
            
            entity.HasOne(d => d.Course)
                .WithMany()
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<QuestionBank>(entity =>
        {
            entity.ToTable("QuestionBank");
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasOne(e => e.Course)
                .WithMany(c => c.QuestionBanks)
                .HasForeignKey(e => e.CourseId)
                .IsRequired(false); // Fix warning 10622
        });
        
        modelBuilder.Entity<StudentAnswer>(entity =>
        {
            entity.ToTable("StudentAnswer");
            entity.HasOne(a => a.Question)
                  .WithMany()
                  .HasForeignKey(a => a.QuestionId)
                  .OnDelete(DeleteBehavior.NoAction)
                  .IsRequired(false); // Fix warning 10622
            entity.HasOne(a => a.Session)
                .WithMany(s => s.StudentAnswers)
                .HasForeignKey(a => a.SessionId)
                .IsRequired(false); // Fix warning 10622

            entity.Property(e => e.Score).HasPrecision(18, 2); // Fix warning 30000
            entity.Property(e => e.GradedAt).HasColumnType("datetime2");
        });

        modelBuilder.Entity<SessionLog>(entity =>
        {
            entity.ToTable("SessionLog");
            entity.HasOne(l => l.Session)
                .WithMany(s => s.Logs)
                .HasForeignKey(l => l.SessionId)
                .IsRequired(false); // Fix warning 10622
        });

        modelBuilder.Entity<DeviceFingerprint>(entity =>
        {
            entity.ToTable("DeviceFingerprint");
            entity.HasOne(f => f.Session)
                .WithMany(s => s.DeviceFingerprints)
                .HasForeignKey(f => f.SessionId)
                .IsRequired(false); // Fix warning 10622
        });

        modelBuilder.Entity<Feedback>(entity => 
        {
            entity.ToTable("Feedback");
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasOne(e => e.Session)
                .WithMany(s => s.Feedbacks)
                .HasForeignKey(e => e.SessionId)
                .IsRequired(false); // Fix warning 10622
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notification");
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .IsRequired(false); // Fix warning 10622
        });

        modelBuilder.Entity<PasswordReset>(entity =>
        {
            entity.ToTable("PasswordReset");
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .IsRequired(false); // Fix warning 10622
        });

        modelBuilder.Entity<SystemSettings>(entity =>
        {
            entity.ToTable("SystemSettings");
            entity.Property(e => e.WarningGpaThreshold).HasColumnType("decimal(4,2)");
            entity.Property(e => e.FailGpaThreshold).HasColumnType("decimal(4,2)");
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.ToTable("Enrollment");
            entity.HasIndex(e => new { e.StudentId, e.CourseId }).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
            
            entity.HasOne(d => d.Student)
                .WithMany()
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Course)
                .WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.FinalScore).HasColumnType("decimal(5,2)");
            entity.Property(e => e.Grade).HasColumnType("nvarchar(2)");
            entity.Property(e => e.GradePoint).HasColumnType("decimal(3,1)");
        });

        modelBuilder.Entity<ExamSchedule>(entity =>
        {
            entity.ToTable("ExamSchedule");
            entity.HasIndex(e => new { e.CourseId, e.StartTime }).IsUnique();
            
            entity.HasOne(d => d.Test)
                .WithMany()
                .HasForeignKey(d => d.TestId)
                .OnDelete(DeleteBehavior.NoAction); // Avoid multiple cascade paths

            entity.HasOne(d => d.Course)
                .WithMany()
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false); // Fix warning 10622

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<Transcript>(entity =>
        {
            entity.ToTable("Transcript");
            entity.Property(e => e.GPA).HasColumnType("decimal(3,2)");
            entity.Property(e => e.YearEndGpa4).HasColumnType("decimal(3,2)");
            entity.Property(e => e.YearEndGpa10).HasColumnType("decimal(4,2)");
            entity.Property(e => e.AcademicStatus).HasColumnType("nvarchar(16)");
            entity.HasIndex(e => new { e.StudentId, e.AcademicYear });
            
            entity.HasOne(d => d.Student)
                .WithMany()
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false); // Fix warning 10622

            entity.HasQueryFilter(e => !e.IsDeleted);
        });
        
        modelBuilder.Entity<Result>(entity =>
        {
            entity.ToTable("Result");
            entity.Property(e => e.Score).HasColumnType("decimal(5,2)");
            entity.Property(e => e.MaxScore).HasColumnType("decimal(5,2)");

            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.NoAction); // Avoid multiple cascade paths

            entity.HasOne(d => d.Test)
                .WithMany()
                .HasForeignKey(d => d.TestId)
                .OnDelete(DeleteBehavior.NoAction); // Avoid multiple cascade paths

            entity.HasOne(d => d.Session)
                .WithMany()
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Course -> Lecturer (User)
        modelBuilder.Entity<Course>(entity =>
        {
            entity.ToTable("Course");
            entity.HasOne(d => d.Lecturer)
                .WithMany()
                .HasForeignKey(d => d.LecturerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);

            entity.HasMany(e => e.QuestionBanks)
                .WithOne(b => b.Course)
                .HasForeignKey(b => b.CourseId)
                .IsRequired(false); // Fix warning 10622
        });

        // SessionQuestionSnapshot
        modelBuilder.Entity<SessionQuestionSnapshot>(entity =>
        {
            entity.ToTable("SessionQuestionSnapshot");
            entity.Property(e => e.Points).HasColumnType("decimal(5,2)");
            entity.HasOne(e => e.Session)
                .WithMany()
                .HasForeignKey(e => e.SessionId)
                .IsRequired(false); // Fix warning 10622
        });

        // TestQuestionSnapshot
        modelBuilder.Entity<TestQuestionSnapshot>(entity =>
        {
            entity.ToTable("TestQuestionSnapshot");
            entity.Property(e => e.Points).HasColumnType("decimal(5,2)");
            entity.HasOne(e => e.Test)
                .WithMany(t => t.QuestionSnapshots)
                .HasForeignKey(e => e.TestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RBAC
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRole");
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId)
                .IsRequired(false); // Fix warning 10622
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermission");
            entity.HasOne(e => e.Permission).WithMany().HasForeignKey(e => e.PermissionId);
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.ToTable("UserSession");
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserSessions)
                .HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshToken");
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is not AuditEntry && (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            .ToList();

        // [SECURE AUDIT] Attempt to resolve actor from HttpContext
        var actor = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "System";

        foreach (var entry in entries)
        {
            var audit = new AuditEntry
            {
                At = DateTime.UtcNow,
                Actor = actor,
                Action = entry.State.ToString(),
                EntityName = entry.Entity.GetType().Name,
                EntityId = GetEntityId(entry),
                Before = entry.State == EntityState.Modified || entry.State == EntityState.Deleted 
                    ? JsonSerializer.Serialize(entry.OriginalValues.ToObject(), JsonOptions) 
                    : null,
                After = entry.State == EntityState.Added || entry.State == EntityState.Modified 
                    ? JsonSerializer.Serialize(entry.CurrentValues.ToObject(), JsonOptions) 
                    : null
            };
            AuditEntries.Add(audit);
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    private static string GetEntityId(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var primaryKey = entry.Metadata.FindPrimaryKey();
        if (primaryKey == null || primaryKey.Properties.Count == 0)
        {
            return "0";
        }

        var keyValues = primaryKey.Properties
            .Select(keyProperty =>
            {
                var propertyEntry = entry.Properties.FirstOrDefault(p => p.Metadata.Name == keyProperty.Name);
                if (propertyEntry == null)
                {
                    return $"{keyProperty.Name}=null";
                }

                var value = entry.State == EntityState.Deleted ? propertyEntry.OriginalValue : propertyEntry.CurrentValue;
                return $"{keyProperty.Name}={value ?? "null"}";
            });

        return string.Join("|", keyValues);
    }
}
