using System;

namespace SiapControl
{
    public class UpdateRegister
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;

        public string AppName { get; set; } = "Default";
        public string AppVersion { get; set; } = "0";
    }
}