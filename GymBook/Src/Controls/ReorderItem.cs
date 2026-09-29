using System.Windows.Input;

namespace GymBook.Controls;

/// <summary>
/// One item of a list that can be reordered by holding it and dragging it: the item itself follows the finger (no
/// copy of it), the others slide aside to show where it will land, and on release it settles into place and
/// <see cref="MoveCommand"/> runs with its new index. The list is the item's parent layout (e.g. a stack with a
/// BindableLayout); every item in it must be a ReorderItem.
/// On Android it is handled natively (see ReorderItemHandler): straight from the item's grip (<see cref="IsHandleProperty"/>)
/// if it has one, otherwise a long-press, so a quick swipe still scrolls;
/// elsewhere it's a plain drag.
/// </summary>
public class ReorderItem : ContentView
{
    public static readonly BindableProperty MoveCommandProperty =
        BindableProperty.Create(nameof(MoveCommand), typeof(ICommand), typeof(ReorderItem));

    /// <summary>Runs with the index the item was dropped at, when that's a different one.</summary>
    public ICommand? MoveCommand
    {
        get => (ICommand?)GetValue(MoveCommandProperty);
        set => SetValue(MoveCommandProperty, value);
    }

    public static readonly BindableProperty OrientationProperty =
        BindableProperty.Create(nameof(Orientation), typeof(StackOrientation), typeof(ReorderItem), StackOrientation.Vertical);

    /// <summary>Which way the list runs: a column of rows, or a strip of chips.</summary>
    public StackOrientation Orientation
    {
        get => (StackOrientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public static readonly BindableProperty IsHandleProperty =
        BindableProperty.CreateAttached("IsHandle", typeof(bool), typeof(ReorderItem), false);

    /// <summary>
    /// Marks the grip inside an item. An item with one is dragged from it straight away, and a touch anywhere else on
    /// the item scrolls the list as usual. Without one, the whole item is held (long-pressed) to drag.
    /// </summary>
    public static bool GetIsHandle(BindableObject view) => (bool)view.GetValue(IsHandleProperty);
    public static void SetIsHandle(BindableObject view, bool value) => view.SetValue(IsHandleProperty, value);

    /// <summary>The item's grip, if it has one.</summary>
    internal View? FindHandle() => Find(Content);

    static View? Find(IView? view) => view switch
    {
        View v when GetIsHandle(v) => v,
        Layout layout => layout.Children.Select(Find).FirstOrDefault(h => h != null),
        IContentView { PresentedContent: { } content } => Find(content),
        _ => null,
    };

    const uint SlideLength = 150, SettleLength = 120;
    const double LiftScale = 1.03;

    // The drag in progress: the list's items and where each one was when it started.
    List<ReorderItem>? _items;
    List<(double Start, double Size)> _slots = [];
    double _spacing;
    int _from, _to;
    bool Vertical => Orientation == StackOrientation.Vertical;

    public ReorderItem()
    {
#if !ANDROID
        var pan = new PanGestureRecognizer();
        pan.PanUpdated += (_, e) =>
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    OnReorderStarted();
                    break;
                case GestureStatus.Running:
                    OnReorderMoved(e.TotalX, e.TotalY);
                    break;
                case GestureStatus.Completed:
                    OnReorderEnded(false);
                    break;
                case GestureStatus.Canceled:
                    OnReorderEnded(true);
                    break;
            }
        };
        GestureRecognizers.Add(pan);
#endif
    }

    internal void OnReorderStarted()
    {
        if (Parent is not Layout layout)
            return;
        _items = layout.Children.OfType<ReorderItem>().ToList();
        _from = _to = _items.IndexOf(this);
        if (_from < 0)
        {
            _items = null;
            return;
        }
        _slots = _items.Select(i => Vertical ? (i.Bounds.Y, i.Bounds.Height) : (i.Bounds.X, i.Bounds.Width)).ToList();
        _spacing = layout is StackBase stack ? stack.Spacing : 0;
        // Above the others while it moves over them; a little bigger, as if picked up. Not with ZIndex on Android: MAUI
        // applies it by taking the view out of the list and putting it back, which cancels the touch and ends the drag
        // at once. The Android handler raises it with its own elevation instead.
#if !ANDROID
        ZIndex = 1;
#endif
        this.ScaleTo(LiftScale, SettleLength, Easing.CubicOut);
    }

    /// <summary><paramref name="dx"/>, <paramref name="dy"/>: how far the finger has moved since the drag started.</summary>
    internal void OnReorderMoved(double dx, double dy)
    {
        if (_items == null)
            return;
        var last = _items.Count - 1;
        var (start, size) = _slots[_from];
        // Not past either end of the list.
        var offset = Math.Clamp(Vertical ? dy : dx, _slots[0].Start - start, _slots[last].Start + _slots[last].Size - (start + size));
        if (Vertical)
            TranslationY = offset;
        else
            TranslationX = offset;

        // Where it would land: past the middle of a neighbour, it takes that neighbour's place.
        var center = start + size / 2 + offset;
        var to = _from;
        while (to < last && center > _slots[to + 1].Start + _slots[to + 1].Size / 2)
            to++;
        while (to > 0 && center < _slots[to - 1].Start + _slots[to - 1].Size / 2)
            to--;
        if (to == _to)
            return;
        _to = to;

        // The items between the old and the new place slide over by the dragged item's size.
        var shift = size + _spacing;
        for (var j = 0; j <= last; j++)
        {
            if (j == _from)
                continue;
            var target = j > _from && j <= _to ? -shift : j < _from && j >= _to ? shift : 0;
            var item = _items[j];
            if ((Vertical ? item.TranslationY : item.TranslationX) != target)
                item.TranslateTo(Vertical ? 0 : target, Vertical ? target : 0, SlideLength, Easing.CubicOut);
        }
    }

    internal async void OnReorderEnded(bool canceled)
    {
        if (_items is not { } items)
            return;
        _items = null;
        var to = canceled ? _from : _to;

        // Settle exactly into the gap the others made.
        var final = 0.0;
        for (var j = Math.Min(_from, to); j <= Math.Max(_from, to); j++)
            if (j != _from)
                final += _slots[j].Size + _spacing;
        if (to < _from)
            final = -final;
        await Task.WhenAll(
            this.TranslateTo(Vertical ? 0 : final, Vertical ? final : 0, SettleLength, Easing.CubicOut),
            this.ScaleTo(1, SettleLength, Easing.CubicOut));

        // Everything back in its own place, and the list rebuilt in the new order, in one go so nothing jumps.
        foreach (var item in items)
        {
            item.CancelAnimations();
            item.TranslationX = item.TranslationY = 0;
        }
#if !ANDROID
        ZIndex = 0;
#endif
        Scale = 1;
        if (to != _from && MoveCommand?.CanExecute(to) != false)
            MoveCommand?.Execute(to);
    }
}
