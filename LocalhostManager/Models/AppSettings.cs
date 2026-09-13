using System.Collections.Generic;

namespace LocalhostManager.Models
{
    public class AppSettings
    {
        public List<string> Commands { get; set; } =
        [
            "npm run dev",
            "npm start",
            "yarn dev",
            "yarn start",
            "pnpm dev",
            "pnpm start",
            "bun dev",
            "bun start",
            "node server.js",
            "python manage.py runserver",
            "dotnet run"
        ];

        public bool StartWithWindows { get; set; }

        public bool MinimizeToTray { get; set; }

        public bool CloseToTray { get; set; }

        public string Theme { get; set; } = "dark";

        public string Language { get; set; } = "ru";

        public bool NotifyOnStart { get; set; } = true;

        public bool NotifyOnStop { get; set; } = true;

        public bool NotifyOnCrash { get; set; } = true;

        public List<string> FavoriteLocalhosts { get; set; } = [];
    }
}