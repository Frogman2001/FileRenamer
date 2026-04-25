namespace FileRenamer
{
    internal sealed class RenamingOptions
    {
        public bool FilterEnabled { get; init; }
        public string FilterText { get; init; } = string.Empty;
        public bool PrependEnabled { get; init; }
        public string PrependText { get; init; } = string.Empty;
        public bool AppendEnabled { get; init; }
        public string AppendText { get; init; } = string.Empty;
        public bool ReplaceEnabled { get; init; }
        public string ReplaceFind { get; init; } = string.Empty;
        public string ReplaceWith { get; init; } = string.Empty;
    }
}
