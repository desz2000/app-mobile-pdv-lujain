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

    public async Task<List<ProdutoDto>> ListarProdutosAsync(string baseUrl, string pin, CancellationToken ct = default)
    {
        var url = $"{baseUrl.TrimEnd('/')}/api/produtos";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("X-Pin", pin);
        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"HTTP {(int)resp.StatusCode}: {body}");
        }
        var produtos = await resp.Content.ReadFromJsonAsync<List<ProdutoDto>>(cancellationToken: ct);
        return produtos ?? new();
    }

    public async Task AdicionarItemPorProdutoAsync(
        string baseUrl,
        string pin,
        int numeroComanda,
        int produtoId,
        int quantidade,
        decimal? valor,
        CancellationToken ct = default)
    {
        var url = $"{baseUrl.TrimEnd('/')}/api/comandas/{numeroComanda}/itens";
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new
            {
                produtoId,
                quantidade,
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

    public async Task AdicionarItemAvulsoAsync(
        string baseUrl,
        string pin,
        int numeroComanda,
        string descricao,
        decimal valor,
        CancellationToken ct = default)
    {
        var url = $"{baseUrl.TrimEnd('/')}/api/comandas/{numeroComanda}/itens";
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new
            {
                descricao,
                valor,
                quantidade = 1,
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

public sealed class ProdutoDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("nome")]
    public string Nome { get; set; } = string.Empty;

    [JsonPropertyName("preco")]
    public decimal Preco { get; set; }

    /// <summary>0 = PorKilo, 1 = PrecoFixo.</summary>
    [JsonPropertyName("tipo")]
    public int Tipo { get; set; }

    [JsonPropertyName("ativo")]
    public bool Ativo { get; set; }

    public bool PrecoFixo => Tipo == 1;
    public bool PorKilo => Tipo == 0;
}
