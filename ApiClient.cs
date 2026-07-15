using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SaveFetch
{
    public enum UploadResult
    {
        Success,
        Unauthorized,
        Failed
    }

    /// <summary>Talks to the website's API. One shared HttpClient for the mod's lifetime.</summary>
    public class ApiClient
    {
        private static readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(30) };

        // camelCase property names so the JSON matches the schema in implement.md
        // (and Laravel's usual conventions) instead of C#'s PascalCase
        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly string baseUrl;

        public ApiClient(string baseUrl)
        {
            this.baseUrl = baseUrl.TrimEnd('/');
        }

        public async Task<(UploadResult Result, string Detail)> UploadSaveAsync(SavePayload payload, string accessToken)
        {
            try
            {
                string json = JsonSerializer.Serialize(payload, jsonOptions);

                using var request = new HttpRequestMessage(HttpMethod.Post, $"{this.baseUrl}/api/saves");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                using HttpResponseMessage response = await http.SendAsync(request);

                if (response.IsSuccessStatusCode)
                    return (UploadResult.Success, $"HTTP {(int)response.StatusCode}");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return (UploadResult.Unauthorized, "HTTP 401");

                string body = await response.Content.ReadAsStringAsync();
                return (UploadResult.Failed, $"HTTP {(int)response.StatusCode}: {Truncate(body, 200)}");
            }
            catch (Exception ex)
            {
                return (UploadResult.Failed, ex.Message);
            }
        }

        private static string Truncate(string value, int max)
            => value.Length <= max ? value : value[..max] + "…";
    }
}
