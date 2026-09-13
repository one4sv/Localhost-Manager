using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalhostManager.Models
{
    public class Project
    {
        public required string Name { get; set; }
        public List<Localhost> Localhosts { get; set; } = [];
        public string? Description { get; set; }
    }
}
