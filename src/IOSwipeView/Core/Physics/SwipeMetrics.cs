namespace IOSwipeView;

/// <summary>
/// The measured facts about a row that the geometry needs but cannot compute itself.
/// </summary>
public readonly record struct SwipeMetrics
{
    private static readonly int[] EmptyIndices = [];

    /// <summary>How many actions the leading side holds.</summary>
    public int LeadingActionCount { get; }

    /// <summary>How many actions the trailing side holds.</summary>
    public int TrailingActionCount { get; }

    /// <summary>The indices of actions on the leading side configured for drag-to-trigger.</summary>
    public IReadOnlyList<int> LeadingTriggerIndices { get; }

    /// <summary>The indices of actions on the trailing side configured for drag-to-trigger.</summary>
    public IReadOnlyList<int> TrailingTriggerIndices { get; }

    /// <summary>The rendered width of the row, used to slide it fully off-screen when triggered.</summary>
    public double RowWidth { get; }

    /// <summary>Constructor supporting single edge triggers per side.</summary>
    public SwipeMetrics(
        int leadingActionCount,
        int trailingActionCount,
        bool leadingEdgeTriggers,
        bool trailingEdgeTriggers,
        double rowWidth)
        : this(
            leadingActionCount,
            trailingActionCount,
            leadingEdgeTriggers && leadingActionCount > 0 ? [0] : EmptyIndices,
            trailingEdgeTriggers && trailingActionCount > 0 ? [trailingActionCount - 1] : EmptyIndices,
            rowWidth)
    {
    }

    /// <summary>Constructor supporting multi-stage triggers per side.</summary>
    public SwipeMetrics(
        int leadingActionCount,
        int trailingActionCount,
        IReadOnlyList<int> leadingTriggerIndices,
        IReadOnlyList<int> trailingTriggerIndices,
        double rowWidth)
    {
        LeadingActionCount = leadingActionCount;
        TrailingActionCount = trailingActionCount;
        LeadingTriggerIndices = leadingTriggerIndices ?? EmptyIndices;
        TrailingTriggerIndices = trailingTriggerIndices ?? EmptyIndices;
        RowWidth = rowWidth;
    }

    /// <summary>Whether the leading side has any triggers configured.</summary>
    public bool LeadingEdgeTriggers => LeadingTriggerIndices.Count > 0;

    /// <summary>Whether the trailing side has any triggers configured.</summary>
    public bool TrailingEdgeTriggers => TrailingTriggerIndices.Count > 0;

    /// <summary>How many actions the given side holds.</summary>
    public int ActionCount(SwipeSide side) =>
        side == SwipeSide.Leading ? LeadingActionCount : TrailingActionCount;

    /// <summary>Whether the given side has any actions that opted into drag-to-trigger.</summary>
    public bool EdgeTriggers(SwipeSide side) =>
        side == SwipeSide.Leading ? LeadingEdgeTriggers : TrailingEdgeTriggers;

    /// <summary>The indices of actions on that side configured for drag-to-trigger.</summary>
    public IReadOnlyList<int> TriggerIndices(SwipeSide side) =>
        side == SwipeSide.Leading ? LeadingTriggerIndices : TrailingTriggerIndices;
}
