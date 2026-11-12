using LocalhostManager.Models;
using LocalhostManager.ViewModels;

namespace LocalhostManager.Services;

public sealed class StartupProjects
{
    private bool _applied;

    public async Task ApplyAsync(AppSettings settings, IEnumerable<ProjectViewModel> projects,
        Func<ProjectViewModel, Task> start, Action<ProjectViewModel, Exception> failed)
    {
        if (_applied) return;
        _applied = true;
        if (!settings.AutoStartProjects) return;
        foreach (var project in projects.Where(p => p.Project.AutoStart).ToArray())
            try { await start(project); } catch (Exception ex) { failed(project, ex); }
    }

    public void Apply(AppSettings settings, IEnumerable<ProjectViewModel> projects,
        Action<ProjectViewModel> start, Action<ProjectViewModel, Exception> failed)
    {
        if (_applied) return;
        _applied = true;
        if (!settings.AutoStartProjects) return;
        foreach (var project in projects.Where(p => p.Project.AutoStart))
        {
            try { start(project); }
            catch (Exception ex) { failed(project, ex); }
        }
    }
}
