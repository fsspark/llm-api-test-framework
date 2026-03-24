using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using LLMTestFramework.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace LLMTestFramework.Core.Services;

public interface IOpenAIService
{
    Task<(ChatCompletionResponse? Response, long LatencyMs)> ChatCompletionAsync(ChatCompletionRequest request);
    Task<HttpResponseMessage> ChatCompletionRawAsync(ChatCompletionRequest request);
    Task<HttpResponseMessage> SendRawRequestAsync(string endpoint, HttpMethod method, string? body = null);
}

public class OpenAIService : IOpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAIConfig _config;
    private readonly ILogger<OpenAIService> _logger;

    public OpenAIService(HttpClient httpClient, OpenAIConfig config, ILogger<OpenAIService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _config.ApiKey);
    }

    public async Task<(ChatCompletionResponse? Response, long LatencyMs)> ChatCompletionAsync(
        ChatCompletionRequest request)
    {
        var sw = Stopwatch.StartNew();
        var json = JsonConvert.SerializeObject(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogDebug("Sending ChatCompletion request to OpenAI model: {Model}", request.Model);

        var response = await _httpClient.PostAsync("/v1/chat/completions", content);
        sw.Stop();

        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("OpenAI returned {StatusCode}: {Body}", response.StatusCode, responseBody);
            return (null, sw.ElapsedMilliseconds);
        }

        var result = JsonConvert.DeserializeObject<ChatCompletionResponse>(responseBody);
        _logger.LogDebug("OpenAI response received in {LatencyMs}ms, tokens: {Tokens}",
            sw.ElapsedMilliseconds, result?.Usage?.TotalTokens);

        return (result, sw.ElapsedMilliseconds);
    }

    public async Task<HttpResponseMessage> ChatCompletionRawAsync(ChatCompletionRequest request)
    {
        var json = JsonConvert.SerializeObject(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _httpClient.PostAsync("/v1/chat/completions", content);
    }

    public async Task<HttpResponseMessage> SendRawRequestAsync(
        string endpoint, HttpMethod method, string? body = null)
    {
        var request = new HttpRequestMessage(method, endpoint);
        if (body != null)
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        return await _httpClient.SendAsync(request);
    }
}
