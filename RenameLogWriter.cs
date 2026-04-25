using System;
using System.IO;
using System.Text.Json;

namespace FileRenamer
{
    internal sealed class RenameLogWriter
    {
        public RenameLogWriteResult WriteLog(
            string selectedFolderPath,
            RenameLogConfiguration configuration,
            RenameExecutionResult executionResult,
            DateTime runTimeUtc,
            DateTime runTimeLocal)
        {
            var logModel = new RenameLogFile(
                runTimeUtc,
                runTimeLocal,
                selectedFolderPath,
                configuration,
                executionResult.LogEntries,
                executionResult.LogEntries.Count,
                executionResult.RenamedCount);

            var timestamp = runTimeLocal.ToString("yyyyMMdd_HHmmss");
            var logFileName = $"zz_FileRenamerLog{timestamp}.json";
            var logPath = Path.Combine(selectedFolderPath, logFileName);

            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(logModel, options);
                File.WriteAllText(logPath, json);
                return new RenameLogWriteResult(logFileName, null);
            }
            catch (Exception ex)
            {
                return new RenameLogWriteResult(logFileName, ex.Message);
            }
        }
    }

    internal sealed class RenameLogWriteResult
    {
        public string LogFileName { get; }
        public string? ErrorMessage { get; }

        public RenameLogWriteResult(string logFileName, string? errorMessage)
        {
            LogFileName = logFileName;
            ErrorMessage = errorMessage;
        }
    }
}
