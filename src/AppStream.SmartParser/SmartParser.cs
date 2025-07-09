using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace AppStream.SmartParser;

/// <summary>
/// Parses text and image inputs into structured data results.
/// </summary>
public interface ISmartParser
{
    /// <summary>
    /// Parses the provided text input into a structured result of type <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="TResult">The type of the structured result to deserialize the parsed data into.</typeparam>
    /// <param name="inputText">The text input to parse.</param>
    /// <param name="considerations">Optional considerations or guidelines for the parsing process.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is an instance of <typeparamref name="TResult"/>,
    /// or <c>null</c> if the parsing process fails or produces no valid output.
    /// </returns>
    Task<TResult> ParseAsync<TResult>(string inputText, string? considerations = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses the provided image input, specified by its URL, into a structured result of type <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="TResult">The type of the structured result to deserialize the parsed data into.</typeparam>
    /// <param name="imageUrl">The URL of the image input to parse.</param>
    /// <param name="considerations">Optional considerations or guidelines for the parsing process.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is an instance of <typeparamref name="TResult"/>,
    /// or <c>null</c> if the parsing process fails or produces no valid output.
    /// </returns>
    Task<TResult> ParseImageAsync<TResult>(string imageUrl, string? considerations = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses the provided image input, specified by its URL, into a structured result of type <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="TResult">The type of the structured result to deserialize the parsed data into.</typeparam>
    /// <param name="imageData">Image content.</param>
    /// <param name="mimeType">The MIME type of the image, e.g., image/png</param>
    /// <param name="imageDetailLevel">The level of detail with which the model should process the image and generate its textual understanding of it.</param>
    /// <param name="considerations">Optional considerations or guidelines for the parsing process.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is an instance of <typeparamref name="TResult"/>,
    /// or <c>null</c> if the parsing process fails or produces no valid output.
    /// </returns>
    Task<TResult> ParseImageAsync<TResult>(
        BinaryData imageData,
        string mimeType,
        ChatImageDetailLevel? imageDetailLevel = null,
        string? considerations = null,
        CancellationToken cancellationToken = default);
}

internal class SmartParser(
    OpenAIClient openAIClient,
    IOptions<SmartParserOptions> options,
    IJsonSchemaGenerator schemaGenerator) : ISmartParser
{
    private readonly SmartParserOptions _options = options.Value;
    private readonly OpenAIClient _openAIClient = openAIClient;
    private readonly IJsonSchemaGenerator _schemaGenerator = schemaGenerator;

    public Task<TResult> ParseAsync<TResult>(string inputText, string? considerations, CancellationToken cancellationToken)
    {
        var userMessageContent = @$"
            ---Begin input message---
            {inputText}
            ---End input message---";

        var userMessage = ChatMessage.CreateUserMessage(
            ChatMessageContentPart.CreateTextPart(userMessageContent));

        return this.ParseInternalAsync<TResult>(userMessage, considerations, cancellationToken);
    }

    public Task<TResult> ParseImageAsync<TResult>(string imageUrl, string? considerations, CancellationToken cancellationToken)
    {
        var userMessage = ChatMessage.CreateUserMessage(
            ChatMessageContentPart.CreateImagePart(new Uri(imageUrl)));

        return this.ParseInternalAsync<TResult>(userMessage, considerations, cancellationToken);
    }

    public Task<TResult> ParseImageAsync<TResult>(
        BinaryData imageData,
        string mimeType,
        ChatImageDetailLevel? imageDetailLevel = null,
        string? considerations = null,
        CancellationToken cancellationToken = default)
    {
        var userMessage = ChatMessage.CreateUserMessage(
            ChatMessageContentPart.CreateImagePart(imageData, mimeType, imageDetailLevel));

        return this.ParseInternalAsync<TResult>(userMessage, considerations, cancellationToken);
    }

    private enum InputType { Text, ImageUrl, ImageBinaryData }

    private async Task<TResult> ParseInternalAsync<TResult>(ChatMessage userMessage, string? considerations, CancellationToken cancellationToken)
    {
        var chatClient = this._openAIClient.GetChatClient(this._options.DeploymentName);
        var messages = new List<ChatMessage>
        {
            BuildSystemMessage(considerations),
            userMessage
        };

        var schema = this._schemaGenerator.GenerateSchema<TResult>();
        var completionsOptions = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                "ExtractionResult",
                schema,
                jsonSchemaIsStrict: false),
            Temperature = 0
        };

        var completions = await chatClient.CompleteChatAsync(messages, completionsOptions, cancellationToken);
        var reason = completions.Value.FinishReason;

        if (reason == ChatFinishReason.ContentFilter)
        {
            throw new UnexpectedCompletionsResponseException(
                "Filtered by the content filter.",
                TryGetRawResponseContent(completions));
        }

        if (reason == ChatFinishReason.Length)
        {
            throw new UnexpectedCompletionsResponseException(
                "Model reached maximum number of tokens allowed.",
                TryGetRawResponseContent(completions));
        }

        if (completions.Value.Content.Count == 0)
        {
            throw new UnexpectedCompletionsResponseException(
                "Cannot parse input. Completions response does not have any contents.",
                TryGetRawResponseContent(completions));
        }

        var completionContent = completions.Value.Content[0].Text
            ?? throw new UnexpectedCompletionsResponseException(
                "Cannot parse input. Chat completion response is null.",
                TryGetRawResponseContent(completions));

        return DeserializeResult<TResult>(completionContent)
            ?? throw new UnexpectedCompletionsResponseException(
                "Cannot parse input. Deserialized content is null.",
                TryGetRawResponseContent(completions));
    }

    private static SystemChatMessage BuildSystemMessage(string? considerations)
    {
        var systemText = """
            You are an analyst that needs to extract information from provided text
            and then build JSON object that contains all the relevant information.
            Don't make stuff up. To build the json object use only data from the text
            provided by the user.
            """;

        if (!string.IsNullOrWhiteSpace(considerations))
        {
            systemText += $"""
                Considerations you have take into account:
                {considerations}
                End considerations.
                """;
        }

        return ChatMessage.CreateSystemMessage(
            ChatMessageContentPart.CreateTextPart(systemText));
    }

    private static TResult? DeserializeResult<TResult>(string completionContent)
    {
        try
        {
            return JsonConvert.DeserializeObject<TResult>(completionContent);
        }
        catch (Exception e)
        {
            throw new ResponseDeserializationException(typeof(TResult).Name, completionContent, e);
        }
    }

    private static string? TryGetRawResponseContent(ClientResult<ChatCompletion> clientResult)
    {
        return clientResult.GetRawResponse().Content.ToString();
    }
}

public class ResponseDeserializationException : Exception
{
    public ResponseDeserializationException(string destTypeName, string completionContent, Exception inner)
        : base($"Deserializing completion content into {destTypeName} failed. Completion content: '{completionContent}'", inner)
    {
        this.CompletionContent = completionContent;
    }

    public string CompletionContent { get; }
}

public class UnexpectedCompletionsResponseException : Exception
{
    public UnexpectedCompletionsResponseException(string message, string? rawResponse)
        : base($"{message}{(rawResponse != null ? $" Raw response: {rawResponse}" : string.Empty)}")
    {
    }
}
