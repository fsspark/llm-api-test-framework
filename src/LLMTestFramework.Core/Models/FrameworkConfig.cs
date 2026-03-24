namespace LLMTestFramework.Core.Models;

public class FrameworkConfig
{
    public OpenAIConfig OpenAI { get; set; } = new();
    public AnthropicConfig Anthropic { get; set; } = new();
    public TestConfig TestSettings { get; set; } = new();
}

public class OpenAIConfig
{
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string ApiKey { get; set; } = string.Empty;
    public string DefaultModel { get; set; } = "gpt-4o-mini";
    public int TimeoutSeconds { get; set; } = 30;
}

public class AnthropicConfig
{
    public string BaseUrl { get; set; } = "https://api.anthropic.com/v1";
    public string ApiKey { get; set; } = string.Empty;
    public string DefaultModel { get; set; } = "claude-3-5-haiku-20241022";
    public string ApiVersion { get; set; } = "2023-06-01";
    public int TimeoutSeconds { get; set; } = 30;
}

public class TestConfig
{
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
    public double AcceptableLatencyMs { get; set; } = 10000;
    public double MinResponseQualityScore { get; set; } = 0.7;
    public bool RunLiveApiTests { get; set; } = false;
}
