using LLMTestFramework.Core.Models;
using LLMTestFramework.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

/// <summary>
/// Base test fixture — sets up DI, config, and shared services.
/// All test classes inherit from this to get access to configured API clients.
/// </summary>
[SetUpFixture]
public class TestBaseFixture
{
    public static IServiceProvider? Services { get; private set; } = null!;
    public static FrameworkConfig? Config { get; private set; } = null!;
    public static bool LiveTestsEnabled => Config?.TestSettings?.RunLiveApiTests ?? false;

    [OneTimeSetUp]
    public void GlobalSetup()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.test.json", optional: false)
            .AddEnvironmentVariables()// ENV vars override file — safe for CI
            .Build();

        Config = configuration.Get<FrameworkConfig>() ?? new FrameworkConfig();

        // Allow env var overrides for CI pipelines
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_API_KEY")))
            Config.OpenAI.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")))
            Config.Anthropic.ApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")!;

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RUN_LIVE_TESTS")))
            Config.TestSettings.RunLiveApiTests = bool.TryParse(
                Environment.GetEnvironmentVariable("RUN_LIVE_TESTS"), out var live) && live;

        var services = new ServiceCollection();

        services.AddSingleton(Config);
        services.AddSingleton(Config.OpenAI);
        services.AddSingleton(Config.Anthropic);
        services.AddSingleton(Config.TestSettings);

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddHttpClient<IOpenAIService, OpenAIService>();
        services.AddHttpClient<IAnthropicService, AnthropicService>();

        Services = services.BuildServiceProvider();
    }

    [OneTimeTearDown]
    public void GlobalTeardown()
    {
        if (Services is IDisposable disposable)
            disposable.Dispose();
    }
}

/// <summary>
/// Base class for all test classes.
/// Provides null-safe access to services — if Services is null,
/// throws a clear message instead of a cryptic NullReferenceException.
/// </summary>
namespace LLMTestFramework.Tests.Fixtures
{
    public abstract class TestBase
    {
        private static IServiceProvider SafeServices =>
            TestBaseFixture.Services
            ?? throw new InvalidOperationException(
                "ServiceProvider is null. Ensure TestBaseFixture.GlobalSetup() ran. " +
                "Check that [SetUpFixture] class is in the root namespace (no namespace wrapper).");
 
        protected IOpenAIService OpenAI =>
            SafeServices.GetRequiredService<IOpenAIService>();
 
        protected IAnthropicService Anthropic =>
            SafeServices.GetRequiredService<IAnthropicService>();
 
        protected FrameworkConfig Config =>
            SafeServices.GetRequiredService<FrameworkConfig>();
 
        protected bool LiveTestsEnabled =>
            TestBaseFixture.LiveTestsEnabled;
    }
}
