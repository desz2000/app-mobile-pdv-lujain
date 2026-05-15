using PdvBalanca.Services;

namespace PdvBalanca;

public partial class ConfiguracoesPage : ContentPage
{
    private readonly PdvApiClient _api = new();

    public ConfiguracoesPage()
    {
        InitializeComponent();
        ApiUrlEntry.Text = AppConfig.ApiUrl;
    }

    private async void OnTestarClicked(object? sender, EventArgs e)
    {
        TestarStatus.IsVisible = false;
        var url = ApiUrlEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            MostrarTeste("Informe a URL.", erro: true);
            return;
        }

        TestarBtn.IsEnabled = false;
        TestarBtn.Text = "Testando...";
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var resp = await http.GetAsync($"{url.TrimEnd('/')}/swagger/index.html");
            MostrarTeste(resp.IsSuccessStatusCode
                ? "Conexão OK."
                : $"Resposta inesperada: HTTP {(int)resp.StatusCode}.",
                erro: !resp.IsSuccessStatusCode);
        }
        catch (Exception ex)
        {
            MostrarTeste($"Falha: {ex.Message}", erro: true);
        }
        finally
        {
            TestarBtn.IsEnabled = true;
            TestarBtn.Text = "Testar conexão";
        }
    }

    private void MostrarTeste(string mensagem, bool erro)
    {
        TestarStatus.Text = mensagem;
        TestarStatus.TextColor = erro ? Color.FromArgb("#dc2626") : Color.FromArgb("#16a34a");
        TestarStatus.IsVisible = true;
    }

    private async void OnSairClicked(object? sender, EventArgs e)
    {
        AppConfig.Limpar();
        await DisplayAlert("Pronto", "PIN apagado. Você precisará logar novamente.", "OK");
    }

    private async void OnSalvarClicked(object? sender, EventArgs e)
    {
        var url = ApiUrlEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            await DisplayAlert("Atenção", "Informe a URL da API antes de salvar.", "OK");
            return;
        }
        AppConfig.ApiUrl = url;
        await Navigation.PopAsync();
    }
}
