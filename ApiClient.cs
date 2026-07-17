using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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

        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly string saveUrl;
        private readonly string refreshUrl;

        public ApiClient(string saveUrl, string refreshUrl)
        {
            this.saveUrl = saveUrl;
            this.refreshUrl = refreshUrl;
        }

        public async Task<(UploadResult Result, string Detail)> UploadSaveAsync(SavePayload payload, string accessToken)
        {
            try
            {
                string json = JsonSerializer.Serialize(payload, jsonOptions);

                using var request = new HttpRequestMessage(HttpMethod.Post, this.saveUrl);
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

  
        /// <returns>The new access token, or null if the refresh window has closed or the call failed.</returns>
        public async Task<string?> RefreshTokenAsync(string oldToken)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, this.refreshUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", oldToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Content = new StringContent(string.Empty, Encoding.UTF8, "application/json");

                using HttpResponseMessage response = await http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return null;

                string json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<RefreshResponse>(json, jsonOptions)?.AccessToken;
            }
            catch
            {
                return null;
            }
        }

        public async Task<(UploadResult Result, string Detail)> UploadAvatarAsync(byte[] pngBytes, string avatarUrl, string accessToken)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, avatarUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using var content = new MultipartFormDataContent();
                var imageContent = new ByteArrayContent(pngBytes);
                imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                content.Add(imageContent, "avatar", "avatar.png");
                request.Content = content;

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

        /// <summary>The token half of the server's refresh response.</summary>
        private class RefreshResponse
        {
            [JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }
        }
    }
}
