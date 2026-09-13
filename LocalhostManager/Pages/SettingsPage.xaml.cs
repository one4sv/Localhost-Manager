using LocalhostManager.ViewModels;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Application = System.Windows.Application;
using ComboBox = System.Windows.Controls.ComboBox;
using MessageBox = System.Windows.MessageBox;

namespace LocalhostManager.Pages
{
    public partial class SettingsPage : Page
    {
        private MainWindow MainWindow =>
            (MainWindow)Application.Current.MainWindow;

        private bool _loading;

        private sealed class FavoriteOption
        {
            public string? Id { get; init; }
            public string Name { get; init; } = "";

            public override string ToString()
            {
                return Name;
            }
        }
        private sealed class FavoriteSource
        {
            public string Id { get; init; } = "";
            public string Name { get; init; } = "";
        }

        public SettingsPage()
        {
            InitializeComponent();

            DataContext = MainWindow;

            _loading = true;

            StartWithWindowsCheckBox.IsChecked =
                MainWindow.Settings.StartWithWindows;

            MinimizeToTrayCheckBox.IsChecked =
                MainWindow.Settings.MinimizeToTray;

            CloseToTrayCheckBox.IsChecked =
                MainWindow.Settings.CloseToTray;

            DarkThemeRadioButton.IsChecked =
                MainWindow.Settings.Theme == "dark";

            LightThemeRadioButton.IsChecked =
                MainWindow.Settings.Theme == "light";

            RussianLanguageRadioButton.IsChecked =
                MainWindow.Settings.Language == "ru";

            EnglishLanguageRadioButton.IsChecked =
                MainWindow.Settings.Language == "en";

            NotifyOnStartCheckBox.IsChecked =
                MainWindow.Settings.NotifyOnStart;

            NotifyOnStopCheckBox.IsChecked =
                MainWindow.Settings.NotifyOnStop;

            NotifyOnCrashCheckBox.IsChecked =
                MainWindow.Settings.NotifyOnCrash;

            LoadFavorites();
            LoadCommands();

            _loading = false;
        }

        private void LoadFavorites()
        {
            var sources =
                MainWindow.Projects
                    .SelectMany(project =>
                        project.Localhosts.Select(localhost =>
                            new FavoriteSource
                            {
                                Id = localhost.Localhost.Id,
                                Name =
                                    $"{project.Name} — {localhost.Name}"
                            }))
                    .ToList();

            var validIds =
                sources
                    .Select(source => source.Id)
                    .ToHashSet();

            var favorites =
                (MainWindow.Settings.FavoriteLocalhosts ?? [])
                    .Where(validIds.Contains)
                    .Distinct()
                    .Take(3)
                    .ToList();

            MainWindow.Settings.FavoriteLocalhosts =
                favorites;

            ConfigureFavoriteCombo(
                FavoriteLocalhost1,
                sources);

            ConfigureFavoriteCombo(
                FavoriteLocalhost2,
                sources);

            ConfigureFavoriteCombo(
                FavoriteLocalhost3,
                sources);

            FavoriteLocalhost1.SelectedValue =
                favorites.ElementAtOrDefault(0);

            FavoriteLocalhost2.SelectedValue =
                favorites.ElementAtOrDefault(1);

            FavoriteLocalhost3.SelectedValue =
                favorites.ElementAtOrDefault(2);
        }

        private void ConfigureFavoriteCombo(
            ComboBox comboBox,
            IEnumerable<FavoriteSource> sources)
        {
            var items =
                sources
                    .Select(source =>
                        new FavoriteOption
                        {
                            Id = source.Id,
                            Name = source.Name
                        })
                    .ToList();

            items.Insert(
                0,
                new FavoriteOption
                {
                    Id = null,
                    Name =
                        Application.Current.TryFindResource(
                            "NotSelected")
                        ?.ToString()
                        ?? "Не выбрано"
                });

            comboBox.ItemsSource = items;
            comboBox.DisplayMemberPath =
                nameof(FavoriteOption.Name);
            comboBox.SelectedValuePath =
                nameof(FavoriteOption.Id);
        }

        private void FavoriteLocalhost_Changed(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (_loading)
            {
                return;
            }

            var selectedIds =
                new[]
                {
                    FavoriteLocalhost1.SelectedValue as string,
                    FavoriteLocalhost2.SelectedValue as string,
                    FavoriteLocalhost3.SelectedValue as string
                };

            var nonEmptyIds =
                selectedIds
                    .Where(id =>
                        !string.IsNullOrWhiteSpace(id))
                    .ToList();

            if (nonEmptyIds.Count !=
                nonEmptyIds.Distinct().Count())
            {
                _loading = true;

                LoadFavorites();

                _loading = false;

                return;
            }

            MainWindow.Settings.FavoriteLocalhosts =
                nonEmptyIds
                    .Take(3)
                    .ToList();

            MainWindow.SaveSettings();
        }

        private void LoadCommands()
        {
            CommandsListBox.ItemsSource = null;
            CommandsListBox.ItemsSource =
                MainWindow.Settings.Commands;
        }

        private void Back_Click(
            object sender,
            RoutedEventArgs e)
        {
            MainWindow.NavigateHome();
        }

        private void Setting_Changed(
            object sender,
            RoutedEventArgs e)
        {
            if (_loading)
            {
                return;
            }

            MainWindow.Settings.StartWithWindows =
                StartWithWindowsCheckBox.IsChecked == true;

            MainWindow.Settings.MinimizeToTray =
                MinimizeToTrayCheckBox.IsChecked == true;

            MainWindow.Settings.CloseToTray =
                CloseToTrayCheckBox.IsChecked == true;

            MainWindow.Settings.NotifyOnStart =
                NotifyOnStartCheckBox.IsChecked == true;

            MainWindow.Settings.NotifyOnStop =
                NotifyOnStopCheckBox.IsChecked == true;

            MainWindow.Settings.NotifyOnCrash =
                NotifyOnCrashCheckBox.IsChecked == true;

            MainWindow.UpdateStartupRegistration();
            MainWindow.SaveSettings();
        }

        private void Theme_Changed(
            object sender,
            RoutedEventArgs e)
        {
            if (_loading)
            {
                return;
            }

            MainWindow.Settings.Theme =
                DarkThemeRadioButton.IsChecked == true
                    ? "dark"
                    : "light";

            MainWindow.ApplyTheme();
            MainWindow.SaveSettings();
        }

        private void Language_Changed(
            object sender,
            RoutedEventArgs e)
        {
            if (_loading)
            {
                return;
            }

            MainWindow.Settings.Language =
                RussianLanguageRadioButton.IsChecked == true
                    ? "ru"
                    : "en";

            MainWindow.ApplyLanguage();
            LoadFavorites();
            MainWindow.SaveSettings();
        }

        private void AddCommand_Click(
            object sender,
            RoutedEventArgs e)
        {
            var command =
                CommandTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(command))
            {
                return;
            }

            if (MainWindow.Settings.Commands.Contains(
                command))
            {
                MessageBox.Show(
                    Application.Current.TryFindResource(
                        "CommandAlreadyExists")
                    ?.ToString()
                    ?? "Такая команда уже существует.",
                    Application.Current.TryFindResource(
                        "Command")
                    ?.ToString()
                    ?? "Команда",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            MainWindow.Settings.Commands.Add(
                command);

            MainWindow.SaveSettings();

            CommandTextBox.Clear();

            LoadCommands();
        }

        private void DeleteCommand_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not
                    System.Windows.Controls.Button button ||
                button.Tag is not string command)
            {
                return;
            }

            var result =
                MessageBox.Show(
                    string.Format(
                        Application.Current.TryFindResource(
                            "DeleteCommandQuestion")
                        ?.ToString()
                        ?? "Удалить команду «{0}»?",
                        command),
                    Application.Current.TryFindResource(
                        "DeleteCommand")
                    ?.ToString()
                    ?? "Удаление команды",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (result !=
                MessageBoxResult.Yes)
            {
                return;
            }

            MainWindow.Settings.Commands.Remove(
                command);

            MainWindow.SaveSettings();

            LoadCommands();
        }

        public void RefreshLanguage()
        {
            var favorites =
                MainWindow.Settings.FavoriteLocalhosts?
                    .ToList()
                ?? [];

            _loading = true;

            LoadFavorites();
            LoadCommands();

            _loading = false;
        }
    }
}