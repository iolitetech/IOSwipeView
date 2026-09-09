namespace IOSwipeView;

/// <summary>
/// Configuration options for <see cref="SwipeView"/>. All distances are in CSS pixels.
/// </summary>
public sealed record SwipeOptions
{
    /// <summary>Default options (Capsule style: 32px radius, 8px spacing).</summary>
    public static SwipeOptions Default { get; } = new();

    /// <summary>
    /// Floating capsule style with rounded pill actions and spacing.
    /// </summary>
    public static SwipeOptions Capsule { get; } = Default;

    /// <summary>
    /// Classic list style with flush square actions (0 spacing, 0 corner radius).
    /// </summary>
    public static SwipeOptions ClassicList { get; } = Default with
    {
        ActionCornerRadius = 0,
        Spacing = 0,
        ActionsMaskCornerRadius = 0,
        ActionWidth = 80,
    };

    /// <summary>
    /// Notification style with rounded actions and moderate spacing.
    /// </summary>
    public static SwipeOptions Notification { get; } = Default with
    {
        ActionCornerRadius = 16,
        Spacing = 6,
        ActionsMaskCornerRadius = 16,
        ActionWidth = 76,
    };

    /// <summary>
    /// Inset grouped style with rounded container mask and flush actions.
    /// </summary>
    public static SwipeOptions InsetGrouped { get; } = Default with
    {
        ActionCornerRadius = 0,
        Spacing = 0,
        ActionsMaskCornerRadius = 12,
        ActionWidth = 80,
    };

    /// <summary>Whether swiping is enabled. Defaults to <see langword="true"/>.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Layout style for revealed actions. Defaults to <see cref="SwipeActionStyle.Mask"/>.</summary>
    public SwipeActionStyle Style { get; init; } = SwipeActionStyle.Mask;

    /// <summary>Natural width of a single action in pixels. Defaults to <c>100</c>.</summary>
    public double ActionWidth { get; init; } = 100;

    /// <summary>Spacing between actions in pixels. Defaults to <c>8</c>.</summary>
    public double Spacing { get; init; } = 8;

    /// <summary>Corner radius of each action in pixels. Defaults to <c>32</c>.</summary>
    public double ActionCornerRadius { get; init; } = 32;

    /// <summary>Corner radius of the action container mask in pixels. Defaults to <c>20</c>.</summary>
    public double ActionsMaskCornerRadius { get; init; } = 20;

    /// <summary>Drag distance at which actions start fading in. Defaults to <c>50</c>.</summary>
    public double ActionsVisibleStartPoint { get; init; } = 50;

    /// <summary>Drag distance at which actions reach full opacity. Defaults to <c>100</c>.</summary>
    public double ActionsVisibleEndPoint { get; init; } = 100;

    /// <summary>Drag distance threshold required to settle open. Defaults to <c>50</c>.</summary>
    public double ReadyToExpandPadding { get; init; } = 50;

    /// <summary>Drag distance past full expansion required to arm trigger. Defaults to <c>20</c>.</summary>
    public double ReadyToTriggerPadding { get; init; } = 20;

    /// <summary>Minimum drag distance required to trigger an action. Defaults to <c>200</c>.</summary>
    public double MinimumPointToTrigger { get; init; } = 200;

    /// <summary>Additional drag distance past the first trigger to arm subsequent stages. Defaults to <c>80</c>.</summary>
    public double DeepTriggerPadding { get; init; } = 80;

    /// <summary>Vibration pattern in milliseconds for deep trigger haptics. Defaults to <c>[15, 30, 15]</c>.</summary>
    public int[] DeepHapticPattern { get; init; } = [15, 30, 15];

    /// <summary>
    /// Progressive vibration patterns in milliseconds for multi-stage trigger tiers.
    /// Stage 1: <c>[12]</c>, Stage 2: <c>[18, 30, 18]</c>, Stage 3+: <c>[25, 40, 25, 40, 25]</c>.
    /// </summary>
    public int[][] StageHapticPatterns { get; init; } =
    [
        [12],
        [18, 30, 18],
        [25, 40, 25, 40, 25],
    ];

    /// <summary>Resistance exponent applied to over-drag distance (between 0 and 1). Defaults to <c>0.7</c>.</summary>
    public double StretchRubberBandingPower { get; init; } = 0.7;

    /// <summary>Whether a single uninterrupted drag can cross from leading to trailing actions. Defaults to <see langword="false"/>.</summary>
    public bool AllowSingleSwipeAcross { get; init; }

    /// <summary>Whether to trigger device vibration on action state changes. Defaults to <see langword="true"/>.</summary>
    public bool EnableTriggerHaptics { get; init; } = true;

    /// <summary>Velocity projection lookahead in seconds. Defaults to <c>0.25</c>.</summary>
    public double VelocityProjectionSeconds { get; init; } = 0.25;

    /// <summary>Whether invoking an action automatically springs the row closed. Defaults to <see langword="true"/>.</summary>
    public bool AutoCloseOnActionInvoked { get; init; } = true;

    /// <summary>Vibration pattern in milliseconds used for trigger haptics. Defaults to <c>[10]</c>.</summary>
    public int[] HapticPattern { get; init; } = [10];

    /// <summary>Spring used when settling closed. Defaults to <see cref="SwipeSpring.Default"/>.</summary>
    public SwipeSpring CloseAnimation { get; init; } = SwipeSpring.Default;

    /// <summary>Spring used when settling open. Defaults to <see cref="SwipeSpring.Default"/>.</summary>
    public SwipeSpring ExpandAnimation { get; init; } = SwipeSpring.Default;

    /// <summary>Spring used when an action is triggered. Defaults to <see cref="SwipeSpring.Default"/>.</summary>
    public SwipeSpring TriggerAnimation { get; init; } = SwipeSpring.Default;
}
