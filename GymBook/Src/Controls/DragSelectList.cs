using System.Windows.Input;

namespace GymBook.Controls;

/// <summary>
/// A list whose rows are selected by dragging over them: the finger goes down on a row and every row it passes over, up
/// or down, is selected (or, starting on a selected row, unselected) along with it. <see cref="StartCommand"/> runs with
/// the row the drag started on, <see cref="ExtendCommand"/> with each row the finger reaches after that; what selecting
/// means is up to them. The list is the content: a layout (e.g. a stack with a BindableLayout) of one view per row.
/// On Android it's handled natively (see DragSelectListHandler): a touch in the first <see cref="CheckColumnWidth"/> of
/// a row (its checkbox) starts the drag at once, anywhere else a long-press, so a quick swipe still scrolls; near the top
/// or bottom of the screen the list scrolls on by itself. Elsewhere rows are only tapped.
/// </summary>
public class DragSelectList : ContentView
{
    public static readonly BindableProperty StartCommandProperty =
        BindableProperty.Create(nameof(StartCommand), typeof(ICommand), typeof(DragSelectList));

    /// <summary>Runs with the index of the row a drag starts on.</summary>
    public ICommand? StartCommand
    {
        get => (ICommand?)GetValue(StartCommandProperty);
        set => SetValue(StartCommandProperty, value);
    }

    public static readonly BindableProperty ExtendCommandProperty =
        BindableProperty.Create(nameof(ExtendCommand), typeof(ICommand), typeof(DragSelectList));

    /// <summary>Runs with the index of each row the finger moves onto during a drag.</summary>
    public ICommand? ExtendCommand
    {
        get => (ICommand?)GetValue(ExtendCommandProperty);
        set => SetValue(ExtendCommandProperty, value);
    }

    public static readonly BindableProperty CheckColumnWidthProperty =
        BindableProperty.Create(nameof(CheckColumnWidth), typeof(double), typeof(DragSelectList), 56.0);

    /// <summary>How far in from the left a touch starts a drag straight away (the rows' checkboxes).</summary>
    public double CheckColumnWidth
    {
        get => (double)GetValue(CheckColumnWidthProperty);
        set => SetValue(CheckColumnWidthProperty, value);
    }

    int _last = -1;

    /// <summary>The row at <paramref name="y"/> (from the top of the list); above or below the rows, the first or last.</summary>
    internal int IndexAt(double y)
    {
        if (Content is not Layout layout)
            return -1;
        var rows = layout.Children.OfType<View>().ToList();
        if (rows.Count == 0)
            return -1;
        var local = y - layout.Bounds.Y;
        for (var i = 0; i < rows.Count; i++)
            if (local < rows[i].Bounds.Bottom)
                return i;
        return rows.Count - 1;
    }

    internal void OnDragStarted(double y)
    {
        _last = IndexAt(y);
        if (_last >= 0 && StartCommand?.CanExecute(_last) != false)
            StartCommand?.Execute(_last);
    }

    internal void OnDragMoved(double y)
    {
        var index = IndexAt(y);
        if (index < 0 || index == _last)
            return;
        _last = index;
        if (ExtendCommand?.CanExecute(index) != false)
            ExtendCommand?.Execute(index);
    }

    internal void OnDragEnded() => _last = -1;
}
