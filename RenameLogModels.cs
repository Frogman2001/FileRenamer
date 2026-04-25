using System;
using System.Collections.Generic;

namespace FileRenamer
{
    internal sealed class RenameLogEntry
    {
        public string OriginalName { get; }
        public string NewName { get; }
        public bool Success { get; }
        public string? Error { get; }

        public RenameLogEntry(string originalName, string newName, bool success, string? error)
        {
            OriginalName = originalName;
            NewName = newName;
            Success = success;
            Error = error;
        }
    }

    internal sealed class RenameLogConfiguration
    {
        public bool FilterEnabled { get; }
        public string FilterText { get; }
        public bool PrependEnabled { get; }
        public string PrependText { get; }
        public bool AppendEnabled { get; }
        public string AppendText { get; }
        public bool ReplaceEnabled { get; }
        public string ReplaceFind { get; }
        public string ReplaceWith { get; }

        public RenameLogConfiguration(
            bool filterEnabled, string filterText,
            bool prependEnabled, string prependText,
            bool appendEnabled, string appendText,
            bool replaceEnabled, string replaceFind, string replaceWith)
        {
            FilterEnabled = filterEnabled;
            FilterText = filterText;
            PrependEnabled = prependEnabled;
            PrependText = prependText;
            AppendEnabled = appendEnabled;
            AppendText = appendText;
            ReplaceEnabled = replaceEnabled;
            ReplaceFind = replaceFind;
            ReplaceWith = replaceWith;
        }
    }

    internal sealed class RenameLogFile
    {
        public DateTime RunTimeUtc { get; }
        public DateTime RunTimeLocal { get; }
        public string FolderPath { get; }
        public RenameLogConfiguration Configuration { get; }
        public IReadOnlyList<RenameLogEntry> Entries { get; }
        public int TotalAttempted { get; }
        public int TotalRenamed { get; }

        public RenameLogFile(
            DateTime runTimeUtc,
            DateTime runTimeLocal,
            string folderPath,
            RenameLogConfiguration configuration,
            IReadOnlyList<RenameLogEntry> entries,
            int totalAttempted,
            int totalRenamed)
        {
            RunTimeUtc = runTimeUtc;
            RunTimeLocal = runTimeLocal;
            FolderPath = folderPath;
            Configuration = configuration;
            Entries = entries;
            TotalAttempted = totalAttempted;
            TotalRenamed = totalRenamed;
        }
    }
}
