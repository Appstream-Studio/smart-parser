using System.ComponentModel.DataAnnotations;
using Azure.Core;

namespace AppStream.SmartParser;

public class SmartParserOptions : IValidatableObject
{
    [Required]
    public required string DeploymentName { get; set; }

    [Required]
    public required int HttpClientNetworkTimeoutSeconds { get; set; }

    [Required]
    public required string OpenAiEndpoint { get; set; }

    /// <summary>
    /// API key for the Azure OpenAI account. Optional: leave unset and supply
    /// <see cref="Credential"/> for keyless (Entra) auth. Exactly one of the two must be set.
    /// </summary>
    public string? OpenAiCredentialKey { get; set; }

    /// <summary>
    /// Token credential for keyless (Entra) auth against the Azure OpenAI account, e.g.
    /// <c>DefaultAzureCredential</c>. Optional: leave unset and supply <see cref="OpenAiCredentialKey"/>
    /// for key auth. Exactly one of the two must be set.
    /// </summary>
    public TokenCredential? Credential { get; set; }

    public void FromOther(SmartParserOptions other)
    {
        this.DeploymentName = other.DeploymentName;
        this.HttpClientNetworkTimeoutSeconds = other.HttpClientNetworkTimeoutSeconds;
        this.OpenAiEndpoint = other.OpenAiEndpoint;
        this.OpenAiCredentialKey = other.OpenAiCredentialKey;
        this.Credential = other.Credential;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var hasKey = !string.IsNullOrWhiteSpace(this.OpenAiCredentialKey);
        var hasCredential = this.Credential is not null;

        if (hasKey == hasCredential)
        {
            yield return new ValidationResult(
                $"Exactly one of {nameof(this.OpenAiCredentialKey)} or {nameof(this.Credential)} must be set.",
                new[] { nameof(this.OpenAiCredentialKey), nameof(this.Credential) });
        }
    }
}
