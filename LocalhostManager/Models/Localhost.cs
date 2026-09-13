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

        public bool IsRunning { get; set; } = false;

        public string? Port { get; set; }

        public required string Path { get; set; }

        public string? Url { get; set; }
    }
}