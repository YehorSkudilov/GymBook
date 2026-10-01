using System.Globalization;

namespace GymBook.Converters;

/// <summary>
/// An exercise thumbnail (<see cref="Services.ExerciseLibrary.Thumbnail"/>) as an image: the illustrations ship inside
/// the app (Resources/Raw/exercises), so they show offline; a web address is loaded as one. One image per exercise is
/// kept, so long lists don't reopen the same file.
/// </summary>
public class ThumbnailConverter : IValueConverter
{
    static readonly Dictionary<string, ImageSource> Cache = [];

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || path.Length == 0)
            return null;
        if (Cache.TryGetValue(path, out var cached))
            return cached;
        ImageSource source = path.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? ImageSource.FromUri(new Uri(path))
            : ImageSource.FromStream(async ct =>
            {
                try
                {
                    return await FileSystem.OpenAppPackageFileAsync(path);
                }
                catch (Exception)
                {
                    // Not in this build: the muscle initials under it stay.
                    return null;
                }
            });
        Cache[path] = source;
        return source;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
