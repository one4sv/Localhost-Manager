using LocalhostManager.Models;
using LocalhostManager.ViewModels;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;

namespace LocalhostManager.Controls
{
    public partial class ProjectExpander : UserControl
    {
        private MainWindow MainWindow =>
            (MainWindow)Application.Current.MainWindow;

        private ProjectViewModel? Project =>
            DataContext as ProjectViewModel;

        public ProjectExpander()
        {
            InitializeComponent();
        }

        private void StartProject_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (Project == null)
            {
                return;
            }

            MainWindow.ProjectManager.StartProject(Project);

            e.Handled = true;
        }

        private void StopProject_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (Project == null)
            {
                return;
            }

            MainWindow.ProjectManager.StopProject(Project);

            e.Handled = true;
        }

        private void RestartProject_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (Project == null)
            {
                return;
            }

            MainWindow.ProjectManager.RestartProject(Project);

            e.Handled = true;
        }

        private void EditProject_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (Project == null)
            {
                return;
            }

            ProjectExpanderControl.IsExpanded = true;

            ProjectForm.Edit(Project.Project);
            ProjectForm.Visibility = Visibility.Visible;

            e.Handled = true;
        }

        private void DeleteProject_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (Project == null)
            {
                return;
            }

            var result = MessageBox.Show(
                $"Удалить проект «{Project.Name}»?\n\nВсе localhost этого проекта будут удалены из Localhost Manager.",
                "Удаление проекта",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            MainWindow.RemoveProject(Project);

            e.Handled = true;
        }

        private void ProjectForm_ProjectCreated(
            object sender,
            Project project)
        {
            MainWindow.AddProject(project);

            ProjectForm.Visibility = Visibility.Collapsed;
        }

        private void ProjectForm_ProjectEdited(
            object sender,
            Project project)
        {
            MainWindow.SaveProjects();

            ProjectForm.Visibility = Visibility.Collapsed;
        }

        private void ProjectForm_Cancelled(
            object sender,
            EventArgs e)
        {
            ProjectForm.Visibility = Visibility.Collapsed;
        }

        private void AddLocalhost_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (Project == null)
            {
                return;
            }

            Project.IsAddingLocalhost = true;
        }

        private void StartLocalhost_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element)
            {
                return;
            }

            if (element.DataContext is not LocalhostViewModel localhost)
            {
                return;
            }

            MainWindow.ProjectManager.StartLocalhost(
                localhost);

            e.Handled = true;
        }

        private void StopLocalhost_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element)
            {
                return;
            }

            if (element.DataContext is not LocalhostViewModel localhost)
            {
                return;
            }

            MainWindow.ProjectManager.StopLocalhost(
                localhost);

            e.Handled = true;
        }

        private void RestartLocalhost_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element)
            {
                return;
            }

            if (element.DataContext is not LocalhostViewModel localhost)
            {
                return;
            }

            MainWindow.ProjectManager.RestartLocalhost(
                localhost);

            e.Handled = true;
        }

        private void ShowLogs_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element)
            {
                return;
            }

            if (element.DataContext is not LocalhostViewModel localhost)
            {
                return;
            }

            var window = new LogsWindow(localhost)
            {
                Owner = MainWindow
            };

            window.Show();

            e.Handled = true;
        }

        private void OpenLocalhost_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element)
            {
                return;
            }

            if (element.DataContext is not LocalhostViewModel localhost)
            {
                return;
            }

            var url = GetLocalhostUrl(localhost);

            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show(
                    "У localhost не задан URL и порт.",
                    "Невозможно открыть",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            try
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось открыть страницу.\n\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            e.Handled = true;
        }

        private string? GetLocalhostUrl(
            LocalhostViewModel localhost)
        {
            if (!string.IsNullOrWhiteSpace(localhost.Url))
            {
                var url = localhost.Url.Trim();

                if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    url = "http://" + url;
                }

                return url;
            }

            if (!string.IsNullOrWhiteSpace(localhost.Port))
            {
                return $"http://localhost:{localhost.Port.Trim()}";
            }

            return null;
        }
        public void EditLocalhost(LocalhostViewModel localhost)
        {
            if (DataContext is not ProjectViewModel project)
            {
                return;
            }

            project.IsAddingLocalhost = true;

            LocalhostForm.Edit(localhost.Localhost);
        }

        public void DeleteLocalhost(LocalhostViewModel localhost)
        {
            if (DataContext is not ProjectViewModel project)
            {
                return;
            }

            MainWindow.ProjectManager.StopLocalhost(localhost);

            project.RemoveLocalhost(localhost);

            MainWindow.SaveProjects();
        }

        private void LocalhostForm_LocalhostEdited(
            object sender,
            Localhost localhost)
        {
            if (DataContext is not ProjectViewModel project)
            {
                return;
            }

            project.IsAddingLocalhost = false;

            MainWindow.SaveProjects();
        }

        private void LocalhostForm_LocalhostCreated(
            object sender,
            Localhost localhost)
        {
            if (DataContext is not ProjectViewModel project)
            {
                return;
            }

            var viewModel =
                project.AddLocalhost(localhost);

            MainWindow.SubscribeNewLocalhost(
                viewModel);

            MainWindow.SaveProjects();
        }

        private void LocalhostForm_Cancelled(
            object sender,
            EventArgs e)
        {
            if (DataContext is ProjectViewModel project)
            {
                project.IsAddingLocalhost = false;
            }

            LocalhostForm.Clear();
        }
    }
}