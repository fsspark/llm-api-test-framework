using System.Diagnostics;
using System.Text;
using LLMTestFramework.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace LLMTestFramework.Core.Services;

public interface IAnthropicService
{
    Task<(AnthropicResponse? Response, long LatencyMs)> MessagesAsync(AnthropicRequest request);
    Task<HttpResponseMessage> MessagesRawAsync(AnthropicRequest request);
    Task<HttpResponseMessage> SendRawRequestAsync(string endpoint, HttpMethod method, string? body = null);
}

public class AnthropicService : IAnthropicService
{
    private readonly HttpClient _httpClient;
    private readonly AnthropicConfig _config;
    private readonly ILogger<AnthropicService> _logger;

    public AnthropicService(HttpClient httpClient, AnthropicConfig config, ILogger<AnthropicService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _config.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", _config.ApiVersion);
    }

    public async Task<(AnthropicResponse? Response, long LatencyMs)> MessagesAsync(AnthropicRequest request)
    {
        var sw = Stopwatch.StartNew();
        var json = JsonConvert.SerializeObject(request, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogDebug("Sending Messages request to Anthropic model: {Model}", request.Model);

        var response = await _httpClient.PostAsync("/v1/messages", content);
        sw.Stop();

        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Anthropic returned {StatusCode}: {Body}", response.StatusCode, responseBody);
            return (null, sw.ElapsedMilliseconds);
        }

        var result = JsonConvert.DeserializeObject<AnthropicResponse>(responseBody);
        _logger.LogDebug("Anthropic response received in {LatencyMs}ms, tokens: {Tokens}",
            sw.ElapsedMilliseconds, result?.Usage?.OutputTokens);

        return (result, sw.ElapsedMilliseconds);
    }

    public async Task<HttpResponseMessage> MessagesRawAsync(AnthropicRequest request)
    {
        var json = JsonConvert.SerializeObject(request, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _httpClient.PostAsync("/v1/messages", content);
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
