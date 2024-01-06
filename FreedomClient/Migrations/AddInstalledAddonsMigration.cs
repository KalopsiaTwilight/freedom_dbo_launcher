using FreedomClient.Models;
using System.Collections.Generic;
using System.IO;

namespace FreedomClient.Migrations
{
    internal class AddInstalledAddonsMigration : IAppStateMigration
    {
        public bool Apply(ApplicationState oldState)
        {
            //var addonsFolder = Path.Join(oldState.InstallPath, "_retail_/Interface/Addons");

            oldState.InstalledAddons = new Dictionary<string, string>{
                { "Elephant", "9.2.7" },
                { "Freedom Builder", "1.0" },
                { "Freedom Helper", "1.0" },
                { "GOMove UI", "9.2.5" },
                { "MogIt", "3.9.7" },
                { "Outfitter", "6.0.1" },
                { "Prat", "3.9.1" },
                { "SlashIn", "9.2.4" },
                { "Total RP 3", "2.3.13" },
            };
            return true;
        }
    }
}
