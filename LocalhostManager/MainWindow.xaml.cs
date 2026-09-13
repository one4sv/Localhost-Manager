using LocalhostManager.Models;
using LocalhostManager.Services;
using LocalhostManager.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using Application = System.Windows.Application;
using FormsContextMenuStrip = System.Windows.Forms.ContextMenuStrip;
using FormsNotifyIcon = System.Windows.Forms.NotifyIcon;
using FormsToolStripMenuItem = System.Windows.Forms.ToolStripMenuItem;
using FormsToolStripSeparator = System.Windows.Forms.ToolStripSeparator;
using MessageBox = System.Windows.MessageBox;

namespace LocalhostManager
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<ProjectViewModel> Projects { get; }

        public AppSettings Settings { get; }

        public ProcessManager ProcessManager { get; }
        public ProjectManager ProjectManager { get; }

        public SettingsManager SettingsManager { get; }
        public ProjectStorage ProjectStorage { get; }

        private readonly FormsNotifyIcon _trayIcon;
        private FormsContextMenuStrip _trayMenu;

        private FormsToolStripMenuItem _projectsMenu;
        private FormsToolStripMenuItem _localhostsMenu;
        private FormsToolStripMenuItem _favoritesMenu;

        private readonly Dictionary<ProjectViewModel, bool> _projectStates = [];

        private bool _isExiting;
        private bool _isStartupLaunch;

        private Bitmap? _greenDot;

        private string T(string key, string fallback)
        {
            return Application.Current.TryFindResource(key)?.ToString()
                ?? fallback;
        }

        public MainWindow()
        {
            InitializeComponent();

            ProcessManager = new ProcessManager();
            ProjectManager = new ProjectManager(ProcessManager);

            SettingsManager = new SettingsManager();
            Settings = SettingsManager.Load();

            ApplyTheme();
            ApplyLanguage();

            ProjectStorage = new ProjectStorage();

            Projects = new ObservableCollection<ProjectViewModel>();

            LoadProjects();

            DataContext = this;

            MainFrame.Navigate(new Pages.HomePage());

            _isStartupLaunch =
                Environment.GetCommandLineArgs()
                    .Any(argument =>
                        string.Equals(
                            argument,
                            "--startup",
                            StringComparison.OrdinalIgnoreCase));

            _trayMenu = CreateTrayMenu();
            _trayIcon = CreateTrayIcon();

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            if (_isStartupLaunch &&
                Settings.MinimizeToTray)
            {
                Hide();
            }
        }

        private FormsNotifyIcon CreateTrayIcon()
        {
            var trayIcon = new FormsNotifyIcon
            {
                Text = "Localhost Manager",
                Visible = true
            };

            var iconPath =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "icon.ico");

            if (File.Exists(iconPath))
            {
                trayIcon.Icon =
                    new System.Drawing.Icon(iconPath);
            }

            trayIcon.ContextMenuStrip = _trayMenu;

            trayIcon.DoubleClick += (_, _) =>
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
            };

            return trayIcon;
        }

        public FormsContextMenuStrip CreateTrayMenu()
        {
            var menu =
                new FormsContextMenuStrip();

            if (_greenDot != null)
            {
                _greenDot.Dispose();
            }

            _greenDot = CreateGreenDot();

            _projectsMenu =
                new FormsToolStripMenuItem(
                    T("Projects", "Проекты"));

            _projectsMenu.DropDownOpening +=
                ProjectsMenu_DropDownOpening;

            _localhostsMenu =
                new FormsToolStripMenuItem(
                    T("Localhosts", "Localhosts"));

            _localhostsMenu.DropDownOpening +=
                LocalhostsMenu_DropDownOpening;

            _favoritesMenu =
                new FormsToolStripMenuItem(
                    T("FavoritesMenu", "Избранное"));

            _favoritesMenu.DropDownOpening +=
                FavoritesMenu_DropDownOpening;

            var startAllItem =
                new FormsToolStripMenuItem(
                    $"▶  {T("StartAll", "Запустить все")}");

            startAllItem.Click += (_, _) =>
            {
                StartAllProjects();
            };

            var stopAllItem =
                new FormsToolStripMenuItem(
                    $"■  {T("StopAll", "Остановить все")}");

            stopAllItem.Click += (_, _) =>
            {
                StopAllProjects();
            };

            var restartAllItem =
                new FormsToolStripMenuItem(
                    $"↻  {T("RestartAll", "Перезапустить все")}");

            restartAllItem.Click += (_, _) =>
            {
                RestartAllProjects();
            };

            var openItem =
                new FormsToolStripMenuItem(
                    T("Open", "Открыть"));

            openItem.Click += (_, _) =>
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
            };

            var settingsItem =
                new FormsToolStripMenuItem(
                    T("SettingsMenu", "Настройки"));

            settingsItem.Click += (_, _) =>
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
                NavigateSettings();
            };

            var exitItem =
                new FormsToolStripMenuItem(
                    T("Exit", "Выйти"));

            exitItem.Click += (_, _) =>
            {
                _isExiting = true;
                _trayIcon.Visible = false;
                Application.Current.Shutdown();
            };

            menu.Items.Add(_projectsMenu);
            menu.Items.Add(_localhostsMenu);
            menu.Items.Add(_favoritesMenu);

            menu.Items.Add(
                new FormsToolStripSeparator());

            menu.Items.Add(startAllItem);
            menu.Items.Add(stopAllItem);
            menu.Items.Add(restartAllItem);

            menu.Items.Add(
                new FormsToolStripSeparator());

            menu.Items.Add(openItem);
            menu.Items.Add(settingsItem);

            menu.Items.Add(
                new FormsToolStripSeparator());

            menu.Items.Add(exitItem);

            return menu;
        }

        private void ProjectsMenu_DropDownOpening(
            object? sender,
            EventArgs e)
        {
            _projectsMenu.DropDownItems.Clear();

            if (Projects.Count == 0)
            {
                _projectsMenu.DropDownItems.Add(
                    new FormsToolStripMenuItem(
                        T("NoProjects", "Нет проектов"))
                    {
                        Enabled = false
                    });

                return;
            }

            foreach (var project in Projects)
            {
                var item =
                    new FormsToolStripMenuItem();

                UpdateProjectMenuItem(
                    item,
                    project);

                item.Click += (_, _) =>
                {
                    ToggleProject(project);
                };

                _projectsMenu.DropDownItems.Add(
                    item);
            }
        }

        private void UpdateProjectMenuItem(
            FormsToolStripMenuItem item,
            ProjectViewModel project)
        {
            item.Tag = project;
            item.Text = project.IsRunning
                ? $"■  {project.Name}"
                : $"▶  {project.Name}";

            item.Image = project.IsRunning
                ? _greenDot
                : null;
        }

        private void LocalhostsMenu_DropDownOpening(
            object? sender,
            EventArgs e)
        {
            _localhostsMenu.DropDownItems.Clear();

            if (Projects.Count == 0)
            {
                _localhostsMenu.DropDownItems.Add(
                    new FormsToolStripMenuItem(
                        T("NoProjects", "Нет проектов"))
                    {
                        Enabled = false
                    });

                return;
            }

            foreach (var project in Projects)
            {
                var projectItem =
                    new FormsToolStripMenuItem(
                        project.Name);

                foreach (var localhost in project.Localhosts)
                {
                    var localhostItem =
                        new FormsToolStripMenuItem();

                    UpdateLocalhostMenuItem(
                        localhostItem,
                        localhost);

                    localhostItem.Click += (_, _) =>
                    {
                        ToggleLocalhost(localhost);

                        UpdateLocalhostMenuItem(
                            localhostItem,
                            localhost);
                    };

                    projectItem.DropDownItems.Add(
                        localhostItem);
                }

                if (project.Localhosts.Count == 0)
                {
                    projectItem.DropDownItems.Add(
                        new FormsToolStripMenuItem(
                            T("NoLocalhosts", "Нет localhost"))
                        {
                            Enabled = false
                        });
                }

                _localhostsMenu.DropDownItems.Add(
                    projectItem);
            }
        }

        private void UpdateLocalhostMenuItem(
            FormsToolStripMenuItem item,
            LocalhostViewModel localhost)
        {
            item.Tag = localhost;

            item.Text = localhost.IsRunning
                ? $"■  {localhost.Name}"
                : $"▶  {localhost.Name}";

            item.Image = localhost.IsRunning
                ? _greenDot
                : null;
        }

        private void FavoritesMenu_DropDownOpening(
            object? sender,
            EventArgs e)
        {
            _favoritesMenu.DropDownItems.Clear();

            var allLocalhosts =
                Projects
                    .SelectMany(project =>
                        project.Localhosts.Select(localhost =>
                            new
                            {
                                Project = project,
                                Localhost = localhost
                            }))
                    .ToList();

            var validIds =
                allLocalhosts
                    .Select(item =>
                        item.Localhost.Localhost.Id)
                    .ToHashSet();

            var cleanedFavorites =
                (Settings.FavoriteLocalhosts ?? [])
                    .Where(validIds.Contains)
                    .Distinct()
                    .Take(3)
                    .ToList();

            var favoritesChanged =
                Settings.FavoriteLocalhosts == null ||
                !Settings.FavoriteLocalhosts.SequenceEqual(
                    cleanedFavorites);

            if (favoritesChanged)
            {
                Settings.FavoriteLocalhosts =
                    cleanedFavorites;

                SettingsManager.Save(Settings);
            }

            var items =
                allLocalhosts
                    .Where(item =>
                        cleanedFavorites.Contains(
                            item.Localhost.Localhost.Id))
                    .OrderBy(item =>
                        cleanedFavorites.IndexOf(
                            item.Localhost.Localhost.Id))
                    .ToList();

            if (items.Count == 0)
            {
                _favoritesMenu.DropDownItems.Add(
                    new FormsToolStripMenuItem(
                        T(
                            "NoFavorites",
                            "Нет избранных localhost"))
                    {
                        Enabled = false
                    });

                return;
            }

            foreach (var item in items)
            {
                var menuItem =
                    new FormsToolStripMenuItem(
                        $"{item.Project.Name} — {item.Localhost.Name}");

                menuItem.Image =
                    item.Localhost.IsRunning
                        ? _greenDot
                        : null;

                menuItem.Click += (_, _) =>
                {
                    OpenLocalhost(
                        item.Localhost.Localhost);
                };

                _favoritesMenu.DropDownItems.Add(
                    menuItem);
            }
        }

        private void ToggleProject(
            ProjectViewModel project)
        {
            try
            {
                var wasRunning =
                    project.IsRunning;

                if (wasRunning)
                {
                    ProjectManager.StopProject(
                        project);
                }
                else
                {
                    ProjectManager.StartProject(
                        project);
                }

                RefreshTrayMenus();

                if (wasRunning)
                {
                    if (Settings.NotifyOnStop)
                    {
                        ShowNotification(
                            T(
                                "ProjectStopped",
                                "Проект остановлен"),
                            project.Name);
                    }
                }
                else
                {
                    if (Settings.NotifyOnStart)
                    {
                        ShowNotification(
                            T(
                                "ProjectStarted",
                                "Проект запущен"),
                            project.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError(
                    "FailedToChangeProject",
                    "Не удалось изменить состояние проекта.",
                    ex);
            }
        }

        private void ToggleLocalhost(
            LocalhostViewModel localhost)
        {
            try
            {
                var wasRunning =
                    localhost.IsRunning;

                if (wasRunning)
                {
                    ProjectManager.StopLocalhost(
                        localhost);
                }
                else
                {
                    ProjectManager.StartLocalhost(
                        localhost);
                }

                RefreshTrayMenus();

                if (wasRunning)
                {
                    if (Settings.NotifyOnStop)
                    {
                        ShowNotification(
                            T(
                                "LocalhostStopped",
                                "Localhost остановлен"),
                            localhost.Name);
                    }
                }
                else
                {
                    if (Settings.NotifyOnStart)
                    {
                        ShowNotification(
                            T(
                                "LocalhostStarted",
                                "Localhost запущен"),
                            localhost.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError(
                    "FailedToChangeLocalhost",
                    "Не удалось изменить состояние localhost.",
                    ex);
            }
        }

        private void StartAllProjects()
        {
            var started = 0;

            foreach (var project in Projects)
            {
                if (project.IsRunning)
                {
                    continue;
                }

                try
                {
                    ProjectManager.StartProject(
                        project);

                    started++;
                }
                catch (Exception ex)
                {
                    if (Settings.NotifyOnCrash)
                    {
                        ShowNotification(
                            T(
                                "StartError",
                                "Ошибка запуска"),
                            $"{project.Name}: {ex.Message}");
                    }
                }
            }

            RefreshTrayMenus();

            if (started > 0 &&
                Settings.NotifyOnStart)
            {
                ShowNotification(
                    T(
                        "ProjectsStarted",
                        "Проекты запущены"),
                    $"{T("StartedProjectsCount", "Запущено проектов")}: {started}");
            }
        }

        private void StopAllProjects()
        {
            var stopped = 0;

            foreach (var project in Projects)
            {
                if (!project.IsRunning)
                {
                    continue;
                }

                ProjectManager.StopProject(
                    project);

                stopped++;
            }

            RefreshTrayMenus();

            if (stopped > 0 &&
                Settings.NotifyOnStop)
            {
                ShowNotification(
                    T(
                        "ProjectsStopped",
                        "Проекты остановлены"),
                    $"{T("StoppedProjectsCount", "Остановлено проектов")}: {stopped}");
            }
        }

        private void RestartAllProjects()
        {
            var restarted = 0;

            foreach (var project in Projects)
            {
                try
                {
                    ProjectManager.RestartProject(
                        project);

                    restarted++;
                }
                catch (Exception ex)
                {
                    if (Settings.NotifyOnCrash)
                    {
                        ShowNotification(
                            T(
                                "RestartError",
                                "Ошибка перезапуска"),
                            $"{project.Name}: {ex.Message}");
                    }
                }
            }

            RefreshTrayMenus();

            if (restarted > 0 &&
                Settings.NotifyOnStart)
            {
                ShowNotification(
                    T(
                        "ProjectsRestarted",
                        "Проекты перезапущены"),
                    $"{T("RestartedProjectsCount", "Перезапущено проектов")}: {restarted}");
            }
        }

        private void RefreshTrayMenus()
        {
            if (_projectsMenu.Visible)
            {
                ProjectsMenu_DropDownOpening(
                    _projectsMenu,
                    EventArgs.Empty);
            }

            if (_localhostsMenu.Visible)
            {
                LocalhostsMenu_DropDownOpening(
                    _localhostsMenu,
                    EventArgs.Empty);
            }

            if (_favoritesMenu.Visible)
            {
                FavoritesMenu_DropDownOpening(
                    _favoritesMenu,
                    EventArgs.Empty);
            }
        }

        private Bitmap CreateGreenDot()
        {
            var bitmap =
                new Bitmap(
                    10,
                    10);

            using var graphics =
                Graphics.FromImage(bitmap);

            graphics.SmoothingMode =
                System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            using var brush =
                new SolidBrush(
                    Color.FromArgb(
                        20,
                        179,
                        20));

            graphics.FillEllipse(
                brush,
                1,
                1,
                8,
                8);

            return bitmap;
        }

        private void ShowNotification(
            string title,
            string text)
        {
            if (_isExiting)
            {
                return;
            }

            _trayIcon.ShowBalloonTip(
                2000,
                title,
                text,
                System.Windows.Forms.ToolTipIcon.Info);
        }

        private void ShowError(
            string messageKey,
            string fallback,
            Exception ex)
        {
            var message =
                T(messageKey, fallback);

            MessageBox.Show(
                $"{message}\n\n{ex.Message}",
                T("Error", "Ошибка"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private void Project_PropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (sender is not ProjectViewModel project ||
                e.PropertyName !=
                nameof(ProjectViewModel.IsRunning))
            {
                return;
            }

            _projectStates[project] =
                project.IsRunning;

            RefreshTrayMenus();
        }

        private void Localhost_Crashed(
            object? sender,
            EventArgs e)
        {
            if (!Settings.NotifyOnCrash)
            {
                return;
            }

            if (sender is not LocalhostViewModel localhost)
            {
                return;
            }

            ShowNotification(
                T(
                    "CriticalError",
                    "Критическая ошибка"),
                $"{T("LocalhostCrashed", "Localhost аварийно завершился")}: {localhost.Name}");
        }

        private void LoadProjects()
        {
            var projects =
                ProjectStorage.Load();

            foreach (var project in projects)
            {
                var viewModel =
                    new ProjectViewModel(
                        project,
                        Settings.Commands);

                Projects.Add(viewModel);

                SubscribeProject(viewModel);
            }

            CleanupFavorites();
        }

        private void SubscribeProject(
            ProjectViewModel project)
        {
            project.PropertyChanged +=
                Project_PropertyChanged;

            _projectStates[project] =
                project.IsRunning;

            foreach (var localhost in project.Localhosts)
            {
                localhost.Crashed +=
                    Localhost_Crashed;
            }
        }

        public void SubscribeLocalhost(
            LocalhostViewModel localhost)
        {
            localhost.Crashed -=
                Localhost_Crashed;

            localhost.Crashed +=
                Localhost_Crashed;
        }

        private void UnsubscribeProject(
            ProjectViewModel project)
        {
            project.PropertyChanged -=
                Project_PropertyChanged;

            foreach (var localhost in project.Localhosts)
            {
                localhost.Crashed -=
                    Localhost_Crashed;
            }

            _projectStates.Remove(project);
        }

        private void OpenLocalhost(
            Localhost localhost)
        {
            var url =
                localhost.Url?.Trim();

            if (string.IsNullOrWhiteSpace(url) &&
                !string.IsNullOrWhiteSpace(
                    localhost.Port))
            {
                url =
                    $"http://localhost:{localhost.Port.Trim()}";
            }
            else if (!string.IsNullOrWhiteSpace(url) &&
                     !url.StartsWith(
                         "http://",
                         StringComparison.OrdinalIgnoreCase) &&
                     !url.StartsWith(
                         "https://",
                         StringComparison.OrdinalIgnoreCase))
            {
                url =
                    "http://" + url;
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                ShowNotification(
                    T(
                        "CannotOpenLocalhost",
                        "Невозможно открыть localhost"),
                    $"{localhost.Name}: {T("NoUrlOrPort", "не задан URL или порт")}");

                return;
            }

            try
            {
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
            }
            catch (Exception ex)
            {
                ShowNotification(
                    T(
                        "OpenError",
                        "Ошибка открытия"),
                    $"{localhost.Name}: {ex.Message}");
            }
        }

        public void AddProject(
            Project project)
        {
            var viewModel =
                new ProjectViewModel(
                    project,
                    Settings.Commands);

            Projects.Add(viewModel);

            SubscribeProject(viewModel);

            SaveProjects();

            CleanupFavorites();
            RefreshTrayMenus();
        }

        public void RemoveProject(
            ProjectViewModel project)
        {
            ProjectManager.StopProject(
                project);

            var localhostIds =
                project.Localhosts
                    .Select(localhost =>
                        localhost.Localhost.Id)
                    .ToHashSet();

            Settings.FavoriteLocalhosts =
                (Settings.FavoriteLocalhosts ?? [])
                    .Where(id =>
                        !localhostIds.Contains(id))
                    .ToList();

            UnsubscribeProject(
                project);

            Projects.Remove(
                project);

            SaveProjects();
            SaveSettings();
            RefreshTrayMenus();
        }

        public void SubscribeNewLocalhost(
            LocalhostViewModel localhost)
        {
            localhost.Crashed -=
                Localhost_Crashed;

            localhost.Crashed +=
                Localhost_Crashed;
        }

        public void RemoveLocalhostFromFavorites(
            string localhostId)
        {
            var changed =
                Settings.FavoriteLocalhosts.Remove(
                    localhostId);

            if (changed)
            {
                SaveSettings();
            }
        }

        private void CleanupFavorites()
        {
            var validIds =
                Projects
                    .SelectMany(project =>
                        project.Localhosts)
                    .Select(localhost =>
                        localhost.Localhost.Id)
                    .ToHashSet();

            var favorites =
                (Settings.FavoriteLocalhosts ?? [])
                    .Where(validIds.Contains)
                    .Distinct()
                    .Take(3)
                    .ToList();

            Settings.FavoriteLocalhosts =
                favorites;
        }

        public void SaveProjects()
        {
            var projects =
                Projects
                    .Select(project =>
                        project.Project)
                    .ToList();

            ProjectStorage.Save(projects);
        }

        public void SaveSettings()
        {
            SettingsManager.Save(
                Settings);

            foreach (var project in Projects)
            {
                project.RefreshCommands(
                    Settings.Commands);
            }

            CleanupFavorites();
            RefreshTrayMenus();
        }

        public void UpdateStartupRegistration()
        {
            const string runKeyPath =
                @"Software\Microsoft\Windows\CurrentVersion\Run";

            using var key =
                Registry.CurrentUser.OpenSubKey(
                    runKeyPath,
                    true);

            if (key == null)
            {
                return;
            }

            if (Settings.StartWithWindows)
            {
                var executablePath =
                    Environment.ProcessPath;

                if (string.IsNullOrWhiteSpace(
                    executablePath))
                {
                    return;
                }

                key.SetValue(
                    "LocalhostManager",
                    $"\"{executablePath}\" --startup");
            }
            else
            {
                key.DeleteValue(
                    "LocalhostManager",
                    false);
            }
        }

        protected override void OnClosing(
            System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExiting &&
                Settings.CloseToTray)
            {
                e.Cancel = true;
                Hide();

                return;
            }

            _isExiting = true;

            base.OnClosing(e);
        }

        protected override void OnClosed(
            EventArgs e)
        {
            _isExiting = true;

            foreach (var project in Projects)
            {
                UnsubscribeProject(
                    project);

                ProjectManager.StopProject(
                    project);
            }

            _trayIcon.Visible = false;
            _trayIcon.Dispose();

            if (_trayMenu != null)
            {
                _trayMenu.Dispose();
            }

            _greenDot?.Dispose();

            base.OnClosed(e);
        }

        public void NavigateHome()
        {
            MainFrame.Navigate(
                new Pages.HomePage());
        }

        public void NavigateSettings()
        {
            MainFrame.Navigate(
                new Pages.SettingsPage());
        }

        public void ApplyTheme()
        {
            var themePath =
                Settings.Theme == "light"
                    ? "Resources/LightTheme.xaml"
                    : "Resources/DarkTheme.xaml";

            var dictionary =
                new ResourceDictionary
                {
                    Source = new Uri(
                        themePath,
                        UriKind.Relative)
                };

            var themeDictionary =
                Application.Current.Resources.MergedDictionaries;

            var oldTheme =
                themeDictionary.FirstOrDefault(
                    dictionary =>
                        dictionary.Source != null &&
                        (dictionary.Source.OriginalString
                            .Contains("DarkTheme.xaml") ||
                         dictionary.Source.OriginalString
                            .Contains("LightTheme.xaml")));

            if (oldTheme != null)
            {
                themeDictionary.Remove(oldTheme);
            }

            themeDictionary.Add(dictionary);
        }

        public void ApplyLanguage()
        {
            var languagePath =
                Settings.Language == "en"
                    ? "Resources/English.xaml"
                    : "Resources/Russian.xaml";

            var dictionary =
                new ResourceDictionary
                {
                    Source = new Uri(
                        languagePath,
                        UriKind.Relative)
                };

            var dictionaries =
                Application.Current.Resources.MergedDictionaries;

            var oldLanguage =
                dictionaries.FirstOrDefault(
                    dictionary =>
                        dictionary.Source != null &&
                        (dictionary.Source.OriginalString
                            .Contains("Russian.xaml") ||
                         dictionary.Source.OriginalString
                            .Contains("English.xaml")));

            if (oldLanguage != null)
            {
                dictionaries.Remove(oldLanguage);
            }

            dictionaries.Add(dictionary);

            if (_trayIcon != null)
            {
                var oldMenu = _trayMenu;

                _trayMenu =
                    CreateTrayMenu();

                _trayIcon.ContextMenuStrip =
                    _trayMenu;

                oldMenu?.Dispose();
            }
        }
    }
}