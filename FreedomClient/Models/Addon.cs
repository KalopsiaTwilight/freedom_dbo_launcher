using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreedomClient.Models
{
    [AddINotifyPropertyChangedInterface]
    public class Addon
    {
        public Addon()
        {
            Author = string.Empty;
            Version = string.Empty;
            ImageSrc = string.Empty;
            Description = string.Empty;
            Title = string.Empty;
            IsInstalled = false;
        }

        public string Author { get; set; }
        public string Version { get; set; }
        public string ImageSrc { get; set; }
        public string Description { get; set; }
        public string Title { get; set; }
        public bool IsInstalled { get; set; }
    }
}
