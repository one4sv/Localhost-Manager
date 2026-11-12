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
        private string _status = "Остановлен";
        public string Status { get => _status; private set { _status = value; OnPropertyChanged(); } }
        public void SetStatus(string status) => Status = status;
        public void RefreshDetails() => OnPropertyChanged(string.Empty);
        public void AddSystemLog(string text) => _pendingLogs.Enqueue($"[{DateTime.Now:HH:mm:ss}] {text}");

        public event EventHandler? Crashed;
        private bool _stoppingByUser;
        public const int MaxLogLines = 2000;
        private readonly System.Collections.Concurrent.ConcurrentQueue<string> _pendingLogs = new();
        private readonly System.Windows.Threading.DispatcherTimer _logTimer;
        private bool _isRunning;

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
            get => _isRunning;
            private set
            {
                if (_isRunning == value)
                {
                    return;
                }

                _isRunning = value;
                OnPropertyChanged();
            }
        }

        public LocalhostViewModel(
            Localhost localhost,
            IEnumerable<string> commands)
        {
            _localhost = localhost;
            _localhost.IsRunning = false;
            _logTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _logTimer.Tick += (_, _) => FlushLogs();

            Commands = new ObservableCollection<string>(
                commands
            );
        }

        public void SetProcess(Process process)
        {
            _process = process;
            _stoppingByUser = false;
            IsRunning = true;
            Status = "Запускается";
            _logTimer.Start();
            process.OutputDataReceived += Process_OutputDataReceived;
            process.ErrorDataReceived += Process_ErrorDataReceived;
            process.Exited += Process_Exited;
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            // A short command may have exited before the handler was attached.
            if (process.HasExited) Process_Exited(process, EventArgs.Empty);
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            _pendingLogs.Enqueue($"[{DateTime.Now:HH:mm:ss}] {e.Data}");
            while (_pendingLogs.Count > MaxLogLines) _pendingLogs.TryDequeue(out _);
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            _pendingLogs.Enqueue($"[{DateTime.Now:HH:mm:ss}] [stderr] {e.Data}");
            while (_pendingLogs.Count > MaxLogLines) _pendingLogs.TryDequeue(out _);
        }

        private void FlushLogs()
        {
            // Bound each UI update even if a service writes continuously.
            for (var count = 0; count < MaxLogLines && _pendingLogs.TryDequeue(out var line); count++)
            {
                Logs.Add(line);
                if (Logs.Count > MaxLogLines) Logs.RemoveAt(0);
            }
        }

        private void Process_Exited(object? sender, EventArgs e)
        {
            if (sender is not Process exited) return;
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.HasShutdownStarted) return;
            dispatcher.BeginInvoke(new Action(() =>
            {
                // An old process must never clear a replacement process's state.
                if (!ReferenceEquals(_process, exited)) return;
                var crashed = !_stoppingByUser && exited.ExitCode != 0;
                // Drain redirected output on a worker: never block the UI dispatcher.
                _ = FinishExitedProcessAsync(exited, crashed);
            }));
        }

        private async System.Threading.Tasks.Task FinishExitedProcessAsync(Process exited, bool crashed)
        {
            try { await System.Threading.Tasks.Task.Run(() => exited.WaitForExit()); }
            catch (InvalidOperationException) { }
            if (!ReferenceEquals(_process, exited)) return;
            SetStopped();
            if (crashed)
            {
                Status = "Ошибка";
                Crashed?.Invoke(this, EventArgs.Empty);
            }
        }

        public Process? GetProcess() => _process;
        public void CancelStopping() => _stoppingByUser = false;

        public void SetStopped()
        {
            var process = _process;
            _process = null;
            if (process != null)
            {
                process.Exited -= Process_Exited;
                process.OutputDataReceived -= Process_OutputDataReceived;
                process.ErrorDataReceived -= Process_ErrorDataReceived;
                process.Dispose();
            }
            _logTimer.Stop();
            FlushLogs();
            _stoppingByUser = false;
            IsRunning = false;
            Status = "Остановлен";
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
