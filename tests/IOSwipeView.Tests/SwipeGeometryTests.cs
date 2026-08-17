using Xunit;

namespace IOSwipeView.Tests;

/// <summary>
/// The feel of the control lives entirely in <see cref="SwipeGeometry"/>, so it is pinned here
/// rather than left to be judged by dragging the demo.
/// </summary>
public class SwipeGeometryTests
{
    private static readonly SwipeOptions Options = SwipeOptions.Default;

    /// <summary>Two leading actions, three trailing, neither edge armed, 400px wide row.</summary>
    private static SwipeGeometry Geometry(
        int leading = 2,
        int trailing = 3,
        bool leadingTriggers = false,
        bool trailingTriggers = false,
        SwipeOptions? options = null) =>
        new(options ?? Options, new SwipeMetrics(leading, trailing, leadingTriggers, trailingTriggers, 400));

    // ---- Widths and offsets -------------------------------------------------------------

    [Theory]
    [InlineData(0, 0)]     // no actions
    [InlineData(1, 100)]   // one action, no gaps
    [InlineData(2, 208)]   // 2*100 + 1*8
    [InlineData(3, 316)]   // 3*100 + 2*8
    public void ActionsWidthCountsGapsBetweenActionsOnly(int count, double expected)
    {
        var geometry = Geometry(leading: count);

        Assert.Equal(expected, geometry.ActionsWidth(SwipeSide.Leading));
    }

    [Fact]
    public void ExpandedOffsetIsSignedBySide()
    {
        var geometry = Geometry();

        // Leading opens positive, trailing negative, each one spacing clear of the row.
        Assert.Equal(216, geometry.ExpandedOffset(SwipeSide.Leading));    //  208 + 8
        Assert.Equal(-324, geometry.ExpandedOffset(SwipeSide.Trailing));  // -(316 + 8)
    }

    [Fact]
    public void ExpandedOffsetIsZeroWhenSideHasNoActions()
    {
        var geometry = Geometry(leading: 0);

        Assert.Equal(0, geometry.ExpandedOffset(SwipeSide.Leading));
    }

    [Fact]
    public void ReadyToTriggerOffsetClearsTheFullyExpandedPosition()
    {
        var geometry = Geometry();

        // 324 expanded + 20 padding = 344, already past the 200 floor.
        Assert.Equal(-344, geometry.ReadyToTriggerOffset(SwipeSide.Trailing));
    }

    [Fact]
    public void ReadyToTriggerOffsetIsFlooredSoNarrowSidesCannotFireInstantly()
    {
        // One action: 108 expanded + 20 padding = 128, which the 200px floor overrides.
        var geometry = Geometry(trailing: 1);

        Assert.Equal(-Options.MinimumPointToTrigger, geometry.ReadyToTriggerOffset(SwipeSide.Trailing));
    }

    // ---- Opacity ------------------------------------------------------------------------

    [Theory]
    [InlineData(0, 0)]       // closed
    [InlineData(50, 0)]      // exactly at the start point
    [InlineData(75, 0.5)]    // halfway through the ramp
    [InlineData(100, 1)]     // at the end point
    [InlineData(400, 1)]     // well past, still clamped
    public void ActionsOpacityRampsBetweenTheVisiblePoints(double offset, double expected)
    {
        var geometry = Geometry();

        Assert.Equal(expected, geometry.ActionsOpacity(offset, SwipeSide.Leading), precision: 6);
    }

    [Fact]
    public void ActionsOpacityIsNeverNaNWhenTheFadeRangeIsZero()
    {
        // The SwiftUI original divides by zero here and is rescued by IEEE infinity. The same
        // expression in .NET yields NaN at offset 0, which would silently blank the actions.
        var noFade = Options with { ActionsVisibleStartPoint = 0, ActionsVisibleEndPoint = 0 };
        var geometry = Geometry(options: noFade);

        var closed = geometry.ActionsOpacity(0, SwipeSide.Leading);
        var dragged = geometry.ActionsOpacity(1, SwipeSide.Leading);

        Assert.False(double.IsNaN(closed));
        Assert.Equal(0, closed);
        Assert.Equal(1, dragged);
    }

    // ---- Rubber banding -----------------------------------------------------------------

    [Fact]
    public void RubberBandDampsOverDragAndKeepsItsDirection()
    {
        var geometry = Geometry();

        var pulled = geometry.RubberBand(100);

        Assert.True(pulled < 100, "over-drag should be damped");
        Assert.True(pulled > 0, "damping should not change direction");
        Assert.Equal(-pulled, geometry.RubberBand(-100), precision: 9);
    }

    [Fact]
    public void RubberBandIsTheOnlyMovementWhenASideHasNoActions()
    {
        var geometry = Geometry(leading: 0);

        var outcome = geometry.Drag(savedOffset: 0, translation: 120, currentSide: SwipeSide.Leading);

        Assert.True(outcome.Offset < 120);
        Assert.Null(outcome.LeadingState);
    }

    // ---- Dragging -----------------------------------------------------------------------

    [Fact]
    public void DragFollowsThePointerBeforeAnyThreshold()
    {
        var geometry = Geometry();

        var outcome = geometry.Drag(savedOffset: 0, translation: -120, currentSide: SwipeSide.Trailing);

        Assert.Equal(-120, outcome.Offset);
        Assert.Null(outcome.TrailingState);
    }

    [Fact]
    public void DragArmsTheEdgeActionOnlyWhenTheSideOptedIn()
    {
        var armed = Geometry(trailingTriggers: true);
        var unarmed = Geometry(trailingTriggers: false);

        // -400 is past the -344 trigger threshold.
        var armedOutcome = armed.Drag(0, -400, SwipeSide.Trailing);
        var unarmedOutcome = unarmed.Drag(0, -400, SwipeSide.Trailing);

        Assert.Equal(SwipeState.Triggering, armedOutcome.TrailingState);
        Assert.Equal(-400, armedOutcome.Offset);

        Assert.Null(unarmedOutcome.TrailingState);
        Assert.True(unarmedOutcome.Offset > -400, "an unarmed side should resist past its threshold");
    }

    [Fact]
    public void DragCannotCrossToTheOtherSideMidGesture()
    {
        var geometry = Geometry();

        // Committed to trailing, but the pointer has swung back past centre.
        var outcome = geometry.Drag(savedOffset: 0, translation: 150, currentSide: SwipeSide.Trailing);

        Assert.True(outcome.Offset < 150, "crossing over should be rubber-banded, not followed");
    }

    [Fact]
    public void DragCanCrossWhenSingleSwipeAcrossIsAllowed()
    {
        var geometry = Geometry(options: Options with { AllowSingleSwipeAcross = true });

        var outcome = geometry.Drag(savedOffset: 0, translation: 150, currentSide: SwipeSide.Trailing);

        Assert.Equal(150, outcome.Offset);
    }

    // ---- Releasing ----------------------------------------------------------------------

    [Fact]
    public void ReleaseSnapsClosedWhenBarelyDragged()
    {
        var geometry = Geometry();

        var outcome = geometry.Release(0, -30, velocity: 0, null, null, SwipeSide.Trailing);

        Assert.Equal(SwipeState.Closed, outcome.State);
        Assert.Equal(0, outcome.TargetOffset);
        Assert.Null(outcome.Side);
    }

    [Fact]
    public void ReleaseSettlesOpenWhenDraggedPastTheExpandThreshold()
    {
        var geometry = Geometry();

        var outcome = geometry.Release(0, -300, velocity: 0, null, null, SwipeSide.Trailing);

        Assert.Equal(SwipeState.Expanded, outcome.State);
        Assert.Equal(SwipeSide.Trailing, outcome.Side);
        Assert.Equal(geometry.ExpandedOffset(SwipeSide.Trailing), outcome.TargetOffset);
    }

    [Fact]
    public void AFlickOpensTheRowWithoutDraggingTheWholeWay()
    {
        var geometry = Geometry();

        // The same short drag: stationary it snaps back, flicked it opens.
        var slow = geometry.Release(0, -80, velocity: 0, null, null, SwipeSide.Trailing);
        var flicked = geometry.Release(0, -80, velocity: -1200, null, null, SwipeSide.Trailing);

        Assert.Equal(SwipeState.Closed, slow.State);
        Assert.Equal(SwipeState.Expanded, flicked.State);
    }

    [Fact]
    public void ReleaseCommitsAnArmedEdgeAction()
    {
        var geometry = Geometry(trailingTriggers: true);

        var outcome = geometry.Release(0, -400, 0, null, SwipeState.Triggering, SwipeSide.Trailing);

        Assert.Equal(SwipeState.Triggered, outcome.State);
        Assert.True(outcome.IsTriggered);
        Assert.Equal(geometry.TriggeredOffset(SwipeSide.Trailing), outcome.TargetOffset);
    }

    [Fact]
    public void TriggeredOffsetCarriesTheRowClearOfTheViewport()
    {
        var geometry = Geometry();

        // 400px row plus one spacing, so nothing of it is left visible.
        Assert.Equal(-408, geometry.TriggeredOffset(SwipeSide.Trailing));
    }

    [Fact]
    public void ReleaseUsesTheSpringMatchingTheOutcome()
    {
        var options = Options with
        {
            CloseAnimation = new SwipeSpring(1, 1),
            ExpandAnimation = new SwipeSpring(2, 2),
            TriggerAnimation = new SwipeSpring(3, 3),
        };
        var geometry = Geometry(trailingTriggers: true, options: options);

        Assert.Equal(options.CloseAnimation, geometry.Release(0, -10, 0, null, null, null).Spring);
        Assert.Equal(options.ExpandAnimation, geometry.Release(0, -300, 0, null, null, null).Spring);
        Assert.Equal(
            options.TriggerAnimation,
            geometry.Release(0, -400, 0, null, SwipeState.Triggering, null).Spring);
    }

    [Fact]
    public void ReleaseWillNotOpenASideThatHasNoActions()
    {
        var geometry = Geometry(leading: 0);

        var outcome = geometry.Release(0, 300, 0, null, null, null);

        Assert.Equal(SwipeState.Closed, outcome.State);
    }

    // ---- Multi-Stage Trigger Tests ------------------------------------------------------

    [Fact]
    public void MultiStageTriggersCalculateProgressiveThresholds()
    {
        var geometry = Geometry(trailing: 3, trailingTriggers: true);

        var stage1 = geometry.ReadyToTriggerOffset(SwipeSide.Trailing, stage: 1);
        var stage2 = geometry.ReadyToTriggerOffset(SwipeSide.Trailing, stage: 2);

        // Stage 1 is -344, Stage 2 is -344 + (-80) = -424
        Assert.Equal(-344, stage1);
        Assert.Equal(-344 - Options.DeepTriggerPadding, stage2);
    }

    [Fact]
    public void DragArmsStage1AndStage2TriggersSequentially()
    {
        // 3 trailing actions, indices [1, 2] configured as triggers (e.g. Archive @ 1, Delete @ 2)
        var metrics = new SwipeMetrics(2, 3, [], [1, 2], 400);
        var geometry = new SwipeGeometry(Options, metrics);

        var stage1Threshold = geometry.ReadyToTriggerOffset(SwipeSide.Trailing, 1); // -344
        var stage2Threshold = geometry.ReadyToTriggerOffset(SwipeSide.Trailing, 2); // -424

        // Drag between Stage 1 and Stage 2 (-360px)
        var outcome1 = geometry.Drag(0, -360, SwipeSide.Trailing);
        Assert.Equal(SwipeState.Triggering, outcome1.TrailingState);
        Assert.Equal(1, outcome1.ArmedActionIndex);

        // Drag past Stage 2 (-450px)
        var outcome2 = geometry.Drag(0, -450, SwipeSide.Trailing);
        Assert.Equal(SwipeState.Triggering, outcome2.TrailingState);
        Assert.Equal(2, outcome2.ArmedActionIndex);
    }

    [Fact]
    public void ReleaseInStage1OrStage2CommitsCorrectTriggeredActionIndex()
    {
        var metrics = new SwipeMetrics(2, 3, [], [1, 2], 400);
        var geometry = new SwipeGeometry(Options, metrics);

        // Release in Stage 1
        var outcome1 = geometry.Release(0, -360, 0, null, SwipeState.Triggering, SwipeSide.Trailing, armedActionIndex: 1);
        Assert.Equal(SwipeState.Triggered, outcome1.State);
        Assert.Equal(1, outcome1.TriggeredActionIndex);

        // Release in Stage 2
        var outcome2 = geometry.Release(0, -450, 0, null, SwipeState.Triggering, SwipeSide.Trailing, armedActionIndex: 2);
        Assert.Equal(SwipeState.Triggered, outcome2.State);
        Assert.Equal(2, outcome2.TriggeredActionIndex);
    }

    [Fact]
    public void DragArmsStage3AndStage4TriggersSequentially()
    {
        // 4 trailing actions, all 4 configured as progressive triggers [0, 1, 2, 3]
        var metrics = new SwipeMetrics(0, 4, [], [0, 1, 2, 3], 600);
        var geometry = new SwipeGeometry(Options, metrics);

        var stage1 = geometry.ReadyToTriggerOffset(SwipeSide.Trailing, 1);
        var stage2 = geometry.ReadyToTriggerOffset(SwipeSide.Trailing, 2);
        var stage3 = geometry.ReadyToTriggerOffset(SwipeSide.Trailing, 3);
        var stage4 = geometry.ReadyToTriggerOffset(SwipeSide.Trailing, 4);

        // Stage 3 pull
        var outcome3 = geometry.Drag(0, stage3 - 10, SwipeSide.Trailing);
        Assert.Equal(SwipeState.Triggering, outcome3.TrailingState);
        Assert.Equal(2, outcome3.ArmedActionIndex);

        // Stage 4 pull
        var outcome4 = geometry.Drag(0, stage4 - 10, SwipeSide.Trailing);
        Assert.Equal(SwipeState.Triggering, outcome4.TrailingState);
        Assert.Equal(3, outcome4.ArmedActionIndex);
    }

    [Fact]
    public void ActionPlacementEnumsAreDefinedCorrectly()
    {
        Assert.Equal(ActionPlacement.Top, (ActionPlacement)0);
        Assert.Equal(ActionPlacement.InlineStart, (ActionPlacement)1);
        Assert.Equal(ActionPlacement.InlineEnd, (ActionPlacement)2);
        Assert.Equal(ActionPlacement.IconOnly, (ActionPlacement)3);
    }
}
