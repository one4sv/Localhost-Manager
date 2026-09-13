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

            _processManager.Stop(process);

            localhost.SetStopped();
        }

        public void RestartLocalhost(
            LocalhostViewModel localhost)
        {
            StopLocalhost(localhost);
            StartLocalhost(localhost);
        }
    }
}