using ClipMaster.Domain.Entities;

namespace ClipMaster.Domain.Interfaces;

public interface IFileManagerService
{
    string OutputDirectory { get; }
    string GenerateClipPath();
    string GenerateRecordingPath();
    string GenerateHybridPath();
    void EnsureOutputDirectory();
    void OpenOutputFolder();
}
