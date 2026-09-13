using LocalhostManager.Models;
using LocalhostManager.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using Forms = System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace LocalhostManager
{
    public partial class AddLocalhostWindow : Window
    {
        private readonly AppSettings _settings;

        public Localhost? Result { get; private set; }

        public AddLocalhostWindow(AppSettings settings)
        {
            InitializeComponent();

            _settings = settings;

            LoadCommands();
        }

        private void LoadCommands()
        {
            CommandComboBox.Items.Clear();

            foreach (var command in _settings.Commands)
            {
                CommandComboBox.Items.Add(command);
            }

            CommandComboBox.Items.Add("Добавить свою...");

            if (CommandComboBox.Items.Count > 0)
            {
                CommandComboBox.SelectedIndex = 0;
            }
        }

        private void CommandComboBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (CommandComboBox.SelectedItem is string command &&
                command == "Добавить свою...")
            {
                CustomCommandPanel.Visibility = Visibility.Visible;
            }
            else
            {
                CustomCommandPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void SelectPath_Click(
            object sender,
            RoutedEventArgs e)
        {
            using var dialog = new Forms.FolderBrowserDialog();

            dialog.Description = "Выберите папку проекта";

            if (dialog.ShowDialog() == Forms.DialogResult.OK)
            {
                PathTextBox.Text = dialog.SelectedPath;
            }
        }

        private void Add_Click(
            object sender,
            RoutedEventArgs e)
        {
            var name = NameTextBox.Text.Trim();
            var description = DescriptionTextBox.Text.Trim();
            var path = PathTextBox.Text.Trim();
            var port = PortTextBox.Text.Trim();
            var url = UrlTextBox.Text.Trim();

            string? command;

            if (CommandComboBox.SelectedItem is string selectedCommand &&
                selectedCommand == "Добавить свою...")
            {
                command = CustomCommandTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(command))
                {
                    MessageBox.Show(
                        "Введите свою команду.",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                if (SaveCustomCommandCheckBox.IsChecked == true &&
                    !_settings.Commands.Contains(command))
                {
                    _settings.Commands.Add(command);
                }
            }
            else
            {
                command = CommandComboBox.SelectedItem as string;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(
                    "Введите название localhost.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                MessageBox.Show(
                    "Укажите путь к проекту.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (string.IsNullOrWhiteSpace(command))
            {
                MessageBox.Show(
                    "Укажите команду запуска.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            Result = new Localhost
            {
                Name = name,
                Description = string.IsNullOrWhiteSpace(description)
                    ? null
                    : description,

                Path = path,
                Command = command,

                Port = string.IsNullOrWhiteSpace(port)
                    ? null
                    : port,

                Url = string.IsNullOrWhiteSpace(url)
                    ? null
                    : url
            };

            DialogResult = true;
        }

        private void Cancel_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}