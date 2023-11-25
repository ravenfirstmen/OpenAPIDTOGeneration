using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Microsoft.Extensions.Logging;

namespace OpenAPIDTOGeneration;

public abstract class AbstractGeneratorCommand : ICommand
{
    [CommandParameter(1, Name = "output-file-name",
        Description = "Output file file path where the generated code is written.")]
    public required string OutputFileName { get; init; }

    [CommandParameter(3, Name = "namespace", Description = "namespace of the generated code")]
    public required string Namespace { get; init; }


    public abstract ValueTask ExecuteAsync(IConsole console);
}

[Command("from-file", Description = "Generate code from a file JSON OPENAPI document.")]
public class FileGeneratorCommand : AbstractGeneratorCommand
{
    private readonly ICodeGenerator _generator;
    private readonly ILogger<FileGeneratorCommand> _logger;

    public FileGeneratorCommand(ILogger<FileGeneratorCommand> logger, ICodeGenerator generator)
    {
        _logger = logger;
        _generator = generator;
    }

    [CommandParameter(0, Name = "file-name", Description = "File path where the OpenAPI document is.")]
    public required string FileName { get; init; }

    public override async ValueTask ExecuteAsync(IConsole console)
    {
        _logger.LogDebug(
            "The current values for file generator are: file-name={Filename}, output-file-name={OutputFileName} and namespace={Namespace}",
            FileName, OutputFileName, Namespace);
        _logger.LogInformation("Checking if the input file name '{Filename}' exists", FileName);
        if (!File.Exists(FileName))
        {
            _logger.LogError("The input file name '{Filename}' does exists. Existing generation.", FileName);
            await console.Output.WriteLineAsync($"{FileName} does not exists!. Check if the path is correct.");
            return;
        }

        try
        {
            _logger.LogInformation("Reading content from '{Filename}'.", FileName);
            var content = await File.ReadAllTextAsync(FileName);
            _logger.LogInformation("Content successful read from '{Filename}'.", FileName);
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogError("The content from file '{FileName}' is empty. Existing generation.", FileName);
                await console.Output.WriteLineAsync(
                    $"The content from url '{FileName}' is empty. Existing generation.");
                return;
            }

            await _generator.GenerateCode(content, Namespace, OutputFileName);
        }
        catch (Exception exp)
        {
            _logger.LogError("Error reading from file '{Filename}'. {Message}.", FileName, exp.Message);
        }
    }
}

[Command("from-uri", Description = "Generate code from a uri JSON OPENAPI document.")]
public class UriGeneratorCommand : AbstractGeneratorCommand
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly ICodeGenerator _generator;
    private readonly ILogger<UriGeneratorCommand> _logger;

    public UriGeneratorCommand(
        ILogger<UriGeneratorCommand> logger,
        IHttpClientFactory clientFactory,
        ICodeGenerator generator)
    {
        _logger = logger;
        _clientFactory = clientFactory;
        _generator = generator;
    }

    [CommandParameter(0, Name = "url", Description = "Url where the OpenAPI document is.")]
    public required string Url { get; init; }

    public override async ValueTask ExecuteAsync(IConsole console)
    {
        _logger.LogDebug(
            "The current values for url generator are: url={Url}, output-file-name={OutputFileName} and namespace={Namespace}",
            Url, OutputFileName, Namespace);
        try
        {
            var client = _clientFactory.CreateClient();
            client.BaseAddress = new Uri(Url);

            _logger.LogInformation("Reading content from '{Url}'.", Url);
            var json = await client.GetStringAsync(Url);
            _logger.LogInformation("Content successful read from '{Url}'.", Url);
            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.LogError("The content from url '{Url}' is empty. Existing generation.", Url);
                await console.Output.WriteLineAsync($"The content from url '{Url}' is empty. Existing generation.");
                return;
            }

            _logger.LogInformation("Content successful read from '{Url}'.", Url);

            await _generator.GenerateCode(json, Namespace, OutputFileName);
        }
        catch (Exception exp)
        {
            _logger.LogError("Error reading from url '{Url}'. {Message}.", Url, exp.Message);
        }
    }
}