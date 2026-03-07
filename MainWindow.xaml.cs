using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
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
                _selectedFolderPath = dialog.SelectedPath;
                SelectedFolderTextBlock.Text = dialog.SelectedPath;
                LoadFiles(dialog.SelectedPath);
                RefreshRenamingLists();
            }
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
                    FilesListView.SelectedIndex = 0;
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

                PreviewImage.Source = bitmap;
                PreviewImage.Visibility = Visibility.Visible;
                PreviewPlaceholderTextBlock.Visibility = Visibility.Collapsed;
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
            PreviewImage.Source = null;
            PreviewImage.Visibility = Visibility.Collapsed;
            PreviewPlaceholderTextBlock.Visibility = Visibility.Visible;
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            MoveToOffset(1);
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            MoveToOffset(-1);
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

            foreach (var item in checkedFiles)
            {
                TryDeleteFile(item);
            }

            RebuildSelectionAfterDeletion();
        }

        private void DeleteCurrentButton_Click(object sender, RoutedEventArgs e)
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

            TryDeleteFile(current);
            RebuildSelectionAfterDeletion();
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

        private void RebuildSelectionAfterDeletion()
        {
            if (_files.Count == 0)
            {
                _currentIndex = -1;
                ClearPreview();
                return;
            }

            if (_currentIndex >= _files.Count)
            {
                _currentIndex = _files.Count - 1;
            }

            if (_currentIndex < 0)
            {
                _currentIndex = 0;
            }

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

            var confirm = MessageBox.Show(
                this,
                $"Rename {_selectedFileNames.Count} file(s)?",
                "Rename Files",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var errors = new List<string>();
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
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"{originalName}: {ex.Message}");
                }
            }

            if (errors.Count > 0)
            {
                MessageBox.Show(
                    this,
                    "Some renames failed:\n\n" + string.Join("\n", errors.Take(10)) +
                    (errors.Count > 10 ? "\n..." : ""),
                    "Rename Files",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

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
    }
}
