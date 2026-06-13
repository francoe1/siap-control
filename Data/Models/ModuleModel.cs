using System;

namespace SiapControl.Data.Models
{
    public class ModuleModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string AppName { get; set; }
        public string AppVersion { get; set; }
        public string ExecutableName { get; set; } = string.Empty;
        public string IconName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string FileDescription { get; set; } = string.Empty;
        public string InternalName { get; set; } = string.Empty;
        public string OriginalFilename { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
        public string FileVersion { get; set; } = string.Empty;
        public DateTime LastUpdate { get; set; }    
    }
}
