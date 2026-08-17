namespace IOSwipeView;

/// <summary>
/// How a side's actions are laid out as they are revealed.
/// </summary>
public enum SwipeActionStyle
{
    /// <summary>
    /// Actions keep their natural width and are progressively uncovered by a mask, so they
    /// appear to sit still beneath the row while it slides away. The default.
    /// </summary>
    Mask,

    /// <summary>
    /// Actions share the revealed width equally, growing as the row is dragged further.
    /// </summary>
    EqualWidths,

    /// <summary>
    /// Actions overlap and fan out from the edge, the outermost stacked on top.
    /// </summary>
    Cascade,
}
