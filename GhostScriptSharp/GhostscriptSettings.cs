using System.Drawing;
using GhostscriptSharp.Settings;

namespace GhostscriptSharp
{
    /// <summary>
    /// Ghostscript settings
    /// </summary>
    public class GhostscriptSettings
    {
        public GhostscriptSettings()
        {
            Size = new Settings.GhostscriptPageSize();
            Page = new Settings.GhostscriptPages();
        }

        public GhostscriptDevices Device { get; set; }

        public GhostscriptPages Page { get; set; }

        public Size Resolution { get; set; }

        public GhostscriptPageSize Size { get; set; }
    }
}