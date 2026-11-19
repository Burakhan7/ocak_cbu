namespace UOM.Application.Abstractions;

/// <summary>
/// Stages a permanent redirect in the current unit of work. The caller saves.
/// </summary>
public interface IRedirectRecorder
{
    Task StagePermanentAsync(string fromPath, string toPath, CancellationToken cancellationToken = default);
}
