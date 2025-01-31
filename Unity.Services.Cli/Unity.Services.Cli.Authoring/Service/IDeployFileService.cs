namespace Unity.Services.Cli.Authoring.Service;

public interface IDeployFileService
{
    /// <summary>
    /// List Files from paths with expected extension
    /// </summary>
    /// <param name="paths">list of file or directory paths to evaluate</param>
    /// <param name="extension">target file extension. For example ".js" to look for java script file</param>
    /// <returns>paths of files with target file extension</returns>
    IReadOnlyList<string> ListFilesToDeploy(IReadOnlyList<string> paths, string extension);
    Task<string> LoadContentAsync(string filePath, CancellationToken cancellationToken);
}
