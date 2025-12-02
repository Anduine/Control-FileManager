using System;

namespace ControlFileManager.Core.Models
{
    public record FileItem
    {
        public string Name { get; init; } = string.Empty;
        public string FullPath { get; init; } = string.Empty;
        public bool IsDirectory { get; init; }
        public long? Size { get; init; }      // null for directories
        public DateTime CreatedTime { get; init; }
        public DateTime LastModified { get; init; }
        public bool IsHidden { get; init; } = false;
        public bool IsSystem { get; init; } = false;
        public bool IsReadOnly { get; init; } = false;
    }
}
