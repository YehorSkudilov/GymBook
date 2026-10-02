using GymBook.Models;
using Microsoft.EntityFrameworkCore;

namespace GymBook.Data;

/// <summary>
/// EF mapping of the shared records, used by both the device database and the server database. Each side
/// adds its own keys and bookkeeping columns on top (shadow properties, so they never appear on the wire).
/// Plans and sessions store their nested workouts/exercises/sets as a JSON column: they are always loaded
/// and saved whole, which is also the unit of sync.
/// </summary>
public static class DomainModel
{
    public static void Map(ModelBuilder model)
    {
        model.Entity<WorkoutPlan>(b =>
        {
            b.ToTable("plans");
            b.ComplexCollection(p => p.Workouts, w =>
            {
                w.ToJson();
                w.ComplexCollection(x => x.Exercises);
            });
        });

        model.Entity<WorkoutSession>(b =>
        {
            b.ToTable("sessions");
            b.Ignore(s => s.WorkingSets);
            b.ComplexCollection(s => s.Exercises, e =>
            {
                e.ToJson();
                e.ComplexCollection(x => x.Sets);
            });
        });

        model.Entity<Exercise>().ToTable("custom_exercises");

        model.Entity<BodyWeightEntry>(b =>
        {
            b.ToTable("body_weights");
            // Id falls back to the date, so it must be read through the property, not the backing field.
            b.Property(e => e.Id).UsePropertyAccessMode(PropertyAccessMode.Property);
        });

        model.Entity<FoodEntry>().ToTable("food_entries");
        model.Entity<SupplementDose>().ToTable("supplement_doses");

        model.Entity<HealthDay>(b =>
        {
            b.ToTable("health_days");
            b.Property(e => e.Id).UsePropertyAccessMode(PropertyAccessMode.Property);
        });

        model.Entity<UserProfile>().ToTable("profiles");
    }
}
