using Microsoft.AspNetCore.Components;

namespace IOSwipeView;

/// <summary>
/// A generic, coordinated list of swipeable items with built-in accordion behavior,
/// iOS Edit Mode multi-selection, customizable deletion animations, and templated slots.
/// </summary>
/// <typeparam name="TItem">The data item type.</typeparam>
public partial class SwipeList<TItem> : ComponentBase where TItem : notnull
{
    private readonly Dictionary<TItem, SwipeView> _rowRefs = [];
    private readonly Dictionary<TItem, SwipeItemContext<TItem>> _contexts = [];
    private readonly HashSet<TItem> _collapsingItems = [];
    private SwipeViewGroup? _group;

    /// <summary>The collection of items to display.</summary>
    [Parameter]
    public IEnumerable<TItem>? Items { get; set; }

    /// <summary>Function to extract a unique key for each item. Defaults to the item instance itself.</summary>
    [Parameter]
    public Func<TItem, object>? KeySelector { get; set; }

    /// <summary>The template rendered for each row's content.</summary>
    [Parameter, EditorRequired]
    public RenderFragment<TItem> ItemTemplate { get; set; } = default!;

    /// <summary>Optional leading actions rendered for each row, receiving the row's context.</summary>
    [Parameter]
    public RenderFragment<SwipeItemContext<TItem>>? LeadingActions { get; set; }

    /// <summary>Optional trailing actions rendered for each row, receiving the row's context.</summary>
    [Parameter]
    public RenderFragment<SwipeItemContext<TItem>>? TrailingActions { get; set; }

    /// <summary>The complete configuration bundle for this list. Defaults to <see cref="SwipeListOptions.Default"/>.</summary>
    [Parameter]
    public SwipeListOptions? ListOptions { get; set; }

    /// <summary>The base <see cref="SwipeOptions"/> applied to all rows in the list.</summary>
    [Parameter]
    public SwipeOptions Options { get; set; } = SwipeOptions.Default;

    /// <summary>Optional function to dynamically resolve <see cref="SwipeOptions"/> per item.</summary>
    [Parameter]
    public Func<TItem, SwipeOptions>? ItemOptions { get; set; }

    /// <summary>Animation configuration used when a row is collapsed and deleted.</summary>
    [Parameter]
    public SwipeListAnimation? DeleteAnimation { get; set; }

    /// <summary>Optional function providing additional CSS classes for a specific row wrapper.</summary>
    [Parameter]
    public Func<TItem, string>? RowClassProvider { get; set; }

    /// <summary>Whether to show dividers between rows. Defaults to <see langword="true"/>.</summary>
    [Parameter]
    public bool? ShowDividers { get; set; }

    /// <summary>Indentation of the divider in CSS pixels. Defaults to <c>0</c>.</summary>
    [Parameter]
    public double? DividerInset { get; set; }

    /// <summary>Custom template for row dividers.</summary>
    [Parameter]
    public RenderFragment? DividerTemplate { get; set; }

    /// <summary>Whether the list is currently in iOS Edit / Multi-Select mode.</summary>
    [Parameter]
    public bool IsEditing { get; set; }

    /// <summary>The set of currently selected items in Edit Mode.</summary>
    [Parameter]
    public ISet<TItem>? SelectedItems { get; set; }

    /// <summary>Callback fired when the selection set changes.</summary>
    [Parameter]
    public EventCallback<ISet<TItem>> SelectedItemsChanged { get; set; }

    /// <summary>Custom template for selection checkmark buttons in Edit Mode.</summary>
    [Parameter]
    public RenderFragment<TItem>? SelectionTemplate { get; set; }

    /// <summary>Callback fired when a row is tapped (disambiguated from swiping).</summary>
    [Parameter]
    public EventCallback<TItem> OnItemClick { get; set; }

    /// <summary>Callback fired when a single row is deleted via drag-to-trigger or programmatically.</summary>
    [Parameter]
    public EventCallback<TItem> OnItemDeleted { get; set; }

    /// <summary>Callback fired when multiple rows are deleted in batch via <see cref="RemoveItemsAnimatedAsync"/>.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<TItem>> OnItemsDeleted { get; set; }

    /// <summary>Fired when any row's swipe state changes.</summary>
    [Parameter]
    public EventCallback<(TItem Item, SwipeStateChange Change)> OnStateChanged { get; set; }

    /// <summary>Whether to automatically play the smooth deletion animation before deleting.</summary>
    [Parameter]
    public bool? AutoAnimateDelete { get; set; }

    /// <summary>Whether to smoothly animate new items entering the list.</summary>
    [Parameter]
    public bool? AnimateInsert { get; set; }

    /// <summary>Optional header content rendered above the list.</summary>
    [Parameter]
    public RenderFragment? HeaderContent { get; set; }

    /// <summary>Optional footer content rendered below the list.</summary>
    [Parameter]
    public RenderFragment? FooterContent { get; set; }

    /// <summary>Optional empty content rendered when <see cref="Items"/> is empty or null.</summary>
    [Parameter]
    public RenderFragment? EmptyContent { get; set; }

    /// <summary>Additional CSS class applied to the list root container.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Additional inline styles applied to the list root container.</summary>
    [Parameter]
    public string? Style { get; set; }

    /// <summary>Any unmatched attributes are applied to the root container element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // Prune stale references if the collection was filtered or replaced externally
        if (Items is not null)
        {
            var currentSet = new HashSet<TItem>(Items);
            var deadContexts = _contexts.Keys.Where(k => !currentSet.Contains(k)).ToList();
            foreach (var k in deadContexts)
            {
                _contexts.Remove(k);
                _rowRefs.Remove(k);
            }
        }
        else
        {
            _contexts.Clear();
            _rowRefs.Clear();
        }
    }

    private SwipeListOptions EffectiveListOptions => ListOptions ?? SwipeListOptions.Default;

    private SwipeListAnimation ResolvedDeleteAnimation =>
        DeleteAnimation ?? ListOptions?.DeleteAnimation ?? SwipeListAnimation.Default;

    private bool ResolvedShowDividers =>
        ShowDividers ?? ListOptions?.ShowDividers ?? true;

    private double ResolvedDividerInset =>
        DividerInset ?? ListOptions?.DividerInset ?? 0;

    private bool ResolvedAutoAnimateDelete =>
        AutoAnimateDelete ?? ListOptions?.AutoAnimateDelete ?? true;

    private bool ResolvedAnimateInsert =>
        AnimateInsert ?? ListOptions?.AnimateInsert ?? true;

    private string ContainerClass => string.IsNullOrWhiteSpace(Class)
        ? "ioswipe-list"
        : $"ioswipe-list {Class}";

    private string ContainerStyle =>
        $"--ioswipe-collapse-duration:{ResolvedDeleteAnimation.DurationMs}ms;" +
        $"--ioswipe-collapse-curve:{ResolvedDeleteAnimation.Curve};" +
        (Style ?? "");

    private string DividerStyle => ResolvedDividerInset > 0
        ? $"--ioswipe-divider-inset:{ResolvedDividerInset.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}px;"
        : "";

    private string GetCollapsingClass()
    {
        var anim = ResolvedDeleteAnimation;
        var baseClass = "ioswipe-list__row-wrapper--collapsing";

        return anim.Style switch
        {
            ListDeleteStyle.SlideLeft => $"{baseClass} ioswipe-list__row-wrapper--collapsing-slide-left",
            ListDeleteStyle.SlideRight => $"{baseClass} ioswipe-list__row-wrapper--collapsing-slide-right",
            ListDeleteStyle.ShrinkSlideLeft => $"{baseClass} ioswipe-list__row-wrapper--collapsing-shrink-slide-left",
            ListDeleteStyle.ShrinkSlideRight => $"{baseClass} ioswipe-list__row-wrapper--collapsing-shrink-slide-right",
            ListDeleteStyle.ScaleDown => $"{baseClass} ioswipe-list__row-wrapper--collapsing-scale",
            ListDeleteStyle.PopOut => $"{baseClass} ioswipe-list__row-wrapper--collapsing-pop-out",
            ListDeleteStyle.CardFold => $"{baseClass} ioswipe-list__row-wrapper--collapsing-card-fold",
            ListDeleteStyle.Fade => $"{baseClass} ioswipe-list__row-wrapper--collapsing-fade",
            ListDeleteStyle.None => baseClass,
            ListDeleteStyle.Custom => $"{baseClass} {anim.CustomClass ?? ""}",
            _ => $"{baseClass} ioswipe-list__row-wrapper--collapsing-spring"
        };
    }

    private SwipeItemContext<TItem> GetItemContext(TItem item, int index, bool isFirst, bool isLast, bool isSelected, bool isEditing)
    {
        if (!_contexts.TryGetValue(item, out var ctx))
        {
            ctx = new SwipeItemContext<TItem>(item, this, index, isFirst, isLast, isSelected, isEditing);
            _contexts[item] = ctx;
        }
        else
        {
            ctx.Index = index;
            ctx.IsFirst = isFirst;
            ctx.IsLast = isLast;
            ctx.IsSelected = isSelected;
            ctx.IsEditing = isEditing;
        }
        return ctx;
    }

    private int GetItemsCount()
    {
        if (Items is IReadOnlyCollection<TItem> roc) return roc.Count;
        if (Items is ICollection<TItem> c) return c.Count;
        return -1;
    }

    private object GetKey(TItem item) => KeySelector?.Invoke(item) ?? item!;

    private SwipeOptions ResolveSwipeOptions(TItem item)
    {
        var baseOpt = ItemOptions?.Invoke(item) ?? (ListOptions is not null ? ListOptions.SwipeOptions : Options);
        // In iOS Edit Mode, row swiping is disabled to avoid accidental gestures while selecting
        return IsEditing && baseOpt.Enabled ? baseOpt with { Enabled = false } : baseOpt;
    }

    private bool IsLastItem(TItem item)
    {
        if (Items is IReadOnlyList<TItem> roList)
        {
            return roList.Count > 0 && EqualityComparer<TItem>.Default.Equals(roList[^1], item);
        }

        if (Items is IList<TItem> list)
        {
            return list.Count > 0 && EqualityComparer<TItem>.Default.Equals(list[^1], item);
        }

        if (Items is not null)
        {
            using var enumerator = Items.GetEnumerator();
            if (enumerator.MoveNext())
            {
                TItem current = enumerator.Current;
                while (enumerator.MoveNext())
                {
                    current = enumerator.Current;
                }

                return EqualityComparer<TItem>.Default.Equals(current, item);
            }
        }

        return false;
    }

    private async Task ToggleSelection(TItem item)
    {
        var set = SelectedItems ?? new HashSet<TItem>();
        if (!set.Remove(item))
        {
            set.Add(item);
        }

        if (SelectedItemsChanged.HasDelegate)
        {
            await SelectedItemsChanged.InvokeAsync(set);
        }
    }

    private async Task HandleItemClick(TItem item)
    {
        if (IsEditing)
        {
            await ToggleSelection(item);
            return;
        }

        if (OnItemClick.HasDelegate)
        {
            await OnItemClick.InvokeAsync(item);
        }
    }

    private async Task OnRowStateChanged(TItem item, SwipeStateChange change)
    {
        if (OnStateChanged.HasDelegate)
        {
            await OnStateChanged.InvokeAsync((item, change));
        }

        if (change.State == SwipeState.Triggered && ResolvedAutoAnimateDelete)
        {
            await RemoveItemAnimatedAsync(item);
        }
    }

    /// <summary>
    /// Smoothly animates the specified item's height to zero before invoking <see cref="OnItemDeleted"/>.
    /// </summary>
    /// <param name="item">The item to collapse and delete.</param>
    public async Task RemoveItemAnimatedAsync(TItem item)
    {
        if (_collapsingItems.Contains(item)) return;

        var anim = ResolvedDeleteAnimation;
        if (anim.DurationMs > 0 && anim.Style != ListDeleteStyle.None)
        {
            if (_rowRefs.TryGetValue(item, out var row))
            {
                _ = row.CloseAsync();
            }

            _collapsingItems.Add(item);
            StateHasChanged();

            await Task.Delay(anim.DurationMs);
        }

        _collapsingItems.Remove(item);
        _rowRefs.Remove(item);
        _contexts.Remove(item);

        if (OnItemDeleted.HasDelegate)
        {
            await OnItemDeleted.InvokeAsync(item);
        }
    }

    /// <summary>
    /// Smoothly animates multiple items collapsing in parallel before invoking <see cref="OnItemDeleted"/>
    /// and <see cref="OnItemsDeleted"/>.
    /// </summary>
    /// <param name="items">The items to collapse and delete in batch.</param>
    public async Task RemoveItemsAnimatedAsync(IEnumerable<TItem> items)
    {
        var list = items.ToList();
        if (list.Count == 0) return;

        var anim = ResolvedDeleteAnimation;
        if (anim.DurationMs > 0 && anim.Style != ListDeleteStyle.None)
        {
            foreach (var item in list)
            {
                if (_rowRefs.TryGetValue(item, out var row))
                {
                    _ = row.CloseAsync();
                }
                _collapsingItems.Add(item);
            }
            StateHasChanged();

            await Task.Delay(anim.DurationMs);
        }

        foreach (var item in list)
        {
            _collapsingItems.Remove(item);
            _rowRefs.Remove(item);
            _contexts.Remove(item);
            if (OnItemDeleted.HasDelegate)
            {
                await OnItemDeleted.InvokeAsync(item);
            }
        }

        if (OnItemsDeleted.HasDelegate)
        {
            await OnItemsDeleted.InvokeAsync(list);
        }
    }

    /// <summary>Closes any currently open swipe rows in this list.</summary>
    public Task CloseAllAsync() => _group is null ? Task.CompletedTask : _group.CloseOthersAsync(null);

    /// <summary>Programmatically closes the actions for a given item.</summary>
    public Task CloseItemAsync(TItem item) =>
        _rowRefs.TryGetValue(item, out var row) ? row.CloseAsync() : Task.CompletedTask;

    /// <summary>Programmatically opens the actions for a given item.</summary>
    public Task OpenItemAsync(TItem item, SwipeSide side) =>
        _rowRefs.TryGetValue(item, out var row) ? row.OpenAsync(side) : Task.CompletedTask;

    /// <summary>Programmatically triggers the edge action for a given item.</summary>
    public Task TriggerItemAsync(TItem item, SwipeSide side) =>
        _rowRefs.TryGetValue(item, out var row) ? row.TriggerAsync(side) : Task.CompletedTask;
}
