namespace PdvBalanca.Services;

public static class AppConfig
{
    private const string ApiUrlKey = "api_url";
    private const string PinKey = "pin";

    public static string ApiUrl
    {
        get => Preferences.Default.Get(ApiUrlKey, string.Empty);
        set => Preferences.Default.Set(ApiUrlKey, value?.TrimEnd('/') ?? string.Empty);
    }

    public static string Pin
    {
        get => Preferences.Default.Get(PinKey, string.Empty);
        set => Preferences.Default.Set(PinKey, value ?? string.Empty);
    }

    public static bool Configurado => !string.IsNullOrWhiteSpace(ApiUrl);
    public static bool Autenticado => Configurado && !string.IsNullOrWhiteSpace(Pin);

    public static void Limpar()
    {
        Preferences.Default.Remove(PinKey);
    }
}
