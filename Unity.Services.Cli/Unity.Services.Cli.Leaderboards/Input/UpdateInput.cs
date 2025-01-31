using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Leaderboards.Input;

internal class UpdateInput : LeaderboardIdInput
{
    private const string k_JsonBodyDescription =
        "Json file path of the leadeboard config \n" +
        "Payload example: https://services.docs.unity.com/leaderboards-admin/v1/#tag/Leaderboards/operation/updateLeaderboardConfig";

    public static readonly Argument<string> RequestBodyArgument = new("file-path", k_JsonBodyDescription);

    [InputBinding(nameof(RequestBodyArgument))]
    public string? JsonFilePath { get; set; }
}

