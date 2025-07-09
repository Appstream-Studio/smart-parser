using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;

namespace AppStream.SmartParser;

/// <summary>
/// Provides extension methods for applying retry policies to <see cref="ISmartParser"/> operations.
/// </summary>
public static class SmartParserPollyExtensions
{
    private static readonly IEnumerable<TimeSpan> DefaultSleepDurations = Backoff.DecorrelatedJitterBackoffV2(
        medianFirstRetryDelay: TimeSpan.FromSeconds(1),
        retryCount: 3);

    private static readonly AsyncRetryPolicy DefaultRetryPolicy = Polly.Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(DefaultSleepDurations);

    /// <summary>
    /// Executes the <see cref="ISmartParser.ParseAsync{TResult}"/> method with a retry policy.
    /// </summary>
    /// <typeparam name="TResult">The type of the structured result to deserialize the parsed data into.</typeparam>
    /// <param name="smartParser">The <see cref="ISmartParser"/> instance to execute the operation on.</param>
    /// <param name="inputText">The text input to parse.</param>
    /// <param name="considerations">Optional considerations or guidelines for the parsing process.</param>
    /// <param name="retryPolicy">The retry policy to apply. If <c>null</c>, a default retry policy will be used.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is an instance of <typeparamref name="TResult"/>,
    /// or <c>null</c> if the parsing process fails or produces no valid output.
    /// </returns>
    public static Task<TResult> ParseWithRetryAsync<TResult>(
        this ISmartParser smartParser,
        string inputText,
        string? considerations = null,
        AsyncRetryPolicy? retryPolicy = null,
        CancellationToken cancellationToken = default)
        where TResult : class
    {
        return ExecuteWithJsonRetryAsync(
            (prompt, ct) => smartParser.ParseAsync<TResult>(prompt!, considerations, ct),
            inputText,
            retryPolicy,
            cancellationToken);
    }

    /// <summary>
    /// Executes the <see cref="ISmartParser.ParseImageAsync{TResult}"/> method with a retry policy.
    /// </summary>
    /// <typeparam name="TResult">The type of the structured result to deserialize the parsed data into.</typeparam>
    /// <param name="smartParser">The <see cref="ISmartParser"/> instance to execute the operation on.</param>
    /// <param name="imageUrl">The URL of the image input to parse.</param>
    /// <param name="considerations">Optional considerations or guidelines for the parsing process.</param>
    /// <param name="retryPolicy">The retry policy to apply. If <c>null</c>, a default retry policy will be used.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is an instance of <typeparamref name="TResult"/>,
    /// or <c>null</c> if the parsing process fails or produces no valid output.
    /// </returns>
    public static Task<TResult> ParseImageWithRetryAsync<TResult>(
        this ISmartParser smartParser,
        string imageUrl,
        string? considerations = null,
        AsyncRetryPolicy? retryPolicy = null,
        CancellationToken cancellationToken = default)
        where TResult : class
    {
        return ExecuteWithJsonRetryAsync(
            (prompt, ct) => smartParser.ParseImageAsync<TResult>(imageUrl, prompt, ct),
            considerations,
            retryPolicy,
            cancellationToken);
    }

    private static Task<TResult> ExecuteWithJsonRetryAsync<TResult>(
        Func<string?, CancellationToken, Task<TResult>> parseFunc,
        string? initialPrompt,
        AsyncRetryPolicy? retryPolicy,
        CancellationToken cancellationToken)
    where TResult : class
    {
        var policy = retryPolicy ?? DefaultRetryPolicy;
        string? lastPartial = null;

        return policy.ExecuteAsync(async (ct) =>
        {
            var prompt = lastPartial is null ? initialPrompt :
                $"{initialPrompt}\n\n# PREVIOUS OUTPUT (incomplete or incorrect):\n{lastPartial}\n# PLEASE CONTINUE AND RETURN COMPLETE AND CORRECT JSON.";

            try
            {
                return await parseFunc(prompt, ct);
            }
            catch (ResponseDeserializationException ex)
            {
                lastPartial = ex.CompletionContent;
                throw;
            }
        }, cancellationToken);
    }
}
