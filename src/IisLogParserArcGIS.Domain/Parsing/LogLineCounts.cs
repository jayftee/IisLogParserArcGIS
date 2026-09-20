namespace IisLogParserArcGIS.Domain.Parsing;

/// <summary>
/// The data-line counts produced by parsing one physical log file (or an aggregate across several).
/// </summary>
/// <param name="Total">The number of data lines encountered (excluding comment and header lines).</param>
/// <param name="Valid">The number of data lines successfully parsed.</param>
/// <param name="Invalid">The number of data lines skipped individually (e.g. a field-count mismatch).</param>
public sealed record LogLineCounts(int Total, int Valid, int Invalid)
{
    /// <summary>
    /// Gets a zero-valued instance, for a file that was skipped before any line was parsed.
    /// </summary>
    public static LogLineCounts Zero { get; } = new(0, 0, 0);

    /// <summary>
    /// Adds two sets of counts together.
    /// </summary>
    /// <param name="left">The first set of counts.</param>
    /// <param name="right">The second set of counts.</param>
    /// <returns>The combined counts.</returns>
    public static LogLineCounts operator +(LogLineCounts left, LogLineCounts right) => Add(left, right);

    /// <summary>
    /// Adds two sets of counts together. A friendlier alternate for languages without operator overloading.
    /// </summary>
    /// <param name="left">The first set of counts.</param>
    /// <param name="right">The second set of counts.</param>
    /// <returns>The combined counts.</returns>
    public static LogLineCounts Add(LogLineCounts left, LogLineCounts right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        return new LogLineCounts(left.Total + right.Total, left.Valid + right.Valid, left.Invalid + right.Invalid);
    }
}
