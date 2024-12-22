using Azure.AI.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;
using System.ClientModel;

namespace AppStream.SmartParser;

public static class SmartParserModule
{
    public static IServiceCollection AddSmartParser(this IServiceCollection services, Action<SmartParserOptions> configureOptions)
    {
        return services.AddSingleton(sp =>
        {
            var moduleServices = new ServiceCollection();
            moduleServices
                .AddOptions<SmartParserOptions>()
                .Configure(configureOptions)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            moduleServices
                .AddScoped<IJsonSchemaGenerator, JsonSchemaGenerator>()
                .AddScoped<ISmartParser, SmartParser>()
                .AddScoped<OpenAIClient>(sp =>
                {
                    var options = sp.GetRequiredService<IOptions<SmartParserOptions>>().Value;
                    return new AzureOpenAIClient(
                        new Uri(options.OpenAiEndpoint),
                        new ApiKeyCredential(options.OpenAiCredentialKey),
                        new AzureOpenAIClientOptions
                        {
                            NetworkTimeout = TimeSpan.FromSeconds(options.HttpClientNetworkTimeoutSeconds)
                        });
                });

            var moduleServiceProvider = moduleServices.BuildServiceProvider();
            return moduleServiceProvider.GetRequiredService<ISmartParser>();
        });
    }
}
