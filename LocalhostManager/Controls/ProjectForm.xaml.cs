using LocalhostManager.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;

namespace LocalhostManager.Controls
{
    public partial class ProjectForm : UserControl
    {
        private Project? _editingProject;

        public event EventHandler<Project>? ProjectCreated;
        public event EventHandler<Project>? ProjectEdited;
        public event EventHandler? Cancelled;

        public ProjectForm()
        {
            InitializeComponent();
        }

        public void Edit(Project project)
        {
            _editingProject = project;

            ProjectNameTextBox.Text = project.Name;
            ProjectDescriptionTextBox.Text =
                project.Description ?? "";

            SaveButton.Content = "Сохранить";
        }

        private void Save_Click(
            object sender,
            RoutedEventArgs e)
        {
            var name = ProjectNameTextBox.Text.Trim();
            var description = ProjectDescriptionTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(
                    "Введите название проекта.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                ProjectNameTextBox.Focus();

                return;
            }

            if (_editingProject != null)
            {
                _editingProject.Name = name;

                _editingProject.Description =
                    string.IsNullOrWhiteSpace(description)
                        ? null
                        : description;

                ProjectEdited?.Invoke(
                    this,
                    _editingProject);
            }
            else
            {
                var project = new Project
                {
                    Name = name,
                    Description =
                        string.IsNullOrWhiteSpace(description)
                            ? null
                            : description
                };

                ProjectCreated?.Invoke(
                    this,
                    project);
            }

            Clear();
        }

        private void Cancel_Click(
            object sender,
            RoutedEventArgs e)
        {
            Clear();

            Cancelled?.Invoke(
                this,
                EventArgs.Empty);
        }

        public void Clear()
        {
            _editingProject = null;

            ProjectNameTextBox.Clear();
            ProjectDescriptionTextBox.Clear();

            SaveButton.Content =
                Application.Current.TryFindResource(
                    _editingProject != null ? "Save" : "Add")
                ?.ToString()
                ?? (_editingProject != null ? "Сохранить" : "Добавить");
        }
        public void RefreshLanguage()
        {
            var editing = _editingProject != null;

            SaveButton.Content =
                Application.Current.TryFindResource(
                    editing ? "Save" : "Add")
                ?.ToString()
                ?? (editing ? "Сохранить" : "Добавить");
        }
    }
}