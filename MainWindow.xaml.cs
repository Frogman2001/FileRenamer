using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;

namespace FileRenamer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<FileItem> _files = new();
        private readonly ObservableCollection<string> _selectedFileNames = new();
        private readonly ObservableCollection<string> _proposedFileNames = new();
        private int _currentIndex = -1;
        private int _previewRotationDegrees;
        private string _selectedFolderPath = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
            FilesListView.ItemsSource = _files;
            SelectedFilesListBox.ItemsSource = _selectedFileNames;
            ProposedNamesListBox.ItemsSource = _proposedFileNames;
        }

        private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new WinForms.FolderBrowserDialog
            {
                Description = "Select the folder that contains the files you want to rename.",
                ShowNewFolderButton = false
            };

            var result = dialog.ShowDialog();

            if (result == WinForms.DialogResult.OK &&
                !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                SetSelectedFolderAndLoad(dialog.SelectedPath);
            }
        }

        private void SelectByFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select a file in the folder you want",
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false,
                Filter = "All files (*.*)|*.*"
            };

            if (!string.IsNullOrWhiteSpace(_selectedFolderPath) && Directory.Exists(_selectedFolderPath))
            {
                dialog.InitialDirectory = _selectedFolderPath;
            }

            var result = dialog.ShowDialog(this);
            if (result != true || string.IsNullOrWhiteSpace(dialog.FileName))
            {
                return;
            }

            var folderPath = Path.GetDirectoryName(dialog.FileName);
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return;
            }

            SetSelectedFolderAndLoad(folderPath);
        }

        private void SetSelectedFolderAndLoad(string folderPath)
        {
            _selectedFolderPath = folderPath;
            SelectedFolderTextBlock.Text = folderPath;
            LoadFiles(folderPath);
            RefreshRenamingLists();
        }

        private void LoadFiles(string folderPath)
        {
            _files.Clear();
            _currentIndex = -1;
            _selectedFileNames.Clear();
            _proposedFileNames.Clear();
            ClearPreview();

            if (!Directory.Exists(folderPath))
            {
                return;
            }

            try
            {
                var files = Directory
                    .GetFiles(folderPath)
                    .OrderBy(f => f, StringComparer.CurrentCultureIgnoreCase);

                foreach (var file in files)
                {
                    _files.Add(new FileItem
                    {
                        Name = Path.GetFileName(file),
                        FullPath = file
                    });
                }

                if (_files.Count > 0)
                {
                    // Defer selection until list containers exist so the row highlights and preview stay in sync.
                    Dispatcher.BeginInvoke(
                        new Action(SelectFirstFileInListAndFocus),
                        DispatcherPriority.Loaded);
                }

                RefreshRenamingLists();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Unable to read files from folder.\n\n{ex.Message}",
                    "Error loading files",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var compliment = Compliments.GetRandomCompliment();
            var message =
                $"{compliment}\n\nAre you sure you want to exit File Renamer?";

            var result = MessageBox.Show(
                this,
                message,
                "Exit File Renamer",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Close();
            }
        }

        private void FilesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilesListView.SelectedIndex < 0 || FilesListView.SelectedIndex >= _files.Count)
            {
                _currentIndex = -1;
                ClearPreview();
                return;
            }

            _currentIndex = FilesListView.SelectedIndex;
            ShowPreviewForCurrent();
        }

        private void ShowPreviewForCurrent()
        {
            if (_currentIndex < 0 || _currentIndex >= _files.Count)
            {
                ClearPreview();
                return;
            }

            var item = _files[_currentIndex];
            var extension = Path.GetExtension(item.FullPath);

            if (!string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                ClearPreview();
                return;
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(item.FullPath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                ResetPreviewRotation();
                PreviewImage.Source = bitmap;
                PreviewImage.Visibility = Visibility.Visible;
                PreviewPlaceholderTextBlock.Visibility = Visibility.Collapsed;
                SetRotateButtonsEnabled(true);
            }
            catch (Exception ex)
            {
                ClearPreview();

                MessageBox.Show(
                    this,
                    $"Unable to load image.\n\n{ex.Message}",
                    "Error loading image",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ClearPreview()
        {
            ResetPreviewRotation();
            PreviewImage.Source = null;
            PreviewImage.Visibility = Visibility.Collapsed;
            PreviewPlaceholderTextBlock.Visibility = Visibility.Visible;
            SetRotateButtonsEnabled(false);
        }

        private void ResetPreviewRotation()
        {
            _previewRotationDegrees = 0;
            PreviewRotateTransform.Angle = 0;
        }

        private void ApplyPreviewRotation()
        {
            PreviewRotateTransform.Angle = _previewRotationDegrees;
        }

        private void SetRotateButtonsEnabled(bool enabled)
        {
            RotateLeftButton.IsEnabled = enabled;
            RotateRightButton.IsEnabled = enabled;
        }

        private void RotateLeftButton_Click(object sender, RoutedEventArgs e)
        {
            _previewRotationDegrees = (_previewRotationDegrees - 90 + 360) % 360;
            ApplyPreviewRotation();
        }

        private void RotateRightButton_Click(object sender, RoutedEventArgs e)
        {
            _previewRotationDegrees = (_previewRotationDegrees + 90) % 360;
            ApplyPreviewRotation();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            MoveToOffset(1);
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            MoveToOffset(-1);
        }

        private void SelectFirstFileInListAndFocus()
        {
            if (_files.Count == 0)
            {
                return;
            }

            FilesListView.SelectedIndex = 0;
            FilesListView.ScrollIntoView(FilesListView.SelectedItem);
            FilesListView.Focus();
        }

        private void MoveToOffset(int offset)
        {
            if (_files.Count == 0)
            {
                return;
            }

            var newIndex = _currentIndex;

            if (newIndex < 0)
            {
                newIndex = 0;
            }
            else
            {
                newIndex += offset;
            }

            if (newIndex < 0 || newIndex >= _files.Count)
            {
                return;
            }

            FilesListView.SelectedIndex = newIndex;
            FilesListView.ScrollIntoView(FilesListView.SelectedItem);
        }

        private static bool IsKeyboardFocusInEditableText()
        {
            var focused = Keyboard.FocusedElement;
            return focused is TextBox or RichTextBox or PasswordBox;
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (IsKeyboardFocusInEditableText())
            {
                return;
            }

            var modifiers = Keyboard.Modifiers;

            if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control &&
                PreviewImage.Source is not null)
            {
                switch (e.Key)
                {
                    case Key.Right:
                    case Key.R when (modifiers & ModifierKeys.Shift) != ModifierKeys.Shift:
                        _previewRotationDegrees = (_previewRotationDegrees + 90) % 360;
                        ApplyPreviewRotation();
                        e.Handled = true;
                        return;

                    case Key.Left:
                    case Key.R when (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift:
                        _previewRotationDegrees = (_previewRotationDegrees - 90 + 360) % 360;
                        ApplyPreviewRotation();
                        e.Handled = true;
                        return;
                }
            }

            switch (e.Key)
            {
                case Key.Down:
                case Key.Right:
                    if (_files.Count > 0)
                    {
                        MoveToOffset(1);
                        e.Handled = true;
                    }

                    break;

                case Key.Up:
                case Key.Left:
                    if (_files.Count > 0)
                    {
                        MoveToOffset(-1);
                        e.Handled = true;
                    }

                    break;

                case Key.Delete:
                case Key.Back:
                    if (_files.Count > 0 && _currentIndex >= 0 && _currentIndex < _files.Count)
                    {
                        DeleteCurrentFileAfterConfirmation();
                        e.Handled = true;
                    }

                    break;

                case Key.Space:
                    if (FilesListView.IsKeyboardFocusWithin &&
                        _currentIndex >= 0 &&
                        _currentIndex < _files.Count)
                    {
                        var item = _files[_currentIndex];
                        item.IsChecked = !item.IsChecked;
                        e.Handled = true;
                    }

                    break;
            }
        }

        private void DeleteCurrentFileAfterConfirmation()
        {
            if (_currentIndex < 0 || _currentIndex >= _files.Count)
            {
                return;
            }

            var current = _files[_currentIndex];

            var confirm = MessageBox.Show(
                this,
                $"Are you sure you want to delete \"{current.Name}\"?",
                "Delete Current File",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var countBefore = _files.Count;
            var deleteIndices = new List<int> { _currentIndex };
            var deleteSet = new HashSet<int> { _currentIndex };

            TryDeleteFile(current);
            ApplySelectionAfterDeletion(countBefore, deleteSet, deleteIndices);
        }

        private void View_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta < 0)
            {
                MoveToOffset(1);
            }
            else if (e.Delta > 0)
            {
                MoveToOffset(-1);
            }

            e.Handled = true;
        }

        private void CheckAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var file in _files)
            {
                file.IsChecked = true;
            }
        }

        private void UncheckAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var file in _files)
            {
                file.IsChecked = false;
            }
        }

        private void DeleteCheckedButton_Click(object sender, RoutedEventArgs e)
        {
            var checkedFiles = _files.Where(f => f.IsChecked).ToList();

            if (checkedFiles.Count == 0)
            {
                MessageBox.Show(
                    this,
                    "No files are checked.",
                    "Delete Checked",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                this,
                $"Are you sure you want to delete {checkedFiles.Count} file(s)?",
                "Delete Checked Files",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var countBefore = _files.Count;
            var deleteIndices = new List<int>();
            for (var i = 0; i < countBefore; i++)
            {
                if (_files[i].IsChecked)
                {
                    deleteIndices.Add(i);
                }
            }

            var deleteSet = new HashSet<int>(deleteIndices);

            foreach (var item in checkedFiles)
            {
                TryDeleteFile(item);
            }

            ApplySelectionAfterDeletion(countBefore, deleteSet, deleteIndices);
        }

        private void DeleteCurrentButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteCurrentFileAfterConfirmation();
        }

        private void TryDeleteFile(FileItem item)
        {
            try
            {
                if (File.Exists(item.FullPath))
                {
                    File.Delete(item.FullPath);
                }

                _files.Remove(item);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Unable to delete \"{item.Name}\".\n\n{ex.Message}",
                    "Error deleting file",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Picks which original (pre-removal) index should be selected: the first item after the
        /// last removed index, or if none, the last remaining item before that block.
        /// </summary>
        private static int? GetOriginalIndexToSelectAfterRemoval(
            int countBefore,
            HashSet<int> deleteSet,
            IReadOnlyList<int> sortedDeleteIndices)
        {
            if (sortedDeleteIndices.Count == 0)
            {
                return null;
            }

            var lastDel = sortedDeleteIndices[sortedDeleteIndices.Count - 1];

            for (var j = lastDel + 1; j < countBefore; j++)
            {
                if (!deleteSet.Contains(j))
                {
                    return j;
                }
            }

            for (var j = lastDel - 1; j >= 0; j--)
            {
                if (!deleteSet.Contains(j))
                {
                    return j;
                }
            }

            return null;
        }

        private void ApplySelectionAfterDeletion(
            int countBefore,
            HashSet<int> deleteSet,
            IReadOnlyList<int> sortedDeleteIndices)
        {
            if (_files.Count == 0)
            {
                _currentIndex = -1;
                ClearPreview();
                return;
            }

            var original = GetOriginalIndexToSelectAfterRemoval(countBefore, deleteSet, sortedDeleteIndices);
            if (!original.HasValue)
            {
                _currentIndex = 0;
            }
            else
            {
                var removedBefore = 0;
                foreach (var d in sortedDeleteIndices)
                {
                    if (d < original.Value)
                    {
                        removedBefore++;
                    }
                }

                _currentIndex = original.Value - removedBefore;
            }

            _currentIndex = Math.Clamp(_currentIndex, 0, _files.Count - 1);
            FilesListView.SelectedIndex = _currentIndex;
            FilesListView.ScrollIntoView(FilesListView.SelectedItem);
        }

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

            var filterEnabled = FilterEnabledCheckBox.IsChecked == true;
            var filterText = FilterTextTextBox.Text ?? string.Empty;

            foreach (var item in _files)
            {
                if (filterEnabled && !string.IsNullOrEmpty(filterText))
                {
                    if (item.Name.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }
                }

                var proposedName = GetProposedFileName(item.Name);
                _selectedFileNames.Add(item.Name);
                _proposedFileNames.Add(proposedName);
            }
        }

        private string GetProposedFileName(string fileName)
        {
            var name = fileName;

            if (PrependEnabledCheckBox.IsChecked == true)
            {
                var prepend = PrependTextTextBox.Text ?? string.Empty;
                name = prepend + name;
            }

            if (ReplaceEnabledCheckBox.IsChecked == true)
            {
                var find = ReplaceFindTextBox.Text ?? string.Empty;
                var replace = ReplaceWithTextBox.Text ?? string.Empty;
                if (!string.IsNullOrEmpty(find))
                {
                    name = name.Replace(find, replace);
                }
            }

            if (AppendEnabledCheckBox.IsChecked == true)
            {
                var append = AppendTextTextBox.Text ?? string.Empty;
                var extension = Path.GetExtension(name);
                var nameWithoutExt = name.Length > 0 && extension.Length > 0
                    ? name.Substring(0, name.Length - extension.Length)
                    : name;
                name = nameWithoutExt + append + extension;
            }

            return name;
        }

        private void RenameFilesButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFolderPath) || !Directory.Exists(_selectedFolderPath))
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

            var duplicates = _proposedFileNames
                .GroupBy(n => n, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

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

            var sourcePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in _selectedFileNames)
            {
                sourcePaths.Add(Path.Combine(_selectedFolderPath, name));
            }

            for (var i = 0; i < _selectedFileNames.Count; i++)
            {
                var proposed = _proposedFileNames[i];
                var destPath = Path.Combine(_selectedFolderPath, proposed);
                if (File.Exists(destPath) && !sourcePaths.Contains(destPath))
                {
                    MessageBox.Show(
                        this,
                        "Proposed names would overwrite existing files. Please adjust the configuration.",
                        "Rename Files",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }

            var countToRename = 0;
            for (var i = 0; i < _selectedFileNames.Count; i++)
            {
                if (!string.Equals(_selectedFileNames[i], _proposedFileNames[i], StringComparison.OrdinalIgnoreCase))
                {
                    countToRename++;
                }
            }

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
            var logEntries = new List<RenameLogEntry>();
            var errors = new List<string>();
            var renamedCount = 0;

            for (var i = 0; i < _selectedFileNames.Count; i++)
            {
                var originalName = _selectedFileNames[i];
                var proposedName = _proposedFileNames[i];
                if (string.Equals(originalName, proposedName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var sourcePath = Path.Combine(_selectedFolderPath, originalName);
                var destPath = Path.Combine(_selectedFolderPath, proposedName);

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

            var config = new RenameLogConfiguration(
                FilterEnabledCheckBox.IsChecked == true,
                FilterTextTextBox.Text ?? string.Empty,
                PrependEnabledCheckBox.IsChecked == true,
                PrependTextTextBox.Text ?? string.Empty,
                AppendEnabledCheckBox.IsChecked == true,
                AppendTextTextBox.Text ?? string.Empty,
                ReplaceEnabledCheckBox.IsChecked == true,
                ReplaceFindTextBox.Text ?? string.Empty,
                ReplaceWithTextBox.Text ?? string.Empty);

            var logModel = new RenameLogFile(
                runTimeUtc,
                runTimeLocal,
                _selectedFolderPath,
                config,
                logEntries,
                logEntries.Count,
                renamedCount);

            var timestamp = runTimeLocal.ToString("yyyyMMdd_HHmmss");
            var logFileName = $"zz_FileRenamerLog{timestamp}.json";
            var logPath = Path.Combine(_selectedFolderPath, logFileName);

            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(logModel, options);
                File.WriteAllText(logPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Renames completed, but the log file could not be written.\n\n{ex.Message}",
                    "Log File Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            if (errors.Count > 0)
            {
                MessageBox.Show(
                    this,
                    $"{renamedCount} file(s) renamed successfully. {errors.Count} failed.\n\nLog saved as: {logFileName}\n\nErrors:\n" +
                    string.Join("\n", errors.Take(10)) + (errors.Count > 10 ? "\n..." : ""),
                    "Rename Files",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show(
                    this,
                    $"{renamedCount} file(s) renamed successfully.\n\nLog saved as: {logFileName}",
                    "Rename Files",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            ResetRenamingConfigurationToDefaults();

            LoadFiles(_selectedFolderPath);
        }

        private class FileItem : INotifyPropertyChanged
        {
            private bool _isChecked;

            public string Name { get; init; } = string.Empty;

            public string FullPath { get; init; } = string.Empty;

            public bool IsChecked
            {
                get => _isChecked;
                set
                {
                    if (_isChecked == value)
                    {
                        return;
                    }

                    _isChecked = value;
                    OnPropertyChanged(nameof(IsChecked));
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private sealed class RenameLogEntry
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

        private sealed class RenameLogConfiguration
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

        private sealed class RenameLogFile
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
}
