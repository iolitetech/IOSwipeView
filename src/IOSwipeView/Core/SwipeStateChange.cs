namespace IOSwipeView;

/// <summary>
/// Reports that a row has settled into a new state.
/// </summary>
/// <remarks>
/// Raised only on settled transitions, never per frame while dragging, so it is safe to do real
/// work in the handler.
/// </remarks>
/// <param name="State">The state the row settled into.</param>
/// <param name="Side">The side left showing, or <see langword="null"/> when the row closed.</param>
public readonly record struct SwipeStateChange(SwipeState State, SwipeSide? Side)
{
    /// <summary>Whether the row is now closed.</summary>
    public bool IsClosed => State == SwipeState.Closed;

    /// <summary>Whether the row is now showing a side's actions.</summary>
    public bool IsOpen => State is SwipeState.Expanded or SwipeState.Triggered;
}
