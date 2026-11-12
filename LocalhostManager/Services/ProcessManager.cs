using LocalhostManager.Models;
using System;
using System.Diagnostics;
using System.IO;

namespace LocalhostManager.Services
{
    public class ProcessManager
    {
        public Process Start(Localhost localhost)
        {
            if (string.IsNullOrWhiteSpace(localhost.Name))
            {
                throw new ArgumentException(
                    "Не задано имя localhost.");
            }

            if (string.IsNullOrWhiteSpace(localhost.Command))
            {
                throw new ArgumentException(
                    "Не задана команда запуска localhost.");
            }

            if (string.IsNullOrWhiteSpace(localhost.Path))
            {
                throw new ArgumentException(
                    "Не задан путь к проекту.");
            }

            if (!Directory.Exists(localhost.Path))
            {
                throw new ArgumentException(
                    "Указанная папка проекта не существует.");
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {localhost.Command}",
                    WorkingDirectory = localhost.Path,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };

            if (!process.Start())
            {
                process.Dispose();

                throw new InvalidOperationException(
                    "Не удалось запустить процесс.");
            }

            return process;
        }

        public async Task StopAsync(Process process, Localhost service)
        {
            if (process.HasExited) return;
            // Services can define their native shutdown command (e.g. a local admin endpoint).
            if (!string.IsNullOrWhiteSpace(service.StopCommand))
            {
                using var shutdown = Process.Start(new ProcessStartInfo("cmd.exe", "/d /c " + service.StopCommand)
                { WorkingDirectory = service.Path, UseShellExecute = false, CreateNoWindow = true });
                if (shutdown != null)
                {
                    try { await shutdown.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5)); }
                    catch (TimeoutException) { shutdown.Kill(true); }
                }
            }
            else
            {
                // EOF lets console services that read stdin finish their own cleanup.
                process.StandardInput.Close();
                if (!process.HasExited) process.CloseMainWindow();
            }
            try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3)); }
            catch (TimeoutException)
            {
                // Console services without a shutdown API cannot receive WM_CLOSE.
                await Task.Run(() => Stop(process));
            }
            catch (InvalidOperationException) { /* Exit handler already released the process. */ }
        }

        public void Stop(Process? process)
        {
            if (process == null)
            {
                return;
            }

            if (!process.HasExited)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException) when (process.HasExited)
                {
                    // The process exited between the check and Kill.
                }
                if (!process.WaitForExit(5000))
                    throw new TimeoutException("Процесс не завершился. Перезапуск отменён.");
            }
        }
    }
}
