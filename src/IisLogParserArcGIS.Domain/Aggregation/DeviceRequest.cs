using IisLogParserArcGIS.Domain.Parsing;

namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// One request of a by-device app, paired with the device id read from its user-agent.
/// </summary>
/// <param name="Request">The request.</param>
/// <param name="DeviceId">The casefolded device id.</param>
internal readonly record struct DeviceRequest(NormalizedLogRequest Request, string DeviceId);
