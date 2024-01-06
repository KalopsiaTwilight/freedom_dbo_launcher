using FreedomClient.Core;
using FreedomClient.Models;
using Newtonsoft.Json;
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
        private ApplicationState _appState;
        public AddonsRepository(HttpClient httpClient, ApplicationState appState)
        {
            _httpClient = httpClient;
            _appState = appState;
        }

        public async Task<List<Addon>> GetAddons()
        {
            var resp = await _httpClient.GetAsync(Constants.CdnUrl + "/client_content/addons.json");
            resp.EnsureSuccessStatusCode();
            var addonsJson = await resp.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<Addon>>(addonsJson);
            if (result == null)
            {
                result = new List<Addon>();
            }
            foreach(var addon in result)
            {
                if (_appState.InstalledAddons.ContainsKey(addon.Title))
                {
                    addon.IsInstalled = true;
                }
            }
            return result;
        }
    }
}
