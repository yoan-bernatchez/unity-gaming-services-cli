using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Player.Handlers;
using Unity.Services.Cli.Player.Input;
using Unity.Services.Cli.Player.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.Player.UnitTest.Handlers;

public class DeleteHandlerTests
{
    private readonly Mock<IPlayerService>? m_MockPlayerService = new();
    private readonly Mock<ILogger>? m_MockLogger = new();
    private const string k_PlayerId = "player-id";
    private const string k_ProjectId = "abcd1234-ab12-cd34-ef56-abcdef123456";

    [SetUp]
    public void SetUp()
    {
        m_MockPlayerService.Reset();
        m_MockLogger.Reset();
    }

    [Test]
    public async Task DeleteAsync_CallsLoadingIndicator()
    {
        Mock<ILoadingIndicator> mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await DeleteHandler.DeleteAsync(null!,  null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(ex => ex
            .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?,Task>>()), Times.Once);
    }

    [Test]
    public async Task DeleteHandler_Valid()
    {
        PlayerInput input = new()
        {
            PlayerId = k_PlayerId,
            CloudProjectId = k_ProjectId,
        };

        m_MockPlayerService?.Setup(x => x.DeleteAsync(k_ProjectId, k_PlayerId,
            CancellationToken.None));

        await DeleteHandler.DeleteAsync(
            input,
            m_MockPlayerService!.Object,
            m_MockLogger!.Object,
            CancellationToken.None
        );

        m_MockPlayerService.Verify(s => s.DeleteAsync(k_ProjectId,k_PlayerId,CancellationToken.None), Times.Once);
        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Information, null, Times.Once);
    }
}
