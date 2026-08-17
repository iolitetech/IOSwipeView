using Microsoft.AspNetCore.Components;

namespace IOSwipeView;

/// <summary>
/// A single action inside a <see cref="SwipeView"/>'s leading or trailing content.
/// </summary>
/// <remarks>
/// <code>
/// &lt;TrailingActions&gt;
///     &lt;SwipeAction Background="#3b82f6" OnInvoked="Archive"&gt;Archive&lt;/SwipeAction&gt;
///     &lt;SwipeAction Background="#ef4444" AllowSwipeToTrigger OnInvoked="Delete"&gt;Delete&lt;/SwipeAction&gt;
/// &lt;/TrailingActions&gt;
/// </code>
/// </remarks>
public partial class SwipeAction : ComponentBase, IDisposable
{
    private bool _registered;

    /// <summary>Set by the parent <see cref="SwipeView"/>. Identifies which side this action is on.</summary>
    [CascadingParameter]
    internal SwipeSlot? Slot { get; set; }

    /// <summary>The action's content, usually a label, an icon, or both.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Any CSS colour for the action's background.</summary>
    [Parameter]
    public string? Background { get; set; }

    /// <summary>Any CSS colour for the action's text and icons.</summary>
    [Parameter]
    public string? Foreground { get; set; }

    /// <summary>
    /// Whether dragging far enough past this action commits it without a tap.
    /// </summary>
    [Parameter]
    public bool AllowSwipeToTrigger { get; set; }

    /// <summary>
    /// Explicit trigger stage order for multi-stage progressive pulls (1 for medium pull, 2 for deep pull, etc.).
    /// Setting this automatically enables drag-to-trigger for this action.
    /// </summary>
    [Parameter]
    public int? TriggerStage { get; set; }

    /// <summary>Whether this action is configured for swipe-to-trigger.</summary>
    public bool IsTrigger => TriggerStage.HasValue || AllowSwipeToTrigger;

    /// <summary>
    /// Constrain the action's label content size so it doesn't wrap awkwardly while dragging.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    [Parameter]
    public bool LabelFixedSize { get; set; } = true;

    /// <summary>
    /// Whether to ramp the opacity of the label only rather than the whole action button.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    [Parameter]
    public bool ChangeLabelVisibilityOnly { get; set; }

    /// <summary>
    /// Custom horizontal padding for the action button in pixels.
    /// </summary>
    [Parameter]
    public double? HorizontalPadding { get; set; }

    /// <summary>
    /// An optional icon rendered alongside or in place of the label.
    /// </summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>
    /// The layout placement of the icon relative to the label.
    /// Defaults to <see cref="ActionPlacement.Top"/> (vertical stack).
    /// </summary>
    [Parameter]
    public ActionPlacement Placement { get; set; } = ActionPlacement.Top;

    /// <summary>
    /// An accessible label for screen readers. Recommended when in <see cref="ActionPlacement.IconOnly"/> mode.
    /// </summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>Additional CSS classes for the action's button.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Additional inline styles for the action's button.</summary>
    [Parameter]
    public string? Style { get; set; }

    /// <summary>
    /// Whether invoking this action automatically closes the row. If not set, uses
    /// <see cref="SwipeOptions.AutoCloseOnActionInvoked"/>.
    /// </summary>
    [Parameter]
    public bool? AutoClose { get; set; }

    /// <summary>Runs when the action is tapped, or triggered by a drag.</summary>
    [Parameter]
    public EventCallback OnInvoked { get; set; }

    /// <summary>Any other attributes are applied to the action's button element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private string CssClass
    {
        get
        {
            var baseClass = ChangeLabelVisibilityOnly
                ? "ioswipe__action ioswipe__action--label-fade-only"
                : "ioswipe__action";

            var placementClass = Placement switch
            {
                ActionPlacement.Top => "ioswipe__action--top",
                ActionPlacement.InlineStart => "ioswipe__action--inline-start",
                ActionPlacement.InlineEnd => "ioswipe__action--inline-end",
                ActionPlacement.IconOnly => "ioswipe__action--icon-only",
                _ => "ioswipe__action--top"
            };

            var combined = $"{baseClass} {placementClass}";
            return string.IsNullOrWhiteSpace(Class) ? combined : $"{combined} {Class}";
        }
    }

    private string CssStyle
    {
        get
        {
            var background = string.IsNullOrWhiteSpace(Background) ? null : $"--ioswipe-action-background:{Background};";
            var foreground = string.IsNullOrWhiteSpace(Foreground) ? null : $"--ioswipe-action-foreground:{Foreground};";
            var padding = HorizontalPadding.HasValue
                ? $"--ioswipe-action-padding:{HorizontalPadding.Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}px;"
                : null;
            return string.Concat(background, foreground, padding, Style);
        }
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Slot is null)
        {
            throw new InvalidOperationException(
                $"{nameof(SwipeAction)} must be placed inside a {nameof(SwipeView)}'s " +
                $"LeadingActions or TrailingActions content.");
        }

        Slot.Owner.RegisterAction(Slot.Side, this);
        _registered = true;
    }

    /// <inheritdoc />
    protected override void OnParametersSet() => Slot?.Owner.NotifyActionsChanged();

    private bool _disposed;

    internal async Task InvokeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (OnInvoked.HasDelegate)
        {
            await OnInvoked.InvokeAsync();
        }

        if (_disposed)
        {
            return;
        }

        var shouldAutoClose = AutoClose ?? Slot?.Owner.Options.AutoCloseOnActionInvoked ?? true;
        if (shouldAutoClose && Slot is not null && !Slot.Owner.IsDisposed)
        {
            if (Slot.Owner.State == SwipeState.Expanded)
            {
                await Slot.Owner.CloseAsync();
            }
            else if (Slot.Owner.State == SwipeState.Triggered)
            {
                // If the item was not unmounted/deleted by the callback, smoothly return to closed position
                await Task.Delay(350);
                if (_registered && !_disposed && Slot is not null && !Slot.Owner.IsDisposed && Slot.Owner.State == SwipeState.Triggered)
                {
                    await Slot.Owner.CloseAsync();
                }
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        GC.SuppressFinalize(this);

        if (_registered && Slot is not null)
        {
            Slot.Owner.UnregisterAction(Slot.Side, this);
            _registered = false;
        }
    }
}

/// <summary>
/// Cascaded by <see cref="SwipeView"/> so each <see cref="SwipeAction"/> knows which side it is on.
/// </summary>
internal sealed class SwipeSlot(SwipeView owner, SwipeSide side)
{
    public SwipeView Owner { get; } = owner;

    public SwipeSide Side { get; } = side;
}
