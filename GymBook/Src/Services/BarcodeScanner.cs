namespace GymBook.Services;

/// <summary>
/// Scans a product's barcode with the camera: on Android, Google's code scanner (Platforms/Android/GoogleBarcodeScanner.cs),
/// which shows its own camera screen and needs no camera permission. Elsewhere there's none yet (<see cref="NoBarcodeScanner"/>).
/// </summary>
public interface IBarcodeScanner
{
    bool IsAvailable { get; }

    /// <summary>The barcode's digits; null when cancelled.</summary>
    Task<string?> ScanAsync();
}

public class NoBarcodeScanner : IBarcodeScanner
{
    public bool IsAvailable => false;

    public Task<string?> ScanAsync() => Task.FromResult<string?>(null);
}
