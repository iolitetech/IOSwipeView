namespace IOSwipeView;

/// <summary>
/// A critically-tunable spring used to settle the row after a drag is released.
/// </summary>
/// <remarks>
/// Mass is fixed at 1, matching SwiftUI's <c>interpolatingSpring</c>, so stiffness and damping
/// alone describe the motion. Higher stiffness snaps faster; higher damping reduces overshoot.
/// </remarks>
/// <param name="Stiffness">The spring constant. Higher values pull towards the target harder.</param>
/// <param name="Damping">The damping coefficient. Higher values settle sooner with less bounce.</param>
public readonly record struct SwipeSpring(double Stiffness, double Damping)
{
    /// <summary>
    /// The default snappy iOS spring: quick, responsive, overshoot-free settle (~0.22s).
    /// </summary>
    public static SwipeSpring Default { get; } = new(300, 32);

    /// <summary>
    /// Snappy spring: immediate, zero bounce (~0.18s).
    /// </summary>
    public static SwipeSpring Snappy { get; } = new(420, 40);

    /// <summary>
    /// Smooth spring: slightly softer settle (~0.26s).
    /// </summary>
    public static SwipeSpring Smooth { get; } = new(240, 28);

    /// <summary>
    /// Bouncy spring: gentle oscillation before settling.
    /// </summary>
    public static SwipeSpring Bouncy { get; } = new(220, 18);

    /// <summary>
    /// Stiff spring: high tension, zero bounce.
    /// </summary>
    public static SwipeSpring Stiff { get; } = new(500, 38);

    /// <summary>
    /// Approximate settling duration in milliseconds derived from damping physics.
    /// </summary>
    public int SettlingDurationMs =>
        Math.Clamp((int)Math.Round(8000.0 / Math.Max(Damping, 10)), 160, 420);

    /// <summary>
    /// Optimal CSS bezier easing curve matching this spring's damping ratio.
    /// </summary>
    public string ToCssCurve()
    {
        var dampingRatio = Damping / (2.0 * Math.Sqrt(Math.Max(Stiffness, 1)));
        return dampingRatio < 0.7
            ? "cubic-bezier(0.175, 0.885, 0.32, 1.275)"
            : "cubic-bezier(0.16, 1, 0.3, 1)";
    }
}
