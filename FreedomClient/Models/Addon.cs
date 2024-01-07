using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
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
            Manifest = string.Empty;
            Signature = string.Empty;
        }

        [AlsoNotifyFor(nameof(DisplayAuthor))]
        public string Author { get; set; }
        [JsonIgnore]
        public string DisplayAuthor => Author.Length > 22 ? string.Concat(Author.AsSpan(0, 19), "...") : Author;
        public string Version { get; set; }
        public string ImageSrc { get; set; }
        public string Description { get; set; }
        public string Title { get; set; }
        public bool IsInstalled { get; set; }
        public string Manifest { get; set; }
        public string Signature{ get; set; }
    }
}
