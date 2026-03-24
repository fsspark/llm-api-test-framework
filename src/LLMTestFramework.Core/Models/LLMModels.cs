using Newtonsoft.Json;

namespace LLMTestFramework.Core.Models;

// ─── OpenAI / Compatible API ───────────────────────────────────────────────

public class ChatMessage
{
    [JsonProperty("role")]
    public string Role { get; set; } = string.Empty;

    [JsonProperty("content")]
    public string Content { get; set; } = string.Empty;
}

public class ChatCompletionRequest
{
    [JsonProperty("model")]
    public string Model { get; set; } = string.Empty;

    [JsonProperty("messages")]
    public List<ChatMessage> Messages { get; set; } = new();

    [JsonProperty("max_tokens")]
    public int MaxTokens { get; set; } = 1024;

    [JsonProperty("temperature")]
    public double Temperature { get; set; } = 0.7;

    [JsonProperty("stream")]
    public bool Stream { get; set; } = false;
}

public class ChatCompletionResponse
{
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("object")]
    public string? Object { get; set; }

    [JsonProperty("created")]
    public long Created { get; set; }

    [JsonProperty("model")]
    public string? Model { get; set; }

    [JsonProperty("choices")]
    public List<Choice>? Choices { get; set; }

    [JsonProperty("usage")]
    public Usage? Usage { get; set; }
}

public class Choice
{
    [JsonProperty("index")]
    public int Index { get; set; }

    [JsonProperty("message")]
    public ChatMessage? Message { get; set; }

    [JsonProperty("finish_reason")]
    public string? FinishReason { get; set; }
}

public class Usage
{
    [JsonProperty("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonProperty("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonProperty("total_tokens")]
    public int TotalTokens { get; set; }
}

// ─── Anthropic API ─────────────────────────────────────────────────────────

public class AnthropicRequest
{
    [JsonProperty("model")]
    public string Model { get; set; } = string.Empty;

    [JsonProperty("max_tokens")]
    public int MaxTokens { get; set; } = 1024;

    [JsonProperty("messages")]
    public List<ChatMessage> Messages { get; set; } = new();

    [JsonProperty("system")]
    public string? System { get; set; }
}

public class AnthropicResponse
{
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("role")]
    public string? Role { get; set; }

    [JsonProperty("content")]
    public List<AnthropicContent>? Content { get; set; }

    [JsonProperty("model")]
    public string? Model { get; set; }

    [JsonProperty("stop_reason")]
    public string? StopReason { get; set; }

    [JsonProperty("usage")]
    public AnthropicUsage? Usage { get; set; }
}

public class AnthropicContent
{
    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("text")]
    public string? Text { get; set; }
}

public class AnthropicUsage
{
    [JsonProperty("input_tokens")]
    public int InputTokens { get; set; }

    [JsonProperty("output_tokens")]
    public int OutputTokens { get; set; }
}

// ─── Generic Error Model ───────────────────────────────────────────────────

public class ApiError
{
    [JsonProperty("error")]
    public ErrorDetail? Error { get; set; }
}

public class ErrorDetail
{
    [JsonProperty("message")]
    public string? Message { get; set; }

    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("code")]
    public string? Code { get; set; }
}

// ─── Test Result Model ─────────────────────────────────────────────────────

public class LLMTestResult
{
    public string TestName { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string? FailureReason { get; set; }
    public long LatencyMs { get; set; }
    public int? TokensUsed { get; set; }
    public string? ModelUsed { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
