using System.Collections.ObjectModel;
using System.Globalization;
using PdvBalanca.Services;

namespace PdvBalanca;

public partial class MainPage : ContentPage
{
    private static readonly CultureInfo CulturaBR = new("pt-BR");
    private readonly PdvApiClient _api = new();
    private readonly ObservableCollection<LancamentoVm> _ultimos = new();
    private readonly ObservableCollection<ProdutoVm> _produtos = new();
    private bool _carregandoProdutos;
    private bool _produtosCarregados;

    public MainPage()
    {
        InitializeComponent();
        UltimosView.ItemsSource = _ultimos;
        ProdutosView.ItemsSource = _produtos;
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
        CardCardapio.IsVisible = autenticado;
        CardUltimos.IsVisible = autenticado && _ultimos.Count > 0;

        LoginErro.IsVisible = false;
        StatusLabel.IsVisible = false;

        if (autenticado)
        {
            ComandaEntry.Focus();
            // Carrega produtos quando autentica pela primeira vez.
            if (!_produtosCarregados && !_carregandoProdutos)
            {
                _ = CarregarProdutosAsync();
            }
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

    private async void OnAtualizarCardapioClicked(object? sender, EventArgs e)
    {
        await CarregarProdutosAsync();
    }

    private async Task CarregarProdutosAsync()
    {
        if (_carregandoProdutos) return;
        _carregandoProdutos = true;
        try
        {
            CarregandoIndicator.IsVisible = true;
            CarregandoIndicator.IsRunning = true;
            CardapioErroLabel.IsVisible = false;
            CardapioVazioLabel.IsVisible = false;

            var produtos = await _api.ListarProdutosAsync(AppConfig.ApiUrl, AppConfig.Pin);

            _produtos.Clear();
            foreach (var p in produtos.Where(p => p.Ativo))
            {
                _produtos.Add(new ProdutoVm(p));
            }
            _produtosCarregados = true;

            CardapioVazioLabel.IsVisible = _produtos.Count == 0;
        }
        catch (Exception ex)
        {
            CardapioErroLabel.Text = $"Falha ao carregar cardápio: {ex.Message}";
            CardapioErroLabel.IsVisible = true;
        }
        finally
        {
            CarregandoIndicator.IsRunning = false;
            CarregandoIndicator.IsVisible = false;
            _carregandoProdutos = false;
        }
    }

    private async void OnProdutoTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject bo || bo.BindingContext is not ProdutoVm vm)
            return;

        if (!TryLerComanda(out var numero)) return;

        var produto = vm.Produto;
        try
        {
            if (produto.PrecoFixo)
            {
                // Toque único adiciona 1 unidade direto, sem popup.
                await _api.AdicionarItemPorProdutoAsync(AppConfig.ApiUrl, AppConfig.Pin, numero, produto.Id, quantidade: 1, valor: null);
                RegistrarLancamento(numero, produto.Nome, produto.Preco, quantidade: 1);
                MostrarStatus($"Comanda #{numero} — {produto.Nome} adicionado.", erro: false);
            }
            else
            {
                // Por kilo: abre popup pra digitar o valor lido na balança.
                var entrada = await DisplayPromptAsync(
                    title: produto.Nome,
                    message: "Valor exibido na balança (R$):",
                    accept: "Adicionar",
                    cancel: "Cancelar",
                    placeholder: "Ex: 35,50",
                    keyboard: Keyboard.Numeric);

                if (string.IsNullOrWhiteSpace(entrada)) return;
                if (!TryParseValor(entrada, out var valor))
                {
                    MostrarStatus("Valor inválido.", erro: true);
                    return;
                }

                await _api.AdicionarItemPorProdutoAsync(AppConfig.ApiUrl, AppConfig.Pin, numero, produto.Id, quantidade: 1, valor: valor);
                RegistrarLancamento(numero, produto.Nome, valor, quantidade: 1);
                MostrarStatus($"Comanda #{numero} — {produto.Nome} R$ {valor.ToString("N2", CulturaBR)}.", erro: false);
            }
        }
        catch (Exception ex)
        {
            MostrarStatus($"Falha ao adicionar: {ex.Message}", erro: true);
        }
    }

    private async void OnAvulsoClicked(object? sender, EventArgs e)
    {
        if (!TryLerComanda(out var numero)) return;

        var descricao = await DisplayPromptAsync(
            title: "Item avulso",
            message: "Descrição do item:",
            accept: "Próximo",
            cancel: "Cancelar",
            placeholder: "Ex: Sobremesa",
            maxLength: 200);

        if (string.IsNullOrWhiteSpace(descricao)) return;

        var valorTexto = await DisplayPromptAsync(
            title: descricao,
            message: "Valor (R$):",
            accept: "Adicionar",
            cancel: "Cancelar",
            placeholder: "Ex: 12,00",
            keyboard: Keyboard.Numeric);

        if (string.IsNullOrWhiteSpace(valorTexto)) return;
        if (!TryParseValor(valorTexto, out var valor))
        {
            MostrarStatus("Valor inválido.", erro: true);
            return;
        }

        try
        {
            await _api.AdicionarItemAvulsoAsync(AppConfig.ApiUrl, AppConfig.Pin, numero, descricao.Trim(), valor);
            RegistrarLancamento(numero, descricao.Trim(), valor, quantidade: 1);
            MostrarStatus($"Comanda #{numero} — {descricao.Trim()} R$ {valor.ToString("N2", CulturaBR)}.", erro: false);
        }
        catch (Exception ex)
        {
            MostrarStatus($"Falha ao adicionar: {ex.Message}", erro: true);
        }
    }

    private bool TryLerComanda(out int numero)
    {
        numero = 0;
        if (!int.TryParse(ComandaEntry.Text?.Trim(), out numero) || numero <= 0)
        {
            MostrarStatus("Digite o número da comanda primeiro.", erro: true);
            ComandaEntry.Focus();
            return false;
        }
        return true;
    }

    private static bool TryParseValor(string entrada, out decimal valor)
    {
        var texto = entrada.Trim().Replace(',', '.');
        return decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out valor) && valor > 0;
    }

    private void RegistrarLancamento(int comanda, string descricao, decimal valor, int quantidade)
    {
        _ultimos.Insert(0, new LancamentoVm(comanda, descricao, valor, quantidade));
        while (_ultimos.Count > 10)
            _ultimos.RemoveAt(_ultimos.Count - 1);
        CardUltimos.IsVisible = true;
    }

    private void MostrarStatus(string mensagem, bool erro)
    {
        StatusLabel.Text = mensagem;
        StatusLabel.TextColor = erro ? Color.FromArgb("#dc2626") : Color.FromArgb("#16a34a");
        StatusLabel.IsVisible = true;
    }

    public sealed record LancamentoVm(int Comanda, string Descricao, decimal Valor, int Quantidade)
    {
        public string Hora { get; } = DateTime.Now.ToString("HH:mm");
        public string ComandaTexto => $"#{Comanda}";
        public string ValorTexto => $"R$ {Valor.ToString("N2", CulturaBR)}";
        public string LinhaPrincipal => Quantidade > 1
            ? $"{ComandaTexto} · {Descricao} ×{Quantidade}"
            : $"{ComandaTexto} · {Descricao}";
    }
}

public sealed class ProdutoVm
{
    public ProdutoVm(ProdutoDto produto)
    {
        Produto = produto;
    }

    public ProdutoDto Produto { get; }
    public string Nome => Produto.Nome;

    public string LinhaPreco => Produto.PrecoFixo
        ? $"R$ {Produto.Preco.ToString("N2", new CultureInfo("pt-BR"))}"
        : "Por kilo";

    public string TextoAcao => Produto.PrecoFixo
        ? "Toque pra adicionar 1×"
        : "Toque pra digitar o valor";

    public Color CorBorda => Produto.PrecoFixo
        ? Color.FromArgb("#16a34a")  // verde — adiciona direto
        : Color.FromArgb("#ea580c"); // laranja — exige valor

    public Color CorFundo => Produto.PrecoFixo
        ? Color.FromArgb("#f0fdf4")
        : Color.FromArgb("#fff7ed");
}
