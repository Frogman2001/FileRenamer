using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

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
        private readonly RenameProposalService _renameProposalService = new();
        private readonly RenameExecutionService _renameExecutionService = new();
        private readonly RenameLogWriter _renameLogWriter = new();
        private readonly ImageRotationConfigStore _imageRotationConfigStore = new();
        private const double PreviewZoomStep = 0.10;
        private const double PreviewZoomMin = 0.10;
        private const double PreviewZoomMax = 6.00;
        private int _currentIndex = -1;
        private int _previewRotationDegrees;
        private double _previewScale = 1.0;
        private bool _isDraggingPreview;
        private Point _previewDragStart;
        private Point _previewTranslateStart;
        private string _selectedFolderPath = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
            FilesListView.ItemsSource = _files;
            SelectedFilesListBox.ItemsSource = _selectedFileNames;
            ProposedNamesListBox.ItemsSource = _proposedFileNames;
            _files.CollectionChanged += (_, _) => UpdateDisplayedFileCount();
            UpdateDisplayedFileCount();
        }

        private void UpdateDisplayedFileCount()
        {
            DisplayedFileCountTextBlock.Text = $"Files displayed: {_files.Count}";
        }
    }
}
