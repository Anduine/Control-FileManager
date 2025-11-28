using System;

namespace ControlFileManager.Core.Models
{
    public record FileItem
    {
        public string Name { get; init; } = string.Empty;
        public string FullPath { get; init; } = string.Empty;
        public bool IsDirectory { get; init; }
        public long? Size { get; init; }      // null for directories
        public DateTime LastModified { get; init; }
    }
}
