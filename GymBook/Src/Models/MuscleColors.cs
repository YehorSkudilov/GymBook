namespace GymBook.Models;

public static class MuscleColors
{
    public static Color Color(this MuscleGroup m) => m switch
    {
        MuscleGroup.Chest => Microsoft.Maui.Graphics.Color.FromArgb("#FF6B6B"),
        MuscleGroup.Back => Microsoft.Maui.Graphics.Color.FromArgb("#4D96FF"),
        MuscleGroup.Traps => Microsoft.Maui.Graphics.Color.FromArgb("#6BCBFF"),
        MuscleGroup.Shoulders => Microsoft.Maui.Graphics.Color.FromArgb("#FFB84D"),
        MuscleGroup.Biceps => Microsoft.Maui.Graphics.Color.FromArgb("#B28DFF"),
        MuscleGroup.Triceps => Microsoft.Maui.Graphics.Color.FromArgb("#8D7BFF"),
        MuscleGroup.Forearms => Microsoft.Maui.Graphics.Color.FromArgb("#A0A7B8"),
        MuscleGroup.Abs => Microsoft.Maui.Graphics.Color.FromArgb("#FFD93D"),
        MuscleGroup.LowerBack => Microsoft.Maui.Graphics.Color.FromArgb("#3FC1C9"),
        MuscleGroup.Glutes => Microsoft.Maui.Graphics.Color.FromArgb("#FF8FAB"),
        MuscleGroup.Quads => Microsoft.Maui.Graphics.Color.FromArgb("#2ED47A"),
        MuscleGroup.Hamstrings => Microsoft.Maui.Graphics.Color.FromArgb("#56E39F"),
        _ => Microsoft.Maui.Graphics.Color.FromArgb("#7ED957"),
    };
}
