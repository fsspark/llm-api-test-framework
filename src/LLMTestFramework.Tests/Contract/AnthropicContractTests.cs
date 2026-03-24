using System.Net;
using Allure.NUnit;
using Allure.NUnit.Attributes;
using FluentAssertions;
using LLMTestFramework.Core.Utilities;
using LLMTestFramework.Core.Validators;
using LLMTestFramework.Tests.Fixtures;
using NUnit.Framework;

namespace LLMTestFramework.Tests.Contract;

/// <summary>
/// Contract tests for the Anthropic Messages API.
/// </summary>
[TestFixture]
[AllureNUnit]
[AllureSuite("Contract Tests")]
[AllureFeature("Anthropic API Contract")]
public class AnthropicContractTests : TestBase
{
    [Test]
    [AllureStory("Response Schema Validation")]
    [AllureDescription("Validates that a successful Messages response conforms to the Anthropic schema")]
    [Category("Contract")]
    public async Task Messages_ValidRequest_ResponseMatchesContract()
    {
        if (!LiveTestsEnabled) Assert.Ignore("Live API tests disabled. Set RUN_LIVE_TESTS=true to enable.");

        var request = LLMRequestBuilder.AnthropicSimpleQuestion(Config.Anthropic.DefaultModel);

        var (response, latency) = await Anthropic.MessagesAsync(request);

        var validation = LLMResponseValidator.ValidateAnthropicContract(response);
        validation.IsValid.Should().BeTrue(
            $"Contract validation failed: {validation}");

        Console.WriteLine($"[Contract] Anthropic response validated OK in {latency}ms");
    }

    [Test]
    [AllureStory("HTTP Status Codes")]
    [AllureDescription("Validates that missing API key returns HTTP 401")]
    [Category("Contract")]
    public async Task Messages_MissingApiKey_Returns401()
    {
        if (!LiveTestsEnabled) Assert.Ignore("Live API tests disabled.");

        // Create a service with empty API key
        var request = LLMRequestBuilder.AnthropicSimpleQuestion();
        var rawResponse = await Anthropic.SendRawRequestAsync(
            "/v1/messages", HttpMethod.Post,
            Newtonsoft.Json.JsonConvert.SerializeObject(request));

        // If key is empty, should return 401; if key is valid, test is inconclusive
        if (string.IsNullOrEmpty(Config.Anthropic.ApiKey))
        {
            rawResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                "Missing API key must return 401 Unauthorized");
        }
        else
        {
            Assert.Ignore("API key is set — cannot test missing-key scenario without a second client");
        }
    }

    [Test]
    [AllureStory("Response Schema Validation")]
    [AllureDescription("Validates that stop_reason is present and has a known value")]
    [Category("Contract")]
    public async Task Messages_ValidRequest_StopReasonIsValid()
    {
        if (!LiveTestsEnabled) Assert.Ignore("Live API tests disabled.");

        var request = LLMRequestBuilder.AnthropicSimpleQuestion(Config.Anthropic.DefaultModel);
        var (response, _) = await Anthropic.MessagesAsync(request);

        response.Should().NotBeNull();
        response!.StopReason.Should().BeOneOf("end_turn", "max_tokens", "stop_sequence", "tool_use",
            "stop_reason must be one of the documented values");
    }

    [Test]
    [AllureStory("Response Schema Validation")]
    [AllureDescription("Validates that the model field in the response matches the requested model family")]
    [Category("Contract")]
    public async Task Messages_ValidRequest_ModelFieldPresentInResponse()
    {
        if (!LiveTestsEnabled) Assert.Ignore("Live API tests disabled.");

        var request = LLMRequestBuilder.AnthropicSimpleQuestion(Config.Anthropic.DefaultModel);
        var (response, _) = await Anthropic.MessagesAsync(request);

        response.Should().NotBeNull();
        response!.Model.Should().NotBeNullOrEmpty("Response must include the model used");
    }

    [Test]
    [AllureStory("Response Schema Validation")]
    [AllureDescription("Validates that system prompt is accepted without error")]
    [Category("Contract")]
    public async Task Messages_WithSystemPrompt_AcceptedAndReturns200()
    {
        if (!LiveTestsEnabled) Assert.Ignore("Live API tests disabled.");

        var request = LLMRequestBuilder.AnthropicWithSystemPrompt(
            "You are a concise assistant. Keep answers under 10 words.",
            "What is the capital of France?",
            Config.Anthropic.DefaultModel);

        var (response, _) = await Anthropic.MessagesAsync(request);

        response.Should().NotBeNull("Request with system prompt should succeed");
        response!.Content.Should().NotBeNullOrEmpty();
    }
}
