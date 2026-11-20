using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace HuaweiSolar.Worker.Api;

public sealed class PvApiClient(HttpClient http, IOptions<InverterOptions> options, ILogger<PvApiClient> logger)
{
    public async Task<HttpStatusCode> PostAsync(JsonObject payload, CancellationToken ct)
    {
        // Buffered body with Content-Length (like the old curl call), not chunked streaming.
        using var content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
        using var response = await http.PostAsync(options.Value.ApiUrl, content, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("API returned {Status}: {Body}", (int)response.StatusCode, body);
        }
        return response.StatusCode;
    }
}
