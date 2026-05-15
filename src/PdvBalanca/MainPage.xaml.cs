using System.Collections.ObjectModel;
using System.Globalization;
using PdvBalanca.Services;

namespace PdvBalanca;

public partial class MainPage : ContentPage
{
    private static readonly CultureInfo CulturaBR = new("pt-BR");
    private readonly PdvApiClient _api = new();
    private readonly ObservableCollection<LancamentoVm> _ultimos = new();

    public MainPage()
    {
        InitializeComponent();
        UltimosView.ItemsSource = _ultimos;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        AtualizarEstado();
    }

    private void AtualizarEstado()
    {
        var configurado = AppConfig.Configurado;
        var autenticado = AppConfig.Autenticado;

        CardConfigurar.IsVisible = !configurado;
        CardLogin.IsVisible = configurado && !autenticado;
        CardBalanca.IsVisible = autenticado;
        CardUltimos.IsVisible = autenticado && _ultimos.Count > 0;

        LoginErro.IsVisible = false;
        StatusLabel.IsVisible = false;

        if (autenticado)
        {
            ComandaEntry.Focus();
        }
        else if (configurado)
        {
            PinEntry.Text = string.Empty;
            PinEntry.Focus();
        }
    }

    private async void OnConfiguracoesClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new ConfiguracoesPage());
    }

    private async void OnEntrarClicked(object? sender, EventArgs e)
    {
        var pin = PinEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(pin))
        {
            MostrarLoginErro("Informe o PIN.");
            return;
        }

        try
        {
            var ok = await _api.ValidarPinAsync(AppConfig.ApiUrl, pin);
            if (!ok)
            {
                MostrarLoginErro("PIN inválido.");
                return;
            }
            AppConfig.Pin = pin;
            AtualizarEstado();
        }
        catch (Exception ex)
        {
            MostrarLoginErro($"Não foi possível conectar à API: {ex.Message}");
        }
    }

    private void MostrarLoginErro(string mensagem)
    {
        LoginErro.Text = mensagem;
        LoginErro.IsVisible = true;
    }

    private async void OnAdicionarClicked(object? sender, EventArgs e)
    {
        StatusLabel.IsVisible = false;

        if (!int.TryParse(ComandaEntry.Text?.Trim(), out var numero) || numero <= 0)
        {
            MostrarStatus("Número de comanda inválido.", erro: true);
            return;
        }

        var valorTexto = (ValorEntry.Text ?? string.Empty).Trim().Replace(',', '.');
        if (!decimal.TryParse(valorTexto, NumberStyles.Number, CultureInfo.InvariantCulture, out var valor) || valor <= 0)
        {
            MostrarStatus("Valor inválido.", erro: true);
            return;
        }

        AdicionarBtn.IsEnabled = false;
        AdicionarBtn.Text = "Enviando...";

        try
        {
            await _api.AdicionarItemAsync(AppConfig.ApiUrl, AppConfig.Pin, numero, "Prato kilo", valor);

            _ultimos.Insert(0, new LancamentoVm(numero, valor));
            while (_ultimos.Count > 10)
                _ultimos.RemoveAt(_ultimos.Count - 1);
            CardUltimos.IsVisible = true;

            ComandaEntry.Text = string.Empty;
            ValorEntry.Text = string.Empty;
            ComandaEntry.Focus();
            MostrarStatus($"Comanda #{numero} — R$ {valor.ToString("N2", CulturaBR)} adicionado.", erro: false);
        }
        catch (Exception ex)
        {
            MostrarStatus($"Falha ao adicionar: {ex.Message}", erro: true);
        }
        finally
        {
            AdicionarBtn.IsEnabled = true;
            AdicionarBtn.Text = "Adicionar à comanda";
        }
    }

    private void MostrarStatus(string mensagem, bool erro)
    {
        StatusLabel.Text = mensagem;
        StatusLabel.TextColor = erro ? Color.FromArgb("#dc2626") : Color.FromArgb("#16a34a");
        StatusLabel.IsVisible = true;
    }

    public sealed record LancamentoVm(int Comanda, decimal Valor)
    {
        public string Hora { get; } = DateTime.Now.ToString("HH:mm");
        public string ComandaTexto => $"#{Comanda}";
        public string ValorTexto => $"R$ {Valor.ToString("N2", CulturaBR)}";
    }
}
