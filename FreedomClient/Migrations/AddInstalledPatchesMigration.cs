using FreedomClient.Models;
using System.Collections.Generic;
using System.IO;

namespace FreedomClient.Migrations
{
    internal class AddInstalledPatchesMigration : IAppStateMigration
    {
        public bool Apply(ApplicationState oldState)
        {
            oldState.InstalledPatches = new List<Patch>{
                new Patch {
                    Manifest = "https://cdn.wowfreedom-rp.com/client_content/patches/dragon_patch.manifest",
                    Title =  "HD Dragon Patch",
                    Signature =  "https://cdn.wowfreedom-rp.com/client_content/patches/dragon_patch.signature",
                    ImageSrc = "https://placekitten.com/180/120",
                    Version = "1.0.0",
                    Author = "Sam",
                    Description= "Updates the dragon models in game to have a higher graphical fidelity!",
                    IsInstalled = true,
                    ListFileMapping = new Dictionary<string, int>
                    {
                        { "northrenddragon02.skin", 474066 },
                        { "dragonskin3black.blp", 123488 },
                        { "northrenddragon03.skin", 474115 },
                        { "dragonskin3emeriss.blp", 123491 },
                        { "dragon00.skin", 473383 },
                        { "dragonskin2red.blp", 123485 },
                        { "northrenddragon00.skin", 474038 },
                        { "dragonskin3blue.blp", 123489 },
                        { "dragonskin3green.blp", 123492 },
                        { "dragonskin3bronze.blp", 123490 },
                        { "dragon01.skin", 473384 },
                        { "dragonskin2emeriss.blp", 123482 },
                        { "northrenddragon01.skin", 474087 },
                        { "dragonskin2green.blp", 123483 },
                        { "dragonskin2blue.blp", 123480 },
                        { "dragonskin2black.blp", 123479 },
                        { "northrenddragon.m2", 123497 },
                        { "dragon02.skin", 474002 },
                        { "dragon.m2", 123225 },
                        { "dragonskin3red.blp", 123494 },
                        { "dragonskin2bronze.blp", 123481 },
                        { "dragon03.skin", 474006 }
                    }
                }
            };
            return true;
        }
    }
}
