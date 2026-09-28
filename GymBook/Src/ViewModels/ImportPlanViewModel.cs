using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Contracts;
using GymBook.Services;
using GymBook.Services.Sync;

namespace GymBook.ViewModels;

/// <summary>
/// Imports a plan from elsewhere: pasted text, a link (to a web page, image or document), or a file (a photo or screenshot, a PDF, or a text
/// file). The AI reads it and rebuilds it from the app's exercises; the new plan opens when it's done.
/// </summary>
public partial class ImportPlanViewModel(DataStore store, AiPlanService ai) : BaseViewModel
{
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

    public bool CanImport => !IsImporting && (Text.Trim().Length > 0 || Link.Trim().Length > 0 || _file != null);

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
        IsImporting = true;
        try
        {
            var plan = await ai.ImportAsync(Text, Link, _file);
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
