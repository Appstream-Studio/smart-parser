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
    /// API key for the Azure OpenAI account. Optional when <see cref="Credential"/> is set —
    /// and ignored then (<see cref="Credential"/> wins), so a key still present in bound
    /// configuration does not break a consumer that switched to keyless auth in code.
    /// </summary>
    public string? OpenAiCredentialKey { get; set; }

    /// <summary>
    /// Token credential for keyless (Entra) auth against the Azure OpenAI account, e.g.
    /// <c>DefaultAzureCredential</c>. Settable only from code (not config-bindable). Takes
    /// precedence over <see cref="OpenAiCredentialKey"/> when both are set. At least one of
    /// the two must be set.
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

        if (!hasKey && !hasCredential)
        {
            yield return new ValidationResult(
                $"Either {nameof(this.OpenAiCredentialKey)} or {nameof(this.Credential)} must be set.",
                new[] { nameof(this.OpenAiCredentialKey), nameof(this.Credential) });
        }
    }
}
