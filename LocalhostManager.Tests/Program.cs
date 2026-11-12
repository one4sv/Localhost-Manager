using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LocalhostManager;
using LocalhostManager.Models;
using LocalhostManager.Services;
using LocalhostManager.ViewModels;

internal static class Program
{
    private static int _checks;
    private static readonly string DataDirectory = Path.Combine(Path.GetTempPath(), "LocalhostManager-tests-" + Guid.NewGuid().ToString("N"));

    [STAThread]
    private static int Main()
    {
        Directory.CreateDirectory(DataDirectory);
        Environment.SetEnvironmentVariable("LOCALHOST_MANAGER_DATA_DIR", DataDirectory);
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/LocalhostManager;component/Resources/Styles.xaml")
        });
        var manager = new ProjectManager(new ProcessManager());
        var services = new List<LocalhostViewModel>();
        try
        {
            var legacy = JsonSerializer.Deserialize<Localhost>("{\"Name\":\"legacy\",\"Path\":\"C:\\\\\",\"IsRunning\":true}")!;
            Check(!legacy.IsRunning, "Old persisted running flag is ignored");
            legacy.IsRunning = true;
            Check(!JsonSerializer.Serialize(legacy).Contains("IsRunning"), "Runtime status is not saved");
            Check(!new LocalhostViewModel(legacy, []).IsRunning, "View model starts stopped");

            var storageDirectory = Path.Combine(DataDirectory, "storage");
            var storage = new ProjectStorage(storageDirectory);
            storage.Save([new Project { Name = "original", AutoStart = true }]);
            storage.Save([new Project { Name = "updated" }]);
            File.WriteAllText(Path.Combine(storageDirectory, "projects.json"), "{broken");
            Check(storage.Load().Single().Name == "original", "Damaged JSON recovers the previous atomic backup");
            Check(storage.Warning != null && Directory.GetFiles(storageDirectory, "*.corrupt-*").Length == 1, "Damaged original is retained and reported");
            storage.Save([new Project { Name = "after recovery" }]);
            Check(storage.Load().Single().Name == "after recovery", "Saving works after recovery");
            Check(Directory.GetFiles(storageDirectory, "*.tmp").Length == 0, "Atomic saves leave no temporary files");

            var projects = new[]
            {
                new ProjectViewModel(new Project { Name = "selected", AutoStart = true }, []),
                new ProjectViewModel(new Project { Name = "unselected" }, [])
            };
            var started = new List<string>();
            new StartupProjects().Apply(new AppSettings(), projects, p => started.Add(p.Name), (_, e) => throw e);
            Check(started.Count == 0, "Autostart is disabled by default");
            var startup = new StartupProjects();
            var enabled = new AppSettings { AutoStartProjects = true };
            startup.Apply(enabled, projects, p => started.Add(p.Name), (_, e) => throw e);
            startup.Apply(enabled, projects, p => started.Add(p.Name), (_, e) => throw e);
            Check(started.SequenceEqual(["selected"]), "Only selected projects start, once per session");
            var failureCount = 0;
            projects[1].Project.AutoStart = true;
            new StartupProjects().Apply(enabled, projects, p =>
            {
                if (p.Name == "selected") throw new IOException("test");
                started.Add(p.Name);
            }, (_, _) => failureCount++);
            Check(failureCount == 1 && started.Last() == "unselected", "One failed startup does not block other projects");

            var running = Service("ping -n 30 127.0.0.1 > nul");
            services.Add(running);
            manager.StartLocalhost(running);
            var old = running.GetProcess()!;
            var oldId = old.Id;
            manager.RestartLocalhost(running);
            var replacement = running.GetProcess();
            Pump(300);
            Check(running.IsRunning && ReferenceEquals(replacement, running.GetProcess()), "Old exit event cannot erase restarted process");
            Check(!IsAlive(oldId), "Previous shell has exited before restart completes");
            manager.StopLocalhost(running);
            Check(!running.IsRunning && running.GetProcess() == null, "Stop clears owned process state");

            var shortLived = Service("echo short-command");
            services.Add(shortLived);
            manager.StartLocalhost(shortLived);
            Until(() => !shortLived.IsRunning);
            Check(shortLived.Logs.Any(l => l.EndsWith("short-command")), "Fast command completion retains output and clears status");
            var crashed = Service("exit /b 7");
            services.Add(crashed);
            var crashes = 0;
            crashed.Crashed += (_, _) => crashes++;
            manager.StartLocalhost(crashed);
            Until(() => crashes > 0);
            Check(crashes == 1 && !crashed.IsRunning, "Nonzero exit reports one crash");
            var verbose = Service("for /L %i in (1,1,10000) do @echo line-%i");
            services.Add(verbose);
            manager.StartLocalhost(verbose);
            Until(() => !verbose.IsRunning);
            Check(verbose.Logs.Count <= LocalhostViewModel.MaxLogLines && verbose.Logs.Last().EndsWith("line-10000"), "Logs are bounded and retain latest output");

            var archive = Path.Combine(DataDirectory, "export.json");
            var exported = new Project { Name = "Portable", AutoStart = true, Localhosts = [
                new Localhost { Name = "worker", Path = DataDirectory, Command = "echo worker", StartOrder = 3, AutoRestart = true }
            ] };
            ProjectArchive.Export(archive, [exported]);
            var imported = ProjectArchive.Read(archive, false).Single();
            Check(!imported.AutoStart && imported.Localhosts[0].Id != exported.Localhosts[0].Id && imported.Localhosts[0].StartOrder == 3, "Import preserves configuration with fresh service identities and startup disabled");
            var restored = ProjectArchive.Read(archive, true).Single();
            Check(restored.AutoStart && restored.Localhosts[0].Id == exported.Localhosts[0].Id, "Restore preserves startup choices and favorite identifiers");
            var console = new LogsWindow(shortLived);
            var logSearch = (TextBox)console.FindName("Search");
            logSearch.Text = "missing";
            Check(((ListBox)console.FindName("LogsList")).Items.Count == 0, "Console filters nonmatching lines");
            logSearch.Text = "short-command";
            Check(((ListBox)console.FindName("LogsList")).Items.Count == 1, "Console shows matching timestamped output");
            console.Close();

            var fixture = Path.Combine(DataDirectory, "discovery");
            Directory.CreateDirectory(fixture);
            File.WriteAllText(Path.Combine(fixture, "package.json"), """{"scripts":{"dev":"vite","build":"vite build"}}""");
            File.WriteAllText(Path.Combine(fixture, "pnpm-lock.yaml"), "");
            File.WriteAllText(Path.Combine(fixture, "manage.py"), "");
            var commands = ProjectDiscovery.Detect(fixture);
            Check(commands[0].StartsWith("pnpm run") && commands.Contains("python manage.py runserver"), "Detects package manager and Python entry point");
            using (var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0))
            {
                listener.Start();
                var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port.ToString();
                var probe = Service("echo test");
                probe.Port = port;
                var ready = ServiceDiagnostics.IsReadyAsync(probe.Localhost, CancellationToken.None);
                Until(() => ready.IsCompleted);
                Check(ready.Result, "TCP readiness confirms a listening endpoint");
                var owner = ServiceDiagnostics.PortOwnerAsync(port);
                Until(() => owner.IsCompleted);
                Check(owner.Result?.Contains(Environment.ProcessId.ToString()) == true, "Port diagnostic identifies the listening PID");
            }
            var ordered = new ProjectViewModel(new Project { Name = "order", Localhosts = [
                new Localhost { Name = "later", Path = DataDirectory, Command = "echo second", StartOrder = 2 },
                new Localhost { Name = "first", Path = DataDirectory, Command = "echo first", StartOrder = 1 }
            ] }, []);
            var launchOrder = new List<string>();
            foreach (var svc in ordered.Localhosts)
                svc.PropertyChanged += (_, e) => { if (e.PropertyName == "IsRunning" && svc.IsRunning) launchOrder.Add(svc.Name); };
            var launch = manager.StartProjectAsync(ordered);
            Until(() => launch.IsCompleted);
            launch.GetAwaiter().GetResult();
            Check(launchOrder.SequenceEqual(["first", "later"]), "Project order is independent of display order");
            Until(() => !ordered.IsRunning);

            using (var webListener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0))
            {
                webListener.Start();
                var web = Service("echo web");
                web.Localhost.HealthUrl = $"http://127.0.0.1:{((System.Net.IPEndPoint)webListener.LocalEndpoint).Port}/health";
                foreach (var code in new[] { 503, 200 })
                {
                    var response = Task.Run(async () =>
                    {
                        using var client = await webListener.AcceptTcpClientAsync();
                        using var stream = client.GetStream();
                        var buffer = new byte[4096];
                        await stream.ReadAsync(buffer);
                        await stream.WriteAsync(System.Text.Encoding.ASCII.GetBytes($"HTTP/1.1 {code} Test\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"));
                    });
                    var probe = ServiceDiagnostics.IsReadyAsync(web.Localhost, CancellationToken.None);
                    Until(() => probe.IsCompleted && response.IsCompleted);
                    Check(probe.Result == (code == 200), $"HTTP readiness handles response {code}");
                }
            }
            File.WriteAllText(Path.Combine(DataDirectory, "worker.cmd"), "@echo off\r\n:wait\r\nif exist stop.flag goto done\r\nping -n 2 127.0.0.1 >nul\r\ngoto wait\r\n:done\r\necho clean>clean.flag\r\n");
            var cooperative = Service("worker.cmd");
            services.Add(cooperative);
            cooperative.Localhost.StopCommand = "echo stop>stop.flag";
            var workerStart = manager.StartLocalhostAsync(cooperative);
            Until(() => workerStart.IsCompleted);
            var workerStop = manager.StopLocalhostAsync(cooperative);
            Until(() => workerStop.IsCompleted);
            workerStop.GetAwaiter().GetResult();
            Check(File.Exists(Path.Combine(DataDirectory, "clean.flag")) && !cooperative.IsRunning, "Graceful stop allows service cleanup");

            var retrying = Service("exit /b 8");
            retrying.Localhost.AutoRestart = true;
            services.Add(retrying);
            var beginRetry = manager.StartLocalhostAsync(retrying);
            Until(() => beginRetry.IsCompleted);
            var retryTimer = Stopwatch.StartNew();
            while (!retrying.Status.Contains("лимит") && retryTimer.ElapsedMilliseconds < 18000) Pump(100);
            Check(retrying.Status.Contains("лимит") && manager.Failures.Count >= 4, "Crash history and three-retry limit");
            var history = new ProjectManager(new ProcessManager());
            Check(history.Failures.Count >= 4, "Crash history survives a new session");
            
            var seed = new ProjectStorage();
            seed.Save([
                new Project { Name = "Website", Description = "Frontend · development", AutoStart = true,
                    Localhosts = [new Localhost { Name = "Web client", Path = DataDirectory, Command = "echo startup-marker" }] },
                new Project { Name = "API", Description = "Backend · local services",
                    Localhosts = [new Localhost { Name = "API server", Path = DataDirectory, Command = "echo should-not-start", Port = "8080" }] }
            ]);
            var settingsStore = new SettingsManager();
            settingsStore.Save(new AppSettings());
            var window = new MainWindow();
            app.MainWindow = window;
            Layout(window);
            window.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            Pump(100);
            Check(window.Projects.All(p => p.Localhosts.All(l => !l.IsRunning && l.Logs.Count == 0)), "Opening app does not start projects by default");
            var screenshots = Path.Combine(Environment.CurrentDirectory, "artifacts", "screenshots");
            Directory.CreateDirectory(screenshots);
            Check(((TextBlock)((LocalhostManager.Pages.HomePage)((Frame)window.FindName("MainFrame")).Content).FindName("ProjectCount")).Text == "2", "Dashboard shows the initial project count");
            var firstProject = Descendants<LocalhostManager.Controls.ProjectExpander>((Visual)window.Content).First();
            ((Expander)firstProject.FindName("ProjectExpanderControl")).IsExpanded = true;
            Render(window, Path.Combine(screenshots, "home-dark.png"));
            window.NavigateSettings();
            Pump(100);
            Layout(window);
            Render(window, Path.Combine(screenshots, "settings-dark.png"));
            var frame = (Frame)window.FindName("MainFrame");
            var page = (LocalhostManager.Pages.SettingsPage)frame.Content;
            var toggle = (CheckBox)page.FindName("AutoStartProjectsCheckBox");
            toggle.IsChecked = true;
            Check(settingsStore.Load().AutoStartProjects, "Startup checkbox persists immediately");
            var apiChoice = Descendants<CheckBox>(page).Single(c => c.Content as string == "API");
            apiChoice.IsChecked = true;
            apiChoice.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            Check(seed.Load()[1].AutoStart, "Per-project startup selection persists");
            apiChoice.IsChecked = false;
            apiChoice.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            Check(window.Projects.All(p => p.Localhosts.All(l => l.Logs.Count == 0)), "Changing setting does not start projects immediately");
            window.Settings.Theme = "light";
            window.Settings.Language = "en";
            window.ApplyTheme();
            window.ApplyLanguage();
            window.NavigateHome();
            Pump(100);
            Layout(window);
            Render(window, Path.Combine(screenshots, "home-light.png"));
            var home = (LocalhostManager.Pages.HomePage)frame.Content;
            ((TextBox)home.FindName("SearchBox")).Text = "API";
            Check(((ItemsControl)home.FindName("ProjectList")).Items.Count == 1, "Project search filters by name");
            window.Close();
            var nextWindow = new MainWindow();
            app.MainWindow = nextWindow;
            nextWindow.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            Until(() => nextWindow.Projects[0].Localhosts[0].Logs.Any(l => l.EndsWith("startup-marker")));
            Check(nextWindow.Projects[1].Localhosts[0].Logs.Count == 0, "Next app session starts only the selected project");
            nextWindow.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            Pump(100);
            Check(nextWindow.Projects[0].Localhosts[0].Logs.Count(l => l.EndsWith("startup-marker")) == 1, "Repeated Loaded does not relaunch projects");
            nextWindow.Close();
            Console.WriteLine($"PASS: {_checks} checks. Screenshots: {screenshots}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            foreach (var service in services) manager.StopLocalhost(service);
            app.Shutdown();
            // Keep isolated fixtures for diagnosis; never touch the user's AppData.
            Console.WriteLine("Isolated data: " + DataDirectory);
        }
    }

    private static LocalhostViewModel Service(string command) => new(new Localhost { Name = "Test", Path = DataDirectory, Command = command }, []);
    private static bool IsAlive(int id) { try { using var p = Process.GetProcessById(id); return !p.HasExited; } catch (ArgumentException) { return false; } }
    private static void Check(bool condition, string label) { if (!condition) throw new Exception("FAIL: " + label); Console.WriteLine("PASS: " + label); _checks++; }
    private static void Until(Func<bool> done) { var timer = Stopwatch.StartNew(); while (!done() && timer.ElapsedMilliseconds < 10000) Pump(25); Check(done(), "Async operation completed"); }
    private static void Pump(int milliseconds)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(milliseconds) };
        timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
        timer.Start();
        Dispatcher.PushFrame(frame);
    }
    private static void Layout(MainWindow window)
    {
        var root = (FrameworkElement)window.Content;
        root.Measure(new Size(1040, 700));
        root.Arrange(new Rect(0, 0, 1040, 700));
        root.UpdateLayout();
        Pump(100);
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T found) yield return found;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
    private static void Render(MainWindow window, string file)
    {
        Layout(window);
        var bitmap = new RenderTargetBitmap(1040, 700, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render((Visual)window.Content);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(file);
        encoder.Save(stream);
    }
}
