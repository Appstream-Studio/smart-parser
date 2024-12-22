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
}
