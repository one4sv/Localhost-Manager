using LocalhostManager.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using Application = System.Windows.Application;

namespace LocalhostManager.Pages
{
    public partial class HomePage : Page
    {
        private MainWindow MainWindow =>
            (MainWindow)Application.Current.MainWindow;

        public HomePage()
        {
            InitializeComponent();

            DataContext = MainWindow;
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