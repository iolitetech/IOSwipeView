using Xunit;

namespace IOSwipeView.Tests;

public class SwipeOptionsTests
{
    [Fact]
    public void DefaultOptionsHaveAutoCloseEnabled()
    {
        var options = SwipeOptions.Default;

        Assert.True(options.AutoCloseOnActionInvoked);
        Assert.Equal([10], options.HapticPattern);
    }

    [Fact]
    public void ClassicListHasZeroSpacingAndZeroRadius()
    {
        var classic = SwipeOptions.ClassicList;

        Assert.Equal(0, classic.Spacing);
        Assert.Equal(0, classic.ActionCornerRadius);
        Assert.Equal(0, classic.ActionsMaskCornerRadius);
        Assert.Equal(80, classic.ActionWidth);
    }
}
