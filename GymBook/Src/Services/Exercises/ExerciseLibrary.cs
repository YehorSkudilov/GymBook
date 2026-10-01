using GymBook.Models;

namespace GymBook.Services;

/// <summary>
/// The built-in exercises, written by hand, one file per area of training (ExerciseLibrary.*.cs). Ids are stable
/// because plans and history reference them: an exercise can be renamed freely, but an id is never changed or
/// removed. Ids from earlier versions of the library resolve through <see cref="Aliases"/>.
/// </summary>
public static partial class ExerciseLibrary
{
    public static IReadOnlyList<Exercise> All { get; }

    static readonly Dictionary<string, Exercise> ById;
    static readonly Dictionary<string, ExerciseDetails> DetailsById;

    static ExerciseLibrary()
    {
        Def[] defs =
        [
            .. ChestExercises(), .. BackExercises(), .. ShoulderExercises(), .. ArmExercises(),
            .. QuadExercises(), .. PosteriorChainExercises(), .. CoreExercises(), .. NeckExercises(),
            .. PowerExercises(), .. CardioExercises(), .. PilatesExercises(), .. MobilityExercises(),
        ];
        All = [.. defs.Select(d => new Exercise
        {
            Id = d.Id,
            Name = d.Name,
            PrimaryMuscle = d.Primary,
            SecondaryMuscles = [.. d.Secondary],
            Equipment = d.Equipment,
            Mechanic = d.Mechanic,
            Instructions = d.Summary,
        })];
        ById = All.ToDictionary(e => e.Id);
        DetailsById = defs.ToDictionary(d => d.Id, d => new ExerciseDetails(d.Category, d.Level, d.Hold, d.Steps, d.Tips, d.Video));
    }

    /// <summary>The exercise with <paramref name="id"/>, or the one an id from an earlier library became.</summary>
    public static Exercise? Find(string id) => ById.GetValueOrDefault(Canonical(id));

    /// <summary>Today's id for <paramref name="id"/>: itself, unless it's from an earlier library.</summary>
    public static string Canonical(string id) => Aliases.GetValueOrDefault(id, id);

    /// <summary>Whether <paramref name="id"/> is from an earlier library and has a new id now.</summary>
    public static bool IsAlias(string id) => Aliases.ContainsKey(id);

    /// <summary>Steps, tips, kind of training and demonstration video of a built-in exercise.</summary>
    public static ExerciseDetails? Details(string id) => DetailsById.GetValueOrDefault(Canonical(id));

    /// <summary>
    /// A small picture of a built-in exercise: its illustration, shipped in the app as Resources/Raw/exercises/&lt;id&gt;.webp
    /// (made by tools/exercises/thumbs.mjs). Null for custom exercises, which show their muscle's initials instead.
    /// </summary>
    public static string? Thumbnail(string id) => ById.ContainsKey(Canonical(id)) && ExerciseThumbnailAssets.Exists(Canonical(id))
        ? $"exercises/{Canonical(id)}.webp"
        : null;

    /// <summary>
    /// A looping demonstration of a built-in exercise in its picture's style, shown instead of the video offline:
    /// shipped in the app as Resources/Raw/exercise-animations/&lt;id&gt;.gif (made by tools/exercises/anims.mjs).
    /// Null when this build has none.
    /// </summary>
    public static string? Animation(string id) => ById.ContainsKey(Canonical(id)) && ExerciseAnimationAssets.Exists(Canonical(id))
        ? $"exercise-animations/{Canonical(id)}.gif"
        : null;

    /// <summary>One exercise as it's written in the library files.</summary>
    sealed class Def(string id, string name, MuscleGroup primary, Equipment equipment, Mechanic mechanic)
    {
        public string Id => id;
        public string Name => name;
        public MuscleGroup Primary => primary;
        public Equipment Equipment => equipment;
        public Mechanic Mechanic => mechanic;
        public IReadOnlyList<MuscleGroup> Secondary { get; init; } = [];
        public ExerciseCategory Category { get; init; } = ExerciseCategory.Strength;
        public ExerciseLevel Level { get; init; } = ExerciseLevel.Beginner;
        public bool Hold { get; init; }
        /// <summary>A sentence or two: what it is and how it's done. Shown above the steps.</summary>
        public required string Summary { get; init; }
        public IReadOnlyList<string> Steps { get; init; } = [];
        /// <summary>Form cues and the common mistakes.</summary>
        public IReadOnlyList<string> Tips { get; init; } = [];
        public ExerciseVideo? Video { get; init; }
    }
}
