using System;
using System.Net.Http;
using System.Net.Http.Headers;
using ClientLocal.Services.Session;

namespace ClientLocal.Services.Api
{
    public static class ApiClientFactory
    {
        public static HttpClient Create(SessionService sessionService)
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri("http://localhost/ProyectoFinalDS/src/Backend/app.php/"),
                Timeout = TimeSpan.FromSeconds(30)
            };

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrWhiteSpace(sessionService.JwtToken))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", sessionService.JwtToken);
            }

            return client;
        }
    }
}
