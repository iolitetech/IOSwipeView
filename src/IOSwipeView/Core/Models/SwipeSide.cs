namespace IOSwipeView;

/// <summary>
/// Which edge of the row a set of actions belongs to.
/// </summary>
/// <remarks>
/// Sides are named by reading order rather than by screen position, so they stay correct
/// under right-to-left layouts: leading actions are revealed by dragging towards the end of
/// the reading direction, trailing actions by dragging towards its start.
/// </remarks>
public enum SwipeSide
{
    /// <summary>Actions revealed by dragging the row towards its trailing edge.</summary>
    Leading,

    /// <summary>Actions revealed by dragging the row towards its leading edge.</summary>
    Trailing,
}

/// <summary>
/// Helpers for working with <see cref="SwipeSide"/>.
/// </summary>
public static class SwipeSideExtensions
{
    /// <summary>
    /// The sign of the content offset when this side's actions are showing.
    /// </summary>
    /// <remarks>
    /// A single signed offset drives the whole control: positive reveals leading actions,
    /// negative reveals trailing ones. Multiplying by this sign converts that offset into a
    /// positive "how far has this side been dragged open" distance.
    /// </remarks>
    /// <param name="side">The side to get the sign for.</param>
    /// <returns><c>1</c> for <see cref="SwipeSide.Leading"/>, <c>-1</c> for <see cref="SwipeSide.Trailing"/>.</returns>
    public static int Sign(this SwipeSide side) => side == SwipeSide.Leading ? 1 : -1;

    /// <summary>
    /// Gets the opposite side.
    /// </summary>
    /// <param name="side">The side to invert.</param>
    /// <returns>The other side.</returns>
    public static SwipeSide Opposite(this SwipeSide side) =>
        side == SwipeSide.Leading ? SwipeSide.Trailing : SwipeSide.Leading;
}
