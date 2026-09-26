using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

public class SessionItem
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string DateText { get; init; }
    public required string Meta { get; init; }
    public required string Muscles { get; init; }
    public required ICommand OpenCommand { get; init; }

    public static SessionItem Create(WorkoutSession s, DataStore store, StatsService stats, Units units) => new()
    {
        Id = s.Id,
        Name = s.Name,
        DateText = s.StartedAt.ToString("ddd, d MMM · HH:mm"),
        Meta = $"{Units.Duration(s.Duration)} · {units.FormatVolume(stats.SessionVolume(s))} · {s.WorkingSets.Count()} sets",
        Muscles = string.Join(", ", s.Exercises.Select(e => store.GetExercise(e.ExerciseId)?.PrimaryMuscle).OfType<MuscleGroup>().Distinct().Take(4).Select(m => m.Display())),
        OpenCommand = new AsyncRelayCommand(() => Shell.Current.GoToAsync($"{Routes.Session}?id={s.Id}")),
    };
}

public class DayItem
{
    public required string Letter { get; init; }
    public required string Day { get; init; }
    public required bool IsToday { get; init; }
    public required bool Done { get; init; }

    public Color Background => Done ? Color.FromArgb("#3F7DFF") : IsToday ? Color.FromArgb("#272C39") : Colors.Transparent;
    public Color Border => IsToday && !Done ? Color.FromArgb("#3F7DFF") : Colors.Transparent;
    public Color DayColor => Done || IsToday ? Colors.White : Color.FromArgb("#9AA3B5");
}

public class WorkoutPreviewItem
{
    public required string Name { get; init; }
    public required string Exercises { get; init; }
    public string Meta { get; init; } = "";
}

public class LineItem
{
    public required string Title { get; init; }
    public string Detail { get; init; } = "";
    public string Value { get; init; } = "";
    public ICommand? OpenCommand { get; init; }
}

public class MuscleBarItem
{
    public required string Name { get; init; }
    public required string Value { get; init; }
    public required double Progress { get; init; }
    public required Color Color { get; init; }
}

public partial class ChipItem(string title, object? value, Action<ChipItem> onSelect) : ObservableObject
{
    public string Title { get; } = title;
    public object? Value { get; } = value;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Background), nameof(TextColor))]
    bool isSelected;

    public Color Background => IsSelected ? Color.FromArgb("#3F7DFF") : Color.FromArgb("#1D212C");
    public Color TextColor => IsSelected ? Colors.White : Color.FromArgb("#9AA3B5");

    [RelayCommand]
    void Select() => onSelect(this);
}

public partial class OptionItem(string title, string subtitle, object value, Action<OptionItem> onSelect) : ObservableObject
{
    public string Title { get; } = title;
    public string Subtitle { get; } = subtitle;
    public bool HasSubtitle => !string.IsNullOrEmpty(Subtitle);
    public object Value { get; } = value;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Background), nameof(StrokeColor))]
    bool isSelected;

    public Color Background => IsSelected ? Color.FromArgb("#1A2A4F") : Color.FromArgb("#151821");
    public Color StrokeColor => IsSelected ? Color.FromArgb("#3F7DFF") : Color.FromArgb("#151821");

    [RelayCommand]
    void Select() => onSelect(this);
}

public partial class ExerciseItem(Exercise exercise, Action<ExerciseItem> onTap) : ObservableObject
{
    public Exercise Exercise { get; } = exercise;
    public string Name => Exercise.Name;
    public string Subtitle => Exercise.Subtitle + (Exercise.IsCustom ? " · Custom" : "");
    public string Initial => Exercise.PrimaryMuscle.Display()[..2];
    public Color MuscleColor => Exercise.PrimaryMuscle.Color();
    public Color MuscleSoft => Exercise.PrimaryMuscle.Color().WithAlpha(0.16f);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CheckBackground))]
    bool isSelected;

    public Color CheckBackground => IsSelected ? Color.FromArgb("#3F7DFF") : Color.FromArgb("#272C39");

    [RelayCommand]
    void Tap() => onTap(this);
}
