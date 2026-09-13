using LocalhostManager.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using Application = System.Windows.Application;
using Forms = System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;

namespace LocalhostManager.Controls
{
    public partial class LocalhostForm : UserControl
    {
        private Localhost? _editingLocalhost;

        public event EventHandler<Localhost>? LocalhostCreated;
        public event EventHandler<Localhost>? LocalhostEdited;
        public event EventHandler? Cancelled;

        private string CustomCommandText =>
            Application.Current
                .TryFindResource("AddYourOwn")
                ?.ToString()
                ?? "Добавить свою...";

        public LocalhostForm()
        {
            InitializeComponent();

            LoadCommands();
        }

        private void LoadCommands()
        {
            var mainWindow =
                (MainWindow)Application.Current.MainWindow;

            CommandComboBox.Items.Clear();

            foreach (var command in mainWindow.Settings.Commands)
            {
                CommandComboBox.Items.Add(command);
            }

            CommandComboBox.Items.Add(
                CustomCommandText);

            if (CommandComboBox.Items.Count > 0)
            {
                CommandComboBox.SelectedIndex = 0;
            }
        }

        private void CommandComboBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (CustomCommandPanel == null)
            {
                return;
            }

            if (CommandComboBox.SelectedItem is string command &&
                command == CustomCommandText)
            {
                CustomCommandPanel.Visibility =
                    Visibility.Visible;
            }
            else
            {
                CustomCommandPanel.Visibility =
                    Visibility.Collapsed;
            }
        }
        private void ShowWarning(string messageKey, string fallback)
        {
            var message =
                Application.Current.TryFindResource(messageKey)?.ToString()
                ?? fallback;

            var title =
                Application.Current.TryFindResource("Warning")?.ToString()
                ?? "Предупреждение";

            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private void SelectPath_Click(
            object sender,
            RoutedEventArgs e)
        {
            using var dialog =
                new Forms.FolderBrowserDialog();

            dialog.Description =
                Application.Current
                    .TryFindResource(
                        "SelectProjectFolder")
                    ?.ToString()
                ?? "Выберите папку проекта";

            if (dialog.ShowDialog() ==
                Forms.DialogResult.OK)
            {
                PathTextBox.Text =
                    dialog.SelectedPath;
            }
        }

        private void Add_Click(
            object sender,
            RoutedEventArgs e)
        {
            var name =
                NameTextBox.Text.Trim();

            var description =
                DescriptionTextBox.Text.Trim();

            var path =
                PathTextBox.Text.Trim();

            var port =
                PortTextBox.Text.Trim();

            var url =
                UrlTextBox.Text.Trim();

            string? command;

            if (CommandComboBox.SelectedItem is string selectedCommand &&
                selectedCommand == CustomCommandText)
            {
                command =
                    CustomCommandTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(command))
                {
                    ShowWarning("EnterCommand", "Введите свою команду.");
                    return;
                }

                if (SaveCustomCommandCheckBox.IsChecked == true)
                {
                    var mainWindow =
                        (MainWindow)Application.Current.MainWindow;

                    if (!mainWindow.Settings.Commands.Contains(
                            command))
                    {
                        mainWindow.Settings.Commands.Add(
                            command);

                        mainWindow.SaveSettings();
                    }
                }
            }
            else
            {
                command =
                    CommandComboBox.SelectedItem as string;
            }


            if (string.IsNullOrWhiteSpace(name))
            {
                ShowWarning("EnterLocalhostName", "Введите название localhost.");
                return;
            }
            if (string.IsNullOrWhiteSpace(path))
            {
                ShowWarning("EnterProjectPath", "Укажите путь к проекту.");
                return;
            }

            if (string.IsNullOrWhiteSpace(command))
            {
                ShowWarning("SelectCommand", "Выберите команду запуска.");
                return;
            }

            if (_editingLocalhost != null)
            {
                _editingLocalhost.Name =
                    name;

                _editingLocalhost.Description =
                    string.IsNullOrWhiteSpace(description)
                        ? null
                        : description;

                _editingLocalhost.Path =
                    path;

                _editingLocalhost.Command =
                    command;

                _editingLocalhost.Port =
                    string.IsNullOrWhiteSpace(port)
                        ? null
                        : port;

                _editingLocalhost.Url =
                    string.IsNullOrWhiteSpace(url)
                        ? null
                        : url;

                LocalhostEdited?.Invoke(
                    this,
                    _editingLocalhost);

                Clear();

                return;
            }

            var localhost =
                new Localhost
                {
                    Name = name,

                    Description =
                        string.IsNullOrWhiteSpace(description)
                            ? null
                            : description,

                    Path = path,

                    Command = command,

                    Port =
                        string.IsNullOrWhiteSpace(port)
                            ? null
                            : port,

                    Url =
                        string.IsNullOrWhiteSpace(url)
                            ? null
                            : url
                };

            LocalhostCreated?.Invoke(
                this,
                localhost);

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

        public void Edit(
            Localhost localhost)
        {
            _editingLocalhost =
                localhost;

            LocalhostTitle.Text =
                Application.Current
                    .TryFindResource(
                        "EditLocalhost")
                    ?.ToString()
                ?? "Изменить localhost";

            NameTextBox.Text =
                localhost.Name;

            DescriptionTextBox.Text =
                localhost.Description ?? "";

            PathTextBox.Text =
                localhost.Path;

            PortTextBox.Text =
                localhost.Port ?? "";

            UrlTextBox.Text =
                localhost.Url ?? "";

            CustomCommandTextBox.Clear();

            SaveCustomCommandCheckBox.IsChecked =
                true;

            var commandIndex =
                CommandComboBox.Items.IndexOf(
                    localhost.Command);

            if (commandIndex >= 0)
            {
                CommandComboBox.SelectedIndex =
                    commandIndex;
            }
            else
            {
                CommandComboBox.SelectedIndex =
                    CommandComboBox.Items.Count - 1;

                CustomCommandTextBox.Text =
                    localhost.Command ?? "";
            }

            CustomCommandPanel.Visibility =
                CommandComboBox.SelectedItem is string selectedCommand &&
                selectedCommand == CustomCommandText
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            AddButton.Content =
                Application.Current
                    .TryFindResource("Add")
                    ?.ToString()
                ?? "Добавить";
        }

        public void Clear()
        {
            _editingLocalhost = null;

            LocalhostTitle.Text =
                Application.Current
                    .TryFindResource(
                        "NewLocalhost")
                    ?.ToString()
                ?? "Новый localhost";

            NameTextBox.Clear();
            DescriptionTextBox.Clear();
            PathTextBox.Clear();
            CustomCommandTextBox.Clear();
            PortTextBox.Clear();
            UrlTextBox.Clear();

            CustomCommandPanel.Visibility =
                Visibility.Collapsed;

            SaveCustomCommandCheckBox.IsChecked =
                true;

            if (CommandComboBox.Items.Count > 0)
            {
                CommandComboBox.SelectedIndex = 0;
            }

            AddButton.Content =
                Application.Current
                    .TryFindResource("Add")
                    ?.ToString()
                ?? "Добавить";
        }
        public void RefreshLanguage()
        {
            var currentCommand = _editingLocalhost?.Command;
            var isEditing = _editingLocalhost != null;

            LoadCommands();

            if (!string.IsNullOrWhiteSpace(currentCommand))
            {
                var index = CommandComboBox.Items.IndexOf(currentCommand);

                if (index >= 0)
                {
                    CommandComboBox.SelectedIndex = index;
                }
                else
                {
                    CommandComboBox.SelectedIndex = CommandComboBox.Items.Count - 1;
                    CustomCommandTextBox.Text = currentCommand;
                    CustomCommandPanel.Visibility = Visibility.Visible;
                }
            }

            LocalhostTitle.Text =
                Application.Current.TryFindResource(
                    isEditing ? "EditLocalhost" : "NewLocalhost")
                ?.ToString()
                ?? (isEditing ? "Изменить localhost" : "Новый localhost");

            AddButton.Content =
                Application.Current.TryFindResource(
                    isEditing ? "Save" : "Add")
                ?.ToString()
                ?? (isEditing ? "Сохранить" : "Добавить");
        }
    }
}