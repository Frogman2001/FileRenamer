using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace FileRenamer
{
    public partial class MainWindow
    {
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

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowKeyboardShortcuts();
        }

        private void ShowKeyboardShortcuts()
        {
            const string shortcuts =
                "Keyboard Shortcuts\n\n" +
                "Navigation\n" +
                "- Right Arrow / Down Arrow: Select next file\n" +
                "- Left Arrow / Up Arrow: Select previous file\n\n" +
                "Selection\n" +
                "- Space: Toggle check/uncheck on selected file (when file list is focused)\n\n" +
                "Preview Actions\n" +
                "- Delete / Backspace: Delete selected file\n" +
                "- Ctrl+Right Arrow or Ctrl+R: Rotate preview right\n" +
                "- Ctrl+Left Arrow or Ctrl+Shift+R: Rotate preview left\n" +
                "- Ctrl+Up Arrow: Zoom in (10%)\n" +
                "- Ctrl+Down Arrow: Zoom out (10%)\n" +
                "- Ctrl+Mouse Wheel: Zoom in/out (10% steps)\n" +
                "- Click and drag in preview: Pan image\n\n" +
                "Help\n" +
                "- F1: Show this keyboard shortcuts help";

            MessageBox.Show(
                this,
                shortcuts,
                "Keyboard Shortcuts",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
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

                var savedRotation = _imageRotationConfigStore.TryGetRotation(item.FullPath);
                _previewRotationDegrees = savedRotation ?? 0;
                PreviewImage.Source = bitmap;
                ResetPreviewViewTransform();
                ApplyPreviewRotation();
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
            ResetPreviewViewTransform();
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
            RotateCurrentPreviewByOffset(-90);
        }

        private void RotateRightButton_Click(object sender, RoutedEventArgs e)
        {
            RotateCurrentPreviewByOffset(90);
        }

        private void RotateCurrentPreviewByOffset(int offset)
        {
            if (_currentIndex < 0 || _currentIndex >= _files.Count || PreviewImage.Source is null)
            {
                return;
            }

            _previewRotationDegrees = (_previewRotationDegrees + offset + 360) % 360;
            ApplyPreviewRotation();
            _imageRotationConfigStore.SaveRotation(_files[_currentIndex].FullPath, _previewRotationDegrees);
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

        private static bool IsKeyboardFocusInEditableText()
        {
            var focused = Keyboard.FocusedElement;
            return focused is TextBox or RichTextBox or PasswordBox;
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1)
            {
                ShowKeyboardShortcuts();
                e.Handled = true;
                return;
            }

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
                    case Key.Up:
                        ZoomPreviewByStep(zoomIn: true);
                        e.Handled = true;
                        return;

                    case Key.Down:
                        ZoomPreviewByStep(zoomIn: false);
                        e.Handled = true;
                        return;

                    case Key.Right:
                    case Key.R when (modifiers & ModifierKeys.Shift) != ModifierKeys.Shift:
                        RotateCurrentPreviewByOffset(90);
                        e.Handled = true;
                        return;

                    case Key.Left:
                    case Key.R when (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift:
                        RotateCurrentPreviewByOffset(-90);
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

        private void View_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control &&
                PreviewImage.Source is not null)
            {
                ZoomPreviewByStep(e.Delta > 0);
                e.Handled = true;
                return;
            }

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

        private void ZoomPreviewByStep(bool zoomIn)
        {
            if (PreviewImage.Source is null)
            {
                return;
            }

            var scaleFactor = zoomIn ? (1.0 + PreviewZoomStep) : (1.0 - PreviewZoomStep);
            var nextScale = _previewScale * scaleFactor;
            _previewScale = Math.Clamp(nextScale, PreviewZoomMin, PreviewZoomMax);
            ApplyPreviewScale();
        }

        private void ResetPreviewViewTransform()
        {
            _previewScale = 1.0;
            ApplyPreviewScale();
            PreviewTranslateTransform.X = 0;
            PreviewTranslateTransform.Y = 0;
            _isDraggingPreview = false;
            PreviewArea.ReleaseMouseCapture();
            PreviewArea.Cursor = Cursors.Arrow;
        }

        private void ApplyPreviewScale()
        {
            PreviewScaleTransform.ScaleX = _previewScale;
            PreviewScaleTransform.ScaleY = _previewScale;
        }

        private void PreviewArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (PreviewImage.Source is null)
            {
                return;
            }

            _isDraggingPreview = true;
            _previewDragStart = e.GetPosition(PreviewArea);
            _previewTranslateStart = new Point(PreviewTranslateTransform.X, PreviewTranslateTransform.Y);
            PreviewArea.CaptureMouse();
            PreviewArea.Cursor = Cursors.SizeAll;
            e.Handled = true;
        }

        private void PreviewArea_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingPreview)
            {
                return;
            }

            var currentPosition = e.GetPosition(PreviewArea);
            var dragDelta = currentPosition - _previewDragStart;
            PreviewTranslateTransform.X = _previewTranslateStart.X + dragDelta.X;
            PreviewTranslateTransform.Y = _previewTranslateStart.Y + dragDelta.Y;
            e.Handled = true;
        }

        private void PreviewArea_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDraggingPreview)
            {
                return;
            }

            _isDraggingPreview = false;
            PreviewArea.ReleaseMouseCapture();
            PreviewArea.Cursor = Cursors.Arrow;
            e.Handled = true;
        }
    }
}
