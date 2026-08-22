namespace IOSwipeView;

/// <summary>
/// Physical spring configuration used for settling and transition animations.
/// </summary>
/// <param name="Stiffness">Spring stiffness coefficient.</param>
/// <param name="Damping">Damping coefficient.</param>
public readonly record struct SwipeSpring(double Stiffness, double Damping)
{
    /// <summary>Default spring configuration (Stiffness: 300, Damping: 32).</summary>
    public static SwipeSpring Default { get; } = new(300, 32);

    /// <summary>Snappy spring with rapid settling (Stiffness: 420, Damping: 40).</summary>
    public static SwipeSpring Snappy { get; } = new(420, 40);

    /// <summary>Smooth spring with gentle deceleration (Stiffness: 240, Damping: 28).</summary>
    public static SwipeSpring Smooth { get; } = new(240, 28);

    /// <summary>Bouncy spring with light oscillation (Stiffness: 220, Damping: 18).</summary>
    public static SwipeSpring Bouncy { get; } = new(220, 18);

    /// <summary>Stiff spring with high resistance (Stiffness: 500, Damping: 38).</summary>
    public static SwipeSpring Stiff { get; } = new(500, 38);

    /// <summary>
    /// Approximate settling duration in milliseconds derived from damping physics.
    /// </summary>
    public int SettlingDurationMs =>
        Math.Clamp((int)Math.Round(8000.0 / Math.Max(Damping, 10)), 160, 420);

    /// <summary>
    /// CSS bezier easing curve approximation matching this spring's damping ratio.
    /// </summary>
    public string ToCssCurve()
    {
        var dampingRatio = Damping / (2.0 * Math.Sqrt(Math.Max(Stiffness, 1)));
        return dampingRatio < 0.7
            ? "cubic-bezier(0.175, 0.885, 0.32, 1.275)"
            : "cubic-bezier(0.16, 1, 0.3, 1)";
    }
}
