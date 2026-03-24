using Allure.NUnit;
using Allure.NUnit.Attributes;
using FluentAssertions;
using LLMTestFramework.Core.Utilities;
using LLMTestFramework.Core.Validators;
using LLMTestFramework.Tests.Fixtures;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LLMTestFramework.Tests.Functional;

/// <summary>
/// Functional tests — validate that the LLM produces contextually correct,
/// coherent, and useful responses for known prompts.
///
/// These are deterministic where possible (temperature=0) so they can run
/// reliably in CI. For inherently non-deterministic prompts, we validate
/// structural correctness rather than exact content.
/// </summary>
[TestFixture]
[AllureNUnit]
[AllureSuite("Functional Tests")]
[AllureFeature("LLM Response Quality")]
public class ResponseQualityTests : TestBase
{
    [Test]
    [AllureStory("Basic Factual Accuracy")]
    [AllureDescription("Validates the model can answer a deterministic arithmetic question correctly")]
    [Category("Functional")]
    public async Task OpenAI_SimpleArithmetic_AnswersCorrectly()
    {
        if (!LiveTestsEnabled) Assert.Ignore("Live API tests disabled.");

        var request = LLMRequestBuilder.SimpleQuestion(
            Config.OpenAI.DefaultModel, "What is 7 multiplied by 8? Answer with only the number.");

        var (response, latency) = await OpenAI.ChatCompletionAsync(request);

        response.Should().NotBeNull();
        var content = response!.Choices![0].Message!.Content;

        content.Should().Contain("56", "7 * 8 = 56 is a deterministic fact");
        Console.WriteLine($"[Functional] Arithmetic response: '{content}' in {latency}ms");
    }

    [Test]
    [AllureStory("Context Retention")]
    [AllureDescription("Validates the model retains context across a multi-turn conversation")]
    [Category("Functional")]
    public async Task OpenAI_MultiTurn_RemembersContext()
    {
        if (!LiveTestsEnabled) Assert.Ignore("Live API tests disabled.");

        var request = LLMRequestBuilder.MultiTurnConversation(Config.OpenAI.DefaultModel);

        var (response, _) = await OpenAI.ChatCompletionAsync(request);

        response.Should().NotBeNull();
        var content = response!.Choices![0].Message!.Content;

        content.Should().Contain("Alex",
            "Model must retain the user's name from earlier in the conversation");
    }

    [Test]
    [AllureStory("Structured Output")]
    [AllureDescription("Validates the model produces valid JSON when instructed to do so")]
    [Category("Functional")]
    public async Task OpenAI_StructuredOutputRequest_ReturnsValidJson()
    {
        if (!LiveTestsEnabled) Assert.Ignore("Live API tests disabled.");

        var request = LLMRequestBuilder.StructuredOutputRequest(Config.OpenAI.DefaultModel);

        var (response, _) = await OpenAI.ChatCompletionAsync(request);

        response.Should().NotBeNull();
        var content = response!.Choices![0].Message!.Content.Trim();

        JObject? parsed = null;
        var act = () => parsed = JObject.Parse(content);
        act.Should().NotThrow("Model instructed to return JSON must return parseable JSON");

        parsed.Should().ContainKey("name");
        parsed.Should().ContainKey("age");
        parsed.Should().ContainKey("city");
        parsed!["name"]!.Value<string>().Should().Be("John Doe");
        parsed["age"]!.Value<int>().Should().Be(30);
    }

    [Test]
    [AllureStory("Structured Output")]
    [AllureDescription("Validates Anthropic produces valid JSON when instructed to do so")]
    [Category("Functional")]
    public async Task Anthropic_StructuredOutputRequest_ReturnsValidJson()
    {
        if (!LiveTestsEnabled) Assert.Ignore("Live API tests disabled.");

        var request = LLMRequestBuilder.AnthropicStructuredOutput(Config.Anthropic.DefaultModel);

        var (response, _) = await Anthropic.MessagesAsync(request);

        response.Should().NotBeNull();
        var content = response!.Content![0].Text!.Trim();

        JObject? parsed = null;
        var act = () => parsed = JObject.Parse(content);
        act.Should().NotThrow("Anthropic model instructed to return JSON must return parseable JSON");

        parsed.Should().ContainKey("name");
        parsed.Should().ContainKey("age");
        parsed.Should().ContainKey("city");
    }

    [Test]
    [AllureStory("Instruction Following")]
    [AllureDescription("Validates the model respects a system prompt's behavioral constraints")]
    [Category("Functional")]
    public async Task OpenAI_SystemPromptConstraint_RespondsWithinScope()
    {
        if (!LiveTestsEnabled) Assert.Ignore("Live API tests disabled.");

        var request = LLMRequestBuilder.WithSystemPrompt(
            "You are a cooking assistant. You ONLY answer questions about cooking and recipes. " +
            "For any other topic, respond with exactly: 'I can only help with cooking topics.'",
            "What is the GDP of France?",
            Config.OpenAI.DefaultModel);

        var (response, _) = await OpenAI.ChatCompletionAsync(request);

        response.Should().NotBeNull();
        var content = response!.Choices![0].Message!.Content;

        // Model should refuse or redirect, not answer about GDP
        var notAboutGdp = !content.Contains("GDP", StringComparison.OrdinalIgnoreCase) ||
                           content.Contains("cooking", StringComparison.OrdinalIgnoreCase);

        notAboutGdp.Should().BeTrue(
            "Model should respect the system prompt and not answer off-topic questions");
    }

    [Test]
    [AllureStory("Response Quality")]
    [AllureDescription("Validates response content is not empty and meets minimum quality criteria")]
    [Category("Functional")]
    public async Task OpenAI_GeneralQuestion_ResponseMeetsQualityCriteria()
    {
        if (!LiveTestsEnabled) Assert.Ignore("Live API tests disabled.");

        var request = LLMRequestBuilder.WithSystemPrompt(
            "You are a helpful assistant.",
            "Explain what an API is in two sentences.",
            Config.OpenAI.DefaultModel);

        var (response, _) = await OpenAI.ChatCompletionAsync(request);

        response.Should().NotBeNull();
        var content = response!.Choices![0].Message!.Content;

        var qualityCheck = LLMResponseValidator.ValidateResponseNotEmpty(content);
        qualityCheck.IsValid.Should().BeTrue(qualityCheck.ToString());

        var containsCheck = LLMResponseValidator.ValidateResponseContains(content, "API", "application");
        containsCheck.IsValid.Should().BeTrue(containsCheck.ToString());
    }

    [Test]
    [AllureStory("Large Input Handling")]
    [AllureDescription("Validates the model handles large input gracefully and returns a meaningful summary")]
    [Category("Functional")]
    public async Task OpenAI_LargeInput_ReturnsMeaningfulSummary()
    {
        if (!LiveTestsEnabled) Assert.Ignore("Live API tests disabled.");

        var request = LLMRequestBuilder.LargeInputRequest(300, Config.OpenAI.DefaultModel);

        var (response, latency) = await OpenAI.ChatCompletionAsync(request);

        response.Should().NotBeNull();
        var content = response!.Choices![0].Message!.Content;

        var quality = LLMResponseValidator.ValidateResponseNotEmpty(content);
        quality.IsValid.Should().BeTrue("Model should return a non-empty summary for large inputs");

        Console.WriteLine($"[Functional] Large input response in {latency}ms: '{content}'");
    }
}
