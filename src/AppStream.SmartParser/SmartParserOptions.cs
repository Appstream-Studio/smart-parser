using System.ComponentModel.DataAnnotations;

namespace AppStream.SmartParser;

public class SmartParserOptions
{
    [Required]
    public required string DeploymentName { get; set; }

    [Required]
    public required int HttpClientNetworkTimeoutSeconds { get; set; }

    [Required]
    public required string OpenAiEndpoint { get; set; }

    [Required]
    public required string OpenAiCredentialKey { get; set; }

    public void FromOther(SmartParserOptions other)
    {
        this.DeploymentName = other.DeploymentName;
        this.HttpClientNetworkTimeoutSeconds = other.HttpClientNetworkTimeoutSeconds;
        this.OpenAiEndpoint = other.OpenAiEndpoint;
        this.OpenAiCredentialKey = other.OpenAiCredentialKey;
    }
}
