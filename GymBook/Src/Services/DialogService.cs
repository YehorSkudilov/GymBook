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

    public Task<string?> Prompt(string title, string message, string? initial = null, Keyboard? keyboard = null, string accept = "Save") =>
        DialogSheet.Prompt(title, message, initial ?? "", keyboard ?? Keyboard.Text, accept);

    /// <summary>A hidden password box, with a button to show what was typed.</summary>
    public Task<string?> PasswordPrompt(string title, string message, string accept) => DialogSheet.PasswordPrompt(title, message, accept);
}
