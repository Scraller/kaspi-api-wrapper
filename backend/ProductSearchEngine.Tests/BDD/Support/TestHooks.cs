using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Reqnroll;
using ProductSearchEngine.Tests.Helpers;

namespace ProductSearchEngine.Tests.BDD.Support;

/// <summary>
/// Hooks for BDD test execution lifecycle
/// </summary>
[Binding]
public class TestHooks : TestBase
{
    private static ILogger<TestHooks>? _logger;

    [BeforeTestRun]
    public static void BeforeTestRun()
    {
        // This runs once before all scenarios
        Console.WriteLine("=== Starting BDD Test Run ===");
    }

    [AfterTestRun]
    public static void AfterTestRun()
    {
        // This runs once after all scenarios
        Console.WriteLine("=== Completed BDD Test Run ===");
        TestBase.GlobalCleanup(); // Clean up static resources
    }

    [BeforeFeature]
    public static void BeforeFeature(FeatureContext featureContext)
    {
        // This runs once before each feature
        Console.WriteLine($"=== Starting Feature: {featureContext.FeatureInfo.Title} ===");
    }

    [AfterFeature]
    public static void AfterFeature(FeatureContext featureContext)
    {
        // This runs once after each feature
        Console.WriteLine($"=== Completed Feature: {featureContext.FeatureInfo.Title} ===");
    }

    [BeforeScenario]
    public void BeforeScenario(ScenarioContext scenarioContext)
    {
        // This runs before each scenario
        OneTimeSetUp(); // Initialize service provider
        _logger = ServiceProvider.GetRequiredService<ILogger<TestHooks>>();

        // Register the ServiceProvider with Reqnroll's DI container
        // so it can be injected into step definitions
        scenarioContext.ScenarioContainer.RegisterInstanceAs<IServiceProvider>(ServiceProvider);

        _logger.LogInformation("=== Starting Scenario: {ScenarioTitle} ===",
            scenarioContext.ScenarioInfo.Title);
    }

    [AfterScenario]
    public void AfterScenario(ScenarioContext scenarioContext)
    {
        // This runs after each scenario
        _logger?.LogInformation("=== Completed Scenario: {ScenarioTitle} with status: {Status} ===",
            scenarioContext.ScenarioInfo.Title,
            scenarioContext.ScenarioExecutionStatus);

        OneTimeTearDown(); // Clean up service provider
    }

    [BeforeStep]
    public void BeforeStep(ScenarioContext scenarioContext)
    {
        // This runs before each step (optional, can be removed if not needed)
        var stepText = scenarioContext.StepContext.StepInfo.Text;
        _logger?.LogDebug("Executing step: {StepText}", stepText);
    }

    [AfterStep]
    public void AfterStep(ScenarioContext scenarioContext)
    {
        // This runs after each step
        var stepText = scenarioContext.StepContext.StepInfo.Text;
        var status = scenarioContext.ScenarioExecutionStatus;

        if (status == ScenarioExecutionStatus.StepDefinitionPending)
        {
            _logger?.LogWarning("Step pending: {StepText}", stepText);
        }
        else if (status == ScenarioExecutionStatus.TestError)
        {
            _logger?.LogError("Step failed: {StepText}", stepText);

            // Log any exception details
            if (scenarioContext.TestError != null)
            {
                _logger?.LogError(scenarioContext.TestError, "Step execution error");
            }
        }
        else
        {
            _logger?.LogDebug("Step completed: {StepText}", stepText);
        }
    }
}
