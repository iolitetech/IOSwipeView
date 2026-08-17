namespace IOSwipeView;

/// <summary>
/// Configuration options for a <see cref="SwipeList{TItem}"/>.
/// </summary>
public sealed record SwipeListOptions
{
    /// <summary>Default options matching native iOS Mail list conventions.</summary>
    public static SwipeListOptions Default { get; } = new();

    /// <summary>Classic flush list style (0 spacing, square actions, 0 divider inset).</summary>
    public static SwipeListOptions ClassicList { get; } = Default with
    {
        SwipeOptions = SwipeOptions.ClassicList,
        DividerInset = 0,
        DeleteAnimation = SwipeListAnimation.AppleSpring
    };

    /// <summary>Inset grouped list style (16px divider inset, scale down deletion).</summary>
    public static SwipeListOptions InsetGrouped { get; } = Default with
    {
        SwipeOptions = SwipeOptions.InsetGrouped,
        DividerInset = 16,
        DeleteAnimation = SwipeListAnimation.ScaleDown
    };

    /// <summary>Floating notification style (no dividers, slide left deletion).</summary>
    public static SwipeListOptions Notification { get; } = Default with
    {
        SwipeOptions = SwipeOptions.Notification,
        ShowDividers = false,
        DeleteAnimation = SwipeListAnimation.SlideLeft
    };

    /// <summary>Floating capsule style (no dividers, scale down deletion).</summary>
    public static SwipeListOptions Capsule { get; } = Default with
    {
        SwipeOptions = SwipeOptions.Capsule,
        ShowDividers = false,
        DeleteAnimation = SwipeListAnimation.ScaleDown
    };

    /// <summary>The base <see cref="SwipeOptions"/> applied to all row swipe drawers.</summary>
    public SwipeOptions SwipeOptions { get; init; } = SwipeOptions.Default;

    /// <summary>The animation used when a row is collapsed and deleted.</summary>
    public SwipeListAnimation DeleteAnimation { get; init; } = SwipeListAnimation.Default;

    /// <summary>Whether to show hairline separators between rows. Defaults to <see langword="true"/>.</summary>
    public bool ShowDividers { get; init; } = true;

    /// <summary>Leading indentation of dividers in CSS pixels. Defaults to <c>0</c>.</summary>
    public double DividerInset { get; init; }

    /// <summary>Whether to automatically play the collapse animation before invoking <see cref="SwipeList{TItem}.OnItemDeleted"/>.</summary>
    public bool AutoAnimateDelete { get; init; } = true;

    /// <summary>Whether to smoothly animate new items entering the list. Defaults to <see langword="true"/>.</summary>
    public bool AnimateInsert { get; init; } = true;
}
