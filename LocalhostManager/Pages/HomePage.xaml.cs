using LocalhostManager.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Application = System.Windows.Application;

namespace LocalhostManager.Pages
{
    public partial class HomePage : Page
    {
        private MainWindow MainWindow =>
            (MainWindow)Application.Current.MainWindow;

        private System.Windows.Data.ListCollectionView? _view;
        private readonly System.Collections.Generic.HashSet<LocalhostManager.ViewModels.ProjectViewModel> _observed = [];

        public HomePage()
        {
            InitializeComponent();

            DataContext = MainWindow;
            _view = new System.Windows.Data.ListCollectionView(MainWindow.Projects);
            _view.Filter = item => item is LocalhostManager.ViewModels.ProjectViewModel project &&
                (project.Name.Contains(SearchBox.Text, StringComparison.OrdinalIgnoreCase) ||
                 (project.Description?.Contains(SearchBox.Text, StringComparison.OrdinalIgnoreCase) ?? false));
            ProjectList.ItemsSource = _view;
            Loaded += (_, _) =>
            {
                MainWindow.Projects.CollectionChanged += Projects_Changed;
                ObserveProjects();
            };
            Unloaded += (_, _) =>
            {
                MainWindow.Projects.CollectionChanged -= Projects_Changed;
                foreach (var project in _observed)
                {
                    project.PropertyChanged -= Project_Changed;
                    project.Localhosts.CollectionChanged -= Projects_Changed;
                }
                _observed.Clear();
            };
            RefreshSummary();
        }

        private void ObserveProjects()
        {
            foreach (var project in _observed)
            {
                project.PropertyChanged -= Project_Changed;
                project.Localhosts.CollectionChanged -= Projects_Changed;
            }
            _observed.Clear();
            foreach (var project in MainWindow.Projects)
            {
                project.PropertyChanged += Project_Changed;
                project.Localhosts.CollectionChanged += Projects_Changed;
                _observed.Add(project);
            }
            RefreshSummary();
        }

        private void Projects_Changed(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => ObserveProjects();
        private void Project_Changed(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => RefreshSummary();
        private void Search_Changed(object sender, TextChangedEventArgs e) => RefreshSummary();

        private void RefreshSummary()
        {
            if (_view == null) return;
            var selected = ProjectList.SelectedItem;
            _view.Refresh();
            if (selected != null && _view.Contains(selected)) ProjectList.SelectedItem = selected;
            if (ProjectList.SelectedIndex < 0 && !_view.IsEmpty) ProjectList.SelectedIndex = 0;
            ProjectCount.Text = MainWindow.Projects.Count.ToString();
            ServiceCount.Text = MainWindow.Projects.Sum(p => p.Localhosts.Count).ToString();
            RunningCount.Text = MainWindow.Projects.Sum(p => p.Localhosts.Count(l => l.IsRunning)).ToString();
            EmptyState.Visibility = MainWindow.Projects.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            NoResults.Visibility = MainWindow.Projects.Count > 0 && _view.IsEmpty ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OpenConsole_Click(object sender, RoutedEventArgs e)
        {
            if (LogService.SelectedItem is LocalhostManager.ViewModels.LocalhostViewModel service)
                new LogsWindow(service) { Owner = MainWindow }.Show();
        }
        private void Settings_Click(
            object sender,
            RoutedEventArgs e)
        {
            MainWindow.NavigateSettings();
        }

        private void AddProject_Click(
            object sender,
            RoutedEventArgs e)
        {
            ProjectForm.Visibility = Visibility.Visible;
        }

        private void ProjectForm_ProjectCreated(
            object sender,
            Project project)
        {
            MainWindow.AddProject(project);

            ProjectForm.Visibility = Visibility.Collapsed;
        }

        private void ProjectForm_Cancelled(
            object sender,
            EventArgs e)
        {
            ProjectForm.Visibility = Visibility.Collapsed;
        }
    }
}