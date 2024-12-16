[![AppStream Studio](https://raw.githubusercontent.com/Appstream-Studio/smart-parser/main/assets/banner.jpg)](https://appstream.studio/)
[![License](https://img.shields.io/badge/license-apache-green)](https://github.com/Appstream-Studio/smart-parser/blob/main/LICENSE)
[![NuGet Package](https://img.shields.io/nuget/v/appstream.smartparser.svg)](https://www.nuget.org/packages/AppStream.SmartParser/)

# SmartParser
<b>SmartParser</b> is an open-source utility library designed to transform unstructured data — such as raw text or images — into structured, strongly-typed objects. By leveraging the power of Large Language Models (LLMs) like those from OpenAI, Smart-Parser helps you integrate AI-driven content parsing directly into your workflows or applications.

# Setup & Installation

## Prerequisites:
- .NET 6.0 or later
- Access to an OpenAI API key (or other compatible LLM provider)

## Installation:
Add the Smart-Parser package via NuGet:

```bash
dotnet add package AppStream.SmartParser
```

## Configure Services and Settings

### Code-Based Configuration

```C#
services.AddSmartParser(options =>
{
    options.DeploymentName = "MyDeployment";
    options.OpenAiEndpoint = "https://your-openai-endpoint.azure.com/";
    options.OpenAiCredentialKey = "my-key";
});
```

### Environment Variables

```C#
services.AddSmartParser(options =>
{
    options.DeploymentName = Environment.GetEnvironmentVariable("DEPLOYMENT_NAME") ?? "DefaultDeployment";
    options.OpenAiEndpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT") ?? "https://your-openai-endpoint.azure.com/";
    options.OpenAiCredentialKey = Environment.GetEnvironmentVariable("OPENAI_CREDENTIAL_KEY") ?? "default-key";
});
```

### AppSettings File

`appsettings.json` sample:
```json
{
  "SmartParser": {
    "DeploymentName": "MyDeployment",
    "OpenAiEndpoint": "https://your-openai-endpoint.azure.com/",
    "OpenAiCredentialKey": "my-key"
  }
}
```

```C#
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

services.AddSmartParser(configuration.GetSection("SmartParser").Bind);
```

# Usage
Once you've configured SmartParser in your project, parsing is straightforward. Whether you're working with unstructured text or scanned images, SmartParser can help produce typed objects tailored to your needs.

## Text
Simply pass your raw text content along with any guiding considerations, and SmartParser will return a typed object reflecting the extracted information.

```C#
public class SimpleResult
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public string? Title { get; set; }
    public string Summary { get; set; } = "";
}

public class Example
{
    private readonly SmartParser _smartParser;

    public Example(SmartParser smartParser)
    {
        _smartParser = smartParser;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        // Input text might be unstructured content describing a person
        var inputText = """
            John Doe is a 29-year-old software engineer who specializes in building 
            cross-platform mobile applications. He has worked at several leading 
            tech companies and contributed to a wide range of open-source projects.
            
            Recently, John has focused on building secure, user-friendly interfaces 
            that help everyday users understand complex data. He occasionally speaks 
            at industry conferences, discussing user experience and modern development 
            practices.
            """;

        var result = await _smartParser.ParseAsync<SimpleResult>(
            inputText,
            // Considerations (optional hints) for the parser:
            """
            - Keep the output factual.
            - If a title isn't clearly stated, set it to null.
            - Keep the summary as concise as possible.
            """
            cancellationToken);

        if (result == null)
        {
            throw new InvalidOperationException("Parsing failed.");
        }

        Console.WriteLine($"Name: {result.Name}");
        Console.WriteLine($"Age: {result.Age}");
        Console.WriteLine($"Title: {(result.Title ?? "none")}");
        Console.WriteLine($"Summary: {result.Summary}");
    }
}

```

## Image

Just like with text, you can supply an image URL and parsing considerations, and the parser will produce a typed result based on the visual content.

```C#
// Suppose this URL points to an image of a CV/resume
var cvImageUrl = "https://example.com/cv.jpg";

var result = await _smartParser.ParseImageAsync<SimpleResult>(
    cvImageUrl,
    """
    Extract the individual's name, approximate age (if stated), any professional title, and a concise summary of their experience.
    If a professional title is not clearly stated, set it to null.
    """,
    cancellationToken);

if (result == null)
{
    Console.WriteLine("CV parsing from image failed.");
    return;
}

Console.WriteLine($"Name: {result.Name}");
Console.WriteLine($"Age: {result.Age}");
Console.WriteLine($"Title: {(result.Title ?? "none")}");
Console.WriteLine($"Summary: {result.Summary}");
```

# Contributing
Contributions to this open source library are highly appreciated! If you're interested in helping out, please feel free to submit a pull request with your changes. We welcome contributions of all kinds, whether it's bug fixes, new features, or just improving the documentation. Please ensure that your code is well-documented, tested, and adheres to the coding conventions used in the project. Don't hesitate to reach out if you have any questions or need help getting started. You can open an issue on GitHub or email us at contact@appstream.studio - we're happy to assist you in any way we can.
