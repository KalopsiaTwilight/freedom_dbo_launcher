using FreedomClient.Models;
using System.Collections.Generic;
using System.IO;

namespace FreedomClient.Migrations
{
    internal class AddInstalledAddonsMigration : IAppStateMigration
    {
        public bool Apply(ApplicationState oldState)
        {
            oldState.InstalledAddons = new List<Addon>{
                new Addon {
                    Manifest = "https://cdn.wowfreedom-rp.com/client_content/addons/Elephant.manifest",
                    Title =  "Elephant",
                    Signature =  "https://cdn.wowfreedom-rp.com/client_content/addons/Elephant.signature",
                    ImageSrc = "https://media.forgecdn.net/avatars/64/706/636155467072456515.jpeg",
                    Version = "9.2.7",
                    Author = "AllInOneMighty",
                    Description= "A friendly companion that remembers the chat.",
                    IsInstalled = true
                },
                new Addon {
                    Manifest = "https://cdn.wowfreedom-rp.com/client_content/addons/Freedom_Builder.manifest",
                    Title = "Freedom Builder",
                    Signature = "https://cdn.wowfreedom-rp.com/client_content/addons/Freedom_Builder.signature",
                    ImageSrc = "https://mm.wowfreedom-rp.com/assets/freedom_icon.png",
                    Version = "1.0",
                    Author = "KalopsiaTwilight",
                    Description = "Graphical user interface of various build commands and features to assist playing on Freedom WoW.",
                    IsInstalled = true
                },
                new Addon {
                    Manifest = "https://cdn.wowfreedom-rp.com/client_content/addons/Freedom_Helper.manifest",
                    Title = "Freedom Helper",
                    Signature = "https://cdn.wowfreedom-rp.com/client_content/addons/Freedom_Helper.signature",
                    ImageSrc = "https://mm.wowfreedom-rp.com/assets/freedom_icon.png",
                    Version = "1.0",
                    Author = "KalopsiaTwilight",
                    Description = "Graphical user interface of various commands and features to assist playing on Freedom WoW.",
                    IsInstalled = true
                },
                new Addon {
                    Manifest = "https://cdn.wowfreedom-rp.com/client_content/addons/GOMove.manifest",
                    Title = "GOMove UI",
                    Signature = "https://cdn.wowfreedom-rp.com/client_content/addons/GOMove.signature",
                    ImageSrc = "https://rochet2.github.io/images/icon_GOMove.png",
                    Version = "9.2.5",
                    Author = "Rochet2",
                    Description = "Only usable by those with storyteller permissions.",
                    IsInstalled = true
                },
                new Addon {
                    Manifest = "https://cdn.wowfreedom-rp.com/client_content/addons/MogIt.manifest",
                    Title = "MogIt",
                    Signature = "https://cdn.wowfreedom-rp.com/client_content/addons/MogIt.signature",
                    ImageSrc = "https://media.forgecdn.net/avatars/61/118/636141604486701240.png",
                    Version = "3.9.7",
                    Author = "Aelobin (The Maelstrom EU) & Lombra (Defias Brotherhood EU)",
                    Description = "Transmogrification Assistant",
                    IsInstalled = true
                },
                new Addon {
                    Manifest = "https://cdn.wowfreedom-rp.com/client_content/addons/Outfitter.manifest",
                    Title = "Outfitter",
                    Signature = "https://cdn.wowfreedom-rp.com/client_content/addons/Outfitter.signature",
                    ImageSrc = "https://media.forgecdn.net/avatars/59/155/636141457048822978.png",
                    Version = "6.0.1",
                    Author = "John Stephen",
                    Description = "Clothing and weapon management and automated equipment changes",
                    IsInstalled = true
                },
                new Addon {
                    Manifest = "https://cdn.wowfreedom-rp.com/client_content/addons/Prat.manifest",
                    Title = "Prat",
                    Signature = "https://cdn.wowfreedom-rp.com/client_content/addons/Prat.signature",
                    ImageSrc = "https://media.forgecdn.net/avatars/823/915/638208715390273976.png",
                    Version = "3.9.1",
                    Author = "Sylvanaar (Sylvaann - Proudmoore/US-Alliance)",
                    Description = "A framework for chat ehancement modules.",
                    IsInstalled = true
                },
                new Addon {
                    Manifest = "https://cdn.wowfreedom-rp.com/client_content/addons/SlashIn.manifest",
                    Title = "SlashIn",
                    Signature = "https://cdn.wowfreedom-rp.com/client_content/addons/SlashIn.signature",
                    ImageSrc = "https://media.forgecdn.net/avatars/231/993/637064120845479885.png",
                    Version = "9.2.4",
                    Author = "Funkydude",
                    Description = "Provides the /in command for delayed execution.",
                    IsInstalled = true
                },
                new Addon {
                    Manifest = "https://cdn.wowfreedom-rp.com/client_content/addons/totalRP3.manifest",
                    Title = "Total RP 3",
                    Signature = "https://cdn.wowfreedom-rp.com/client_content/addons/totalRP3.signature",
                    ImageSrc = "https://media.forgecdn.net/avatars/219/404/637015802143725785.png",
                    Version = "2.3.13",
                    Author = "Telkostrasz & Ellypse",
                    Description = "The best roleplaying addon for World of Warcraft.",
                    IsInstalled = true
                }
            };
            return true;
        }
    }
}
