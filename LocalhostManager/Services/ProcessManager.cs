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

        public void Stop(Process? process)
        {
            if (process == null)
            {
                return;
            }

            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
            }
            catch
            {
            }
        }
    }
}