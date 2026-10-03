using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Contracts;
using GymBook.Models;
using GymBook.Services;
using GymBook.Services.Sync;

namespace GymBook.ViewModels;

/// <summary>
/// A chat with the AI about one plan: the user asks for changes (or why something is the way it is) and the AI
/// replies, changing the plan when asked. It only talks about the plan. Opened from the wizard's result (the plan
/// isn't saved yet, so changes just show up there) and from a saved plan (changes are saved straight away).
/// </summary>
public partial class PlanChatViewModel(DataStore store, AiPlanService ai, GymBook.Services.Billing.SubscriptionService subscriptions)
    : BaseViewModel, IQueryAttributable
{
    WorkoutPlan? _plan;
    UserProfile? _answers;
    bool _save;
    Action? _onChanged;

    public ObservableCollection<ChatMessageItem> Messages { get; } = [];

    /// <summary>One-tap starters, shown until the first message.</summary>
    public List<SuggestionItem> Suggestions => _suggestions ??= [.. new[] { "Make the workouts shorter", "Swap an exercise I don't like", "More focus on arms", "I have a sore knee", "Why this split?" }
        .Select(s => new SuggestionItem(s, new AsyncRelayCommand(() => Suggest(s))))];
    List<SuggestionItem>? _suggestions;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    string draft = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    bool isSending;

    [ObservableProperty] bool showSuggestions = true;
    [ObservableProperty] string title = "";
    [ObservableProperty] string quotaText = "";

    public bool CanSend => !IsSending && Draft.Trim().Length > 0;

    /// <summary>Opens the chat for <paramref name="plan"/>; <paramref name="onChanged"/> runs after each change the AI makes.</summary>
    public static Task OpenAsync(WorkoutPlan plan, UserProfile answers, bool save, Action? onChanged = null) =>
        Shell.Current.GoToAsync(Routes.PlanChat, new Dictionary<string, object>
        {
            ["plan"] = plan,
            ["answers"] = answers,
            ["save"] = save,
            ["changed"] = onChanged ?? (() => { }),
        });

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (_plan != null)
            return;
        _plan = query["plan"] as WorkoutPlan;
        _answers = query["answers"] as UserProfile;
        _save = query["save"] is true;
        _onChanged = query["changed"] as Action;
        Title = _plan?.Name ?? "Plan chat";
        Messages.Add(ChatMessageItem.Ai(
            $"Hi! I can change {_plan?.Name ?? "your plan"} for you: swap exercises, change the days or the length of the workouts, " +
            "work around an injury, or explain why something is there. What would you like?"));
        ShowQuota();
    }

    void ShowQuota() => QuotaText = ai.ChatQuota is { } q ? AiPlanService.Describe(q, "messages") : "";

    Task Suggest(string text)
    {
        Draft = text;
        return Send();
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    async Task Send()
    {
        if (_plan == null || _answers == null || !CanSend)
            return;
        // The AI coach needs Gym Book Pro.
        if (!await ProViewModel.RequireAsync(subscriptions, $"The AI coach needs {GymBook.Contracts.SubscriptionProducts.Name}. Start with a free trial."))
            return;
        var text = Draft.Trim();
        if (text.Length > PlanLimits.ChatMessageLength)
            text = text[..PlanLimits.ChatMessageLength];
        Draft = "";
        ShowSuggestions = false;
        Messages.Add(ChatMessageItem.User(text));
        var thinking = ChatMessageItem.Note("Thinking…");
        Messages.Add(thinking);
        IsSending = true;
        try
        {
            // Everything said so far (not the notes), with the greeting kept out: it's the app's, not the AI's.
            var history = Messages.Skip(1)
                .Where(m => !m.IsNote)
                .Select(m => new PlanChatMessage { FromUser = m.FromUser, Text = m.Text })
                .TakeLast(PlanLimits.MaxChatMessages)
                .ToList();
            var (reply, changed) = await ai.ChatAsync(_plan, _answers, history);
            Messages.Remove(thinking);
            Messages.Add(ChatMessageItem.Ai(reply));
            if (changed)
            {
                if (_save)
                    store.Save();
                Title = _plan.Name;
                Messages.Add(ChatMessageItem.Note(_save ? "Plan updated and saved" : "Plan updated"));
                _onChanged?.Invoke();
            }
        }
        catch (Exception e)
        {
            Messages.Remove(thinking);
            Messages.Add(ChatMessageItem.Note(e is ApiException or SessionExpiredException ? e.Message : "Couldn't reach the AI. Check your connection and try again."));
        }
        finally
        {
            IsSending = false;
            ShowQuota();
        }
    }

    [RelayCommand]
    Task Close() => GoBack();
}

/// <summary>A one-tap starter message.</summary>
public record SuggestionItem(string Text, IAsyncRelayCommand Command);

/// <summary>A bubble in the plan chat: the user's, the AI's, or a small note from the app (e.g. "Plan updated").</summary>
public class ChatMessageItem
{
    public string Text { get; private init; } = "";
    public bool FromUser { get; private init; }
    public bool IsNote { get; private init; }
    public bool IsBubble => !IsNote;

    public LayoutOptions Alignment => IsNote ? LayoutOptions.Center : FromUser ? LayoutOptions.End : LayoutOptions.Start;
    public Color Background => FromUser ? Color.FromArgb("#3F7DFF") : Color.FromArgb("#1D212C");
    public Color TextColor => FromUser ? Colors.White : Color.FromArgb("#F4F6FB");

    public static ChatMessageItem User(string text) => new() { Text = text, FromUser = true };
    public static ChatMessageItem Ai(string text) => new() { Text = text };
    public static ChatMessageItem Note(string text) => new() { Text = text, IsNote = true };
}
