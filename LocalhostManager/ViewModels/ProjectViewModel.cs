using LocalhostManager.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;

namespace LocalhostManager.ViewModels
{
    public class ProjectViewModel : INotifyPropertyChanged
    {
        private readonly Project _project;
        private List<string> _commands;

        private bool _isAddingLocalhost;

        public Project Project => _project;

        public AddLocalhostViewModel LocalhostForm { get; } = new();

        public bool IsAddingLocalhost
        {
            get => _isAddingLocalhost;
            set
            {
                if (_isAddingLocalhost == value)
                {
                    return;
                }

                _isAddingLocalhost = value;
                OnPropertyChanged();
            }
        }

        public bool IsRunning =>
            Localhosts.Any(localhost => localhost.IsRunning);

        public string Name
        {
            get => _project.Name;
            set
            {
                if (_project.Name == value)
                {
                    return;
                }

                _project.Name = value;
                OnPropertyChanged();
            }
        }

        public string? Description
        {
            get => _project.Description;
            set
            {
                if (_project.Description == value)
                {
                    return;
                }

                _project.Description = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<LocalhostViewModel> Localhosts { get; }

        public ProjectViewModel(
            Project project,
            IEnumerable<string> commands)
        {
            _project = project;
            _commands = commands.ToList();

            Localhosts =
                new ObservableCollection<LocalhostViewModel>();

            foreach (var localhost in project.Localhosts)
            {
                AddViewModel(localhost);
            }
        }

        private LocalhostViewModel AddViewModel(
            Localhost localhost)
        {
            var viewModel =
                new LocalhostViewModel(
                    localhost,
                    _commands);

            viewModel.PropertyChanged +=
                Localhost_PropertyChanged;

            Localhosts.Add(viewModel);

            return viewModel;
        }

        private void Localhost_PropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName ==
                nameof(LocalhostViewModel.IsRunning))
            {
                OnPropertyChanged(
                    nameof(IsRunning));
            }
        }

        public LocalhostViewModel AddLocalhost(
            Localhost localhost)
        {
            _project.Localhosts.Add(localhost);

            var viewModel =
                AddViewModel(localhost);

            LocalhostForm.Clear();

            IsAddingLocalhost = false;

            return viewModel;
        }

        public void RemoveLocalhost(LocalhostViewModel localhost)
        {
            localhost.PropertyChanged -= Localhost_PropertyChanged;

            _project.Localhosts.Remove(localhost.Localhost);
            Localhosts.Remove(localhost);

            OnPropertyChanged(nameof(IsRunning));
        }

        public void RefreshCommands(
            IEnumerable<string> commands)
        {
            _commands =
                commands.ToList();

            foreach (var localhost in Localhosts)
            {
                localhost.RefreshCommands(
                    _commands);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(
                    propertyName));
        }
    }
}