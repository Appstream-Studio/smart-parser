using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenAI;
using OpenAI.Chat;

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
    Task<TResult?> ParseAsync<TResult>(string inputText, string? considerations = null, CancellationToken cancellationToken = default)
        where TResult : class;

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
    Task<TResult?> ParseImageAsync<TResult>(string imageUrl, string? considerations = null, CancellationToken cancellationToken = default)
        where TResult : class;
}


internal class SmartParser(
    OpenAIClient openAIClient,
    IOptions<SmartParserOptions> options,
    IJsonSchemaGenerator schemaGenerator) : ISmartParser
{
    private readonly SmartParserOptions _options = options.Value;
    private readonly OpenAIClient _openAIClient = openAIClient;
    private readonly IJsonSchemaGenerator _schemaGenerator = schemaGenerator;

    public Task<TResult?> ParseAsync<TResult>(string inputText, string? considerations, CancellationToken cancellationToken)
        where TResult : class
    {
        return this.ParseAsyncInternal<TResult>(inputText, InputType.Text, considerations, cancellationToken);
    }

    public Task<TResult?> ParseImageAsync<TResult>(string imageUrl, string? considerations, CancellationToken cancellationToken)
        where TResult : class
    {
        return this.ParseAsyncInternal<TResult>(imageUrl, InputType.ImageUrl, considerations, cancellationToken);
    }

    private enum InputType { Text, ImageUrl }

    private async Task<TResult?> ParseAsyncInternal<TResult>(string input, InputType type, string? considerations, CancellationToken cancellationToken)
        where TResult : class
    {
        var chatClient = this._openAIClient.GetChatClient(this._options.DeploymentName);
        var messages = new List<ChatMessage>
        {
            BuildSystemMessage(considerations),
            type switch
            {
                InputType.Text => BuildUserMessage(input),
                InputType.ImageUrl => BuildUserImageMessage(input),
                _ => throw new ArgumentException("Incorrect type value provided")
            }
        };

        var schema = this._schemaGenerator.GenerateSchema<TResult>();
        if (schema != null)
        {
            var completionsOptions = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    "ExtractionResult",
                    schema,
                    jsonSchemaIsStrict: false)
            };

            var completions = await chatClient.CompleteChatAsync(messages, completionsOptions, cancellationToken);

            var responseContent = completions.Value.Content[0].Text
                ?? throw new InvalidOperationException($"Chat completion response is null.");

            try
            {
                return JsonConvert.DeserializeObject<TResult>(responseContent);
            }
            catch (Exception e)
            {
                throw new ResponseDeserializationException(
                    $"Deserializing response content into {typeof(TResult).Name} failed. Response content: '{responseContent}'",
                    e);
            }
        }
        else
        {
            return default;
        }
    }

    private static UserChatMessage BuildUserMessage(string inputText)
    {
        var userMessage = @$"
            ---Begin input message---
            {inputText}
            ---End input message---";

        return ChatMessage.CreateUserMessage(
            ChatMessageContentPart.CreateTextPart(userMessage));
    }

    private static UserChatMessage BuildUserImageMessage(string imageUrl)
    {
        return ChatMessage.CreateUserMessage(ChatMessageContentPart.CreateImagePart(new Uri(imageUrl)));
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
}

public class ResponseDeserializationException(string message, Exception inner) : Exception(message, inner)
{
}
