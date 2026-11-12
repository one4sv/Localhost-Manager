using System.Diagnostics;
using System.Net.Http;
using System.Net.Sockets;
using LocalhostManager.Models;
namespace LocalhostManager.Services;

public static class ServiceDiagnostics
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(2) };
    public static bool HasProbe(Localhost service) => !string.IsNullOrWhiteSpace(service.HealthUrl) || int.TryParse(service.Port, out _);
    public static async Task<bool> IsReadyAsync(Localhost service, CancellationToken token)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(service.HealthUrl))
            {
                using var response = await Http.GetAsync(service.HealthUrl, HttpCompletionOption.ResponseHeadersRead, token);
                return response.IsSuccessStatusCode;
            }
            if (int.TryParse(service.Port, out var port))
            {
                using var client = new TcpClient();
                await client.ConnectAsync("127.0.0.1", port, token).AsTask().WaitAsync(TimeSpan.FromSeconds(2), token);
                return true;
            }
        }
        catch (Exception e) when (e is HttpRequestException or SocketException or TimeoutException or OperationCanceledException) { }
        return false;
    }
    public static async Task<string?> PortOwnerAsync(string? port)
    {
        if (!int.TryParse(port, out var number) || number is < 1 or > 65535) return null;
        using var process = Process.Start(new ProcessStartInfo("netstat.exe", "-ano -p tcp")
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true })!;
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        foreach (var row in output.Split('\n'))
        {
            var fields = row.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length < 5 || !fields[1].EndsWith(":" + number) || fields[3] != "LISTENING") continue;
            if (!int.TryParse(fields[4], out var pid)) continue;
            try { using var owner = Process.GetProcessById(pid); return $"{owner.ProcessName} (PID {pid})"; }
            catch (ArgumentException) { }
        }
        return null;
    }
}
