using System.Net.Http.Headers;
using System.Text.Json;

namespace JobScraper.IntegrationTests.Utils;

public static class HttpExtensions
{
    private static readonly MediaTypeHeaderValue jsonTypeHeader = MediaTypeHeaderValue.Parse("application/json");

    extension(HttpContent)
    {
        public static HttpContent From(object? obj, JsonSerializerOptions? jsonOptions = null)
        {
            ArgumentNullException.ThrowIfNull(obj);

            var json = JsonSerializer.SerializeToUtf8Bytes(obj, jsonOptions);
            var content = new ByteArrayContent(json);
            content.Headers.ContentType = jsonTypeHeader;

            return content;
        }

        public static HttpContent FromJson(string json)
        {
            var content = new StringContent(json);
            content.Headers.ContentType = jsonTypeHeader;

            return content;
        }
    }
}
