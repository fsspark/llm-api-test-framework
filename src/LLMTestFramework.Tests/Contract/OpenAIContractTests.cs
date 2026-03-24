using System.Net;
using Allure.NUnit;
using Allure.NUnit.Attributes;
using FluentAssertions;
using LLMTestFramework.Core.Utilities;
using LLMTestFramework.Core.Validators;
using LLMTestFramework.Tests.Fixtures;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LLMTestFramework.Tests.Contract;

/// <summary>
/// Contract tests for the OpenAI Chat Completions API.
/// These tests verify the API adheres to its documented schema/contract.
/// They do NOT evaluate AI response quality — that is done in Functional tests.
/// </summary>
[TestFixture]
[AllureNUnit]
[AllureSuite("Contract Tests")]
[AllureFeature("OpenAI API Contract")]
public class OpenAIContractTests : TestBase
{
    [Test]
    [AllureStory("Response Schema Validation")]
    [AllureDescription("Validates that a successful ChatCompletion response conforms to the documented OpenAI schema")]
    [Category("Contract")]
    public async Task ChatCompletion_ValidRequest_ResponseMatchesContract()
    {
        if (!LiveTestsEnabled) Assert.Ignore("Live API tests disabled. Set RUN_LIVE_TESTS=true to enable.");

        var request = LLMRequestBuilder.SimpleQuestion(Config.OpenAI.DefaultModel);

        var (response, latency) = await OpenAI.ChatCompletionAsync(request);

        var validation = LLMResponseValidator.ValidateOpenAIContract(response);
        validation.IsValid.Should().BeTrue(
            $"Contract validation failed: {validation}");

        Console.WriteLine($"[Contract] OpenAI response validated OK in {latency}ms");
    }

    [Test]
    [AllureStory("HTTP Status Codes")]
    [AllureDescription("Validates that an invalid model name returns HTTP 400 or 404")]
    [Category("Contract")]
    public async Task ChatCompletion_InvalidModel_Returns4xxError()
    {
        if (!LiveTestsEnabled) Assert.Ignore("Live API tests disabled.");

        var request = LLMRequestBuilder.WithInvalidModel();
        var response = await OpenAI.ChatCompletionRawAsync(request);

        ((int)response.StatusCode).Should().BeInRange(400, 499,
            "API should return a 4xx error for an invalid model name");

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrEmpty("Error response should include a body");

        Console.WriteLine($"[Contract] Invalid model -> {(int)response.StatusCode}");
    }

    [Test]
    [AllureStory("HTTP Status Codes")]
    [AllureDescription("Validates that empty messages array returns HTTP 400")]
    [Category("Contract")]
    public async Task ChatCompletion_EmptyMessages_Returns400()
    {
        if (!LiveTestsEnabled) Assert.Ignore("Live API tests disabled.");

        var request = LLMRequestBuilder.WithEmptyMessages();
        var response = await OpenAI.ChatCompletionRawAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Empty messages array should return 400 Bad Request");
    }

    [Test]
    [AllureStory("Response Schema Validation")]
    [AllureDescription("Validates that response headers include Content-Type: application/json")]
    [Category("Contract")]
    public async Task ChatCompletion_ValidRequest_ReturnsJsonContentType()
    {
        if (!LiveTestsEnabled) Assert.Ignore("Live API tests disabled.");

        var request = LLMRequestBuilder.SimpleQuestion(Config.OpenAI.DefaultModel);
        var response = await OpenAI.ChatCompletionRawAsync(request);

        response.Content.Headers.ContentType?.MediaType
            .Should().Be("application/json", "API must return application/json content type");
    }

    [Test]
    [AllureStory("Response Schema Validation")]
    [AllureDescription("Validates token usage fields are consistent in the response")]
    [Category("Contract")]
    public async Task ChatCompletion_ValidRequest_TokenUsageIsConsistent()
    {
        if (!LiveTestsEnabled) Assert.Ignore("Live API tests disabled.");

        var request = LLMRequestBuilder.SimpleQuestion(Config.OpenAI.DefaultModel);
        var (response, _) = await OpenAI.ChatCompletionAsync(request);

        response.Should().NotBeNull();
        response!.Usage.Should().NotBeNull();
        response.Usage!.TotalTokens.Should().Be(
            response.Usage.PromptTokens + response.Usage.CompletionTokens,
            "total_tokens must equal prompt_tokens + completion_tokens");
    }

    [Test]
    [AllureStory("Response Schema Validation")]
    [AllureDescription("Validates that max_tokens is respected in the response")]
    [Category("Contract")]
    public async Task ChatCompletion_WithMaxTokensLimit_RespectsLimit()
    {
        if (!LiveTestsEnabled) Assert.Ignore("Live API tests disabled.");

        var request = LLMRequestBuilder.SimpleQuestion(Config.OpenAI.DefaultModel, "Write a very long story.");
        request.MaxTokens = 20;

        var (response, _) = await OpenAI.ChatCompletionAsync(request);

        response.Should().NotBeNull();
        response!.Usage!.CompletionTokens.Should().BeLessOrEqualTo(20,
            "Model must not exceed the requested max_tokens");
        response.Choices![0].FinishReason.Should().BeOneOf("stop", "length",
            "Finish reason should be 'stop' or 'length' when token limit is hit");
    }

    [Test]
    [AllureStory("Error Contract")]
    [AllureDescription("Validates that error responses follow the documented error schema")]
    [Category("Contract")]
    public async Task ChatCompletion_InvalidRequest_ErrorResponseHasCorrectSchema()
    {
        if (!LiveTestsEnabled) Assert.Ignore("Live API tests disabled.");

        var request = LLMRequestBuilder.WithInvalidModel();
        var response = await OpenAI.ChatCompletionRawAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        var json = JObject.Parse(body);

        json.Should().ContainKey("error",
            "Error responses must have an 'error' key per the OpenAI error schema");

        json["error"]!.Should().NotBeNull();
        json["error"]!["message"]!.Value<string>()
            .Should().NotBeNullOrEmpty("Error object must have a non-empty 'message'");
    }
}
