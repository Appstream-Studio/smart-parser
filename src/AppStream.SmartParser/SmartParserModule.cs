using Azure.AI.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;
using System.ClientModel;

namespace AppStream.SmartParser;

public static class SmartParserModule
{
    public static IServiceCollection AddSmartParser<TDep>(this IServiceCollection services, Action<SmartParserOptions, TDep> configureOptions)
        where TDep : class
    {
        return AddSmartParserInternal(
            services,
            optionsBuilder => optionsBuilder.Configure(configureOptions));
    }

    public static IServiceCollection AddSmartParser(this IServiceCollection services, Action<SmartParserOptions> configureOptions)
    {
        return AddSmartParserInternal(
            services,
            optionsBuilder => optionsBuilder.Configure(configureOptions));
    }

    public static IServiceCollection AddSmartParser(this IServiceCollection services, SmartParserOptions options)
    {
        return AddSmartParser(
            services,
            opts =>
            {
                opts.DeploymentName = options.DeploymentName;
                opts.HttpClientNetworkTimeoutSeconds = options.HttpClientNetworkTimeoutSeconds;
                opts.OpenAiCredentialKey = options.OpenAiCredentialKey;
                opts.OpenAiEndpoint = options.OpenAiEndpoint;
            });
    }

    private static IServiceCollection AddSmartParserInternal(
        IServiceCollection services, 
        Func<OptionsBuilder<SmartParserOptions>, OptionsBuilder<SmartParserOptions>> configureOptions)
    {
        return services.AddSingleton(sp =>
        {
            var moduleServices = new ServiceCollection();
            configureOptions(moduleServices.AddOptions<SmartParserOptions>())
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
