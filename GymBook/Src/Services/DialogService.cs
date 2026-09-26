namespace GymBook.Services;

/// <summary>Thin wrapper over page dialogs so view models don't reach into the visual tree.</summary>
public class DialogService
{
    static Page Page => Shell.Current?.CurrentPage ?? Application.Current!.Windows[0].Page!;

    public Task Alert(string title, string message) => Page.DisplayAlertAsync(title, message, "OK");

    public Task<bool> Confirm(string title, string message, string accept = "Yes", string cancel = "Cancel") =>
        Page.DisplayAlertAsync(title, message, accept, cancel);

    public async Task<string?> ActionSheet(string title, string? destructive, params string[] options)
    {
        var result = await Page.DisplayActionSheetAsync(title, "Cancel", destructive, options);
        return result is null or "Cancel" ? null : result;
    }

    public Task<string?> Prompt(string title, string message, string? initial = null, Keyboard? keyboard = null, string accept = "Save") =>
        Page.DisplayPromptAsync(title, message, accept, "Cancel", initialValue: initial ?? "", keyboard: keyboard ?? Keyboard.Text);
}
