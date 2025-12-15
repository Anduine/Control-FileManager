using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ControlFileManager.Core.Models;

namespace ControlFileManager.Core.Services
{
  public interface IFileSystemService
  {
    Task<IEnumerable<FileItem>> GetDrivesAsync(CancellationToken ct = default);
    Task<IEnumerable<FileItem>> GetDirectoryItemsAsync(string path, bool showHidden, bool showSystem, CancellationToken ct = default);
    Task CreateDirectoryAsync(string parentPath, string name, CancellationToken ct = default);
    Task CreateFileAsync(string parentPath, string name, CancellationToken ct = default);
    Task DeleteAsync(string path, CancellationToken ct = default);
    Task CopyAsync(string sourcePath, string destinationPath, bool overwrite = false, CancellationToken ct = default);
    Task MoveAsync(string sourcePath, string destinationPath, CancellationToken ct = default);
    Task<FileItem> RenameAsync(string oldFullPath, string newName, CancellationToken ct = default);
    IAsyncEnumerable<FileItem> SearchAsync(SearchOptions options, CancellationToken ct);
  }
}
