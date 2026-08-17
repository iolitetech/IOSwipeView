namespace IOSwipeView;

/// <summary>
/// Context passed to contextual actions in a <see cref="SwipeList{TItem}"/>.
/// Exposes the row's data item, positional metadata, selection state, and lifecycle methods.
/// </summary>
/// <typeparam name="TItem">The data item type.</typeparam>
public sealed class SwipeItemContext<TItem> where TItem : notnull
{
    private readonly SwipeList<TItem> _list;

    internal SwipeItemContext(TItem item, SwipeList<TItem> list, int index, bool isFirst, bool isLast, bool isSelected, bool isEditing)
    {
        Item = item;
        _list = list;
        Index = index;
        IsFirst = isFirst;
        IsLast = isLast;
        IsSelected = isSelected;
        IsEditing = isEditing;
    }

    /// <summary>The data item for this row.</summary>
    public TItem Item { get; }

    /// <summary>Zero-based index of this item in the list.</summary>
    public int Index { get; internal set; }

    /// <summary>Whether this is the first item in the list.</summary>
    public bool IsFirst { get; internal set; }

    /// <summary>Whether this is the last item in the list.</summary>
    public bool IsLast { get; internal set; }

    /// <summary>Whether this item is currently selected in Edit Mode.</summary>
    public bool IsSelected { get; internal set; }

    /// <summary>Whether the list is currently in Edit Mode.</summary>
    public bool IsEditing { get; internal set; }

    /// <summary>Closes the swipe drawer for this row.</summary>
    public Task CloseAsync() => _list.CloseItemAsync(Item);

    /// <summary>Opens the specified side's action drawer for this row.</summary>
    public Task OpenAsync(SwipeSide side) => _list.OpenItemAsync(Item, side);

    /// <summary>
    /// Smoothly animates this row's height to zero with an iOS spring curve,
    /// then invokes <see cref="SwipeList{TItem}.OnItemDeleted"/>.
    /// </summary>
    public Task DeleteAsync() => _list.RemoveItemAnimatedAsync(Item);
}
