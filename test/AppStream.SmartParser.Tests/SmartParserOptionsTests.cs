using System.ComponentModel.DataAnnotations;
using Azure.Core;
using Xunit;

namespace AppStream.SmartParser.Tests;

public class SmartParserOptionsTests
{
    private sealed class FakeTokenCredential : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private static SmartParserOptions BuildOptions(string? key, TokenCredential? credential) => new()
    {
        DeploymentName = "gpt-4o",
        HttpClientNetworkTimeoutSeconds = 30,
        OpenAiEndpoint = "https://example.openai.azure.com/",
        OpenAiCredentialKey = key,
        Credential = credential,
    };

    // Mirrors what AddSmartParser's ValidateDataAnnotations() runs at startup.
    private static IReadOnlyList<ValidationResult> Validate(SmartParserOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Validate_WithKeyOnly_Passes()
    {
        var results = Validate(BuildOptions(key: "sk-test", credential: null));

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_WithCredentialOnly_Passes()
    {
        var results = Validate(BuildOptions(key: null, credential: new FakeTokenCredential()));

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_WithNeither_Fails()
    {
        var results = Validate(BuildOptions(key: null, credential: null));

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SmartParserOptions.Credential)));
    }

    [Fact]
    public void Validate_WithBoth_Fails()
    {
        var results = Validate(BuildOptions(key: "sk-test", credential: new FakeTokenCredential()));

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SmartParserOptions.Credential)));
    }
}
