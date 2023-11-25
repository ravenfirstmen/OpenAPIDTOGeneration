using Microsoft.Extensions.Logging;
using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;
using NSwag;
using NSwag.CodeGeneration;
using NSwag.CodeGeneration.CSharp;

namespace OpenAPIDTOGeneration;

public interface ICodeGenerator
{
    Task GenerateCode(string json, string @namespace, string outputFile);
}

public class CamelCaseWithNonUnderscorePropertyNames : CSharpPropertyNameGenerator
{
    public override string Generate(JsonSchemaProperty property)
    {
        var generatedPropertyName = base.Generate(property);

        var split = generatedPropertyName.Split('_');

        return string.Join("", split.Select(s => ConversionUtilities.ConvertToUpperCamelCase(s, false)));
    }
}

public class CodeGenerator : ICodeGenerator
{
    private static readonly CSharpClientGeneratorSettings Settings = new()
    {
        GenerateClientClasses = false,
        GenerateClientInterfaces = false,
        GenerateDtoTypes = true,
        GenerateUpdateJsonSerializerSettingsMethod = false,
        WrapDtoExceptions = false,
        CSharpGeneratorSettings =
        {
            ClassStyle = CSharpClassStyle.Record,
            SchemaType = SchemaType.OpenApi3,
            JsonLibrary = CSharpJsonLibrary.SystemTextJson,
            //GenerateImmutableDictionaryProperties = true,
            GenerateOptionalPropertiesAsNullable = true,
            GenerateNullableReferenceTypes = true,
            GenerateNativeRecords = true,
            GenerateDataAnnotations = false,
            PropertyNameGenerator = new CamelCaseWithNonUnderscorePropertyNames()
        }
    };

    private readonly ILogger<CodeGenerator> _logger;

    public CodeGenerator(ILogger<CodeGenerator> logger)
    {
        _logger = logger;
    }

    public async Task GenerateCode(string json, string @namespace, string outputFile)
    {
        _logger.LogDebug("Generation code in namespace '{namespace}'", @namespace);
        Settings.CSharpGeneratorSettings.Namespace = @namespace;

        try
        {
            _logger.LogDebug("Generation code in namespace '{namespace}'", @namespace);
            _logger.LogDebug("Parsing JSON document");
            var document = await OpenApiDocument.FromJsonAsync(json);
            _logger.LogInformation("Generating code.");
            var generator = new CSharpClientGenerator(document, Settings);
            _logger.LogDebug("Finish generating code.");
            var code = generator.GenerateFile(ClientGeneratorOutputType.Contracts);
            await File.WriteAllTextAsync(outputFile, code);
            _logger.LogInformation("Finish generating code. Code written in file '{File}'.", outputFile);
        }
        catch (Exception exp)
        {
            _logger.LogError("Error generating code. {Message}.", exp.Message);
        }
    }
}