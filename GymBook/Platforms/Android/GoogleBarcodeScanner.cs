using Android.Gms.Extensions;
using GymBook.Services;
using Xamarin.Google.MLKit.Vision.Barcode.Common;
using Xamarin.Google.MLKit.Vision.CodeScanner;

namespace GymBook;

/// <summary>
/// Barcodes through Google's code scanner (Google Play services): it shows its own camera screen, so Gym Book needs no
/// camera permission and never sees the camera. Product barcodes only (EAN and UPC).
/// </summary>
public class GoogleBarcodeScanner : IBarcodeScanner
{
    public bool IsAvailable => true;

    public async Task<string?> ScanAsync()
    {
        var options = new GmsBarcodeScannerOptions.Builder()
            .SetBarcodeFormats(Barcode.FormatEan13, Barcode.FormatEan8, Barcode.FormatUpcA, Barcode.FormatUpcE)
            .Build();
        var activity = Platform.CurrentActivity ?? throw new InvalidOperationException("No screen to scan from.");
        var scanner = GmsBarcodeScanning.GetClient(activity, options);
        try
        {
            var result = await scanner.StartScan().AsAsync<Barcode>();
            return result?.RawValue;
        }
        catch (Android.Gms.Common.Apis.ApiException)
        {
            // Cancelled, or the scanner module couldn't be downloaded.
            return null;
        }
        catch (Java.Lang.Exception)
        {
            return null;
        }
    }
}
