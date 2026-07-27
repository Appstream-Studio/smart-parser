using Azure.AI.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace AppStream.SmartParser;

public static class SmartParserModule
{
    public static IServiceCollection AddSmartParser<TConfigurationDependency>(
        this IServiceCollection services, 
        Action<SmartParserOptions, TConfigurationDependency> configureOptions,
        Action<ChatCompletionOptions>? chatCompletionOptionsConfiguration = null)
        where TConfigurationDependency : class
    {
        return AddSmartParserInternal(
            services,
            optionsBuilder => optionsBuilder.Configure(configureOptions),
            chatCompletionOptionsConfiguration);
    }

    public static IServiceCollection AddSmartParser(
        this IServiceCollection services, 
        Action<SmartParserOptions> configureOptions,
        Action<ChatCompletionOptions>? chatCompletionOptionsConfiguration = null)
    {
        return AddSmartParserInternal(
            services,
            optionsBuilder => optionsBuilder.Configure(configureOptions),
            chatCompletionOptionsConfiguration);
    }

    private static IServiceCollection AddSmartParserInternal(
        IServiceCollection services, 
        Func<OptionsBuilder<SmartParserOptions>, OptionsBuilder<SmartParserOptions>> configureOptions,
        Action<ChatCompletionOptions>? chatCompletionOptionsConfiguration)
    {
        chatCompletionOptionsConfiguration ??= options => options.Temperature = 0;

        return services.AddSingleton(sp =>
        {
            var moduleServices = new ServiceCollection();
            configureOptions(moduleServices.AddOptions<SmartParserOptions>())
                .ValidateDataAnnotations()
                .ValidateOnStart();

            moduleServices
                .AddScoped<IJsonSchemaGenerator, JsonSchemaGenerator>()
                .AddScoped<ISmartParser, SmartParser>()
                .AddSingleton<Action<ChatCompletionOptions>>(chatCompletionOptionsConfiguration)
                .AddScoped<OpenAIClient>(sp =>
                {
                    var options = sp.GetRequiredService<IOptions<SmartParserOptions>>().Value;
                    var endpoint = new Uri(options.OpenAiEndpoint);
                    var clientOptions = new AzureOpenAIClientOptions
                    {
                        NetworkTimeout = TimeSpan.FromSeconds(options.HttpClientNetworkTimeoutSeconds)
                    };

                    // Credential wins when both are set (a key may legitimately still arrive from bound config
                    // during the keyless transition); validation only rejects neither-set.
                    return options.Credential is not null
                        ? new AzureOpenAIClient(endpoint, options.Credential, clientOptions)
                        : new AzureOpenAIClient(endpoint, new ApiKeyCredential(options.OpenAiCredentialKey!), clientOptions);
                });

            var moduleServiceProvider = moduleServices.BuildServiceProvider();
            return moduleServiceProvider.GetRequiredService<ISmartParser>();
        });
    }
}
