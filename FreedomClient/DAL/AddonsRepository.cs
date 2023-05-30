using FreedomClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FreedomClient.DAL
{
    public class AddonsRepository: IRepository
    {
        private HttpClient _httpClient;
        public AddonsRepository(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Addon>> GetAddons()
        {
            // TODO: Call API / retrieve json config.
            await Task.Delay(1000);
            return new List<Addon>
            {
                new Addon {
                    Title = "Mock Addon 1",
                    Description = "This is a sample addon that I made to show you what an addon could look like.",
                    Author = "KalopsiaTwilight",
                    ImageSrc = "https://placekitten.com/180/120",
                    IsInstalled = false,
                    Version = "1.0.0"
                }
            };
        }
    }
}
