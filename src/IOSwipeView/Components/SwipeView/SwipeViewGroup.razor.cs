using Microsoft.AspNetCore.Components;

namespace IOSwipeView;

/// <summary>
/// Makes the <see cref="SwipeView"/> rows inside it behave like an accordion: opening one closes
/// the rest.
/// </summary>
/// <remarks>
/// <code>
/// &lt;SwipeViewGroup&gt;
///     @foreach (var item in Items)
///     {
///         &lt;SwipeView&gt;...&lt;/SwipeView&gt;
///     }
/// &lt;/SwipeViewGroup&gt;
/// </code>
/// Rows register themselves, so the group works with any nesting depth in between.
/// </remarks>
public partial class SwipeViewGroup : ComponentBase
{
    private readonly List<SwipeView> _members = [];

    /// <summary>The rows to coordinate.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    internal void Register(SwipeView member) => _members.Add(member);

    internal void Unregister(SwipeView member) => _members.Remove(member);

    /// <summary>
    /// Closes every row except the one that just opened (or closes all rows if null).
    /// </summary>
    internal async Task CloseOthersAsync(SwipeView? opener)
    {
        // Snapshot first: closing a row can unregister it, which would mutate the list mid-loop.
        foreach (var member in _members.ToArray())
        {
            if (opener is null || !ReferenceEquals(member, opener))
            {
                await member.CloseAsync();
            }
        }
    }
}
