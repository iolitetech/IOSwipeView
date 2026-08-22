using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace IOSwipeView.Tests;

public class SwipeComponentBunitTests : IDisposable
{
    private readonly BunitContext _ctx;

    public SwipeComponentBunitTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public void Dispose()
    {
        _ctx.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void SwipeAction_RendersBasicButtonWithColors()
    {
        var invoked = false;
        var cut = _ctx.Render(builder =>
        {
            builder.OpenComponent<SwipeView>(0);
            builder.AddAttribute(1, nameof(SwipeView.TrailingActions), (RenderFragment<SwipeContext>)(_ => actionBuilder =>
            {
                actionBuilder.OpenComponent<SwipeAction>(0);
                actionBuilder.AddAttribute(1, nameof(SwipeAction.Background), "#ff9500");
                actionBuilder.AddAttribute(2, nameof(SwipeAction.Foreground), "#ffffff");
                actionBuilder.AddAttribute(3, nameof(SwipeAction.ChildContent), (RenderFragment)(b => b.AddContent(0, "Flag")));
                actionBuilder.AddAttribute(4, nameof(SwipeAction.OnInvoked), EventCallback.Factory.Create(this, () => invoked = true));
                actionBuilder.CloseComponent();
            }));
            builder.AddAttribute(2, nameof(SwipeView.ChildContent), (RenderFragment)(b => b.AddMarkupContent(0, "<div>Row</div>")));
            builder.CloseComponent();
        });

        var button = cut.Find("button.ioswipe__action");
        Assert.NotNull(button);
        Assert.Contains("Flag", button.TextContent);
        Assert.Contains("--ioswipe-action-background:#ff9500", button.GetAttribute("style"));
        Assert.Contains("--ioswipe-action-foreground:#ffffff", button.GetAttribute("style"));

        // Simulate click
        button.Click();
        Assert.True(invoked);
    }

    [Fact]
    public void SwipeAction_RendersIconAndPlacementClass()
    {
        var cut = _ctx.Render(builder =>
        {
            builder.OpenComponent<SwipeView>(0);
            builder.AddAttribute(1, nameof(SwipeView.TrailingActions), (RenderFragment<SwipeContext>)(_ => actionBuilder =>
            {
                actionBuilder.OpenComponent<SwipeAction>(0);
                actionBuilder.AddAttribute(1, nameof(SwipeAction.Placement), ActionPlacement.InlineStart);
                actionBuilder.AddAttribute(2, nameof(SwipeAction.Icon), (RenderFragment)(b => b.AddMarkupContent(0, "<svg class=\"icon\"></svg>")));
                actionBuilder.AddAttribute(3, nameof(SwipeAction.ChildContent), (RenderFragment)(b => b.AddContent(0, "Archive")));
                actionBuilder.CloseComponent();
            }));
            builder.AddAttribute(2, nameof(SwipeView.ChildContent), (RenderFragment)(b => b.AddMarkupContent(0, "<div>Row</div>")));
            builder.CloseComponent();
        });

        var button = cut.Find("button.ioswipe__action");
        Assert.Contains("ioswipe__action--inline-start", button.ClassName);
        Assert.NotNull(cut.Find(".icon"));
        Assert.Contains("Archive", button.TextContent);
    }

    [Fact]
    public void SwipeAction_RendersAriaLabel_WhenProvided()
    {
        var cut = _ctx.Render(builder =>
        {
            builder.OpenComponent<SwipeView>(0);
            builder.AddAttribute(1, nameof(SwipeView.TrailingActions), (RenderFragment<SwipeContext>)(_ => actionBuilder =>
            {
                actionBuilder.OpenComponent<SwipeAction>(0);
                actionBuilder.AddAttribute(1, nameof(SwipeAction.Placement), ActionPlacement.IconOnly);
                actionBuilder.AddAttribute(2, nameof(SwipeAction.AriaLabel), "Delete message");
                actionBuilder.AddAttribute(3, nameof(SwipeAction.ChildContent), (RenderFragment)(b => b.AddContent(0, "Delete")));
                actionBuilder.CloseComponent();
            }));
            builder.AddAttribute(2, nameof(SwipeView.ChildContent), (RenderFragment)(b => b.AddMarkupContent(0, "<div>Row</div>")));
            builder.CloseComponent();
        });

        var button = cut.Find("button.ioswipe__action");
        Assert.Equal("Delete message", button.GetAttribute("aria-label"));
        Assert.Contains("ioswipe__action--icon-only", button.ClassName);
    }

    [Fact]
    public void SwipeViewGroup_RendersChildContent()
    {
        var cut = _ctx.Render<SwipeViewGroup>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddMarkupContent(0, "<div class=\"test-child\">Item</div>")));

        Assert.NotNull(cut.Find(".test-child"));
        Assert.Equal("Item", cut.Find(".test-child").TextContent);
    }

    [Fact]
    public void SwipeList_RendersEmptyContent_WhenItemsEmpty()
    {
        var cut = _ctx.Render<SwipeList<string>>(parameters => parameters
            .Add(p => p.Items, Array.Empty<string>())
            .Add(p => p.ItemTemplate, item => builder => builder.AddContent(0, item))
            .Add(p => p.EmptyContent, builder => builder.AddMarkupContent(0, "<p class=\"empty\">No Items</p>")));

        var empty = cut.Find(".empty");
        Assert.NotNull(empty);
        Assert.Equal("No Items", empty.TextContent);
    }

    [Fact]
    public void SwipeList_RendersHeaderAndFooterContent()
    {
        var items = new[] { "Alpha", "Beta" };
        var cut = _ctx.Render<SwipeList<string>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.ItemTemplate, item => builder => builder.AddMarkupContent(0, $"<span class=\"row-text\">{item}</span>"))
            .Add(p => p.HeaderContent, builder => builder.AddMarkupContent(0, "<header class=\"list-hdr\">Header</header>"))
            .Add(p => p.FooterContent, builder => builder.AddMarkupContent(0, "<footer class=\"list-ftr\">Footer</footer>")));

        Assert.NotNull(cut.Find(".list-hdr"));
        Assert.NotNull(cut.Find(".list-ftr"));
        var rows = cut.FindAll(".row-text");
        Assert.Equal(2, rows.Count);
        Assert.Equal("Alpha", rows[0].TextContent);
        Assert.Equal("Beta", rows[1].TextContent);
    }

    [Fact]
    public void SwipeList_RendersEditModeSelectionCheckmarks()
    {
        var items = new[] { "Item 1", "Item 2" };
        var selected = new HashSet<string>();

        var cut = _ctx.Render<SwipeList<string>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.IsEditing, true)
            .Add(p => p.SelectedItems, selected)
            .Add(p => p.SelectedItemsChanged, EventCallback.Factory.Create<ISet<string>>(this, s => selected = new HashSet<string>(s)))
            .Add(p => p.ItemTemplate, item => builder => builder.AddContent(0, item)));

        var selectButtons = cut.FindAll(".ioswipe-list__select-btn");
        Assert.Equal(2, selectButtons.Count);

        // Click first item select checkmark
        selectButtons[0].Click();
        Assert.Contains("Item 1", selected);

        // Re-render with updated selected items
        cut.Render(parameters => parameters
            .Add(p => p.SelectedItems, selected));

        var rowWrappers = cut.FindAll(".ioswipe-list__row-wrapper");
        Assert.Equal("true", rowWrappers[0].GetAttribute("aria-selected"));
        Assert.Equal("false", rowWrappers[1].GetAttribute("aria-selected"));

        var updatedButtons = cut.FindAll(".ioswipe-list__select-btn");
        Assert.Contains("ioswipe-list__select-btn--selected", updatedButtons[0].ClassName);
        Assert.DoesNotContain("ioswipe-list__select-btn--selected", updatedButtons[1].ClassName);
    }

    [Fact]
    public void SwipeList_RendersDividers_WhenEnabled()
    {
        var items = new[] { "First", "Second", "Third" };
        var cut = _ctx.Render<SwipeList<string>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.ShowDividers, true)
            .Add(p => p.DividerInset, 16)
            .Add(p => p.ItemTemplate, item => builder => builder.AddContent(0, item)));

        var dividers = cut.FindAll(".ioswipe-list__divider");
        Assert.Equal(2, dividers.Count); // n-1 dividers between n items
        Assert.Contains("--ioswipe-divider-inset:16px", dividers[0].GetAttribute("style"));
    }

    [Fact]
    public void SwipeView_RendersWithAriaRegionAndRoleDescription()
    {
        var cut = _ctx.Render(builder =>
        {
            builder.OpenComponent<SwipeView>(0);
            builder.AddAttribute(1, nameof(SwipeView.Class), "custom-swipe-row");
            builder.AddAttribute(2, nameof(SwipeView.Style), "max-width: 400px;");
            builder.AddAttribute(3, nameof(SwipeView.LeadingActions), (RenderFragment<SwipeContext>)(_ => actionBuilder =>
            {
                actionBuilder.OpenComponent<SwipeAction>(0);
                actionBuilder.AddAttribute(1, nameof(SwipeAction.Background), "#34c759");
                actionBuilder.AddAttribute(2, nameof(SwipeAction.ChildContent), (RenderFragment)(b => b.AddContent(0, "Done")));
                actionBuilder.CloseComponent();
            }));
            builder.AddAttribute(4, nameof(SwipeView.TrailingActions), (RenderFragment<SwipeContext>)(_ => actionBuilder =>
            {
                actionBuilder.OpenComponent<SwipeAction>(0);
                actionBuilder.AddAttribute(1, nameof(SwipeAction.Background), "#ff3b30");
                actionBuilder.AddAttribute(2, nameof(SwipeAction.ChildContent), (RenderFragment)(b => b.AddContent(0, "Delete")));
                actionBuilder.CloseComponent();
            }));
            builder.AddAttribute(5, nameof(SwipeView.ChildContent), (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"row-content\">Swipe Me</div>")));
            builder.CloseComponent();
        });

        var root = cut.Find(".ioswipe");
        Assert.NotNull(root);
        Assert.Equal("region", root.GetAttribute("role"));
        Assert.Equal("swipe row", root.GetAttribute("aria-roledescription"));
        Assert.Contains("custom-swipe-row", root.ClassName);
        Assert.Contains("max-width: 400px;", root.GetAttribute("style"));

        var leadingActions = cut.Find(".ioswipe__actions--leading");
        var trailingActions = cut.Find(".ioswipe__actions--trailing");
        Assert.NotNull(leadingActions);
        Assert.NotNull(trailingActions);
        Assert.Contains("Done", leadingActions.TextContent);
        Assert.Contains("Delete", trailingActions.TextContent);

        var content = cut.Find(".row-content");
        Assert.NotNull(content);
        Assert.Equal("Swipe Me", content.TextContent);
    }
}
