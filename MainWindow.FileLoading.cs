using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using WinForms = System.Windows.Forms;

namespace FileRenamer
{
    public partial class MainWindow
    {
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
                    .Where(f => !string.Equals(Path.GetExtension(f), ".json", StringComparison.OrdinalIgnoreCase))
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
    }
}
