using IOGesture.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace IOSwipeView;

/// <summary>
/// Adds customisable swipe actions to any content.
/// </summary>
/// <remarks>
/// <para>
/// The drag maths live in <see cref="SwipeGeometry"/> as pure functions; this component owns the
/// state machine and hands the resulting offset to a small JavaScript renderer. That renderer
/// writes CSS custom properties, so nothing here re-renders while a drag is in flight.
/// </para>
/// <code>
/// &lt;SwipeView&gt;
///     &lt;TrailingActions&gt;
///         &lt;SwipeAction Background="#ef4444" OnInvoked="Delete"&gt;Delete&lt;/SwipeAction&gt;
///     &lt;/TrailingActions&gt;
///     &lt;ChildContent&gt;
///         &lt;div class="row"&gt;Swipe me&lt;/div&gt;
///     &lt;/ChildContent&gt;
/// &lt;/SwipeView&gt;
/// </code>
/// </remarks>
public partial class SwipeView : ComponentBase, IAsyncDisposable
{
    private const string ModulePath = "./_content/IOSwipeView/SwipeView.razor.js";

    private readonly List<SwipeAction> _leadingActions = [];
    private readonly List<SwipeAction> _trailingActions = [];
    private readonly SwipeVelocityTracker _velocity = new();

    private SwipeSlot _leadingSlot = default!;
    private SwipeSlot _trailingSlot = default!;
    private SwipeContext _leadingContext = default!;
    private SwipeContext _trailingContext = default!;

    private ElementReference _root;
    private Gesture? _gesture;
    private IJSObjectReference? _module;
    private DotNetObjectReference<SwipeView>? _selfRef;
    private GestureOptions _gestureOptions = new();
    private int _handle = -1;

    private double _savedOffset;
    private double _rowWidth;
    private SwipeSide? _currentSide;
    private SwipeState? _leadingState;
    private SwipeState? _trailingState;
    private SwipeSide? _armedSide;
    private int? _armedIndex;
    private bool _isDragging;
    private bool _actionsDirty;
    private bool _isRtl;
    private bool _disposed;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    /// <summary>The enclosing accordion group, if the row is inside one.</summary>
    [CascadingParameter]
    public SwipeViewGroup? Group { get; set; }

    /// <summary>The row's own content — the part that slides.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Actions revealed by dragging towards the end of the reading direction.</summary>
    [Parameter]
    public RenderFragment<SwipeContext>? LeadingActions { get; set; }

    /// <summary>Actions revealed by dragging towards the start of the reading direction.</summary>
    [Parameter]
    public RenderFragment<SwipeContext>? TrailingActions { get; set; }

    /// <summary>Tuning for this row. Defaults to <see cref="SwipeOptions.Default"/>.</summary>
    [Parameter]
    public SwipeOptions Options { get; set; } = SwipeOptions.Default;

    /// <summary>Additional CSS classes for the row's root element.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Additional inline styles for the row's root element.</summary>
    [Parameter]
    public string? Style { get; set; }

    /// <summary>Raised whenever the row settles into a new state. Never raised per frame.</summary>
    [Parameter]
    public EventCallback<SwipeStateChange> OnStateChanged { get; set; }

    /// <summary>Any other attributes are applied to the row's root element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>The row's current settled state.</summary>
    public SwipeState State { get; private set; } = SwipeState.Closed;

    /// <summary>The side currently showing its actions, or <see langword="null"/> when closed.</summary>
    public SwipeSide? OpenSide { get; private set; }

    private SwipeGeometry Geometry => new(
        Options,
        new SwipeMetrics(
            _leadingActions.Count,
            _trailingActions.Count,
            ResolveTriggerIndices(_leadingActions),
            ResolveTriggerIndices(_trailingActions),
            _rowWidth));

    private static List<int> ResolveTriggerIndices(List<SwipeAction> actions)
    {
        var triggerItems = new List<(int Index, int Stage)>();
        for (var i = 0; i < actions.Count; i++)
        {
            var action = actions[i];
            if (action.TriggerStage is { } stage)
            {
                triggerItems.Add((i, stage));
            }
            else if (action.AllowSwipeToTrigger)
            {
                triggerItems.Add((i, 1000 + i));
            }
        }

        return triggerItems
            .OrderBy(t => t.Stage)
            .Select(t => t.Index)
            .ToList();
    }

    private string RootCssClass =>
        string.IsNullOrWhiteSpace(Class) ? RootStyleClass : $"{RootStyleClass} {Class}";

    private string RootStyleClass => Options.Style switch
    {
        SwipeActionStyle.EqualWidths => "ioswipe ioswipe--equal-widths",
        SwipeActionStyle.Cascade => "ioswipe ioswipe--cascade",
        _ => "ioswipe ioswipe--mask",
    };

    private string RootCssStyle
    {
        get
        {
            var spring = Options.TriggerAnimation;

            return $"--ioswipe-action-width:{Css(Options.ActionWidth)}px;" +
                   $"--ioswipe-spacing:{Css(Options.Spacing)}px;" +
                   $"--ioswipe-action-radius:{Css(Options.ActionCornerRadius)}px;" +
                   $"--ioswipe-mask-radius:{Css(Options.ActionsMaskCornerRadius)}px;" +
                   $"--ioswipe-spring-duration:{spring.SettlingDurationMs}ms;" +
                   $"--ioswipe-spring-curve:{spring.ToCssCurve()};" +
                   Style;
        }
    }

    /// <summary>
    /// Handles keyboard interaction for WCAG AA accessible row navigation.
    /// </summary>
    public async Task HandleKeyDownAsync(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
    {
        if (!Options.Enabled) return;

        switch (e.Key)
        {
            case "ArrowLeft":
                if (_isRtl)
                {
                    if (OpenSide == SwipeSide.Trailing) await CloseAsync();
                    else if (_leadingActions.Count > 0) await OpenAsync(SwipeSide.Leading);
                }
                else
                {
                    if (OpenSide == SwipeSide.Leading) await CloseAsync();
                    else if (_trailingActions.Count > 0) await OpenAsync(SwipeSide.Trailing);
                }
                break;

            case "ArrowRight":
                if (_isRtl)
                {
                    if (OpenSide == SwipeSide.Leading) await CloseAsync();
                    else if (_trailingActions.Count > 0) await OpenAsync(SwipeSide.Trailing);
                }
                else
                {
                    if (OpenSide == SwipeSide.Trailing) await CloseAsync();
                    else if (_leadingActions.Count > 0) await OpenAsync(SwipeSide.Leading);
                }
                break;

            case "Escape":
                if (State != SwipeState.Closed)
                {
                    await CloseAsync();
                }
                break;

            case "Delete":
            case "Backspace":
                if (_trailingActions.Count > 0)
                {
                    await TriggerAsync(SwipeSide.Trailing);
                }
                break;
        }
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _leadingSlot = new SwipeSlot(this, SwipeSide.Leading);
        _trailingSlot = new SwipeSlot(this, SwipeSide.Trailing);
        _leadingContext = new SwipeContext(this, SwipeSide.Leading);
        _trailingContext = new SwipeContext(this, SwipeSide.Trailing);

        Group?.Register(this);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _actionsDirty = true;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JS.InvokeAsync<IJSObjectReference>("import", ModulePath);
            _selfRef = DotNetObjectReference.Create(this);
            _handle = await _module.InvokeAsync<int>("create", _root, JsOptions(), _selfRef);
        }
        else if (_actionsDirty && _module is not null && _handle >= 0)
        {
            _actionsDirty = false;
            await _module.InvokeVoidAsync("setOptions", _handle, JsOptions());
        }
    }

    // Blazor must not diff the render tree while a drag is in flight; the JavaScript renderer
    // owns the visuals for the duration of the gesture.
    /// <inheritdoc />
    protected override bool ShouldRender() => !_isDragging;

    // ---- Gesture handling ---------------------------------------------------------------

    private Task HandlePanStartAsync()
    {
        _isDragging = true;
        _currentSide = State == SwipeState.Expanded ? OpenSide : null;
        _velocity.Reset();

        _leadingContext.IsDragging = true;
        _trailingContext.IsDragging = true;

        return Group is null ? Task.CompletedTask : Group.CloseOthersAsync(this);
    }

    private async Task HandlePanMoveAsync()
    {
        if (!Options.Enabled || _module is null || _handle < 0)
        {
            return;
        }

        var translation = Translation;
        _velocity.Add(translation);

        var totalOffset = _savedOffset + translation;

        // When closed, commit to the side based on net offset direction
        if (_currentSide is null && Math.Abs(totalOffset) > 0.5)
        {
            _currentSide = totalOffset > 0 ? SwipeSide.Leading : SwipeSide.Trailing;
        }

        var outcome = Geometry.Drag(_savedOffset, translation, _currentSide);

        await ApplyArmedSideAsync(outcome);
        await _module!.InvokeVoidAsync("setOffset", _handle, outcome.Offset);
    }

    private async Task HandlePanEndAsync()
    {
        _isDragging = false;
        _leadingContext.IsDragging = false;
        _trailingContext.IsDragging = false;

        if (!Options.Enabled || _module is null || _handle < 0)
        {
            return;
        }

        var outcome = Geometry.Release(
            _savedOffset, Translation, _velocity.Velocity, _leadingState, _trailingState, _currentSide, _armedIndex);

        _velocity.Reset();
        await SettleAsync(outcome);
    }

    private double Translation
    {
        get
        {
            var raw = _gesture?.Properties.TouchMoveX ?? 0;
            return _isRtl ? -raw : raw;
        }
    }

    /// <summary>
    /// Fires haptics and flips the triggering class exactly on the arm/disarm transition.
    /// </summary>
    private async Task ApplyArmedSideAsync(SwipeDragOutcome outcome)
    {
        _leadingState = outcome.LeadingState;
        _trailingState = outcome.TrailingState;

        var armed = outcome.LeadingState == SwipeState.Triggering ? SwipeSide.Leading
            : outcome.TrailingState == SwipeState.Triggering ? SwipeSide.Trailing
            : (SwipeSide?)null;

        var armedIndex = outcome.ArmedActionIndex;

        if (armed == _armedSide && armedIndex == _armedIndex)
        {
            return;
        }

        var isDeep = false;
        if (armed is { } s && armedIndex is { } idx)
        {
            var triggers = Geometry.Metrics.TriggerIndices(s);
            if (triggers.Count >= 2 && idx == triggers[^1])
            {
                isDeep = true;
            }
        }

        _armedSide = armed;
        _armedIndex = armedIndex;

        var pattern = isDeep ? Options.DeepHapticPattern : Options.HapticPattern;

        await _module!.InvokeVoidAsync(
            "setArmed",
            _handle,
            armed?.ToString().ToLowerInvariant(),
            Options.EnableTriggerHaptics,
            pattern,
            armedIndex);
    }

    // ---- Settling -----------------------------------------------------------------------

    internal bool IsDisposed => _disposed;

    private async Task SettleAsync(SwipeReleaseOutcome outcome)
    {
        if (_disposed)
        {
            return;
        }

        _savedOffset = outcome.TargetOffset;
        _currentSide = outcome.State == SwipeState.Closed ? null : outcome.Side;
        _armedSide = outcome.State == SwipeState.Triggered ? outcome.Side : null;
        _armedIndex = outcome.State == SwipeState.Triggered ? outcome.TriggeredActionIndex : null;

        _leadingState = outcome.Side == SwipeSide.Leading ? outcome.State : SwipeState.Closed;
        _trailingState = outcome.Side == SwipeSide.Trailing ? outcome.State : SwipeState.Closed;

        State = outcome.State;
        OpenSide = outcome.State == SwipeState.Closed ? null : outcome.Side;
        _leadingContext.State = _leadingState.Value;
        _trailingContext.State = _trailingState.Value;

        if (_module is not null && _handle >= 0 && !_disposed)
        {
            try
            {
                var armedSide = _armedSide?.ToString().ToLowerInvariant();
                await _module.InvokeVoidAsync("setArmed", _handle, armedSide, false, null, _armedIndex);

                await _module.InvokeVoidAsync(
                    "settle",
                    _handle,
                    outcome.TargetOffset,
                    outcome.Spring.Stiffness,
                    outcome.Spring.Damping,
                    _velocity.Velocity);
            }
            catch (Exception ex) when (ex is ObjectDisposedException or JSDisconnectedException)
            {
                // Ignored when component or JS runtime is disposed during animation
            }
        }

        if (_disposed)
        {
            return;
        }

        if (outcome.IsTriggered && outcome.Side is { } side)
        {
            await InvokeTriggeredActionAsync(side, outcome.TriggeredActionIndex);
        }

        if (_disposed)
        {
            return;
        }

        if (OnStateChanged.HasDelegate)
        {
            await OnStateChanged.InvokeAsync(new SwipeStateChange(outcome.State, outcome.Side));
        }

        if (!_disposed)
        {
            StateHasChanged();
        }
    }

    private Task InvokeTriggeredActionAsync(SwipeSide side, int? actionIndex)
    {
        var actions = side == SwipeSide.Leading ? _leadingActions : _trailingActions;

        if (actions.Count == 0)
        {
            return Task.CompletedTask;
        }

        if (actionIndex is { } idx && idx >= 0 && idx < actions.Count)
        {
            return actions[idx].InvokeAsync();
        }

        return (side == SwipeSide.Leading ? actions[0] : actions[^1]).InvokeAsync();
    }

    // ---- Programmatic control -----------------------------------------------------------

    /// <summary>Reveals a side's actions.</summary>
    /// <param name="side">The side to open.</param>
    /// <returns>A task that completes once the row has been told to settle.</returns>
    public Task OpenAsync(SwipeSide side) =>
        _disposed ? Task.CompletedTask : SettleAsync(Geometry.MoveTo(side, SwipeState.Expanded));

    /// <summary>Closes the row.</summary>
    /// <returns>A task that completes once the row has been told to settle.</returns>
    public Task CloseAsync() =>
        _disposed || State == SwipeState.Closed
            ? Task.CompletedTask
            : SettleAsync(Geometry.MoveTo(SwipeSide.Leading, SwipeState.Closed));

    /// <summary>Toggles a side between opened and closed.</summary>
    /// <param name="side">The side to toggle.</param>
    /// <returns>A task that completes once the row has been told to settle.</returns>
    public Task ToggleAsync(SwipeSide side) =>
        _disposed
            ? Task.CompletedTask
            : (State == SwipeState.Expanded && OpenSide == side
                ? CloseAsync()
                : OpenAsync(side));

    /// <summary>Triggers a side's edge action, sliding the row clear.</summary>
    /// <param name="side">The side whose edge action should fire.</param>
    /// <returns>A task that completes once the row has been told to settle.</returns>
    public Task TriggerAsync(SwipeSide side) =>
        _disposed ? Task.CompletedTask : SettleAsync(Geometry.MoveTo(side, SwipeState.Triggered));

    // ---- Wiring from children and JavaScript ---------------------------------------------

    /// <summary>Called by the JavaScript renderer whenever the row is resized or changes direction.</summary>
    /// <param name="width">The row's new width in CSS pixels.</param>
    /// <param name="isRtl">Whether the row is laid out in Right-to-Left (RTL) reading mode.</param>
    [JSInvokable]
    public void OnRowResized(double width, bool isRtl = false)
    {
        _rowWidth = width;
        _isRtl = isRtl;
    }

    internal void RegisterAction(SwipeSide side, SwipeAction action)
    {
        (side == SwipeSide.Leading ? _leadingActions : _trailingActions).Add(action);
        NotifyActionsChanged();
    }

    internal void UnregisterAction(SwipeSide side, SwipeAction action)
    {
        (side == SwipeSide.Leading ? _leadingActions : _trailingActions).Remove(action);
        NotifyActionsChanged();
    }

    internal void NotifyActionsChanged()
    {
        _leadingContext.ActionCount = _leadingActions.Count;
        _trailingContext.ActionCount = _trailingActions.Count;
        _actionsDirty = true;
    }

    private object JsOptions() => new
    {
        spacing = Options.Spacing,
        actionWidth = Options.ActionWidth,
        actionsVisibleStartPoint = Options.ActionsVisibleStartPoint,
        actionsVisibleEndPoint = Options.ActionsVisibleEndPoint,
        leadingCount = _leadingActions.Count,
        trailingCount = _trailingActions.Count,
    };

    private static string Css(double value) => value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Group?.Unregister(this);

        try
        {
            if (_module is not null && _handle >= 0)
            {
                await _module.InvokeVoidAsync("dispose", _handle);
            }

            if (_module is not null)
            {
                await _module.DisposeAsync();
            }
        }
        catch (Exception ex) when (ex is JSDisconnectedException or ObjectDisposedException)
        {
            // The circuit or JS runtime is already gone; the browser-side state went with it.
        }

        _selfRef?.Dispose();
    }
}
