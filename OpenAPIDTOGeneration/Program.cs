using CliFx;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAPIDTOGeneration;

// "https://raw.githubusercontent.com/hashicorp/vault-client-dotnet/main/openapi.json"

return await new CliApplicationBuilder()
    .AddCommand<FileGeneratorCommand>()
    .AddCommand<UriGeneratorCommand>()
    .UseTypeActivator(commandTypes =>
    {
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });
        services.AddHttpClient();
        services.AddSingleton<ICodeGenerator, CodeGenerator>();
        foreach (var commandType in commandTypes) services.AddTransient(commandType);

        return services.BuildServiceProvider();
    })
    .Build()
    .RunAsync();