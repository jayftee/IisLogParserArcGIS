namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// An aggregate row that is attributed to a username where one is known - the shape shared by the by-device
/// aggregates (Field Maps and Survey123), so attribution can be counted the same way for either.
/// </summary>
public interface IUsernameAttributedRow
{
    /// <summary>
    /// Gets the casefolded username the row is attributed to, or <see langword="null"/> when none is known yet.
    /// </summary>
    string? Username { get; }
}
