namespace IOSwipeView;

/// <summary>
/// Where the row should sit, and how each side should be labelled, part-way through a drag.
/// </summary>
/// <param name="Offset">
/// The signed content offset in pixels. Positive reveals leading actions, negative trailing ones.
/// </param>
/// <param name="LeadingState">
/// <see cref="SwipeState.Triggering"/> while the leading edge action is armed, otherwise
/// <see langword="null"/> to mean "in transit" — the drag is in progress and the side has not
/// settled into any state yet.
/// </param>
/// <param name="TrailingState">The same for the trailing side. At most one side is ever non-null.</param>
/// <param name="ArmedActionIndex">The 0-based index of the specific action currently armed, or <see langword="null"/>.</param>
public readonly record struct SwipeDragOutcome(
    double Offset,
    SwipeState? LeadingState,
    SwipeState? TrailingState,
    int? ArmedActionIndex = null)
{
    /// <summary>
    /// The state of the given side.
    /// </summary>
    /// <param name="side">The side to read.</param>
    /// <returns>That side's state, or <see langword="null"/> if it has not settled.</returns>
    public SwipeState? State(SwipeSide side) =>
        side == SwipeSide.Leading ? LeadingState : TrailingState;
}

/// <summary>
/// Where the row should settle once the drag is released.
/// </summary>
/// <param name="TargetOffset">The signed offset to animate to, in pixels.</param>
/// <param name="Side">
/// The side left showing, or <see langword="null"/> when the row settles closed.
/// </param>
/// <param name="State">The settled state. Always one of Closed, Expanded, or Triggered.</param>
/// <param name="Spring">The spring to animate with, chosen to match <paramref name="State"/>.</param>
/// <param name="TriggeredActionIndex">The 0-based index of the action triggered, or <see langword="null"/>.</param>
public readonly record struct SwipeReleaseOutcome(
    double TargetOffset,
    SwipeSide? Side,
    SwipeState State,
    SwipeSpring Spring,
    int? TriggeredActionIndex = null)
{
    /// <summary>
    /// Whether this release commits an action via drag-to-trigger.
    /// </summary>
    public bool IsTriggered => State == SwipeState.Triggered;
}
