using System.ComponentModel;

namespace FileRenamer
{
    internal class FileItem : INotifyPropertyChanged
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
