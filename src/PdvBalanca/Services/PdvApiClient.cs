using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace PdvBalanca.Services;

public class PdvApiClient
{
    private readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    public async Task<bool> ValidarPinAsync(string baseUrl, string pin, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(pin))
            return false;
        var url = $"{baseUrl.TrimEnd('/')}/api/auth/validar-pin";
        var resp = await _http.PostAsJsonAsync(url, new { pin }, ct);
        if (!resp.IsSuccessStatusCode) return false;
        var result = await resp.Content.ReadFromJsonAsync<ValidarPinResponse>(cancellationToken: ct);
        return result?.Valido ?? false;
    }

    public async Task AdicionarItemAsync(string baseUrl, string pin, int numeroComanda, string descricao, decimal valor, CancellationToken ct = default)
    {
        var url = $"{baseUrl.TrimEnd('/')}/api/comandas/{numeroComanda}/itens";
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new
            {
                descricao,
                valor,
                origem = 0 // Balanca
            })
        };
        req.Headers.Add("X-Pin", pin);
        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"HTTP {(int)resp.StatusCode}: {body}");
        }
    }

    private sealed class ValidarPinResponse
    {
        [JsonPropertyName("valido")]
        public bool Valido { get; set; }
    }
}
