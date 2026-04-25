using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FileRenamer
{
    public partial class MainWindow
    {
        private void FilterTextTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(FilterTextTextBox.Text))
            {
                FilterEnabledCheckBox.IsChecked = true;
            }

            RefreshRenamingLists();
        }

        private void PrependTextTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(PrependTextTextBox.Text))
            {
                PrependEnabledCheckBox.IsChecked = true;
            }

            RefreshRenamingLists();
        }

        private void AppendTextTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(AppendTextTextBox.Text))
            {
                AppendEnabledCheckBox.IsChecked = true;
            }

            RefreshRenamingLists();
        }

        private void ReplaceConfigurationTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(ReplaceFindTextBox.Text) || !string.IsNullOrWhiteSpace(ReplaceWithTextBox.Text))
            {
                ReplaceEnabledCheckBox.IsChecked = true;
            }

            RefreshRenamingLists();
        }

        private void RenamingSection_Changed(object sender, RoutedEventArgs e)
        {
            RefreshRenamingLists();
        }

        private void ResetRenamingConfigurationToDefaults()
        {
            PrependEnabledCheckBox.IsChecked = false;
            PrependTextTextBox.Text = string.Empty;
            AppendEnabledCheckBox.IsChecked = false;
            AppendTextTextBox.Text = string.Empty;
            ReplaceEnabledCheckBox.IsChecked = false;
            ReplaceFindTextBox.Text = string.Empty;
            ReplaceWithTextBox.Text = string.Empty;
        }

        private void RefreshRenamingLists()
        {
            _selectedFileNames.Clear();
            _proposedFileNames.Clear();

            var options = GetCurrentRenamingOptions();
            var proposals = _renameProposalService.BuildProposals(_files, options);

            foreach (var proposal in proposals)
            {
                _selectedFileNames.Add(proposal.OriginalName);
                _proposedFileNames.Add(proposal.ProposedName);
            }
        }

        private RenamingOptions GetCurrentRenamingOptions()
        {
            return new RenamingOptions
            {
                FilterEnabled = FilterEnabledCheckBox.IsChecked == true,
                FilterText = FilterTextTextBox.Text ?? string.Empty,
                PrependEnabled = PrependEnabledCheckBox.IsChecked == true,
                PrependText = PrependTextTextBox.Text ?? string.Empty,
                AppendEnabled = AppendEnabledCheckBox.IsChecked == true,
                AppendText = AppendTextTextBox.Text ?? string.Empty,
                ReplaceEnabled = ReplaceEnabledCheckBox.IsChecked == true,
                ReplaceFind = ReplaceFindTextBox.Text ?? string.Empty,
                ReplaceWith = ReplaceWithTextBox.Text ?? string.Empty
            };
        }

        private void RenameFilesButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFolderPath) || !System.IO.Directory.Exists(_selectedFolderPath))
            {
                MessageBox.Show(
                    this,
                    "Please select a folder first.",
                    "Rename Files",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (_selectedFileNames.Count == 0)
            {
                MessageBox.Show(
                    this,
                    "There are no files to rename.",
                    "Rename Files",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var duplicates = _renameExecutionService.FindDuplicateProposedNames(_proposedFileNames);

            if (duplicates.Count > 0)
            {
                MessageBox.Show(
                    this,
                    "Proposed names would create duplicates. Please adjust the configuration.\n\nDuplicate: " +
                    string.Join(", ", duplicates.Take(5)) + (duplicates.Count > 5 ? "..." : ""),
                    "Rename Files",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (_renameExecutionService.HasOverwriteConflict(_selectedFolderPath, _selectedFileNames, _proposedFileNames))
            {
                MessageBox.Show(
                    this,
                    "Proposed names would overwrite existing files. Please adjust the configuration.",
                    "Rename Files",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var countToRename = _renameExecutionService.CountFilesNeedingRename(_selectedFileNames, _proposedFileNames);

            var confirm = MessageBox.Show(
                this,
                countToRename == 0
                    ? "No files need renaming (selected names match proposed names)."
                    : $"{countToRename} file(s) will be renamed if you proceed. Do you want to continue?",
                "Rename Files",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            if (countToRename == 0)
            {
                LoadFiles(_selectedFolderPath);
                return;
            }

            var runTimeUtc = DateTime.UtcNow;
            var runTimeLocal = DateTime.Now;
            var executionResult = _renameExecutionService.ExecuteRenames(_selectedFolderPath, _selectedFileNames, _proposedFileNames);

            var options = GetCurrentRenamingOptions();
            var config = new RenameLogConfiguration(
                options.FilterEnabled,
                options.FilterText,
                options.PrependEnabled,
                options.PrependText,
                options.AppendEnabled,
                options.AppendText,
                options.ReplaceEnabled,
                options.ReplaceFind,
                options.ReplaceWith);

            var logWriteResult = _renameLogWriter.WriteLog(
                _selectedFolderPath,
                config,
                executionResult,
                runTimeUtc,
                runTimeLocal);

            if (!string.IsNullOrWhiteSpace(logWriteResult.ErrorMessage))
            {
                MessageBox.Show(
                    this,
                    $"Renames completed, but the log file could not be written.\n\n{logWriteResult.ErrorMessage}",
                    "Log File Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            if (executionResult.Errors.Count > 0)
            {
                MessageBox.Show(
                    this,
                    $"{executionResult.RenamedCount} file(s) renamed successfully. {executionResult.Errors.Count} failed.\n\nLog saved as: {logWriteResult.LogFileName}\n\nErrors:\n" +
                    string.Join("\n", executionResult.Errors.Take(10)) + (executionResult.Errors.Count > 10 ? "\n..." : ""),
                    "Rename Files",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show(
                    this,
                    $"{executionResult.RenamedCount} file(s) renamed successfully.\n\nLog saved as: {logWriteResult.LogFileName}",
                    "Rename Files",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            ResetRenamingConfigurationToDefaults();

            LoadFiles(_selectedFolderPath);
        }
    }
}
