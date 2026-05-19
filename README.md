# PDV Lujain — Mobile (Balança)

App Android para o operador da balança lançar pratos nas comandas do PDV Lujain.

Faz parte do sistema [`desz2000/app-pdv-lujain`](https://github.com/desz2000/app-pdv-lujain) — este repositório contém **só** o app mobile. A API e o front do caixa estão no repo principal.

## Download do APK

> **Última versão:** [v1.0.0 — Cardápio em botões](https://github.com/desz2000/app-mobile-pdv-lujain/releases/latest) — baixe `pdv-balanca-v1.0.0.apk` na seção **Assets**.

O APK é assinado com a chave de debug do Android, pronto pra sideload. Não requer Play Store.

A página de [Releases](https://github.com/desz2000/app-mobile-pdv-lujain/releases) mantém o histórico de versões — sempre que sair uma versão nova, ela aparece lá com changelog.

## Fluxo

1. Cliente passa na balança e o operador vê o preço no visor.
2. Operador digita o **número da comanda** do cliente no topo da tela e **toca no produto** correspondente:
   - **Cardápio em botões**: produtos cadastrados no caixa aparecem como cartões coloridos.
     - **Verde** (preço fixo, ex.: Coca-Cola) — toque adiciona 1 unidade direto.
     - **Laranja** (por kilo, ex.: prato) — toque abre prompt do valor exibido na balança.
   - **+ Item avulso**: pra qualquer coisa fora do cardápio (pede descrição + valor).
3. O app chama `POST /api/comandas/{numero}/itens` na API do PDV com `origem=Balanca`.
4. O caixa, ao buscar a comanda no PC, já vê o item lançado.

Se a comanda ainda não existe no sistema, a API cria automaticamente. Se a comanda anterior com aquele número já estava fechada, uma nova é criada (cartões físicos reutilizáveis).

## Stack

- .NET MAUI 8 (target único: `net8.0-android`)
- Sem libs externas além do MAUI Controls — sem MVVM frameworks, sem DI containers; código direto pra ficar fácil de manter.

## Como o operador instala

1. Baixar o APK da [última Release](https://github.com/desz2000/app-mobile-pdv-lujain/releases/latest) na seção **Assets**.
2. No celular Android: **Configurações → Segurança → Instalar apps de fontes desconhecidas** (autoriza só a fonte que vai abrir o APK).
3. Abrir o APK e instalar.
4. Na primeira execução, **Configurações** → digitar `http://IP-DO-PC-DO-CAIXA:5170` (ex.: `http://192.168.0.10:5170`) → tocar em **Testar conexão** → **Salvar e voltar**.
5. Digitar o PIN (configurado em `appsettings.json` da API).
6. Pronto: a tela principal mostra o cardápio em botões para lançamento rápido.

> Importante: o celular e o PC do caixa precisam estar na **mesma rede Wi‑Fi**, e a API precisa estar bindada em `0.0.0.0:5170` (o `start.cmd` do PDV já faz isso). O Firewall do Windows precisa liberar a porta TCP 5170 pra rede privada.

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
- `GET /api/produtos` — carrega o cardápio (cartões da tela principal)
- `POST /api/comandas/{numero}/itens` — adiciona item à comanda (cria a comanda automaticamente se não existir), com cabeçalho `X-Pin: {pin}` e payload:
  - **Preço fixo**: `{"produtoId":1,"quantidade":1,"origem":0}` (API usa `produto.Preço × quantidade`)
  - **Por kilo**: `{"produtoId":2,"quantidade":1,"valor":35.5,"origem":0}` (operador informa o valor)
  - **Avulso**: `{"descricao":"Sobremesa","valor":12.0,"quantidade":1,"origem":0}`

## Lançar uma nova versão

Quando quiser publicar um APK novo:

```bash
# 1. Build do APK assinado (chave de debug)
dotnet publish src/PdvBalanca/PdvBalanca.csproj -c Release -f net8.0-android -p:AndroidPackageFormat=apk

# 2. Renomear pra um nome amigável
cp src/PdvBalanca/bin/Release/net8.0-android/publish/br.com.lujain.pdv.balanca-Signed.apk \
   pdv-balanca-vX.Y.Z.apk

# 3. Criar a Release no GitHub com o APK como asset
gh release create vX.Y.Z \
  --repo desz2000/app-mobile-pdv-lujain \
  --target main \
  --title "vX.Y.Z — <titulo>" \
  --notes "<kenchangelog>" \
  pdv-balanca-vX.Y.Z.apk
```
