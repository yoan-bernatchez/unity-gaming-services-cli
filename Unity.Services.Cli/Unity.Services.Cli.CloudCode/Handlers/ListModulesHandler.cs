using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudCode.Handlers;

static class ListModulesHandler
{
    public static async Task ListModulesAsync(
        CommonInput input,
        IUnityEnvironment unityEnvironment,
        ICloudCodeService cloudCodeService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Fetching module list...",
            _ => ListModulesAsync(input, unityEnvironment, cloudCodeService, logger, cancellationToken));
    }

    internal static async Task ListModulesAsync(
        CommonInput input,
        IUnityEnvironment unityEnvironment,
        ICloudCodeService cloudCodeService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var projectId = input.CloudProjectId!;
        var environmentId = await unityEnvironment.FetchIdentifierAsync();

        var modules = await cloudCodeService
            .ListModulesAsync(projectId, environmentId, cancellationToken);

        var result = modules.Select(m => m.Name);

        logger.LogResultValue(result);
    }
}
