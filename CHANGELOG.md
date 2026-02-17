# Changelog

## v1.3.2 - 2026-02-17

- Adicionado atraso de 7 segundos antes da primeira exibicao da logo no menu.
- Mantido comportamento normal nas exibicoes seguintes (sem novo atraso).
- Logs de diagnostico agora mostram estado do gate de primeira exibicao.

## v1.3.1 - 2026-02-17

- Implementado efeito de hover na logo (leve escala + brilho).
- Implementado efeito de clique (compressao e retorno para hover ao soltar).
- Clique abre o X/Twitter quando o mouse solta sobre a logo.

## v1.3.0 - 2026-02-17

- Overlay migrado de IMGUI para Canvas ScreenSpaceOverlay (mais estavel no jogo).
- Logo PNG clicavel no canto inferior esquerdo.
- Exibicao limitada ao contexto de menu/lobby.

## v1.2.5 - 2026-02-17

- Instrumentacao de debug para investigar renderizacao da logo.

## v1.2.4 - 2026-02-17

- Suporte robusto para carregar `ptbr_logo.png` embutido ou externo.
