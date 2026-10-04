using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Contracts;
using GymBook.Services;
using GymBook.Services.Sync;

namespace GymBook.ViewModels;

/// <summary>
/// The public profile, at the top of the Profile tab: the username the app calls the user by and everyone else sees
/// them as (on leaderboards, friends lists and shared plans), a picture, a short bio and a home gym, plus the friend
/// code to give friends. Nothing else about the user is public. It needs the account.
/// </summary>
public partial class PublicProfileViewModel(ApiClient api, DialogService dialogs, AccountService account) : BaseViewModel
{
    /// <summary>The picture as sent: a JPEG this many pixels on its longer side.</summary>
    const int AvatarPixels = 320;

    [ObservableProperty] bool isLoading = true;
    [ObservableProperty] bool loadFailed;
    [ObservableProperty] bool exists;
    [ObservableProperty] string username = "";
    [ObservableProperty] string bio = "";
    [ObservableProperty] string homeGym = "";
    [ObservableProperty] string? avatarUrl;
    [ObservableProperty] string friendCode = "";
    [ObservableProperty] bool isBusy;

    public string Initial => Username.Length > 0 ? Username[..1].ToUpperInvariant() : "?";
    public bool HasAvatar => AvatarUrl != null;
    public string BioText => Bio.Length > 0 ? Bio : "Add a few words about your training";
    public string HomeGymText => HomeGym.Length > 0 ? HomeGym : "Not set";

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
    }

    partial void OnUsernameChanged(string value) => OnPropertyChanged(nameof(Initial));
    partial void OnAvatarUrlChanged(string? value) => OnPropertyChanged(nameof(HasAvatar));
    partial void OnBioChanged(string value) => OnPropertyChanged(nameof(BioText));
    partial void OnHomeGymChanged(string value) => OnPropertyChanged(nameof(HomeGymText));

    public override async Task OnAppearingAsync()
    {
        OnStateChanged();
        if (!account.IsSignedIn)
        {
            (Exists, Username, Bio, HomeGym, AvatarUrl, FriendCode) = (false, "", "", "", null, "");
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
        HomeGym = p.HomeGym;
        AvatarUrl = ApiClient.AvatarUrl(p.AvatarPath);
        FriendCode = p.FriendCode;
    }

    [RelayCommand]
    Task Retry() => OnAppearingAsync();

    [RelayCommand]
    async Task EditUsername()
    {
        var name = await dialogs.Prompt("Username",
            $"What others see you as. {SocialLimits.UsernameMinLength}–{SocialLimits.UsernameMaxLength} letters, numbers, dots or underscores.",
            Username, Keyboard.Plain);
        if (name == null || (name = name.Trim().TrimStart('@')) == Username)
            return;
        await SaveAsync(name, Bio, HomeGym);
    }

    [RelayCommand]
    async Task EditBio()
    {
        if (!Exists)
            return;
        var bio = await dialogs.Prompt("Bio", $"A line or two about your training, up to {SocialLimits.BioLength} characters.", Bio, Keyboard.Text);
        if (bio != null)
            await SaveAsync(Username, Clip(bio, SocialLimits.BioLength), HomeGym);
    }

    [RelayCommand]
    async Task EditHomeGym()
    {
        if (!Exists)
            return;
        var gym = await dialogs.Prompt("Home gym", "Where you usually train. Leave it empty to not show one.", HomeGym, Keyboard.Text);
        if (gym != null)
            await SaveAsync(Username, Bio, Clip(gym, SocialLimits.HomeGymLength));
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

    [RelayCommand]
    async Task CopyFriendCode()
    {
        if (FriendCode.Length == 0)
            return;
        await Clipboard.Default.SetTextAsync(FriendCode);
        await dialogs.Alert("Copied", $"Your friend code {FriendCode} is on the clipboard.");
    }

    Task SaveAsync(string username, string bio, string homeGym) =>
        RunAsync("Couldn't save your profile",
            () => api.SavePublicProfileAsync(new SavePublicProfileRequest { Username = username, Bio = bio, HomeGym = homeGym }));

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
