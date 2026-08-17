namespace IOSwipeView;

/// <summary>
/// Tuning for a <see cref="SwipeView"/>. All distances are in CSS pixels.
/// </summary>
/// <remarks>
/// This is an immutable record, so a customised set is best built once and reused:
/// <code>
/// private static readonly SwipeOptions Compact =
///     SwipeOptions.Default with { ActionWidth = 72, Spacing = 0, ActionCornerRadius = 0 };
/// </code>
/// The defaults reproduce the feel of aheze's SwipeActions for SwiftUI.
/// </remarks>
public sealed record SwipeOptions
{
    /// <summary>The default options (Capsule style: 32px radius, 8px spacing).</summary>
    public static SwipeOptions Default { get; } = new();

    /// <summary>
    /// Modern iOS Floating Capsule / Bubble style (aheze original, 32px pill radius, 8px spacing).
    /// </summary>
    public static SwipeOptions Capsule { get; } = Default;

    /// <summary>
    /// Classic native iOS TableView / Mail.app / Messages.app list style (flush, 0 spacing, square actions).
    /// </summary>
    public static SwipeOptions ClassicList { get; } = Default with
    {
        ActionCornerRadius = 0,
        Spacing = 0,
        ActionsMaskCornerRadius = 0,
        ActionWidth = 80,
    };

    /// <summary>
    /// iOS Lock Screen and Notification style (rounded pills, subtle spacing).
    /// </summary>
    public static SwipeOptions Notification { get; } = Default with
    {
        ActionCornerRadius = 16,
        Spacing = 6,
        ActionsMaskCornerRadius = 16,
        ActionWidth = 76,
    };

    /// <summary>
    /// iOS Inset Grouped style (rounded outer mask matching list section, flush internal actions).
    /// </summary>
    public static SwipeOptions InsetGrouped { get; } = Default with
    {
        ActionCornerRadius = 0,
        Spacing = 0,
        ActionsMaskCornerRadius = 12,
        ActionWidth = 80,
    };

    /// <summary>Whether swiping is currently allowed. Defaults to <see langword="true"/>.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>How the revealed actions are laid out. Defaults to <see cref="SwipeActionStyle.Mask"/>.</summary>
    public SwipeActionStyle Style { get; init; } = SwipeActionStyle.Mask;

    /// <summary>The natural width of a single action. Defaults to <c>100</c>.</summary>
    public double ActionWidth { get; init; } = 100;

    /// <summary>The gap between actions, and between the actions and the row. Defaults to <c>8</c>.</summary>
    public double Spacing { get; init; } = 8;

    /// <summary>The corner radius of each individual action. Defaults to <c>32</c>.</summary>
    public double ActionCornerRadius { get; init; } = 32;

    /// <summary>The corner radius of the mask that reveals the actions as a group. Defaults to <c>20</c>.</summary>
    public double ActionsMaskCornerRadius { get; init; } = 20;

    /// <summary>How far the row must be dragged before the actions start fading in. Defaults to <c>50</c>.</summary>
    public double ActionsVisibleStartPoint { get; init; } = 50;

    /// <summary>
    /// How far the row must be dragged before the actions are fully opaque. Defaults to <c>100</c>.
    /// </summary>
    /// <remarks>
    /// Setting this equal to <see cref="ActionsVisibleStartPoint"/> disables the fade entirely,
    /// so actions are fully visible the instant the start point is passed.
    /// </remarks>
    public double ActionsVisibleEndPoint { get; init; } = 100;

    /// <summary>
    /// How far past the resting position the row must be released to settle open rather than
    /// snap closed. Defaults to <c>50</c>.
    /// </summary>
    public double ReadyToExpandPadding { get; init; } = 50;

    /// <summary>
    /// How far past fully-expanded the row must be dragged to enter <see cref="SwipeState.Triggering"/>.
    /// Defaults to <c>20</c>.
    /// </summary>
    public double ReadyToTriggerPadding { get; init; } = 20;

    /// <summary>
    /// A floor on the drag distance needed to trigger the edge action. Defaults to <c>200</c>.
    /// </summary>
    /// <remarks>
    /// Without this, a side holding one narrow action would trigger almost immediately, which
    /// makes destructive actions far too easy to fire by accident.
    /// </remarks>
    public double MinimumPointToTrigger { get; init; } = 200;

    /// <summary>
    /// Additional drag distance past the first trigger threshold to arm the second (deep) trigger.
    /// Defaults to <c>80</c>.
    /// </summary>
    public double DeepTriggerPadding { get; init; } = 80;

    /// <summary>
    /// Vibration pattern in milliseconds used for deep (Stage 2) trigger haptics. Defaults to <c>[15, 30, 15]</c>.
    /// </summary>
    public int[] DeepHapticPattern { get; init; } = [15, 30, 15];

    /// <summary>
    /// The exponent applied to over-drag past a limit, between 0 and 1. Defaults to <c>0.7</c>.
    /// </summary>
    /// <remarks>
    /// Lower values make the row feel stiffer as it is pulled past its limit; <c>1</c> disables
    /// rubber banding and lets the row follow the finger exactly.
    /// </remarks>
    public double StretchRubberBandingPower { get; init; } = 0.7;

    /// <summary>
    /// Whether a single uninterrupted drag may cross from one side's actions to the other's.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    public bool AllowSingleSwipeAcross { get; init; }

    /// <summary>
    /// Whether to vibrate when the row enters or leaves <see cref="SwipeState.Triggering"/>.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// Uses the Vibration API, which browsers only honour on devices with a vibration motor and
    /// after the user has interacted with the page. It is a no-op everywhere else.
    /// </remarks>
    public bool EnableTriggerHaptics { get; init; } = true;

    /// <summary>
    /// How far ahead of the finger the release position is projected, in seconds. Defaults to <c>0.25</c>.
    /// </summary>
    /// <remarks>
    /// A release is resolved against where the row <em>would</em> come to rest, not where it
    /// physically is, so a quick flick opens the row without having to drag the whole way.
    /// </remarks>
    public double VelocityProjectionSeconds { get; init; } = 0.25;

    /// <summary>
    /// Whether invoking an action automatically springs the row closed. Defaults to <see langword="true"/>.
    /// </summary>
    public bool AutoCloseOnActionInvoked { get; init; } = true;

    /// <summary>
    /// Vibration pattern in milliseconds used for trigger haptics. Defaults to <c>[10]</c>.
    /// </summary>
    public int[] HapticPattern { get; init; } = [10];

    /// <summary>The spring used when settling closed. Defaults to <see cref="SwipeSpring.Default"/>.</summary>
    public SwipeSpring CloseAnimation { get; init; } = SwipeSpring.Default;

    /// <summary>The spring used when settling open. Defaults to <see cref="SwipeSpring.Default"/>.</summary>
    public SwipeSpring ExpandAnimation { get; init; } = SwipeSpring.Default;

    /// <summary>The spring used when the edge action is triggered. Defaults to <see cref="SwipeSpring.Default"/>.</summary>
    public SwipeSpring TriggerAnimation { get; init; } = SwipeSpring.Default;
}
