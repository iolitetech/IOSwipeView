using System.Globalization;

namespace IOSwipeView.Demo.Components;

public sealed class PlaygroundState
{
    public SwipeActionStyle Style { get; set; } = SwipeActionStyle.Mask;
    public int LeadingCount { get; set; } = 1;
    public int TrailingCount { get; set; } = 3;
    public double ActionWidth { get; set; } = 80;
    public double Spacing { get; set; }
    public double ActionCornerRadius { get; set; }
    public double MaskCornerRadius { get; set; }
    public double FadeStart { get; set; }
    public double FadeEnd { get; set; }
    public double ReadyToExpandPadding { get; set; } = 50;
    public double ReadyToTriggerPadding { get; set; } = 20;
    public double MinimumPointToTrigger { get; set; } = 200;
    public double RubberBanding { get; set; } = 0.7;
    public double Stiffness { get; set; } = 300;
    public double Damping { get; set; } = 32;
    public bool AllowSwipeToTrigger { get; set; } = true;
    public bool AllowSingleSwipeAcross { get; set; }
    public bool ChangeLabelVisibilityOnly { get; set; }
    public bool EnableTriggerHaptics { get; set; } = true;

    public string PresetId { get; private set; } = "classic";
    public string SpringId { get; private set; } = "default";

    public SwipeOptions Options => new()
    {
        Style = Style,
        ActionWidth = ActionWidth,
        Spacing = Spacing,
        ActionCornerRadius = ActionCornerRadius,
        ActionsMaskCornerRadius = MaskCornerRadius,
        ActionsVisibleStartPoint = FadeStart,
        ActionsVisibleEndPoint = FadeEnd,
        ReadyToExpandPadding = ReadyToExpandPadding,
        ReadyToTriggerPadding = ReadyToTriggerPadding,
        MinimumPointToTrigger = MinimumPointToTrigger,
        StretchRubberBandingPower = RubberBanding,
        AllowSingleSwipeAcross = AllowSingleSwipeAcross,
        EnableTriggerHaptics = EnableTriggerHaptics,
        CloseAnimation = new SwipeSpring(Stiffness, Damping),
        ExpandAnimation = new SwipeSpring(Stiffness, Damping),
        TriggerAnimation = new SwipeSpring(Stiffness, Damping),
    };

    public static IReadOnlyList<(string Id, string Label, string Blurb)> Presets =>
    [
        ("classic", "Classic List", "Flush square actions, the Mail and Messages look"),
        ("grouped", "Inset Grouped", "Rounded section mask with flush interior actions"),
        ("notification", "Notification", "Rounded pills with a little breathing room"),
        ("capsule", "Capsule", "Floating pill bubbles, the SwipeActions default"),
    ];

    public static IReadOnlyList<(string Id, string Label, double Stiffness, double Damping)> Springs =>
    [
        ("snappy", "Snappy", 420, 40),
        ("default", "Default", 300, 32),
        ("smooth", "Smooth", 240, 28),
        ("bouncy", "Bouncy", 220, 18),
        ("gentle", "Gentle", 160, 22),
        ("wobbly", "Wobbly", 180, 12),
    ];

    public void ApplyPreset(string id)
    {
        PresetId = id;
        Style = SwipeActionStyle.Mask;

        switch (id)
        {
            case "classic":
                (ActionWidth, Spacing, ActionCornerRadius, MaskCornerRadius, FadeStart, FadeEnd) = (80d, 0d, 0d, 0d, 0d, 0d);
                break;
            case "grouped":
                (ActionWidth, Spacing, ActionCornerRadius, MaskCornerRadius, FadeStart, FadeEnd) = (80d, 0d, 0d, 12d, 0d, 0d);
                break;
            case "notification":
                (ActionWidth, Spacing, ActionCornerRadius, MaskCornerRadius, FadeStart, FadeEnd) = (76d, 6d, 16d, 16d, 10d, 40d);
                break;
            case "capsule":
                (ActionWidth, Spacing, ActionCornerRadius, MaskCornerRadius, FadeStart, FadeEnd) = (100d, 8d, 32d, 20d, 20d, 60d);
                break;
        }
    }

    public void ApplySpring(string id)
    {
        var match = Springs.FirstOrDefault(s => s.Id == id);

        if (match.Id is null)
        {
            return;
        }

        SpringId = id;
        Stiffness = match.Stiffness;
        Damping = match.Damping;
    }

    public void MarkCustomSpring() => SpringId = "custom";

    public void MarkCustomPreset() => PresetId = "custom";

    public string ToRazor()
    {
        var leading = string.Join(
            Environment.NewLine,
            Enumerable.Range(0, LeadingCount).Select(i =>
                $"""        <SwipeAction Background="{LeadingColors[i]}" Foreground="white" OnInvoked="{LeadingNames[i]}">{LeadingNames[i]}</SwipeAction>"""));

        var trailing = string.Join(
            Environment.NewLine,
            Enumerable.Range(0, TrailingCount).Select(i =>
            {
                var isEdge = i == TrailingCount - 1;
                var trigger = isEdge && AllowSwipeToTrigger ? " AllowSwipeToTrigger" : string.Empty;
                return $"""        <SwipeAction Background="{TrailingColors[i]}" Foreground="white"{trigger} OnInvoked="{TrailingNames[i]}">{TrailingNames[i]}</SwipeAction>""";
            }));

        var leadingBlock = LeadingCount == 0
            ? string.Empty
            : $"""
                   <LeadingActions>
               {leading}
                   </LeadingActions>

               """;

        return $$"""
            <SwipeView Options="Options">
            {{leadingBlock}}    <ChildContent>
                    <div class="row">Swipe me</div>
                </ChildContent>

                <TrailingActions>
            {{trailing}}
                </TrailingActions>
            </SwipeView>

            @code {
                private static readonly SwipeOptions Options = new()
                {
                    Style = SwipeActionStyle.{{Style}},
                    ActionWidth = {{N(ActionWidth)}},
                    Spacing = {{N(Spacing)}},
                    ActionCornerRadius = {{N(ActionCornerRadius)}},
                    ActionsMaskCornerRadius = {{N(MaskCornerRadius)}},
                    ActionsVisibleStartPoint = {{N(FadeStart)}},
                    ActionsVisibleEndPoint = {{N(FadeEnd)}},
                    ReadyToExpandPadding = {{N(ReadyToExpandPadding)}},
                    ReadyToTriggerPadding = {{N(ReadyToTriggerPadding)}},
                    MinimumPointToTrigger = {{N(MinimumPointToTrigger)}},
                    StretchRubberBandingPower = {{N(RubberBanding)}},
                    AllowSingleSwipeAcross = {{L(AllowSingleSwipeAcross)}},
                    EnableTriggerHaptics = {{L(EnableTriggerHaptics)}},
                    CloseAnimation = new SwipeSpring({{N(Stiffness)}}, {{N(Damping)}}),
                    ExpandAnimation = new SwipeSpring({{N(Stiffness)}}, {{N(Damping)}}),
                    TriggerAnimation = new SwipeSpring({{N(Stiffness)}}, {{N(Damping)}}),
                };
            }
            """;
    }

    internal static readonly string[] LeadingNames = ["Done", "Pin"];
    internal static readonly string[] LeadingColors = ["#34c759", "#5856d6"];
    internal static readonly string[] TrailingNames = ["More", "Flag", "Delete"];
    internal static readonly string[] TrailingColors = ["#8e8e93", "#ff9500", "#ff3b30"];

    private static string N(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string L(bool value) => value ? "true" : "false";
}
