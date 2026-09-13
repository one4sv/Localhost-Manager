using LocalhostManager.ViewModels;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;

namespace LocalhostManager.Controls
{
    public partial class LocalhostExpander : UserControl
    {
        private MainWindow MainWindow =>
            (MainWindow)Application.Current.MainWindow;

        private LocalhostViewModel? Localhost =>
            DataContext as LocalhostViewModel;

        public LocalhostExpander()
        {
            InitializeComponent();
        }

        private void StartLocalhost_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (Localhost == null)
            {
                return;
            }

            MainWindow.ProjectManager.StartLocalhost(Localhost);

            e.Handled = true;
        }

        private void StopLocalhost_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (Localhost == null)
            {
                return;
            }

            MainWindow.ProjectManager.StopLocalhost(Localhost);

            e.Handled = true;
        }

        private void RestartLocalhost_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (Localhost == null)
            {
                return;
            }

            MainWindow.ProjectManager.RestartLocalhost(Localhost);

            e.Handled = true;
        }

        private void OpenLocalhost_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (Localhost == null)
            {
                return;
            }

            var url = Localhost.Url?.Trim();

            if (string.IsNullOrWhiteSpace(url))
            {
                if (!string.IsNullOrWhiteSpace(Localhost.Port))
                {
                    url = $"http://localhost:{Localhost.Port.Trim()}";
                }
            }
            else if (!url.StartsWith("http://",
                         StringComparison.OrdinalIgnoreCase) &&
                     !url.StartsWith("https://",
                         StringComparison.OrdinalIgnoreCase))
            {
                url = "http://" + url;
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show(
                    "У localhost не задан URL или порт.",
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
                    $"Не удалось открыть адрес.\n\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            e.Handled = true;
        }

        private void ShowLogs_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (Localhost == null)
            {
                return;
            }

            var window = new LogsWindow(Localhost)
            {
                Owner = MainWindow
            };

            window.Show();

            e.Handled = true;
        }

        private void EditLocalhost_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (Localhost == null)
            {
                return;
            }

            var parent = VisualTreeHelper.GetParent(this);

            while (parent != null &&
                   parent is not ProjectExpander)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            if (parent is ProjectExpander projectExpander)
            {
                projectExpander.EditLocalhost(Localhost);
            }

            e.Handled = true;
        }

        private void DeleteLocalhost_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (Localhost == null)
            {
                return;
            }

            var result = MessageBox.Show(
                $"Удалить localhost «{Localhost.Name}»?",
                "Удаление localhost",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                e.Handled = true;
                return;
            }

            var parent = VisualTreeHelper.GetParent(this);

            while (parent != null &&
                   parent is not ProjectExpander)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            if (parent is ProjectExpander projectExpander)
            {
                projectExpander.DeleteLocalhost(Localhost);
            }

            e.Handled = true;
        }
    }
}