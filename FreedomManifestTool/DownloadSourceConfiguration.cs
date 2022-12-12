using FreedomClient.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreedomManifestTool
{
    internal class DownloadSourceConfiguration
    {
        public string HttpDownloadSourceUri { get; set; } = Constants.CdnUrl;

        public List<DownloadSource> DownloadSources { get; set; } = new List<DownloadSource>();
        public Dictionary<string, int> FileSources { get; set; } = new Dictionary<string, int>();
    }
}
