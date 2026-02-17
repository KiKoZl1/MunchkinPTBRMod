# Munchkin PT-BR Mod

Mod de localizacao para **Munchkin Digital** (Unity + MelonLoader), com override de `es_ES` para PT-BR.

## Requisitos

- Jogo: `Munchkin Digital`
- Loader: `MelonLoader 0.7.1+` (IL2CPP)

## Pre-requisito: MelonLoader

1. Baixe o MelonLoader no site oficial:
   - `https://melonloader.net/`
2. Execute o instalador e selecione o executavel do jogo (`Munchkin Digital`).
3. Confirme instalacao para jogo `IL2CPP`.
4. Abra o jogo uma vez para o MelonLoader finalizar a estrutura de pastas.

## Instalacao (usuario final)

1. Inicie o jogo sem o mod e abra `Settings`.
2. Mude o idioma do jogo para `Espanhol (es_ES)`.
3. Feche o jogo.
4. Baixe o arquivo de release `MunchkinPTBRMod-v1.3.2.zip`.
5. Extraia `MunchkinPTBRMod.dll`.
6. Copie para `...\Munchkin Digital\Mods\MunchkinPTBRMod.dll`.
7. Abra o jogo novamente.

## Troubleshooting rapido

- O mod nao aparece:
  - confirme que a DLL esta em `...\Munchkin Digital\Mods\`.
- Traducoes nao aplicam:
  - confirme que o idioma do jogo esta em `Espanhol (es_ES)`.
- MelonLoader nao inicia:
  - reinstale o MelonLoader e abra o jogo 1x sem mods para gerar logs.
- Overlay da logo nao aparece:
  - pressione `F9` para garantir que nao esta oculto manualmente.

## Controles no jogo

- `F9`: liga/desliga overlay da logo.

## Como buildar localmente

Pre-requisitos para build:
- `.NET SDK 6.0`
- Jogo + MelonLoader ja instalados (para existir `MelonLoader\net6` e `MelonLoader\Il2CppAssemblies`)

1. Abra a pasta do projeto no VS Code/Visual Studio.
2. Edite `MunchkinPTBRMod.csproj` e ajuste os `HintPath` para o seu caminho real do jogo.
3. Rode o build:

```powershell
dotnet build MunchkinPTBRMod.sln -c Release
```

4. Verifique a DLL gerada:
- `bin\Release\net6.0\MunchkinPTBRMod.dll`

5. (Opcional) Copie direto para a pasta de mods:

```powershell
Copy-Item .\bin\Release\net6.0\MunchkinPTBRMod.dll "E:\SteamLibrary\steamapps\common\Munchkin Digital\Mods\" -Force
```

Erros comuns de build:
- `CS0006` / assembly nao encontrado: `HintPath` do `MunchkinPTBRMod.csproj` esta apontando para pasta errada.
- Referencia Unity/Melon faltando: abra o jogo 1x com MelonLoader para gerar as assemblies IL2CPP.

## Publicacao (release)

Arquivos gerados neste repositorio:
- `release\MunchkinPTBRMod-v1.3.2.zip`
- `release\SHA256SUMS.txt`
- `release\RELEASE_NOTES_v1.3.2.md`

## Aviso

Projeto de modding nao-oficial, sem afiliacao com os desenvolvedores/publicadora do jogo.
