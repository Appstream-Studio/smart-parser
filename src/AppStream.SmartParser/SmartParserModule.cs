using Azure.AI.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;
using System.ClientModel;
using System.ClientModel.Primitives;

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
                .AddMemoryCache()
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
                            RetryPolicy = new ClientRetryPolicy(3)
                        });
                });

            var moduleServiceProvider = moduleServices.BuildServiceProvider();
            return moduleServiceProvider.GetRequiredService<ISmartParser>();
        });
    }
}
