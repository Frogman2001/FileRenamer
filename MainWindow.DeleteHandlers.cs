using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace FileRenamer
{
    public partial class MainWindow
    {
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
                    _imageRotationConfigStore.RemoveImage(item.FullPath);
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

            var original = SelectionAfterDeleteCalculator.GetOriginalIndexToSelectAfterRemoval(countBefore, deleteSet, sortedDeleteIndices);
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
    }
}
