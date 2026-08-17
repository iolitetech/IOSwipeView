namespace IOSwipeView;

/// <summary>
/// The state of one side of a swipe row.
/// </summary>
/// <remarks>
/// Each side tracks its own state, and at most one side is ever non-<see cref="Closed"/>.
/// </remarks>
public enum SwipeState
{
    /// <summary>No actions are showing. The resting state.</summary>
    Closed,

    /// <summary>All of the side's actions are showing at their natural width.</summary>
    Expanded,

    /// <summary>
    /// The row has been dragged past the trigger threshold and the edge action is highlighted
    /// and filling the row, but the drag has not been released yet. Releasing here commits to
    /// <see cref="Triggered"/>; dragging back cancels.
    /// </summary>
    /// <remarks>Only reachable when the edge action opts in via <c>AllowSwipeToTrigger</c>.</remarks>
    Triggering,

    /// <summary>
    /// The edge action has been committed by releasing from <see cref="Triggering"/>. The row
    /// slides fully off-screen and the action's callback runs.
    /// </summary>
    Triggered,
}
