using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ClientLocal.Services.Api
{
    public class ConnectivityService
    {
        private readonly HttpClient _httpClient;

        public ConnectivityService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost/ProyectoFinalDS/src/Backend/app.php/")
            };
        }

        public async Task<bool> IsBackendAvailableAsync()
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "");
                using var response = await _httpClient.SendAsync(request);

                return response != null;
            }
            catch
            {
                return false;
            }
        }
    }
}