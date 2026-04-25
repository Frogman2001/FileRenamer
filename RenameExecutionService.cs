using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileRenamer
{
    internal sealed class RenameExecutionService
    {
        public IReadOnlyList<string> FindDuplicateProposedNames(IReadOnlyList<string> proposedNames)
        {
            return proposedNames
                .GroupBy(n => n, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
        }

        public bool HasOverwriteConflict(
            string selectedFolderPath,
            IReadOnlyList<string> selectedFileNames,
            IReadOnlyList<string> proposedFileNames)
        {
            var sourcePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in selectedFileNames)
            {
                sourcePaths.Add(Path.Combine(selectedFolderPath, name));
            }

            for (var i = 0; i < selectedFileNames.Count; i++)
            {
                var proposed = proposedFileNames[i];
                var destPath = Path.Combine(selectedFolderPath, proposed);
                if (File.Exists(destPath) && !sourcePaths.Contains(destPath))
                {
                    return true;
                }
            }

            return false;
        }

        public int CountFilesNeedingRename(IReadOnlyList<string> selectedFileNames, IReadOnlyList<string> proposedFileNames)
        {
            var countToRename = 0;
            for (var i = 0; i < selectedFileNames.Count; i++)
            {
                if (!string.Equals(selectedFileNames[i], proposedFileNames[i], StringComparison.OrdinalIgnoreCase))
                {
                    countToRename++;
                }
            }

            return countToRename;
        }

        public RenameExecutionResult ExecuteRenames(
            string selectedFolderPath,
            IReadOnlyList<string> selectedFileNames,
            IReadOnlyList<string> proposedFileNames)
        {
            var logEntries = new List<RenameLogEntry>();
            var errors = new List<string>();
            var renamedCount = 0;

            for (var i = 0; i < selectedFileNames.Count; i++)
            {
                var originalName = selectedFileNames[i];
                var proposedName = proposedFileNames[i];
                if (string.Equals(originalName, proposedName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var sourcePath = Path.Combine(selectedFolderPath, originalName);
                var destPath = Path.Combine(selectedFolderPath, proposedName);

                try
                {
                    if (File.Exists(sourcePath))
                    {
                        File.Move(sourcePath, destPath);
                        renamedCount++;
                        logEntries.Add(new RenameLogEntry(originalName, proposedName, true, null));
                    }
                    else
                    {
                        logEntries.Add(new RenameLogEntry(originalName, proposedName, false, "File not found."));
                        errors.Add($"{originalName}: File not found.");
                    }
                }
                catch (Exception ex)
                {
                    logEntries.Add(new RenameLogEntry(originalName, proposedName, false, ex.Message));
                    errors.Add($"{originalName}: {ex.Message}");
                }
            }

            return new RenameExecutionResult(logEntries, errors, renamedCount);
        }
    }

    internal sealed class RenameExecutionResult
    {
        public IReadOnlyList<RenameLogEntry> LogEntries { get; }
        public IReadOnlyList<string> Errors { get; }
        public int RenamedCount { get; }

        public RenameExecutionResult(
            IReadOnlyList<RenameLogEntry> logEntries,
            IReadOnlyList<string> errors,
            int renamedCount)
        {
            LogEntries = logEntries;
            Errors = errors;
            RenamedCount = renamedCount;
        }
    }
}
