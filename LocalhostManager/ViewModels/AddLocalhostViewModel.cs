using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LocalhostManager.ViewModels
{
    public class AddLocalhostViewModel : INotifyPropertyChanged
    {
        private string _name = "";
        private string _description = "";
        private string _path = "";
        private string _command = "";
        private string _customCommand = "";
        private string _port = "";
        private string _url = "";

        private bool _isCustomCommand;
        private bool _saveCustomCommand = true;

        public string Name
        {
            get => _name;
            set
            {
                if (_name == value)
                    return;

                _name = value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (_description == value)
                    return;

                _description = value;
                OnPropertyChanged();
            }
        }

        public string Path
        {
            get => _path;
            set
            {
                if (_path == value)
                    return;

                _path = value;
                OnPropertyChanged();
            }
        }

        public string Command
        {
            get => _command;
            set
            {
                if (_command == value)
                    return;

                _command = value;
                OnPropertyChanged();
            }
        }

        public string CustomCommand
        {
            get => _customCommand;
            set
            {
                if (_customCommand == value)
                    return;

                _customCommand = value;
                OnPropertyChanged();
            }
        }

        public string Port
        {
            get => _port;
            set
            {
                if (_port == value)
                    return;

                _port = value;
                OnPropertyChanged();
            }
        }

        public string Url
        {
            get => _url;
            set
            {
                if (_url == value)
                    return;

                _url = value;
                OnPropertyChanged();
            }
        }

        public bool IsCustomCommand
        {
            get => _isCustomCommand;
            set
            {
                if (_isCustomCommand == value)
                    return;

                _isCustomCommand = value;
                OnPropertyChanged();
            }
        }

        public bool SaveCustomCommand
        {
            get => _saveCustomCommand;
            set
            {
                if (_saveCustomCommand == value)
                    return;

                _saveCustomCommand = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName)
            );
        }

        public void Clear()
        {
            Name = "";
            Description = "";
            Path = "";
            Command = "";
            CustomCommand = "";
            Port = "";
            Url = "";
            IsCustomCommand = false;
            SaveCustomCommand = true;
        }
    }
}