using FreedomClient.Core;
using FreedomClient.Models;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
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
        private ApplicationState _appState;
        public PatchesRepository(HttpClient httpClient, ApplicationState appState)
        {
            _httpClient = httpClient;
            _appState = appState;
        }

        public async Task<List<Patch>> GetPatches()
        {
            var resp = await _httpClient.GetAsync(Constants.CdnUrl + "/client_content/patches.json");
            resp.EnsureSuccessStatusCode();
            var patchesJson = await resp.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<Patch>>(patchesJson);
            result ??= new List<Patch>();
            foreach (var patch in result)
            {
                if (_appState.InstalledPatches.Any(x => x.Title == patch.Title))
                {
                    patch.IsInstalled = true;
                }
            }
            return result;
        }
    }
}
