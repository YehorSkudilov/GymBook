using System.ComponentModel.DataAnnotations;
using GymBook.Data;
using GymBook.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GymBook.Api.Data;

public class AppUser : IdentityUser
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Bumped on every sync write; each written record is stamped with it so clients can pull "everything after N".</summary>
    [ConcurrencyCheck]
    public long SyncVersion { get; set; }
}

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = "";

    /// <summary>SHA-256 of the token. The token itself is never stored, so a database leak doesn't hand out sessions.</summary>
    public string TokenHash { get; set; } = "";

    /// <summary>All tokens descended from one sign-in. Reusing a rotated token revokes the whole family.</summary>
    public Guid FamilyId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}

/// <summary>One AI-generated plan, kept to enforce the per-user quota (see <see cref="Plans.PlanQuota"/>).</summary>
public class PlanGeneration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = "";
    /// <summary>What was generated: "plan" or "chat" (a message to the plan chat), each with its own quota.</summary>
    public string Kind { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Server database. User data reuses the shared records; ownership (<see cref="UserIdColumn"/>) and the
/// sync stamp (<see cref="VersionColumn"/>) are shadow properties, so nothing a client sends can set them,
/// and a global query filter scopes every query to the signed-in user.
/// </summary>
public class ApiDbContext(DbContextOptions<ApiDbContext> options, ICurrentUser currentUser) : IdentityUserContext<AppUser>(options)
{
    public const string UserIdColumn = "UserId";
    public const string VersionColumn = "Version";

    public DbSet<WorkoutPlan> Plans => Set<WorkoutPlan>();
    public DbSet<WorkoutSession> Sessions => Set<WorkoutSession>();
    public DbSet<Exercise> CustomExercises => Set<Exercise>();
    public DbSet<BodyWeightEntry> BodyWeights => Set<BodyWeightEntry>();
    public DbSet<UserProfile> Profiles => Set<UserProfile>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PlanGeneration> PlanGenerations => Set<PlanGeneration>();

    /// <summary>Read by the query filters on every query; null (no signed-in user) matches nothing.</summary>
    string? CurrentUserId => currentUser.UserId;

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        DomainModel.Map(model);

        OwnedByUser<WorkoutPlan>(model).HasKey(UserIdColumn, nameof(WorkoutPlan.Id));
        OwnedByUser<WorkoutSession>(model).HasKey(UserIdColumn, nameof(WorkoutSession.Id));
        OwnedByUser<Exercise>(model).HasKey(UserIdColumn, nameof(Exercise.Id));
        OwnedByUser<BodyWeightEntry>(model).HasKey(UserIdColumn, nameof(BodyWeightEntry.Id));
        OwnedByUser<UserProfile>(model).HasKey(UserIdColumn);

        // Workout times are wall-clock local times (see WallClockDateTimeConverter), so they carry no zone.
        const string wallClock = "timestamp without time zone";
        model.Entity<WorkoutPlan>().Property(p => p.CreatedAt).HasColumnType(wallClock);
        model.Entity<WorkoutSession>().Property(s => s.StartedAt).HasColumnType(wallClock);
        model.Entity<WorkoutSession>().Property(s => s.EndedAt).HasColumnType(wallClock);
        model.Entity<BodyWeightEntry>().Property(b => b.Date).HasColumnType(wallClock);
        // Inside jsonb, Npgsql only writes UTC-kind DateTimes. Label the wall-clock value UTC on the way in and
        // drop the label on the way out; the stored digits are the same.
        model.Entity<WorkoutSession>().ComplexCollection(s => s.Exercises, e => e.ComplexCollection(x => x.Sets, s =>
            s.Property(p => p.CompletedAt).HasConversion(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc), v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified)))));

        model.Entity<RefreshToken>(b =>
        {
            b.ToTable("refresh_tokens");
            b.Property(t => t.TokenHash).HasMaxLength(64);
            b.HasIndex(t => t.TokenHash).IsUnique();
            b.HasIndex(t => t.FamilyId);
            b.HasOne<AppUser>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<PlanGeneration>(b =>
        {
            b.ToTable("plan_generations");
            b.Property(g => g.Kind).HasMaxLength(16);
            b.HasIndex(g => new { g.UserId, g.Kind, g.CreatedAt });
            b.HasOne<AppUser>().WithMany().HasForeignKey(g => g.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> OwnedByUser<T>(ModelBuilder model) where T : class
    {
        var b = model.Entity<T>();
        b.Property<string>(UserIdColumn).HasMaxLength(450);
        b.Property<long>(VersionColumn);
        b.HasIndex(UserIdColumn, VersionColumn);
        // Deleting an account deletes all of its data.
        b.HasOne<AppUser>().WithMany().HasForeignKey(UserIdColumn).OnDelete(DeleteBehavior.Cascade);
        b.HasQueryFilter(e => EF.Property<string>(e, UserIdColumn) == CurrentUserId);
        return b;
    }
}

public interface ICurrentUser
{
    string? UserId { get; }
}

public class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string? UserId => accessor.HttpContext?.User.FindFirst("sub")?.Value;
}

/// <summary>Used by `dotnet ef` only. Never connects anywhere real.</summary>
public class ApiDbContextFactory : IDesignTimeDbContextFactory<ApiDbContext>
{
    public ApiDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<ApiDbContext>().UseNpgsql("Host=localhost;Database=design_time").Options, new NoUser());

    sealed class NoUser : ICurrentUser
    {
        public string? UserId => null;
    }
}
