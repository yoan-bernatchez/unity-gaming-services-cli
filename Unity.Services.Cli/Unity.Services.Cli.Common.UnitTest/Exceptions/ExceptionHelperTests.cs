using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Spectre.Console.Rendering;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Telemetry;
using Unity.Services.Cli.TestUtils;
using IdentityApiException = Unity.Services.Gateway.IdentityApiV1.Generated.Client.ApiException;
using CloudCodeApiException = Unity.Services.Gateway.CloudCodeApiV1.Generated.Client.ApiException;

namespace Unity.Services.Cli.Common.UnitTest.Exceptions;

[TestFixture]
class ExceptionHelperTests
{
    static readonly MockHelper k_MockHelper = new();
    InvocationContext? m_Context;
    readonly Mock<IAnsiConsole> k_MockAnsiConsole = new();
    ExceptionHelper? m_ExceptionHelper;

    public static IEnumerable<TestCaseData> ApiExceptionTestCases
    {
        get
        {
            yield return new TestCaseData(new IdentityApiException());
            yield return new TestCaseData(new CloudCodeApiException());
        }
    }

    [SetUp]
    public void SetUp()
    {
        k_MockHelper.MockDiagnostics.Reset();
        m_ExceptionHelper = new(k_MockHelper.MockDiagnostics.Object, k_MockAnsiConsole.Object);
        k_MockAnsiConsole.Reset();
        var parser = new Parser(new RootCommand("Test root command"));
        var result = parser.Parse(Array.Empty<string>());
        m_Context = new InvocationContext(result);
    }

    [TearDown]
    public void TearDown()
    {
        k_MockHelper.ClearInvocations();
    }

    [TestCaseSource(nameof(ApiExceptionTestCases))]
    public void HandleApiException(Exception exception)
    {
        Assert.DoesNotThrow(() => m_ExceptionHelper!.HandleException(exception, k_MockHelper.MockLogger.Object, m_Context!));
        TestsHelper.VerifyLoggerWasCalled(k_MockHelper.MockLogger, LogLevel.Error);
        Assert.AreEqual(ExitCode.HandledError, m_Context!.ExitCode);
    }

    [Test]
    public void HandleCliException()
    {
        var expectedExitCode = -5;
        Assert.DoesNotThrow(
            () => m_ExceptionHelper!.HandleException(
                new CliException(expectedExitCode),
                k_MockHelper.MockLogger.Object, m_Context!));
        TestsHelper.VerifyLoggerWasCalled(k_MockHelper.MockLogger, LogLevel.Error);
        Assert.AreEqual(expectedExitCode, m_Context!.ExitCode);
    }

    [Test]
    public void HandleUnhandledException()
    {
        var expectedExitCode = ExitCode.UnhandledError;
        Assert.DoesNotThrow(
            () => m_ExceptionHelper!.HandleException(
                new CookieException(),
                k_MockHelper.MockLogger.Object, m_Context!));
        k_MockAnsiConsole.Verify(ex => ex.Write(It.IsAny<IRenderable>()));
        Assert.AreEqual(expectedExitCode, m_Context!.ExitCode);
    }

    [Test]
    public void HandleAggregateExceptionWithUnhandled()
    {
        var exceptions = new List<Exception>()
        {
            new CookieException(),
            new CliException("test", 1)
        };
        // var aggregateException = new
        var expectedExitCode = ExitCode.UnhandledError;
        Assert.DoesNotThrow(
            () => m_ExceptionHelper!.HandleException(
                new AggregateException(exceptions),
                k_MockHelper.MockLogger.Object, m_Context!));
        k_MockAnsiConsole.Verify(ex => ex.Write(It.IsAny<IRenderable>()));
        Assert.AreEqual(expectedExitCode, m_Context!.ExitCode);
    }

    [Test]
    public void HandleAggregateExceptionWithoutUnhandled()
    {
        var exceptions = new List<Exception>()
        {
            new MissingConfigurationException("test", 1),
            new CliException("test2", 1)
        };
        // var aggregateException = new
        var expectedExitCode = ExitCode.HandledError;
        Assert.DoesNotThrow(
            () => m_ExceptionHelper!.HandleException(
                new AggregateException(exceptions),
                k_MockHelper.MockLogger.Object, m_Context!));
        k_MockAnsiConsole.Verify(ex => ex.Write(It.IsAny<IRenderable>()), Times.Never);
        Assert.AreEqual(expectedExitCode, m_Context!.ExitCode);
    }

    [Test]
    public void HandleDeploymentFailureException()
    {
        var expectedExitCode = ExitCode.HandledError;
        Assert.DoesNotThrow(
            () => m_ExceptionHelper!.HandleException(
                new DeploymentFailureException(),
                k_MockHelper.MockLogger.Object, m_Context!));
        k_MockAnsiConsole.Verify(ex => ex.Write(It.IsAny<IRenderable>()), Times.Never);
        Assert.AreEqual(expectedExitCode, m_Context!.ExitCode);
    }

    [Test]
    public void HandleForbidden403Exception()
    {
        var identityApiException = new IdentityApiException(Convert.ToInt32(HttpStatusCode.Forbidden), null);
        Assert.DoesNotThrow(
            () => m_ExceptionHelper!.HandleException(
                identityApiException,
                k_MockHelper.MockLogger.Object, m_Context!));
        TestsHelper.VerifyLoggerWasCalled(
            k_MockHelper.MockLogger,
            LogLevel.Error,
            null,
            Times.Once,
            $"{ExceptionHelper.TroubleshootingHelp}{System.Environment.NewLine}" +
            $"{m_ExceptionHelper!.HttpErrorTroubleshootingLinks[HttpStatusCode.Forbidden]}");
    }

    [Test]
    public void ExceptionHandler_DoesNotThrowWhenDiagnosticsFailToSend()
    {
        var exception = new Exception("");

        k_MockHelper.MockDiagnostics.Setup(
                ex => ex
                    .Send())
            .Throws(new Exception());

        Assert.DoesNotThrow(
            () => m_ExceptionHelper!.HandleException(
                exception,
                k_MockHelper.MockLogger.Object, m_Context!));
    }

    [Test]
    public void ExceptionHandler_ExecuteUnhandledExceptionFlowCorrectly()
    {
        var exception = new CookieException();
        Assert.DoesNotThrow(
            () => m_ExceptionHelper!.HandleException(
                exception,
                k_MockHelper.MockLogger.Object, m_Context!));

        k_MockHelper.MockDiagnostics.Verify(ex =>
            ex.AddData(TagKeys.DiagnosticName, "cli_unhandled_exception"), Times.Once);
        k_MockHelper.MockDiagnostics.Verify(ex =>
            ex.AddData(TagKeys.DiagnosticMessage, exception.ToString()), Times.Once);

        var command = new StringBuilder("ugs");
        foreach (var arg in m_Context!.ParseResult.Tokens)
        {
            command.Append("_" + arg);
        }

        k_MockHelper.MockDiagnostics.Verify(ex =>
            ex.AddData(TagKeys.Command, command.ToString()), Times.Once);

        k_MockHelper.MockDiagnostics.Verify(ex =>
            ex.AddData(TagKeys.Timestamp, It.IsAny<long>()), Times.Once);
    }
}
