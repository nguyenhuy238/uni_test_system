using Microsoft.EntityFrameworkCore;
using Employee_Survey.Domain;
using System.Text.Json;

namespace Employee_Survey.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Test> Tests { get; set; }
    public DbSet<Assignment> Assignments { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }
    public DbSet<RolePermissionMapping> RolePermissions { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<PasswordReset> PasswordResets { get; set; }
    public DbSet<SystemSettings> SystemSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Question entity
        modelBuilder.Entity<Question>(entity =>
        {
            entity.Property(e => e.Options)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<string>>(v, JsonOptions) ?? new List<string>())
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.CorrectKeys)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<string>>(v, JsonOptions) ?? new List<string>())
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.MatchingPairs)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<MatchPair>>(v, JsonOptions) ?? new List<MatchPair>())
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.DragDrop)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<DragDropConfig>(v, JsonOptions))
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.Tags)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<string>>(v, JsonOptions) ?? new List<string>())
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.Media)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<MediaFile>>(v, JsonOptions) ?? new List<MediaFile>())
                .HasColumnType("nvarchar(max)");
        });

        // Configure Test entity
        modelBuilder.Entity<Test>(entity =>
        {
            entity.Property(e => e.QuestionIds)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<string>>(v, JsonOptions) ?? new List<string>())
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.Items)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<TestItem>>(v, JsonOptions) ?? new List<TestItem>())
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.FrozenRandom)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<FrozenRandomConfig>(v, JsonOptions))
                .HasColumnType("nvarchar(max)");
        });

        // Configure Session entity
        modelBuilder.Entity<Session>(entity =>
        {
            entity.Property(e => e.Answers)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<Answer>>(v, JsonOptions) ?? new List<Answer>())
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.Snapshot)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<Question>>(v, JsonOptions) ?? new List<Question>())
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.Items)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<SessionItem>>(v, JsonOptions) ?? new List<SessionItem>())
                .HasColumnType("nvarchar(max)");
        });

        modelBuilder.Entity<RolePermissionMapping>(entity =>
        {
            entity.Property(e => e.Permissions)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<string>>(v, JsonOptions) ?? new List<string>())
                .HasColumnType("nvarchar(max)");
        });

        // --- Relationships & Foreign Keys ---

        // User -> Team
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasOne(d => d.Team)
                .WithMany(p => p.Users)
                .HasForeignKey(d => d.TeamId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Team -> Department
        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasOne(d => d.Department)
                .WithMany(p => p.Teams)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Session -> User & Test
        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Test)
                .WithMany(p => p.Sessions)
                .HasForeignKey(d => d.TestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Feedback -> Session
        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasOne(d => d.Session)
                .WithMany()
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Notification -> User
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PasswordReset -> User
        modelBuilder.Entity<PasswordReset>(entity =>
        {
            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
