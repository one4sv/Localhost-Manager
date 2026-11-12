using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace LocalhostManager;

public partial class App : System.Windows.Application
{
    private Mutex? _instance;
    private readonly CancellationTokenSource _shutdown = new();
    private string _channel = "";

    protected override void OnStartup(StartupEventArgs e)
    {
        var identity = Environment.UserDomainName + "\\" + Environment.UserName;
        identity += "|" + Environment.GetEnvironmentVariable("LOCALHOST_MANAGER_DATA_DIR");
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(identity)))[..16];
        _channel = "LocalhostManager-" + hash;
        _instance = new Mutex(true, "Local\\" + _channel, out var first);
        if (!first)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", _channel, PipeDirection.Out);
                client.Connect(2000);
                client.WriteByte(1);
            }
            catch (IOException) { }
            catch (TimeoutException) { }
            Shutdown();
            return;
        }
        base.OnStartup(e);
        _ = ListenAsync(_shutdown.Token);
    }

    private async Task ListenAsync(CancellationToken cancellation)
    {
        while (!cancellation.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(_channel, PipeDirection.In, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await server.WaitForConnectionAsync(cancellation);
                var signal = new byte[1];
                if (await server.ReadAsync(signal, cancellation) == 0) continue;
                await Dispatcher.InvokeAsync(() =>
                {
                    if (MainWindow == null) return;
                    MainWindow.Show();
                    if (MainWindow.WindowState == WindowState.Minimized) MainWindow.WindowState = WindowState.Normal;
                    MainWindow.Activate();
                });
            }
            catch (OperationCanceledException) { break; }
            catch (IOException) { await Task.Delay(100, cancellation); }
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _shutdown.Cancel();
        _instance?.Dispose();
        base.OnExit(e);
    }
}
