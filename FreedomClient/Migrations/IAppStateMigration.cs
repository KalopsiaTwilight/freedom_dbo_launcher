using FreedomClient.Models;

namespace FreedomClient.Migrations
{
    public interface IAppStateMigration
    {
        public bool Apply(ApplicationState oldState);
    }
}
