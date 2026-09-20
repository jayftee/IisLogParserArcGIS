namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// Reads a device id out of a <c>cs(User-Agent)</c> value - the one rule that differs between the by-device
/// aggregates (Field Maps and Survey123).
/// </summary>
/// <param name="userAgent">The raw <c>cs(User-Agent)</c> field value.</param>
/// <param name="deviceId">The casefolded device id when parsing succeeds; otherwise <see langword="null"/>.</param>
/// <returns><see langword="true"/> when <paramref name="userAgent"/> carries a device id.</returns>
internal delegate bool DeviceIdParser(string userAgent, out string? deviceId);
