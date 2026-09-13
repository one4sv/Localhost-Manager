using LocalhostManager.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using Application = System.Windows.Application;

namespace LocalhostManager.ViewModels
{
    public class LocalhostViewModel : INotifyPropertyChanged
    {
        private readonly Localhost _localhost;
        private Process? _process;

        public Localhost Localhost => _localhost;

        public event EventHandler? Crashed;
        private bool _stoppingByUser;

        public ObservableCollection<string> Commands { get; }

        public ObservableCollection<string> Logs { get; } = [];
        public void MarkStopping()
        {
            _stoppingByUser = true;
        }
        public string Name
        {
            get => _localhost.Name;
            set
            {
                if (_localhost.Name == value)
                {
                    return;
                }

                _localhost.Name = value;
                OnPropertyChanged();
            }
        }

        public string? Description
        {
            get => _localhost.Description;
            set
            {
                if (_localhost.Description == value)
                {
                    return;
                }

                _localhost.Description = value;
                OnPropertyChanged();
            }
        }

        public string? Command
        {
            get => _localhost.Command;
            set
            {
                if (_localhost.Command == value)
                {
                    return;
                }

                _localhost.Command = value;
                OnPropertyChanged();
            }
        }

        public string Path
        {
            get => _localhost.Path;
            set
            {
                if (_localhost.Path == value)
                {
                    return;
                }

                _localhost.Path = value;
                OnPropertyChanged();
            }
        }

        public string? Port
        {
            get => _localhost.Port;
            set
            {
                if (_localhost.Port == value)
                {
                    return;
                }

                _localhost.Port = value;
                OnPropertyChanged();
            }
        }

        public string? Url
        {
            get => _localhost.Url;
            set
            {
                if (_localhost.Url == value)
                {
                    return;
                }

                _localhost.Url = value;
                OnPropertyChanged();
            }
        }

        public bool IsRunning
        {
            get => _localhost.IsRunning;
            private set
            {
                if (_localhost.IsRunning == value)
                {
                    return;
                }

                _localhost.IsRunning = value;
                OnPropertyChanged();
            }
        }

        public LocalhostViewModel(
            Localhost localhost,
            IEnumerable<string> commands)
        {
            _localhost = localhost;

            Commands = new ObservableCollection<string>(
                commands
            );
        }

        public void SetProcess(Process process)
        {
            _process = process;
            IsRunning = true;

            process.OutputDataReceived += Process_OutputDataReceived;
            process.ErrorDataReceived += Process_ErrorDataReceived;
            process.Exited += Process_Exited;

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        private void Process_OutputDataReceived(
            object sender,
            DataReceivedEventArgs e)
        {
            AddLog(e.Data);
        }

        private void Process_ErrorDataReceived(
            object sender,
            DataReceivedEventArgs e)
        {
            AddLog(e.Data);
        }

        private void AddLog(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                Logs.Add(text);
            });
        }

        private void Process_Exited(
            object? sender,
            EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var process = _process;

                int? exitCode = null;

                try
                {
                    if (process != null)
                    {
                        exitCode = process.ExitCode;
                    }
                }
                catch
                {
                }

                var crashed =
                    !_stoppingByUser &&
                    exitCode.HasValue &&
                    exitCode.Value != 0;

                IsRunning = false;
                _process = null;

                _stoppingByUser = false;

                if (crashed)
                {
                    Crashed?.Invoke(
                        this,
                        EventArgs.Empty);
                }
            });
        }

        public Process? GetProcess()
        {
            return _process;
        }

        public void SetStopped()
        {
            IsRunning = false;
            _process = null;
        }

        public void RefreshCommands(
            IEnumerable<string> commands)
        {
            var currentCommand = Command;

            Commands.Clear();

            foreach (var command in commands)
            {
                Commands.Add(command);
            }

            if (!string.IsNullOrWhiteSpace(currentCommand) &&
                !Commands.Contains(currentCommand))
            {
                Commands.Add(currentCommand);
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
    }
}