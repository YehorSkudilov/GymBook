using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Contracts;
using GymBook.Models;
using GymBook.Services;
using GymBook.Services.Sync;

namespace GymBook.ViewModels;

/// <summary>
/// The public profile, at the top of the Profile tab: the username the app calls the user by and everyone else sees
/// them as (on leaderboards, friends lists and shared plans), a picture and a short bio, plus the friend
/// code to give friends. Nothing else about the user is public. It needs the account.
/// </summary>
public partial class PublicProfileViewModel(ApiClient api, DialogService dialogs, AccountService account,
    GymBook.Services.Billing.SubscriptionService subscriptions, IServiceProvider services, RanksViewModel ranks, NutritionService nutrition) : BaseViewModel
{
    /// <summary>The picture as sent: a JPEG this many pixels on its longer side.</summary>
    const int AvatarPixels = 320;

    [ObservableProperty] bool isLoading = true;
    [ObservableProperty] bool loadFailed;
    [ObservableProperty] bool exists;
    [ObservableProperty] string username = "";
    [ObservableProperty] string bio = "";
    [ObservableProperty] string? avatarUrl;
    [ObservableProperty] string friendCode = "";
    /// <summary>On the leaderboards (strength and nutrition): the trophy by the name is lit.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RanksIconColor))]
    bool inRanks;
    public Color RanksIconColor => InRanks ? Color.FromArgb("#FFC857") : Color.FromArgb("#626B7E");
    [ObservableProperty] bool isBusy;

    // ---- The badges under the profile, on the leaderboards only: the overall tiers of Progress's and Nutrition's Ranks ----

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowBadges))]
    bool hasBadges;
    public bool ShowBadges => ShowProfile && InRanks && HasBadges;
    [ObservableProperty] string strengthTier = "";
    [ObservableProperty] Color strengthColor = Colors.Gray;
    [ObservableProperty] string nutritionTier = "";
    [ObservableProperty] Color nutritionColor = Colors.Gray;

    partial void OnInRanksChanged(bool value) => OnPropertyChanged(nameof(ShowBadges));

    /// <summary>
    /// Strength from the server (it ranks the synced workouts); nutrition as the app works it out, the numbers it sends to
    /// Nutrition's boards. Left as they were when the server can't be reached.
    /// </summary>
    async Task LoadBadgesAsync()
    {
        if (!InRanks)
            return;
        var streaks = nutrition.Streaks();
        var food = Ranks.Tier(Ranks.NutritionScore(streaks.Logging, streaks.OnTargetLast30));
        NutritionTier = RanksViewModel.TierName(food);
        NutritionColor = RanksViewModel.TierColor(food);
        try
        {
            var strength = Ranks.Tier((await api.GetRanksAsync()).Score);
            StrengthTier = RanksViewModel.TierName(strength);
            StrengthColor = RanksViewModel.TierColor(strength);
            HasBadges = true;
        }
        catch (Exception e) when (Online.Message(e) != null)
        {
        }
    }

    public string Initial => Username.Length > 0 ? Username[..1].ToUpperInvariant() : "?";
    public bool HasAvatar => AvatarUrl != null;
    /// <summary>A Gym Book Pro member: a ring in Pro's gradient round the picture.</summary>
    public bool IsPro => subscriptions.IsMember;

    /// <summary>The account's email, under the name. Not public: only the user sees it.</summary>
    public string Email => account.Email ?? "";

    /// <summary>The account or subscription changed (see ProfileViewModel): the email and the ring follow.</summary>
    public void RefreshAccount()
    {
        OnPropertyChanged(nameof(Email));
        OnPropertyChanged(nameof(IsPro));
    }

    /// <summary>The pencil next to the name: a menu of what to change, each in its own popup.</summary>
    [RelayCommand]
    async Task EditProfile()
    {
        if (!Exists)
            return;
        const string username = "Username", bio = "Bio", picture = "Picture", email = "Email";
        switch (await dialogs.ActionSheet("Edit profile", null, username, bio, picture, email))
        {
            case username:
                await EditUsername();
                break;
            case bio:
                await EditBio();
                break;
            case picture:
                await ChangePicture();
                break;
            case email:
                await Views.ManageAccountPage.ShowAsync(services, AccountChange.Email);
                break;
        }
    }

    public string BioText => Bio.Length > 0 ? Bio : "Add a few words about your training";

    public bool IsSignedIn => account.IsSignedIn;
    /// <summary>Signed out: no username, just the invitation to sign in.</summary>
    public bool ShowSignIn => !IsSignedIn;
    // Always the first section once loaded, username or not (a server from before every account had one may say none).
    public bool ShowProfile => IsSignedIn && !IsLoading && (Exists || !LoadFailed);
    /// <summary>Couldn't load, and nothing saved to show instead.</summary>
    public bool ShowLoadFailed => IsSignedIn && LoadFailed && !Exists;
    public bool ShowLoading => IsSignedIn && IsLoading;

    partial void OnIsLoadingChanged(bool value) => OnStateChanged();
    partial void OnLoadFailedChanged(bool value) => OnStateChanged();
    partial void OnExistsChanged(bool value) => OnStateChanged();

    void OnStateChanged()
    {
        OnPropertyChanged(nameof(IsSignedIn));
        OnPropertyChanged(nameof(ShowSignIn));
        OnPropertyChanged(nameof(ShowProfile));
        OnPropertyChanged(nameof(ShowLoadFailed));
        OnPropertyChanged(nameof(ShowLoading));
        OnPropertyChanged(nameof(ShowBadges));
    }

    partial void OnUsernameChanged(string value) => OnPropertyChanged(nameof(Initial));
    partial void OnAvatarUrlChanged(string? value) => OnPropertyChanged(nameof(HasAvatar));
    partial void OnBioChanged(string value) => OnPropertyChanged(nameof(BioText));

    public override async Task OnAppearingAsync()
    {
        OnStateChanged();
        if (!account.IsSignedIn)
        {
            (Exists, Username, Bio, AvatarUrl, FriendCode) = (false, "", "", null, "");
            IsLoading = false;
            return;
        }
        // The saved username shows at once; the rest comes from the server.
        if (!Exists && account.Username is { } saved)
            (Exists, Username) = (true, saved);
        IsLoading = !Exists;
        try
        {
            Show(await api.GetMyPublicProfileAsync());
            LoadFailed = false;
            await LoadBadgesAsync();
        }
        catch (Exception e) when (Online.Message(e) != null)
        {
            LoadFailed = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    void Show(MyPublicProfile p)
    {
        // What the app calls the user (Home, a finished workout), offline too.
        account.Username = p.Exists ? p.Username : null;
        Exists = p.Exists;
        Username = p.Username;
        Bio = p.Bio;
        AvatarUrl = ApiClient.AvatarUrl(p.AvatarPath);
        FriendCode = p.FriendCode;
        InRanks = p.InRanks;
    }

    [RelayCommand]
    Task Retry() => OnAppearingAsync();

    /// <summary>
    /// The trophy by the name: joins the leaderboards, or leaves them, as the Ranks tabs do (the same questions: the
    /// gender if it isn't set, and confirming), then shows where things stand.
    /// </summary>
    [RelayCommand]
    async Task ToggleRanks()
    {
        if (!Exists)
            return;
        if (InRanks)
            await ranks.LeaveCommand.ExecuteAsync(null);
        else
            await ranks.JoinCommand.ExecuteAsync(null);
        await OnAppearingAsync();
    }

    async Task EditUsername()
    {
        var name = await dialogs.Prompt("Username",
            $"What others see you as. {SocialLimits.UsernameMinLength}–{SocialLimits.UsernameMaxLength} letters, numbers, dots or underscores.",
            Username, Keyboard.Plain);
        if (name == null || (name = name.Trim().TrimStart('@')) == Username)
            return;
        await SaveAsync(name, Bio);
    }

    async Task EditBio()
    {
        if (!Exists)
            return;
        var bio = await dialogs.Prompt("Bio", $"A line or two about your training, up to {SocialLimits.BioLength} characters.", Bio, Keyboard.Text);
        if (bio != null && (bio = Clip(bio, SocialLimits.BioLength)) != Bio)
            await SaveAsync(Username, bio);
    }

    [RelayCommand]
    async Task ChangePicture()
    {
        if (!Exists)
            return;
        if (HasAvatar)
        {
            const string choose = "Choose a new picture";
            var choice = await dialogs.ActionSheet("Profile picture", "Remove picture", choose);
            if (choice == null)
                return;
            if (choice != choose)
            {
                await RunAsync("Couldn't remove the picture", () => api.RemoveAvatarAsync());
                return;
            }
        }

        FileResult? photo;
        try
        {
            photo = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions { Title = "Profile picture" });
        }
        catch (PermissionException)
        {
            await dialogs.Alert("No access to photos", "Allow Gym Book to access your photos in the system settings to pick a picture.");
            return;
        }
        if (photo == null)
            return;

        var bytes = await ShrinkAsync(photo);
        if (bytes == null || bytes.Length > SocialLimits.AvatarBytes)
        {
            await dialogs.Alert("That picture didn't work", "Pick a JPEG or PNG photo.");
            return;
        }
        await RunAsync("Couldn't save the picture", () => api.SetAvatarAsync(bytes));
    }

    /// <summary>The photo as a small JPEG: profile pictures show at most a few dozen points wide.</summary>
    static async Task<byte[]?> ShrinkAsync(FileResult photo)
    {
        await using var stream = await photo.OpenReadAsync();
#if ANDROID || IOS || MACCATALYST
        try
        {
            using var image = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(stream);
            var small = image.Width > AvatarPixels || image.Height > AvatarPixels ? image.Downsize(AvatarPixels) : image;
            using var output = new MemoryStream();
            await small.SaveAsync(output, ImageFormat.Jpeg, 0.85f);
            if (!ReferenceEquals(small, image))
                small.Dispose();
            return output.ToArray();
        }
        catch (Exception)
        {
            return null;
        }
#else
        // No image scaling here: the picture goes as it is, if it's small enough.
        using var output = new MemoryStream();
        await stream.CopyToAsync(output);
        return output.ToArray();
#endif
    }

    [RelayCommand]
    async Task ShareFriendCode()
    {
        if (FriendCode.Length == 0)
            return;
        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = "Add me on Gym Book",
            Text = $"Add me on Gym Book: my friend code is {FriendCode} (or find me as @{Username}).",
        });
    }

    Task SaveAsync(string username, string bio) =>
        RunAsync("Couldn't save your profile",
            () => api.SavePublicProfileAsync(new SavePublicProfileRequest { Username = username, Bio = bio }));

    async Task RunAsync(string failTitle, Func<Task<MyPublicProfile>> call)
    {
        if (IsBusy)
            return;
        IsBusy = true;
        try
        {
            if (await Online.Try(dialogs, failTitle, call) is { } profile)
                Show(profile);
        }
        finally
        {
            IsBusy = false;
        }
    }

    static string Clip(string s, int max)
    {
        s = s.Trim();
        return s.Length <= max ? s : s[..max];
    }
}
