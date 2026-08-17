namespace IOSwipeView;

/// <summary>
/// Controls the visual layout and position of an icon relative to its label inside a <see cref="SwipeAction"/>.
/// </summary>
public enum ActionPlacement
{
    /// <summary>
    /// Icon is positioned above the label text (Classic iOS Mail / Notes vertical stack).
    /// </summary>
    Top,

    /// <summary>
    /// Icon is placed at the inline start of the label text (Left in LTR, Right in RTL).
    /// </summary>
    InlineStart,

    /// <summary>
    /// Icon is placed at the inline end of the label text (Right in LTR, Left in RTL).
    /// </summary>
    InlineEnd,

    /// <summary>
    /// Only the icon is rendered, centered. The label or text is used as an accessible aria-label.
    /// </summary>
    IconOnly
}
