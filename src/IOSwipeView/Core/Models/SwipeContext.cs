namespace IOSwipeView;

/// <summary>
/// Passed to a side's action content, giving it the row's live state and programmatic control.
/// </summary>
/// <remarks>
/// This replaces the SwiftUI original's <c>PassthroughSubject</c> pattern, which has no Blazor
/// equivalent. An action can close the row it lives in directly:
/// <code>
/// &lt;TrailingActions Context="swipe"&gt;
///     &lt;SwipeAction OnInvoked="swipe.CloseAsync"&gt;Dismiss&lt;/SwipeAction&gt;
/// &lt;/TrailingActions&gt;
/// </code>
/// </remarks>
public sealed class SwipeContext
{
    private readonly SwipeView _owner;

    internal SwipeContext(SwipeView owner, SwipeSide side)
    {
        _owner = owner;
        Side = side;
    }

    /// <summary>The side this content belongs to.</summary>
    public SwipeSide Side { get; }

    /// <summary>This side's current state.</summary>
    public SwipeState State { get; internal set; }

    /// <summary>How many actions this side holds.</summary>
    public int ActionCount { get; internal set; }

    /// <summary>Whether the row is being dragged right now.</summary>
    public bool IsDragging { get; internal set; }

    /// <summary>Whether this side is currently open and settled.</summary>
    public bool IsOpen => _owner.State == SwipeState.Expanded && _owner.OpenSide == Side;

    /// <summary>Whether this side is currently armed for drag-to-trigger.</summary>
    public bool IsArmed => State == SwipeState.Triggering;

    /// <summary>Reveals this side's actions.</summary>
    /// <returns>A task that completes once the row has been told to settle open.</returns>
    public Task OpenAsync() => _owner.OpenAsync(Side);

    /// <summary>Closes the row.</summary>
    /// <returns>A task that completes once the row has been told to settle closed.</returns>
    public Task CloseAsync() => _owner.CloseAsync();

    /// <summary>Toggles this side between opened and closed.</summary>
    /// <returns>A task that completes once the row has been told to settle.</returns>
    public Task ToggleAsync() => _owner.ToggleAsync(Side);

    /// <summary>Triggers this side's edge action, sliding the row clear.</summary>
    /// <returns>A task that completes once the row has been told to settle triggered.</returns>
    public Task TriggerAsync() => _owner.TriggerAsync(Side);
}
