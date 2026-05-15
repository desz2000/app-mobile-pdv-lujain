# PDV Lujain — Mobile (Balança)

App Android para o operador da balança lançar pratos nas comandas do PDV Lujain.

Faz parte do sistema [`desz2000/app-pdv-lujain`](https://github.com/desz2000/app-pdv-lujain) — este repositório contém **só** o app mobile. A API e o front do caixa estão no repo principal.

## Fluxo

1. Cliente passa na balança e o operador vê o preço no visor.
2. Operador abre o app, digita o **número da comanda** do cliente e o **valor exibido**.
3. Toca em **Adicionar à comanda**. O app chama `POST /api/comandas/{numero}/itens` na API do PDV com `origem=Balanca`.
4. O caixa, ao buscar a comanda no PC, já vê o item lançado.

Se a comanda ainda não existe no sistema, a API cria automaticamente.

## Stack

- .NET MAUI 8 (target único: `net8.0-android`)
- Sem libs externas além do MAUI Controls — sem MVVM frameworks, sem DI containers; código direto pra ficar fácil de manter.

## Como o operador instala

1. Baixar o APK do **Release** do GitHub (o CI publica como artifact em todo PR e push) ou pedir pra quem fez a build mandar o `.apk` por outro meio.
2. No celular Android: **Configurações → Segurança → Instalar apps de fontes desconhecidas** (autoriza só a fonte que vai abrir o APK).
3. Abrir o APK e instalar.
4. Na primeira execução, **Configurações** → digitar `http://IP-DO-PC-DO-CAIXA:5170` (ex.: `http://192.168.0.10:5170`) → tocar em **Testar conexão** → **Salvar e voltar**.
5. Digitar o PIN (configurado em `appsettings.json` da API).
6. Pronto: a tela principal mostra os campos de número de comanda e valor.

> Importante: o celular e o PC do caixa precisam estar na **mesma rede Wi‑Fi**.

## Como rodar localmente (dev)

Requisitos:
- .NET 8 SDK
- Workload `maui-android` (`dotnet workload install maui-android`)
- Android SDK (platform-tools, platforms;android-34, build-tools;34.0.0)
- Java 17

Build/debug:

```bash
# Build do projeto
dotnet build src/PdvBalanca/PdvBalanca.csproj -c Debug -f net8.0-android

# Gerar APK assinado para instalar
dotnet publish src/PdvBalanca/PdvBalanca.csproj -c Release -f net8.0-android -p:AndroidPackageFormat=apk
# O APK fica em src/PdvBalanca/bin/Release/net8.0-android/publish/*-Signed.apk
```

## Estrutura

```
src/PdvBalanca/
├── MainPage.xaml(.cs)           tela principal (cards: configurar / login / balanca / ultimos)
├── ConfiguracoesPage.xaml(.cs)  URL da API e logout
├── Services/
│   ├── AppConfig.cs             persistencia de URL e PIN via Preferences
│   └── PdvApiClient.cs          chamadas HTTP para a API
├── AppShell.xaml(.cs)           shell raiz
├── App.xaml(.cs)                bootstrap MAUI
└── Platforms/Android/
    ├── AndroidManifest.xml      libera cleartext traffic (HTTP) na LAN
    └── Resources/xml/network_security_config.xml
```

## API consumida

Exige a API do PDV principal rodando (porta 5170 por padrão):

- `POST /api/auth/validar-pin` — valida o PIN digitado no app
- `POST /api/comandas/{numero}/itens` — adiciona item à comanda (cria a comanda automaticamente se não existir), com cabeçalho `X-Pin: {pin}` e payload `{"descricao":"Prato kilo","valor":35.5,"origem":0}` (origem `0` = Balança)
