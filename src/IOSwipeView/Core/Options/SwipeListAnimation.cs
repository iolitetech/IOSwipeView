namespace IOSwipeView;

/// <summary>
/// Transition style applied when a row is collapsed and deleted from a <see cref="SwipeList{TItem}"/>.
/// </summary>
public enum ListDeleteStyle
{
    /// <summary>Signature Apple vertical spring height collapse and fade.</summary>
    AppleSpring,

    /// <summary>Row slides off-screen to the left while collapsing height.</summary>
    SlideLeft,

    /// <summary>Row slides off-screen to the right while collapsing height.</summary>
    SlideRight,

    /// <summary>Row scales down slightly while sliding off-screen to the left.</summary>
    ShrinkSlideLeft,

    /// <summary>Row scales down slightly while sliding off-screen to the right.</summary>
    ShrinkSlideRight,

    /// <summary>Row shrinks inward (scale 0.75) and fades out while collapsing height.</summary>
    ScaleDown,

    /// <summary>Row executes an elastic spring micro-pop, then snaps down with a fade.</summary>
    PopOut,

    /// <summary>Row tilts with a 3D perspective fold while collapsing height.</summary>
    CardFold,

    /// <summary>Row smoothly dissolves to opacity 0 while collapsing height.</summary>
    Fade,

    /// <summary>Instant deletion with zero animation delay.</summary>
    None,

    /// <summary>Uses a custom CSS class specified by the developer.</summary>
    Custom
}

/// <summary>
/// Animation configuration for row deletions and transitions in <see cref="SwipeList{TItem}"/>.
/// </summary>
/// <param name="Style">The visual transition effect style.</param>
/// <param name="DurationMs">Duration of the animation in milliseconds. Defaults to <c>320</c>.</param>
/// <param name="Curve">CSS timing curve. Defaults to Apple spring <c>cubic-bezier(0.16, 1, 0.3, 1)</c>.</param>
/// <param name="CustomClass">Optional custom CSS class applied during collapse.</param>
public readonly record struct SwipeListAnimation(
    ListDeleteStyle Style = ListDeleteStyle.AppleSpring,
    int DurationMs = 320,
    string Curve = "cubic-bezier(0.16, 1, 0.3, 1)",
    string? CustomClass = null)
{
    /// <summary>Default Apple Mail spring height-collapse animation (~320ms).</summary>
    public static SwipeListAnimation Default { get; } = new(ListDeleteStyle.AppleSpring, 320);

    /// <summary>Apple Spring collapse (~320ms).</summary>
    public static SwipeListAnimation AppleSpring { get; } = Default;

    /// <summary>Slide off-screen left and collapse (~300ms).</summary>
    public static SwipeListAnimation SlideLeft { get; } = new(ListDeleteStyle.SlideLeft, 300, "cubic-bezier(0.2, 0.9, 0.3, 1)");

    /// <summary>Slide off-screen right and collapse (~300ms).</summary>
    public static SwipeListAnimation SlideRight { get; } = new(ListDeleteStyle.SlideRight, 300, "cubic-bezier(0.2, 0.9, 0.3, 1)");

    /// <summary>Shrink scale down and slide off-screen left (~320ms).</summary>
    public static SwipeListAnimation ShrinkSlideLeft { get; } = new(ListDeleteStyle.ShrinkSlideLeft, 320, "cubic-bezier(0.2, 0.9, 0.3, 1)");

    /// <summary>Shrink scale down and slide off-screen right (~320ms).</summary>
    public static SwipeListAnimation ShrinkSlideRight { get; } = new(ListDeleteStyle.ShrinkSlideRight, 320, "cubic-bezier(0.2, 0.9, 0.3, 1)");

    /// <summary>Scale down vortex shrink and collapse (~300ms).</summary>
    public static SwipeListAnimation ScaleDown { get; } = new(ListDeleteStyle.ScaleDown, 300, "cubic-bezier(0.2, 1, 0.3, 1)");

    /// <summary>Elastic spring pop-out and collapse (~340ms).</summary>
    public static SwipeListAnimation PopOut { get; } = new(ListDeleteStyle.PopOut, 340, "cubic-bezier(0.34, 1.56, 0.64, 1)");

    /// <summary>3D perspective origami card fold and collapse (~360ms).</summary>
    public static SwipeListAnimation CardFold { get; } = new(ListDeleteStyle.CardFold, 360, "cubic-bezier(0.16, 1, 0.3, 1)");

    /// <summary>Smooth dissolve fade and collapse (~240ms).</summary>
    public static SwipeListAnimation Fade { get; } = new(ListDeleteStyle.Fade, 240, "ease");

    /// <summary>Instant deletion without animation delay.</summary>
    public static SwipeListAnimation None { get; } = new(ListDeleteStyle.None, 0, "linear");

    /// <summary>
    /// Derives an animation configuration directly from a physical <see cref="SwipeSpring"/>.
    /// </summary>
    /// <param name="style">The visual transition style.</param>
    /// <param name="spring">The spring physics to derive duration and curve from.</param>
    /// <param name="customClass">Optional custom CSS class.</param>
    public static SwipeListAnimation FromSpring(ListDeleteStyle style, SwipeSpring spring, string? customClass = null) =>
        new(style, spring.SettlingDurationMs, spring.ToCssCurve(), customClass);
}
