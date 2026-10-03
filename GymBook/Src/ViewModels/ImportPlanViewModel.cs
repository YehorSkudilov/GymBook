using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Contracts;
using GymBook.Services;
using GymBook.Services.Import;
using GymBook.Services.Sync;

namespace GymBook.ViewModels;

/// <summary>The import sheet's tabs: the AI's three ways in, and another app's CSV exports, plans and workouts apart.</summary>
public enum ImportMode { Paste, Link, File, Plans, Workouts }

/// <summary>A tab at the top of the import sheet; the selected one is filled with the accent.</summary>
public partial class ImportTab(string title, ImportMode mode, Action<ImportMode> select) : ObservableObject
{
    public string Title => title;
    public ImportMode Mode => mode;
    public IRelayCommand SelectCommand { get; } = new RelayCommand(() => select(mode));

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Background), nameof(TextColor))]
    bool isSelected;

    public Color Background => IsSelected ? Color.FromArgb("#3F7DFF") : Color.FromArgb("#1D212C");
    public Color TextColor => IsSelected ? Colors.White : Color.FromArgb("#9AA3B5");
}

/// <summary>
/// Imports from elsewhere, one way per tab. The AI tabs: pasted text, a link (to a web page, image or document), or a file
/// (a photo or screenshot, a PDF, or a text file), which the AI reads and rebuilds as a plan from the app's exercises; the
/// new plan opens when it's done. The CSV tabs: another app's exports of plans and of workout history, separate files
/// (see <see cref="ImportCsvViewModel"/>).
/// </summary>
public partial class ImportPlanViewModel : BaseViewModel
{
    readonly DataStore store;
    readonly AiPlanService ai;

    readonly GymBook.Services.Billing.SubscriptionService subscriptions;

    public ImportPlanViewModel(DataStore store, AiPlanService ai, CsvImporter importer, ExercisePickerService picker, DialogService dialogs,
        GymBook.Services.Billing.SubscriptionService subscriptions)
    {
        this.store = store;
        this.ai = ai;
        this.subscriptions = subscriptions;
        Tabs =
        [
            new("Paste", ImportMode.Paste, SelectMode),
            new("Link", ImportMode.Link, SelectMode),
            new("File", ImportMode.File, SelectMode),
            new("Plans CSV", ImportMode.Plans, SelectMode),
            new("Workouts CSV", ImportMode.Workouts, SelectMode),
        ];
        Tabs[0].IsSelected = true;
        PlansImport = new ImportCsvViewModel(CsvImportKind.Plans, importer, picker, dialogs);
        WorkoutsImport = new ImportCsvViewModel(CsvImportKind.Workouts, importer, picker, dialogs);
        PlansImport.Imported += () => _ = GoBack();
        WorkoutsImport.Imported += () => _ = GoBack();
    }

    public IReadOnlyList<ImportTab> Tabs { get; }
    public ImportCsvViewModel PlansImport { get; }
    public ImportCsvViewModel WorkoutsImport { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPaste), nameof(IsLink), nameof(IsFile), nameof(IsAi), nameof(IsPlans), nameof(IsWorkouts), nameof(CanImport))]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
    ImportMode mode;

    public bool IsPaste => Mode == ImportMode.Paste;
    public bool IsLink => Mode == ImportMode.Link;
    public bool IsFile => Mode == ImportMode.File;
    public bool IsAi => Mode is ImportMode.Paste or ImportMode.Link or ImportMode.File;
    public bool IsPlans => Mode == ImportMode.Plans;
    public bool IsWorkouts => Mode == ImportMode.Workouts;

    void SelectMode(ImportMode value)
    {
        Mode = value;
        foreach (var tab in Tabs)
            tab.IsSelected = tab.Mode == value;
        Error = "";
    }

    /// <summary>What the API accepts once base64-encoded.</summary>
    const long MaxFileBytes = 7 * 1024 * 1024;

    ImportFile? _file;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanImport))]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
    string text = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanImport))]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
    string link = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFile), nameof(CanImport))]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
    string fileName = "";
    public bool HasFile => FileName.Length > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanImport))]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
    bool isImporting;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    string error = "";
    public bool HasError => Error.Length > 0;

    [ObservableProperty] string quotaText = "";

    /// <summary>The AI tab's own input is there: each tab imports only what it shows.</summary>
    public bool CanImport => !IsImporting && Mode switch
    {
        ImportMode.Paste => Text.Trim().Length > 0,
        ImportMode.Link => Link.Trim().Length > 0,
        ImportMode.File => _file != null,
        _ => false,
    };

    public override async Task OnAppearingAsync()
    {
        QuotaText = ai.Quota is { } cached ? AiPlanService.Describe(cached) : "";
        if (await ai.RefreshQuotaAsync() is { } q)
            QuotaText = AiPlanService.Describe(q);
    }

    static readonly FilePickerFileType FileTypes = new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        [DevicePlatform.Android] = ["image/*", "application/pdf", "text/plain"],
        [DevicePlatform.iOS] = ["public.image", "com.adobe.pdf", "public.plain-text"],
        [DevicePlatform.MacCatalyst] = ["public.image", "com.adobe.pdf", "public.plain-text"],
        [DevicePlatform.WinUI] = [".jpg", ".jpeg", ".png", ".webp", ".gif", ".pdf", ".txt", ".csv", ".md"],
    });

    [RelayCommand]
    async Task PickFile()
    {
        Error = "";
        FileResult? picked;
        try
        {
            picked = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Choose a plan to import", FileTypes = FileTypes });
        }
        catch (Exception)
        {
            Error = "Couldn't open the file picker.";
            return;
        }
        if (picked == null)
            return;

        await using var stream = await picked.OpenReadAsync();
        if (stream.CanSeek && stream.Length > MaxFileBytes)
        {
            Error = "That file is too big. Pick one under 7 MB, or paste the plan as text.";
            return;
        }
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        if (buffer.Length > MaxFileBytes)
        {
            Error = "That file is too big. Pick one under 7 MB, or paste the plan as text.";
            return;
        }
        _file = new ImportFile
        {
            Name = picked.FileName,
            ContentType = ContentType(picked),
            Base64 = Convert.ToBase64String(buffer.ToArray()),
        };
        FileName = picked.FileName;
    }

    [RelayCommand]
    void RemoveFile()
    {
        _file = null;
        FileName = "";
    }

    /// <summary>Not every platform reports a content type, so fall back to the extension.</summary>
    static string ContentType(FileResult file)
    {
        if (!string.IsNullOrEmpty(file.ContentType) && file.ContentType != "application/octet-stream")
            return file.ContentType;
        return Path.GetExtension(file.FileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            _ => "text/plain",
        };
    }

    [RelayCommand(CanExecute = nameof(CanImport))]
    async Task Import()
    {
        Error = "";
        // The AI tabs need the account (the CSV tabs don't).
        if (!ai.IsAvailable)
        {
            Error = "Importing with AI needs an account. Sign in from the Profile tab.";
            return;
        }
        if (!await ProViewModel.RequireAsync(subscriptions, $"Importing with AI needs {GymBook.Contracts.SubscriptionProducts.Name}. Start with a free trial. (Importing a CSV from another app doesn't.)"))
            return;
        IsImporting = true;
        try
        {
            var plan = Mode switch
            {
                ImportMode.Paste => await ai.ImportAsync(Text, "", null),
                ImportMode.Link => await ai.ImportAsync("", Link, null),
                _ => await ai.ImportAsync("", "", _file),
            };
            store.Data.Plans.Add(plan);
            store.Data.ActivePlanId ??= plan.Id;
            store.Save();
            // Close the sheet and open the new plan.
            await GoTo($"../{Routes.Plan}?id={plan.Id}");
        }
        catch (Exception e)
        {
            Error = e is ApiException or SessionExpiredException ? e.Message : "Couldn't import the plan. Check your connection and try again.";
            if (ai.Quota is { } q)
                QuotaText = AiPlanService.Describe(q);
        }
        finally
        {
            IsImporting = false;
        }
    }

    [RelayCommand]
    Task Close() => GoBack();
}
