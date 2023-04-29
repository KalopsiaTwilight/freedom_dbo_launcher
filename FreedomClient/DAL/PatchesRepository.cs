using FreedomClient.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FreedomClient.DAL
{
    public class PatchesRepository: IRepository
    {
        private HttpClient _httpClient;
        public PatchesRepository(HttpClient httpClient) 
        {
            _httpClient = httpClient;
        }

        public async Task<List<Patch>> GetPatches()
        {
            // TODO: Call API / retrieve json config.
            await Task.Delay(1000);
            return new List<Patch>
            {
                new Patch {
                    Title = "Mock Patch 1",
                    Description = "This is a sample patch that I made to show you what a patch could look like.",
                    Author = "KalopsiaTwilight",
                    ImageSrc = "https://placekitten.com/180/120",
                    IsInstalled = false,
                    Version = "1.0.0"
                }
            };
        }
    }
}
