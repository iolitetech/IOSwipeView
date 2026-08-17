namespace IOSwipeView;

/// <summary>
/// The complete geometry and state-machine maths for a swipe row, as pure functions.
/// </summary>
/// <remarks>
/// <para>
/// Everything the control does is derived from a single signed number: the content offset.
/// Positive reveals leading actions, negative reveals trailing ones, and action widths, opacity
/// and mask sizes all fall out of it. There is no second source of truth.
/// </para>
/// <para>
/// Nothing here touches the DOM, JavaScript, or Blazor, so the feel of the control is fully
/// testable in isolation.
/// </para>
/// </remarks>
/// <param name="Options">The tuning to apply.</param>
/// <param name="Metrics">The measured facts about the row.</param>
public readonly record struct SwipeGeometry(SwipeOptions Options, SwipeMetrics Metrics)
{
    /// <summary>
    /// How much the projected release position is pulled back towards the finger.
    /// </summary>
    /// <remarks>
    /// Taking the velocity projection at face value makes the control feel twitchy — a small
    /// flick throws the predicted position far past anything the user intended. Halving it keeps
    /// flicks responsive without letting them overshoot. The value is inherited from the SwiftUI
    /// original, where it appears as an unexplained <c>* 0.5</c>.
    /// </remarks>
    private const double PredictionDamping = 0.5;

    /// <summary>
    /// The combined natural width of one side's actions, including the gaps between them.
    /// </summary>
    /// <param name="side">The side to measure.</param>
    /// <returns>The width in pixels, or <c>0</c> if the side has no actions.</returns>
    public double ActionsWidth(SwipeSide side)
    {
        var count = Metrics.ActionCount(side);
        return count <= 0 ? 0 : (count * Options.ActionWidth) + ((count - 1) * Options.Spacing);
    }

    /// <summary>
    /// The offset at which one side's actions are exactly fully revealed.
    /// </summary>
    /// <param name="side">The side to measure.</param>
    /// <returns>The signed offset in pixels, or <c>0</c> if the side has no actions.</returns>
    public double ExpandedOffset(SwipeSide side) =>
        Metrics.ActionCount(side) <= 0
            ? 0
            : (ActionsWidth(side) + Options.Spacing) * side.Sign();

    /// <summary>
    /// The offset a release must be projected past for the row to settle open rather than closed.
    /// </summary>
    /// <param name="side">The side to measure.</param>
    /// <returns>The signed offset in pixels.</returns>
    public double ReadyToExpandOffset(SwipeSide side) =>
        Options.ReadyToExpandPadding * side.Sign();

    /// <summary>
    /// The offset past which the edge action arms itself for triggering.
    /// </summary>
    /// <param name="side">The side to measure.</param>
    /// <param name="stage">The trigger tier/stage (1 for first trigger, 2 for deep trigger).</param>
    /// <returns>The signed offset in pixels.</returns>
    public double ReadyToTriggerOffset(SwipeSide side, int stage = 1)
    {
        var beyondExpanded = Math.Abs(ExpandedOffset(side)) + Options.ReadyToTriggerPadding;

        // A side holding one narrow action would otherwise arm almost immediately, which makes
        // destructive actions far too easy to fire by accident.
        var baseMagnitude = Math.Max(beyondExpanded, Options.MinimumPointToTrigger);
        var magnitude = stage > 1
            ? baseMagnitude + ((stage - 1) * Options.DeepTriggerPadding)
            : baseMagnitude;

        return magnitude * side.Sign();
    }

    /// <summary>
    /// The offset that carries the row fully off-screen when its edge action is triggered.
    /// </summary>
    /// <param name="side">The side to measure.</param>
    /// <returns>The signed offset in pixels.</returns>
    public double TriggeredOffset(SwipeSide side) =>
        (Metrics.RowWidth + Options.Spacing) * side.Sign();

    /// <summary>
    /// How much of one side's action strip is currently uncovered.
    /// </summary>
    /// <param name="offset">The current signed content offset.</param>
    /// <param name="side">The side to measure.</param>
    /// <returns>A non-negative width in pixels.</returns>
    public double VisibleWidth(double offset, SwipeSide side) =>
        Math.Max(0, (offset * side.Sign()) - Options.Spacing);

    /// <summary>
    /// How opaque one side's actions should be at the current offset.
    /// </summary>
    /// <param name="offset">The current signed content offset.</param>
    /// <param name="side">The side to measure.</param>
    /// <returns>A value between <c>0</c> and <c>1</c>.</returns>
    public double ActionsOpacity(double offset, SwipeSide side)
    {
        var beyondStart = Math.Max(0, (offset * side.Sign()) - Options.ActionsVisibleStartPoint);
        var range = Options.ActionsVisibleEndPoint - Options.ActionsVisibleStartPoint;

        // A zero-width range means "no fade". The SwiftUI original divides by zero here and is
        // rescued by IEEE infinity; in .NET that same expression yields NaN when the numerator is
        // also zero, which would silently blank the actions instead. Handle it explicitly.
        if (range <= 0)
        {
            return beyondStart > 0 ? 1 : 0;
        }

        return Math.Clamp(beyondStart / range, 0, 1);
    }

    /// <summary>
    /// Applies rubber banding to an over-drag distance, preserving its direction.
    /// </summary>
    /// <param name="distance">How far past the limit the row has been dragged, signed.</param>
    /// <returns>The damped distance, signed the same way.</returns>
    public double RubberBand(double distance) =>
        Math.CopySign(Math.Pow(Math.Abs(distance), Options.StretchRubberBandingPower), distance);

    /// <summary>
    /// The side, if any, that this drag is not allowed to reveal.
    /// </summary>
    /// <remarks>
    /// Unless <see cref="SwipeOptions.AllowSingleSwipeAcross"/> is set, a drag that started
    /// towards one side may not cross over and reveal the other side's actions without being
    /// released first.
    /// </remarks>
    /// <param name="currentSide">The side this drag committed to, if it has committed.</param>
    /// <param name="offset">The signed offset being tested.</param>
    /// <returns>The forbidden side, or <see langword="null"/> if the offset is allowed.</returns>
    public SwipeSide? DisallowedSide(SwipeSide? currentSide, double offset)
    {
        if (Options.AllowSingleSwipeAcross || currentSide is null)
        {
            return null;
        }

        return currentSide switch
        {
            SwipeSide.Leading when offset < 0 => SwipeSide.Trailing,
            SwipeSide.Trailing when offset > 0 => SwipeSide.Leading,
            _ => null,
        };
    }

    /// <summary>
    /// Resolves where the row should sit part-way through a drag.
    /// </summary>
    /// <param name="savedOffset">The offset the row rested at before this drag began.</param>
    /// <param name="translation">How far the pointer has moved during this drag, in pixels.</param>
    /// <param name="currentSide">The side this drag committed to, if it has committed.</param>
    /// <returns>The offset to render at, and each side's state.</returns>
    public SwipeDragOutcome Drag(double savedOffset, double translation, SwipeSide? currentSide)
    {
        var totalOffset = savedOffset + translation;
        var disallowed = DisallowedSide(currentSide, totalOffset);

        // Nothing to reveal on the side being pulled open, so the row simply stretches.
        if (totalOffset > 0 && (Metrics.LeadingActionCount == 0 || disallowed == SwipeSide.Leading))
        {
            return new SwipeDragOutcome(RubberBand(totalOffset), null, null);
        }

        if (totalOffset < 0 && (Metrics.TrailingActionCount == 0 || disallowed == SwipeSide.Trailing))
        {
            return new SwipeDragOutcome(RubberBand(totalOffset), null, null);
        }

        if (TryTrigger(SwipeSide.Leading, totalOffset, out var leading))
        {
            return leading;
        }

        if (TryTrigger(SwipeSide.Trailing, totalOffset, out var trailing))
        {
            return trailing;
        }

        return new SwipeDragOutcome(totalOffset, null, null);
    }

    /// <summary>
    /// Resolves where the row should settle once the drag is released.
    /// </summary>
    /// <param name="savedOffset">The offset the row rested at before this drag began.</param>
    /// <param name="translation">How far the pointer moved in total, in pixels.</param>
    /// <param name="velocity">The release velocity in pixels per second.</param>
    /// <param name="leadingState">The leading side's state at the moment of release.</param>
    /// <param name="trailingState">The trailing side's state at the moment of release.</param>
    /// <param name="currentSide">The side this drag committed to, if it has committed.</param>
    /// <param name="armedActionIndex">The index of the action armed on release.</param>
    /// <returns>The offset to settle at, the resulting state, and the spring to use.</returns>
    public SwipeReleaseOutcome Release(
        double savedOffset,
        double translation,
        double velocity,
        SwipeState? leadingState,
        SwipeState? trailingState,
        SwipeSide? currentSide,
        int? armedActionIndex = null)
    {
        // Resolve against where the row would come to rest, not where the finger left it, so a
        // quick flick opens the row without having to drag the whole way.
        var projected = translation + (velocity * Options.VelocityProjectionSeconds);
        var predictedOffset = (savedOffset + projected) * PredictionDamping;

        if (DisallowedSide(currentSide, predictedOffset) is not null)
        {
            return Close();
        }

        // An armed trigger action wins over any expand threshold it happens to also satisfy.
        if (trailingState == SwipeState.Triggering)
        {
            return Trigger(SwipeSide.Trailing, armedActionIndex);
        }

        if (leadingState == SwipeState.Triggering)
        {
            return Trigger(SwipeSide.Leading, armedActionIndex);
        }

        if (predictedOffset > ReadyToExpandOffset(SwipeSide.Leading) && Metrics.LeadingActionCount > 0)
        {
            return Expand(SwipeSide.Leading);
        }

        if (predictedOffset < ReadyToExpandOffset(SwipeSide.Trailing) && Metrics.TrailingActionCount > 0)
        {
            return Expand(SwipeSide.Trailing);
        }

        return Close();
    }

    /// <summary>
    /// The offset and spring for settling a side into a given state, for programmatic control.
    /// </summary>
    /// <param name="side">The side to move.</param>
    /// <param name="state">The state to move it into.</param>
    /// <returns>The corresponding settle outcome.</returns>
    public SwipeReleaseOutcome MoveTo(SwipeSide side, SwipeState state) => state switch
    {
        SwipeState.Expanded => Expand(side),
        SwipeState.Triggered => Trigger(side),
        SwipeState.Triggering => new SwipeReleaseOutcome(
            ReadyToTriggerOffset(side), side, SwipeState.Triggering, Options.TriggerAnimation),
        _ => Close(),
    };

    private bool TryTrigger(SwipeSide side, double totalOffset, out SwipeDragOutcome outcome)
    {
        var triggers = Metrics.TriggerIndices(side);
        if (triggers.Count == 0)
        {
            var threshold = ReadyToTriggerOffset(side, 1);
            var beyond = (totalOffset - threshold) * side.Sign();

            if (beyond <= 0)
            {
                outcome = default;
                return false;
            }

            // No trigger on this side, so the row resists past the point actions are fully shown.
            outcome = new SwipeDragOutcome(threshold + RubberBand(totalOffset - threshold), null, null);
            return true;
        }

        // Evaluate from highest stage (deep trigger) to lowest stage (first trigger)
        for (var stage = triggers.Count; stage >= 1; stage--)
        {
            var stageThreshold = ReadyToTriggerOffset(side, stage);
            var stageBeyond = (totalOffset - stageThreshold) * side.Sign();

            if (stageBeyond > 0)
            {
                var armedActionIndex = triggers[stage - 1];
                outcome = side == SwipeSide.Leading
                    ? new SwipeDragOutcome(totalOffset, SwipeState.Triggering, null, armedActionIndex)
                    : new SwipeDragOutcome(totalOffset, null, SwipeState.Triggering, armedActionIndex);
                return true;
            }
        }

        outcome = default;
        return false;
    }

    private SwipeReleaseOutcome Close() =>
        new(0, null, SwipeState.Closed, Options.CloseAnimation);

    private SwipeReleaseOutcome Expand(SwipeSide side) =>
        new(ExpandedOffset(side), side, SwipeState.Expanded, Options.ExpandAnimation);

    private SwipeReleaseOutcome Trigger(SwipeSide side, int? actionIndex = null) =>
        new(TriggeredOffset(side), side, SwipeState.Triggered, Options.TriggerAnimation, actionIndex);
}
