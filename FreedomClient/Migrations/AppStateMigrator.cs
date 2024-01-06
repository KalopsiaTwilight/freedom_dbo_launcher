using FreedomClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreedomClient.Migrations
{
    public static class AppStateMigrator
    {
        public static bool Migrate(ApplicationState oldState)
        {
            bool status = true;
            if (Version.Parse(oldState.Version) < new Version(1, 1, 0))
            {
                status &= new AddInstalledAddonsMigration().Apply(oldState);
            }
            return status;
        }
    }
}
