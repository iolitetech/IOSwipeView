#pragma warning disable BL0005

using Xunit;

namespace IOSwipeView.Tests;

public class SwipeListTests
{
    private sealed record TestItem(int Id, string Name, bool IsSpecial = false);

    [Fact]
    public void OptionsResolution_FallsBackToBaseOptions()
    {
        var baseOptions = SwipeOptions.ClassicList;
        var list = new SwipeList<TestItem>
        {
            Options = baseOptions,
        };

        var item = new TestItem(1, "Item 1");
        var resolved = list.ItemOptions?.Invoke(item) ?? list.Options;

        Assert.Equal(0, resolved.ActionCornerRadius);
        Assert.Equal(0, resolved.Spacing);
    }

    [Fact]
    public void OptionsResolution_AppliesPerItemProvider()
    {
        var baseOptions = SwipeOptions.ClassicList;
        var customOptions = SwipeOptions.Capsule;

        var list = new SwipeList<TestItem>
        {
            Options = baseOptions,
            ItemOptions = item => item.IsSpecial ? customOptions : baseOptions,
        };

        var regularItem = new TestItem(1, "Regular", IsSpecial: false);
        var specialItem = new TestItem(2, "Special", IsSpecial: true);

        var resolvedRegular = list.ItemOptions(regularItem);
        var resolvedSpecial = list.ItemOptions(specialItem);

        Assert.Equal(0, resolvedRegular.ActionCornerRadius);
        Assert.Equal(32, resolvedSpecial.ActionCornerRadius);
    }

    [Fact]
    public void SelectionManagement_MaintainsSetIntegrity()
    {
        var item1 = new TestItem(1, "Item 1");
        var item2 = new TestItem(2, "Item 2");

        var selected = new HashSet<TestItem>();
        
        // Select item 1
        selected.Add(item1);
        Assert.Contains(item1, selected);
        Assert.DoesNotContain(item2, selected);

        // Select item 2
        selected.Add(item2);
        Assert.Equal(2, selected.Count);

        // Deselect item 1
        selected.Remove(item1);
        Assert.Single(selected);
        Assert.Contains(item2, selected);
    }

    [Fact]
    public void SwipeListOptions_PresetsConfiguredProperly()
    {
        var classic = SwipeListOptions.ClassicList;
        Assert.Equal(0, classic.DividerInset);
        Assert.True(classic.ShowDividers);
        Assert.Equal(ListDeleteStyle.AppleSpring, classic.DeleteAnimation.Style);

        var notification = SwipeListOptions.Notification;
        Assert.False(notification.ShowDividers);
        Assert.Equal(ListDeleteStyle.SlideLeft, notification.DeleteAnimation.Style);

        var inset = SwipeListOptions.InsetGrouped;
        Assert.Equal(16, inset.DividerInset);
        Assert.Equal(ListDeleteStyle.ScaleDown, inset.DeleteAnimation.Style);
    }

    [Fact]
    public void SwipeListAnimation_PresetsConfiguredProperly()
    {
        Assert.Equal(320, SwipeListAnimation.AppleSpring.DurationMs);
        Assert.Equal(300, SwipeListAnimation.SlideLeft.DurationMs);
        Assert.Equal(300, SwipeListAnimation.SlideRight.DurationMs);
        Assert.Equal(320, SwipeListAnimation.ShrinkSlideLeft.DurationMs);
        Assert.Equal(320, SwipeListAnimation.ShrinkSlideRight.DurationMs);
        Assert.Equal(300, SwipeListAnimation.ScaleDown.DurationMs);
        Assert.Equal(340, SwipeListAnimation.PopOut.DurationMs);
        Assert.Equal(360, SwipeListAnimation.CardFold.DurationMs);
        Assert.Equal(240, SwipeListAnimation.Fade.DurationMs);
        Assert.Equal(0, SwipeListAnimation.None.DurationMs);
    }

    [Fact]
    public void SwipeListAnimation_FromSpring_DerivesPhysicsParameters()
    {
        var anim = SwipeListAnimation.FromSpring(ListDeleteStyle.PopOut, SwipeSpring.Bouncy);
        Assert.Equal(ListDeleteStyle.PopOut, anim.Style);
        Assert.Equal(SwipeSpring.Bouncy.SettlingDurationMs, anim.DurationMs);
        Assert.Equal(SwipeSpring.Bouncy.ToCssCurve(), anim.Curve);
    }

    [Fact]
    public void SwipeItemContext_ExposesPositionalMetadata()
    {
        var item = new TestItem(1, "Item 1");
        var list = new SwipeList<TestItem>();
        var ctx = new SwipeItemContext<TestItem>(item, list, index: 2, isFirst: false, isLast: true, isSelected: true, isEditing: true);

        Assert.Equal(item, ctx.Item);
        Assert.Equal(2, ctx.Index);
        Assert.False(ctx.IsFirst);
        Assert.True(ctx.IsLast);
        Assert.True(ctx.IsSelected);
        Assert.True(ctx.IsEditing);
    }

    [Fact]
    public void SwipeSpring_CalculatesSettlingDurationAndCssCurve()
    {
        var snappy = SwipeSpring.Snappy;
        Assert.True(snappy.SettlingDurationMs >= 160 && snappy.SettlingDurationMs <= 420);
        Assert.NotEmpty(snappy.ToCssCurve());
    }
}
