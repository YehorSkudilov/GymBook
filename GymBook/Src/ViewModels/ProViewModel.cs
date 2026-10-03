using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Contracts;
using GymBook.Services;
using GymBook.Services.Billing;
using GymBook.Services.Sync;

namespace GymBook.ViewModels;

public class ProOfferItem
{
    public required string Title { get; init; }
    public required string Price { get; init; }
    public string? Trial { get; init; }
    public bool HasTrial => Trial != null;
    public string? Badge { get; init; }
    public bool HasBadge => Badge != null;
    public required IAsyncRelayCommand BuyCommand { get; init; }
}

/// <summary>
/// The Gym Book Pro sheet: what it unlocks (the AI features), the store's plans with their free trial, restoring a
/// purchase, and, once subscribed, managing it. <see cref="RequireAsync"/> opens it before an AI feature for someone
/// without Pro.
/// </summary>
public partial class ProViewModel(SubscriptionService subscriptions, DialogService dialogs) : BaseViewModel, IQueryAttributable
{
    TaskCompletionSource<bool>? _result;
    bool _closing;

    [ObservableProperty] bool isLoading = true;
    [ObservableProperty] bool isBusy;
    [ObservableProperty] bool isActive;
    [ObservableProperty] string activeText = "";
    [ObservableProperty] List<ProOfferItem> offers = [];
    [ObservableProperty] string? offersError;
    /// <summary>Why the sheet opened, e.g. "AI plans need Gym Book Pro".</summary>
    [ObservableProperty] string reason = "";

    public bool HasOffersError => OffersError != null;
    public bool HasReason => Reason.Length > 0;
    public bool CanBuy => !IsActive && Offers.Count > 0;
    public string Name => SubscriptionProducts.Name;

    partial void OnOffersErrorChanged(string? value) => OnPropertyChanged(nameof(HasOffersError));
    partial void OnReasonChanged(string value) => OnPropertyChanged(nameof(HasReason));
    partial void OnOffersChanged(List<ProOfferItem> value) => OnPropertyChanged(nameof(CanBuy));
    partial void OnIsActiveChanged(bool value) => OnPropertyChanged(nameof(CanBuy));

    /// <summary>
    /// True when the account has Pro; otherwise opens the sheet and waits: true if the user subscribed (or restored)
    /// there, false if they closed it.
    /// </summary>
    public static async Task<bool> RequireAsync(SubscriptionService subscriptions, string reason)
    {
        if (subscriptions.IsActive || (await subscriptions.RefreshAsync())?.Active == true)
            return true;
        return await ShowAsync(reason);
    }

    /// <summary>Opens the sheet; the result says whether the account has Pro when it closes.</summary>
    public static Task<bool> ShowAsync(string reason = "")
    {
        var result = new TaskCompletionSource<bool>();
        var query = new Dictionary<string, object> { ["result"] = result, ["reason"] = reason };
        if (Shell.Current is { } shell)
            _ = shell.GoToAsync(Routes.Pro, query);
        // First-run onboarding has no Shell yet: the sheet goes over it as a modal page.
        else if (Application.Current?.Windows.FirstOrDefault()?.Page is { } root
                 && IPlatformApplication.Current?.Services.GetService<Views.ProPage>() is { BindingContext: ProViewModel vm } page)
        {
            query["modal"] = true;
            vm.ApplyQueryAttributes(query);
            _ = root.Navigation.PushModalAsync(page, false);
        }
        else
            result.TrySetResult(false);
        return result.Task;
    }

    bool _modal;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _result ??= query.TryGetValue("result", out var r) ? r as TaskCompletionSource<bool> : null;
        Reason = query.TryGetValue("reason", out var why) ? why as string ?? "" : "";
        _modal = query.ContainsKey("modal");
    }

    public override async Task OnAppearingAsync()
    {
        if (!IsLoading)
            return;
        ShowStatus();
        await subscriptions.RefreshAsync();
        ShowStatus();
        if (!IsActive)
            await LoadOffersAsync();
        IsLoading = false;
    }

    void ShowStatus()
    {
        IsActive = subscriptions.IsActive;
        var s = subscriptions.Status;
        ActiveText = s is { Active: true, ExpiresAt: { } until }
            ? (s.InTrial ? "Free trial" : "Subscribed") + (s.AutoRenewing ? $", renews {until.LocalDateTime:d MMM yyyy}" : $", ends {until.LocalDateTime:d MMM yyyy}")
              + (s.Store != subscriptions.Store && s.Store is { } other ? $" · bought in the {StoreName(other)}" : "")
            : "Subscribed";
    }

    async Task LoadOffersAsync()
    {
        OffersError = null;
        if (subscriptions.Store == null)
        {
            OffersError = $"{Name} can't be bought on this device yet. Subscribe on your phone, and it works here too.";
            return;
        }
        try
        {
            var offers = await subscriptions.GetOffersAsync();
            Offers = [.. offers.Select(o => new ProOfferItem
            {
                Title = o.Period == "year" ? "Yearly" : "Monthly",
                Price = $"{o.Price} / {o.Period}",
                Trial = o.Trial is { } t ? $"{char.ToUpper(t[0])}{t[1..]}, then {o.Price} / {o.Period}" : null,
                Badge = o.Period == "year" ? "Best value" : null,
                BuyCommand = new AsyncRelayCommand(() => BuyAsync(o.ProductId)),
            })];
            if (Offers.Count == 0)
                OffersError = "The plans aren't available from the store right now. Try again later.";
        }
        catch (StoreBillingException e)
        {
            OffersError = e.Message;
        }
    }

    async Task BuyAsync(string productId)
    {
        if (IsBusy)
            return;
        IsBusy = true;
        try
        {
            if (await subscriptions.PurchaseAsync(productId))
            {
                ShowStatus();
                await dialogs.Alert($"Welcome to {Name}", "The AI features are unlocked: build plans, import them and ask the AI coach anything.");
                await CloseAsync();
            }
        }
        catch (Exception e) when (e is StoreBillingException || Online.Message(e) != null)
        {
            await dialogs.Alert("The purchase didn't go through", e is StoreBillingException ? e.Message : Online.Message(e)!);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    async Task Restore()
    {
        if (IsBusy)
            return;
        IsBusy = true;
        try
        {
            if (await subscriptions.RestoreAsync())
            {
                ShowStatus();
                await dialogs.Alert("Restored", $"{Name} is active on this account.");
                await CloseAsync();
            }
            else
                await dialogs.Alert("Nothing to restore", $"No {Name} subscription was found for this store account.");
        }
        catch (Exception e) when (e is StoreBillingException || Online.Message(e) != null)
        {
            await dialogs.Alert("Couldn't restore", e is StoreBillingException ? e.Message : Online.Message(e)!);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>The store's subscription page, where it's changed or cancelled.</summary>
    [RelayCommand]
    Task Manage() => Launcher.Default.OpenAsync(subscriptions.Status?.Store is { } bought && bought != subscriptions.Store
        ? StoreManageUrl(bought)
        : subscriptions.ManageUrl);

    [RelayCommand]
    Task OpenTerms() => Launcher.Default.OpenAsync(new Uri("https://gymbook.app/terms"));

    [RelayCommand]
    Task OpenPrivacy() => Launcher.Default.OpenAsync(new Uri("https://gymbook.app/privacy"));

    [RelayCommand]
    Task Close() => CloseAsync();

    async Task CloseAsync()
    {
        _closing = true;
        if (_modal && Application.Current?.Windows.FirstOrDefault()?.Page is { } root)
            await root.Navigation.PopModalAsync(false);
        else
            await GoBack();
        _result?.TrySetResult(subscriptions.IsActive);
    }

    // Closed with the back button or a swipe.
    public override void OnDisappearing()
    {
        if (!_closing)
            _result?.TrySetResult(subscriptions.IsActive);
    }

    static string StoreName(SubscriptionStore store) => store switch
    {
        SubscriptionStore.GooglePlay => "Google Play Store",
        SubscriptionStore.AppStore => "App Store",
        _ => "Microsoft Store",
    };

    static Uri StoreManageUrl(SubscriptionStore store) => new(store switch
    {
        SubscriptionStore.GooglePlay => "https://play.google.com/store/account/subscriptions",
        SubscriptionStore.AppStore => "https://apps.apple.com/account/subscriptions",
        _ => "https://account.microsoft.com/services",
    });
}
