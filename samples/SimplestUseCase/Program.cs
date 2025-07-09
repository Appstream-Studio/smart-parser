using AppStream.SmartParser;
using Microsoft.Extensions.DependencyInjection;

namespace SimplestUseCase;

internal class Program
{
    static async Task Main(string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .AddSmartParser(options =>
            {
                options.HttpClientNetworkTimeoutSeconds = 10;
                options.DeploymentName = "MyDeployment";
                options.OpenAiEndpoint = "https://your-openai-endpoint.azure.com/";
                options.OpenAiCredentialKey = "my-key";
            })
            .BuildServiceProvider();

        var parser = serviceProvider.GetRequiredService<ISmartParser>();
        await UseAsync(parser);
    }

    private static async Task UseAsync(ISmartParser parser, CancellationToken cancellationToken = default)
    {
        // Input text might be unstructured content describing a person.
        var inputText = """
            John Doe is a 29-year-old software engineer who specializes in building 
            cross-platform mobile applications. He has worked at several leading 
            tech companies and contributed to a wide range of open-source projects.
            
            Recently, John has focused on building secure, user-friendly interfaces 
            that help everyday users understand complex data. He occasionally speaks 
            at industry conferences, discussing user experience and modern development 
            practices.
            """;

        var result = await parser.ParseWithRetryAsync<SimpleResult>(
            inputText,
            // Considerations (optional hints) for the parser:
            """
            - Keep the output factual.
            - If a title isn't clearly stated, set it to null.
            - Keep the summary as concise as possible.
            """,
            cancellationToken: cancellationToken);

        if (result == null)
        {
            throw new InvalidOperationException("Parsing failed.");
        }

        Console.WriteLine($"Name: {result.Name}");
        Console.WriteLine($"Age: {result.Age}");
        Console.WriteLine($"Title: {(result.JobTitle ?? "none")}");
        Console.WriteLine($"Summary: {result.Summary}");
    }

    public class SimpleResult
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public string? JobTitle { get; set; }
        public string Summary { get; set; } = "";
    }
}
