using CommandLine;
using IisLogParserArcGIS.Cli;

var runEnvironment = new RunEnvironment(
    AppContext.BaseDirectory,
    Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production",
    Console.Error,
    TimeProvider.System);

using var parser = new Parser(settings => settings.HelpWriter = Console.Error);
var parserResult = parser.ParseArguments<HarvestArguments, RegenerateArguments, HarvestRegenerateArguments>(args);

return parserResult.MapResult(
    (HarvestArguments arguments) => new HarvestRunner(runEnvironment).Harvest(arguments),
    (RegenerateArguments arguments) => new RegenerateRunner(runEnvironment).Run(arguments),
    (HarvestRegenerateArguments arguments) => new HarvestRunner(runEnvironment).HarvestAndRegenerate(arguments),
    _ => 1);
