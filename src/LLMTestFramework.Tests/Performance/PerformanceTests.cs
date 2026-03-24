using Allure.NUnit;
using Allure.NUnit.Attributes;
using FluentAssertions;
using LLMTestFramework.Core.Utilities;
using LLMTestFramework.Core.Validators;
using LLMTestFramework.Tests.Fixtures;
using System.Diagnostics;
using NUnit.Framework;

namespace LLMTestFramework.Tests.Performance;

/// <summary>
/// Performance tests for LLM APIs.
///
/// These tests establish baselines and catch regressions in:
/// - Response latency (P50, P95)
/// - Token throughput
/// - Concurrency handling
/// - Rate limiting behavior
///
/// NOTE: These are NOT load tests. For production load testing, use a
/// dedicated tool like k6, Gatling, or NBomber.
/// </summary>
[TestFixture]
[AllureNUnit]
[AllureSuite("Performance Tests")]
[AllureFeature("LLM API Performance")]
public class PerformanceTests : TestBase
{
    [Test]
    [AllureStory("Latency Baseline")]
    [AllureDescription("Validates that a simple request completes within the acceptable latency threshold")]
    [Category("Performance")]
    public async Task OpenAI_SimpleRequest_CompletesWithinLatencyThreshold()
    {
        if (!LiveTestsEnabled) Assert.Ignore("Live API tests disabled.");

        var request = LLMRequestBuilder.SimpleQuestion(Config.OpenAI.DefaultModel);

        var (response, latencyMs) = await OpenAI.ChatCompletionAsync(request);

        response.Should().NotBeNull();

        var latencyCheck = LLMResponseValidator.ValidateLatency(latencyMs, Config.TestSettings.AcceptableLatencyMs);
        latencyCheck.IsValid.Should().BeTrue(latencyCheck.ToString());

        Console.WriteLine($"[Performance] Simple request latency: {latencyMs}ms (threshold: {Config.TestSettings.AcceptableLatencyMs}ms)");
    }

    [Test]
    [AllureStory("Latency Baseline")]
    [AllureDescription("Validates Anthropic API latency for a simple request")]
    [Category("Performance")]
    public async Task Anthropic_SimpleRequest_CompletesWithinLatencyThreshold()
    {
        if (!LiveTestsEnabled) Assert.Ignore("Live API tests disabled.");

        var request = LLMRequestBuilder.AnthropicSimpleQuestion(Config.Anthropic.DefaultModel);

        var (response, latencyMs) = await Anthropic.MessagesAsync(request);

        response.Should().NotBeNull();

        var latencyCheck = LLMResponseValidator.ValidateLatency(latencyMs, Config.TestSettings.AcceptableLatencyMs);
        latencyCheck.IsValid.Should().BeTrue(latencyCheck.ToString());

        Console.WriteLine($"[Performance] Anthropic latency: {latencyMs}ms");
    }

    [Test]
    [AllureStory("P95 Latency")]
    [AllureDescription("Runs 5 sequential requests and validates that P95 latency is within threshold")]
    [Category("Performance")]
    public async Task OpenAI_SequentialRequests_P95LatencyWithinThreshold()
    {
        if (!LiveTestsEnabled) Assert.Ignore("Live API tests disabled.");

        const int requestCount = 5;
        var latencies = new List<long>();

        for (int i = 0; i < requestCount; i++)
        {
            var request = LLMRequestBuilder.SimpleQuestion(Config.OpenAI.DefaultModel);
            var (_, latencyMs) = await OpenAI.ChatCompletionAsync(request);
            latencies.Add(latencyMs);
            Console.WriteLine($"  Request {i + 1}/{requestCount}: {latencyMs}ms");
        }

        latencies.Sort();
        var p50 = latencies[requestCount / 2];
        var p95 = latencies[(int)(requestCount * 0.95)];
        var avg = (long)latencies.Average();

        Console.WriteLine($"[Performance] P50={p50}ms, P95={p95}ms, Avg={avg}ms");

        p95.Should().BeLessOrEqualTo((long)(Config.TestSettings.AcceptableLatencyMs * 1.5),
            $"P95 latency {p95}ms should be within 1.5x the acceptable threshold");
    }

    [Test]
    [AllureStory("Token Efficiency")]
    [AllureDescription("Validates that token usage for a simple request stays within an efficient budget")]
    [Category("Performance")]
    public async Task OpenAI_SimpleRequest_TokenUsageIsEfficient()
    {
        if (!LiveTestsEnabled) Assert.Ignore("Live API tests disabled.");

        var request = LLMRequestBuilder.SimpleQuestion(
            Config.OpenAI.DefaultModel, "What is 5 + 5? Answer with only the number.");

        var (response, _) = await OpenAI.ChatCompletionAsync(request);

        response.Should().NotBeNull();
        var usage = response!.Usage!;

        var efficiency = LLMResponseValidator.ValidateTokenEfficiency(usage, maxTokensAllowed: 100);
        efficiency.IsValid.Should().BeTrue(
            $"Simple question should not use excessive tokens. {efficiency}");

        Console.WriteLine($"[Performance] Tokens: prompt={usage.PromptTokens}, completion={usage.CompletionTokens}, total={usage.TotalTokens}");
    }

    [Test]
    [AllureStory("Concurrent Requests")]
    [AllureDescription("Validates that 3 concurrent requests all succeed without errors")]
    [Category("Performance")]
    public async Task OpenAI_ConcurrentRequests_AllSucceed()
    {
        if (!LiveTestsEnabled) Assert.Ignore("Live API tests disabled.");

        const int concurrency = 3;

        var tasks = Enumerable.Range(0, concurrency).Select(async i =>
        {
            var request = LLMRequestBuilder.SimpleQuestion(
                Config.OpenAI.DefaultModel, $"What is {i + 1} + {i + 1}? Answer with only the number.");
            var sw = Stopwatch.StartNew();
            var (response, latency) = await OpenAI.ChatCompletionAsync(request);
            return (Index: i, Response: response, Latency: latency);
        });

        var results = await Task.WhenAll(tasks);

        foreach (var result in results)
        {
            result.Response.Should().NotBeNull(
                $"Concurrent request {result.Index} should not fail");
            Console.WriteLine($"  Request {result.Index}: {result.Latency}ms");
        }

        var allSucceeded = results.All(r => r.Response != null);
        allSucceeded.Should().BeTrue("All concurrent requests should succeed");
    }
}
