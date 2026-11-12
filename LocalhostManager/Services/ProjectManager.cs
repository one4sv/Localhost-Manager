using LocalhostManager.Models;
using LocalhostManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LocalhostManager.Services
{
    public class ProjectManager
    {
        private readonly ProcessManager _processManager;

        public ProjectManager(ProcessManager processManager)
        {
            _processManager = processManager;
            try { foreach (var item in new JsonFileStore<List<string>>("failures.json").Load().Take(100)) Failures.Add(item); }
            catch (Exception) { /* History must not prevent service management. */ }
        }

        private readonly Dictionary<LocalhostViewModel, SemaphoreSlim> _locks = new();
        private readonly Dictionary<LocalhostViewModel, int> _retries = new();
        private readonly Dictionary<LocalhostViewModel, CancellationTokenSource> _sessions = new();
        public System.Collections.ObjectModel.ObservableCollection<string> Failures { get; } = [];
        public bool HasActivity(ProjectViewModel project) =>
            project.IsRunning || _projectStarts.ContainsKey(project) ||
            project.Localhosts.Any(s => _locks.TryGetValue(s, out var gate) && gate.CurrentCount == 0);

        public void Forget(LocalhostViewModel service)
        {
            service.Crashed -= HandleCrash;
            if (_sessions.Remove(service, out var session))
            {
                session.Cancel();
                session.Dispose();
            }
            _retries.Remove(service);
            _locks.Remove(service);
        }

        private SemaphoreSlim Gate(LocalhostViewModel service)
        {
            if (!_locks.TryGetValue(service, out var gate)) _locks[service] = gate = new(1, 1);
            return gate;
        }
        private readonly Dictionary<ProjectViewModel, CancellationTokenSource> _projectStarts = new();
        public async Task StartProjectAsync(ProjectViewModel project)
        {
            if (_projectStarts.ContainsKey(project)) return;
            using var startup = new CancellationTokenSource();
            _projectStarts.Add(project, startup);
            try
            {
                foreach (var service in project.Localhosts.OrderBy(s => s.Localhost.StartOrder).ToArray())
                {
                    startup.Token.ThrowIfCancellationRequested();
                    await StartLocalhostAsync(service);
                }
            }
            finally { _projectStarts.Remove(project); }
        }
        public async Task StopProjectAsync(ProjectViewModel project)
        {
            if (_projectStarts.TryGetValue(project, out var startup)) startup.Cancel();
            var errors = new List<Exception>();
            foreach (var service in project.Localhosts.OrderByDescending(s => s.Localhost.StartOrder).ToArray())
                try { await StopLocalhostAsync(service); } catch (Exception ex) { errors.Add(ex); }
            if (errors.Count > 0) throw new AggregateException(errors);
        }
        public async Task RestartProjectAsync(ProjectViewModel project)
        { await StopProjectAsync(project); await StartProjectAsync(project); }
        public async Task StartLocalhostAsync(LocalhostViewModel service)
        {
            var gate = Gate(service);
            await gate.WaitAsync();
            CancellationToken token;
            try
            {
                if (service.IsRunning)
                {
                    token = _sessions.TryGetValue(service, out var current) ? current.Token : CancellationToken.None;
                }
                else
                {
                    var owner = await ServiceDiagnostics.PortOwnerAsync(service.Port);
                    if (owner != null) throw new InvalidOperationException($"Порт {service.Port} занят: {owner}. Измените порт или остановите процесс в его приложении.");
                    _sessions.TryGetValue(service, out var previous);
                    previous?.Cancel();
                    previous?.Dispose();
                    var session = new CancellationTokenSource();
                    _sessions[service] = session;
                    token = session.Token;
                    service.Crashed -= HandleCrash;
                    service.Crashed += HandleCrash;
                    StartLocalhost(service);
                }
            }
            finally { gate.Release(); }
            if (!ServiceDiagnostics.HasProbe(service.Localhost))
            {
                service.SetStatus("Процесс запущен · без проверки");
                return;
            }
            var process = service.GetProcess();
            var deadline = DateTime.UtcNow.AddSeconds(Math.Clamp(service.Localhost.ReadyTimeoutSeconds, 1, 300));
            while (!token.IsCancellationRequested && ReferenceEquals(process, service.GetProcess()) && service.IsRunning)
            {
                if (await ServiceDiagnostics.IsReadyAsync(service.Localhost, token))
                {
                    token.ThrowIfCancellationRequested();
                    if (!ReferenceEquals(process, service.GetProcess())) break;
                    service.SetStatus("Готов");
                    return;
                }
                if (DateTime.UtcNow >= deadline)
                {
                    service.SetStatus("Ошибка проверки готовности");
                    throw new TimeoutException($"{service.Name}: сервис не готов. Проверьте логи и адрес проверки.");
                }
                try { await Task.Delay(250, token); } catch (OperationCanceledException) { break; }
            }
            throw new OperationCanceledException($"{service.Name}: запуск прерван; зависимые сервисы не запущены.");
        }
        public async Task StopLocalhostAsync(LocalhostViewModel service)
        {
            if (_sessions.TryGetValue(service, out var session)) session.Cancel();
            _retries[service] = 0;
            var gate = Gate(service);
            await gate.WaitAsync();
            try
            {
                if (_sessions.TryGetValue(service, out var active)) active.Cancel();
                var process = service.GetProcess();
                if (process == null) { service.SetStopped(); return; }
                service.MarkStopping();
                service.SetStatus("Останавливается");
                try
                {
                    await _processManager.StopAsync(process, service.Localhost);
                    if (ReferenceEquals(process, service.GetProcess())) service.SetStopped();
                }
                catch { service.CancelStopping(); service.SetStatus("Ошибка остановки"); throw; }
            }
            finally { gate.Release(); }
        }
        public async Task RestartLocalhostAsync(LocalhostViewModel service)
        { await StopLocalhostAsync(service); await StartLocalhostAsync(service); }
        private async void HandleCrash(object? sender, EventArgs e)
        {
            if (sender is not LocalhostViewModel service) return;
            var entry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} · {service.Name} · аварийное завершение";
            Failures.Insert(0, entry);
            while (Failures.Count > 100) Failures.RemoveAt(Failures.Count - 1);
            try { new JsonFileStore<List<string>>("failures.json").Save(Failures.ToList()); }
            catch (Exception ex) { service.AddSystemLog("Не удалось сохранить историю: " + ex.Message); }
            if (!service.Localhost.AutoRestart || !_sessions.TryGetValue(service, out var session)) return;
            var token = session.Token;
            var attempt = _retries.GetValueOrDefault(service) + 1;
            _retries[service] = attempt;
            if (attempt > 3) { service.SetStatus("Ошибка · лимит 3 перезапусков"); return; }
            try
            {
                service.SetStatus($"Перезапуск {attempt}/3 через {attempt * 2} с");
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2), token);
                await StartLocalhostAsync(service);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { service.SetStatus("Ошибка перезапуска"); service.AddSystemLog(ex.Message); }
        }

        public void StartProject(ProjectViewModel project)
        {
            foreach (var localhost in project.Localhosts)
            {
                StartLocalhost(localhost);
            }
        }

        public void StopProject(ProjectViewModel project)
        {
            foreach (var localhost in project.Localhosts)
            {
                StopLocalhost(localhost);
            }
        }

        public void RestartProject(ProjectViewModel project)
        {
            StopProject(project);
            StartProject(project);
        }

        public void StartLocalhost(
            LocalhostViewModel localhost)
        {
            if (localhost.IsRunning)
            {
                return;
            }

            var process = _processManager.Start(
                localhost.Localhost
            );

            localhost.SetProcess(process);
        }

        public void StopLocalhost(LocalhostViewModel localhost)
        {
            var process = localhost.GetProcess();

            if (process == null)
            {
                localhost.SetStopped();
                return;
            }

            localhost.MarkStopping();

            try
            {
                _processManager.Stop(process);
                localhost.SetStopped();
            }
            catch
            {
                localhost.CancelStopping();
                throw;
            }
        }

        public void RestartLocalhost(
            LocalhostViewModel localhost)
        {
            StopLocalhost(localhost);
            StartLocalhost(localhost);
        }
    }
}
