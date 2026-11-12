using System;

namespace LocalhostManager.Models
{
    public class Localhost
    {
        public string Id { get; set; } =
            Guid.NewGuid().ToString("N");

        public required string Name { get; set; }

        public string? Description { get; set; }

        public string? Command { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsRunning { get; set; } = false;

        public string? Port { get; set; }

        public required string Path { get; set; }

        public int StartOrder { get; set; }
        public int ReadyTimeoutSeconds { get; set; } = 30;
        public bool AutoRestart { get; set; }
        public string? StopCommand { get; set; }
        public string? HealthUrl { get; set; }

        public string? Url { get; set; }
    }
}
