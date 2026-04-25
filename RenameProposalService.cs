using System;
using System.Collections.Generic;
using System.IO;

namespace FileRenamer
{
    internal sealed class RenameProposalService
    {
        public IReadOnlyList<(string OriginalName, string ProposedName)> BuildProposals(
            IEnumerable<FileItem> files,
            RenamingOptions options)
        {
            var proposals = new List<(string OriginalName, string ProposedName)>();

            foreach (var item in files)
            {
                if (options.FilterEnabled && !string.IsNullOrEmpty(options.FilterText))
                {
                    if (item.Name.IndexOf(options.FilterText, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }
                }

                proposals.Add((item.Name, GetProposedFileName(item.Name, options)));
            }

            return proposals;
        }

        public string GetProposedFileName(string fileName, RenamingOptions options)
        {
            var name = fileName;

            if (options.PrependEnabled)
            {
                name = options.PrependText + name;
            }

            if (options.ReplaceEnabled && !string.IsNullOrEmpty(options.ReplaceFind))
            {
                name = name.Replace(options.ReplaceFind, options.ReplaceWith);
            }

            if (options.AppendEnabled)
            {
                var extension = Path.GetExtension(name);
                var nameWithoutExt = name.Length > 0 && extension.Length > 0
                    ? name.Substring(0, name.Length - extension.Length)
                    : name;
                name = nameWithoutExt + options.AppendText + extension;
            }

            return name;
        }
    }
}
