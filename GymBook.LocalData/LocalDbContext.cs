using GymBook.Data;
using GymBook.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GymBook.LocalData;

/// <summary>The on-device database. Works fully offline; <see cref="Dirty"/> marks rows not yet pushed to the server.</summary>
public class LocalDbContext(DbContextOptions<LocalDbContext> options) : DbContext(options)
{
    /// <summary>Shadow column set when a row changes locally and cleared once the server has it.</summary>
    public const string Dirty = "Dirty";

    /// <summary>Shadow key of the single profile row.</summary>
    public const string ProfileKey = "Key";

    public DbSet<WorkoutPlan> Plans => Set<WorkoutPlan>();
    public DbSet<WorkoutSession> Sessions => Set<WorkoutSession>();
    public DbSet<Exercise> CustomExercises => Set<Exercise>();
    public DbSet<BodyWeightEntry> BodyWeights => Set<BodyWeightEntry>();
    public DbSet<FoodEntry> FoodEntries => Set<FoodEntry>();
    public DbSet<HealthDay> HealthDays => Set<HealthDay>();
    public DbSet<UserProfile> Profiles => Set<UserProfile>();
    public DbSet<LocalSetting> Settings => Set<LocalSetting>();

    public static DbContextOptions<LocalDbContext> CreateOptions(string path) =>
        new DbContextOptionsBuilder<LocalDbContext>()
            .UseSqlite(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder { DataSource = path }.ToString())
            .Options;

    protected override void OnModelCreating(ModelBuilder model)
    {
        DomainModel.Map(model);

        foreach (var type in new[] { typeof(WorkoutPlan), typeof(WorkoutSession), typeof(Exercise), typeof(BodyWeightEntry), typeof(FoodEntry), typeof(HealthDay), typeof(UserProfile) })
            model.Entity(type).Property<bool>(Dirty);

        model.Entity<UserProfile>(b =>
        {
            b.Property<int>(ProfileKey).ValueGeneratedNever();
            b.HasKey(ProfileKey);
        });

        model.Entity<LocalSetting>(b =>
        {
            b.ToTable("settings");
            b.HasKey(s => s.Key);
        });
    }
}

/// <summary>Device-only key/value state: the in-progress workout, sync cursor, signed-in account.</summary>
public class LocalSetting
{
    public string Key { get; set; } = "";
    public string? Value { get; set; }
}

/// <summary>Lets `dotnet ef migrations add` build the context outside the app.</summary>
public class LocalDbContextFactory : IDesignTimeDbContextFactory<LocalDbContext>
{
    public LocalDbContext CreateDbContext(string[] args) => new(LocalDbContext.CreateOptions("design-time.db"));
}
