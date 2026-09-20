namespace IisLogParserArcGIS.Reports.Rendering;

/// <summary>
/// One entry in the Dashboard's persistent sidebar: either a leaf page (<see cref="Href"/> set, no children) or
/// an expandable group (no <see cref="Href"/> of its own, one or more <see cref="Children"/>).
/// </summary>
public sealed record SidebarNode
{
    /// <summary>
    /// Gets the entry's display text.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets this entry's page path, relative to the Dashboard's output root (e.g.
    /// <c>summary/successful-requests/all.html</c>), or <see langword="null"/> for a group with no page of its
    /// own.
    /// </summary>
    public string? Href { get; init; }

    /// <summary>
    /// Gets this entry's nested entries, or an empty list for a leaf. A non-empty list always renders as a
    /// native <c>&lt;details&gt;</c> disclosure widget - collapsed by default, auto-expanded only along the
    /// path to the current page - rather than an always-expanded nested list, so a page with many groups (one
    /// per Included Root, on top of every section's own metric groups) doesn't force every group's contents
    /// into the sidebar at once. Static HTML only - no JavaScript is needed for the expand/collapse behavior.
    /// </summary>
    public IReadOnlyList<SidebarNode> Children { get; init; } = [];
}
