using GymBook.Views;

namespace GymBook.Services;

/// <summary>
/// Dialogs for view models, so they don't reach into the visual tree. Shown as the app's own bottom sheets
/// (<see cref="DialogSheet"/>) rather than the platform's alert boxes, so they look like the rest of the app.
/// </summary>
public class DialogService
{
    public Task Alert(string title, string message) => DialogSheet.Alert(title, message);

    public Task<bool> Confirm(string title, string message, string accept = "Yes", string cancel = "Cancel") =>
        DialogSheet.Confirm(title, message, accept, cancel);

    /// <summary>A menu of <paramref name="options"/>, with <paramref name="destructive"/> (if any) last and in red. Null when cancelled.</summary>
    public Task<string?> ActionSheet(string title, string? destructive, params string[] options) =>
        DialogSheet.Menu(title, destructive, options);

    /// <summary>A menu as above, with on/off <paramref name="switches"/> above the options, applied as they're flipped.</summary>
    public Task<string?> ActionSheet(string title, string? destructive, IReadOnlyList<MenuSwitch> switches, params string[] options) =>
        DialogSheet.Menu(title, destructive, options, switches);

    /// <summary>Whole numbers, each picked on the number pad or stepped with − and +; null when cancelled.</summary>
    public Task<int[]?> Numbers(string title, string? message, string accept, params NumberField[] fields) =>
        DialogSheet.Numbers(title, message, fields, accept);

    /// <summary>Reps per set, exact or a range; (min, max), the same twice for exact, or null when cancelled.</summary>
    public Task<(int Min, int Max)?> Reps(string title, int min, int max) => DialogSheet.Reps(title, min, max);

    /// <summary>An amount to add or subtract, typed or stepped; signed (negative to subtract), or null when cancelled.</summary>
    public Task<double?> Change(string title, string? message, string unit, double step) => DialogSheet.Change(title, message, unit, step);

    /// <summary>A rest time in seconds, typed as m:ss or stepped by 15 s; null when cancelled.</summary>
    public Task<int?> RestTime(string title, int seconds) => DialogSheet.RestTime(title, seconds);

    /// <summary>A date and time of day (no later than today); null when cancelled.</summary>
    public Task<DateTime?> DateAndTime(string title, string? message, DateTime initial, string accept = "Save") =>
        DialogSheet.DateAndTime(title, message, initial, accept);

    public Task<string?> Prompt(string title, string message, string? initial = null, Keyboard? keyboard = null, string accept = "Save") =>
        DialogSheet.Prompt(title, message, initial ?? "", keyboard ?? Keyboard.Text, accept);

    /// <summary>A hidden password box, with a button to show what was typed.</summary>
    public Task<string?> PasswordPrompt(string title, string message, string accept) => DialogSheet.PasswordPrompt(title, message, accept);
}
