using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace LibrarySystem.UI.Helpers;

internal static class ApiResponseHelper
{
    public static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, string fallback)
    {
        try
        {
            var payload = await response.Content.ReadFromJsonAsync<ApiErrorDto>();
            if (!string.IsNullOrWhiteSpace(payload?.Message))
            {
                return payload.Message;
            }
        }
        catch
        {
            // ignore parse errors
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Oturum geçersiz. Lütfen tekrar giriş yapın.",
            HttpStatusCode.Forbidden => "Bu işlem için yetkiniz yok.",
            HttpStatusCode.BadRequest => fallback,
            _ => $"{fallback} (HTTP {(int)response.StatusCode})"
        };
    }

    private sealed class ApiErrorDto
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
