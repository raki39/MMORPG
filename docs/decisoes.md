# Registro de decisões

Decisões que afetam persistência, determinismo, desempenho, ferramentas ou continuidade. Cada entrada registra contexto, decisão e consequências. Para mudar uma decisão, adicione uma nova entrada que a substitua; não apague o histórico.

Referência de design: [design.md](design.md).

---

## D-001 — Engine: Godot 4.7.2 (edição .NET) no lugar da Unity
**Data:** 04/11/2026 · **Estado:** aceita

**Contexto.** O plano original fixava a Unity. O criador não quer usar o editor da engine; todo o desenvolvimento acontece pela linha de comando com o Claude.

**Decisão.** Usar Godot 4.7.2 stable, edição .NET (`Godot_v4.7.2-stable_mono_win64`), a versão estável mais recente publicada em [github.com/godotengine/godot/releases](https://github.com/godotengine/godot/releases) (18/08/2026).

**Motivos.**
- Cenas (`.tscn`), temas e recursos são texto legível: o Claude cria e revisa sem abrir o editor.
- Importação, execução, capturas e exportação funcionam pela linha de comando (`--headless`).
- O cliente referencia `Simulation.Core` por `ProjectReference`, com fonte única e .NET moderno (ver D-002).
- Sem login nem ativação de licença.

**Consequências.** `UnityClient/` vira `GameClient/`. O 3D precisa de técnicas de lote (`MultiMeshInstance3D`) para multidões; isso é medido no M4. Instalação local: `C:\Users\Pichau\Tools\Godot_v4.7.2-stable_mono_win64\` (SHA-512 do zip conferido com `SHA512-SUMS.txt` oficial).

## D-002 — .NET 10 (LTS) em todos os projetos
**Data:** 04/11/2026 · **Estado:** aceita

**Contexto.** Com a Unity, o Core estaria preso a `netstandard2.1` e C# 9. O .NET 8 encerra o suporte em 10/11/2026.

**Decisão.** Todos os projetos (Core, Infrastructure, Runner, Tests e o futuro GameClient) usam `net10.0`. SDK local: 10.0.403.

**Verificação.** Projeto Godot mínimo com `Godot.NET.Sdk/4.7.2` e `<TargetFramework>net10.0</TargetFramework>`, compilado com `dotnet build` e executado com `--headless`: imprimiu `.NET 10.0.14`. Teste executado em 04/11/2026.

## D-003 — JSON de conteúdo: System.Text.Json, campos desconhecidos são erro
**Data:** 04/11/2026 · **Estado:** aceita

- Biblioteca nativa do .NET (sem dependência externa).
- Nomes de campo em camelCase; comentários e vírgula final tolerados.
- `UnmappedMemberHandling.Disallow`: campo digitado errado vira erro com arquivo e linha.
- Cada arquivo tem envelope `{ "schemaVersion": 1, ... }`. Versão diferente é recusada até existir migração.
- Hash do conteúdo: SHA-256 dos nomes e textos dos arquivos, normalizados para LF.

## D-004 — RNG: xoshiro256** 1.0, semeado por SplitMix64, fluxos derivados
**Data:** 04/11/2026 · **Estado:** aceita

- Algoritmo `xoshiro256**-1.0/splitmix64`, conferido com vetores de referência nos testes.
- Seed de cada fluxo: `Mix64(seed ^ FNV1a64(nome))`, combinada com o ID da entidade quando aplicável. `string.GetHashCode` é proibido (aleatório por processo).
- Fluxos persistentes por subsistema (`RngStreams.Get`), estados incluídos no hash e no save.
- Geração inicial por entidade (`RngStreams.ForEntity` e `PopulationGenerator`): a pessoa N é sempre igual para a mesma seed, independentemente do tamanho da população.
- Somente inteiros no domínio (`NextInt`, `ChancePermille`, `PickWeighted`).
- Mudar o algoritmo ou a ordem dos sorteios da geração exige nova versão e invalida comparações antigas.

## D-005 — Tempo: tick de 100 ms, ordem dentro do tick e salto de ticks ociosos
**Data:** 04/11/2026 · **Estado:** aceita (hipótese calibrável)

- 1 tick = 100 ms simulados; 1 dia = 864.000 ticks. Tick 0 = 00:00 da `startDate` do mundo.
- Conversão de ms autorais: arredonda para o tick mais próximo (meio arredonda para cima); duração positiva dura no mínimo 1 tick.
- Ordem em cada tick: (1) comandos previstos, por sequência; (2) eventos agendados, por (tick, sequência); (3) sistemas contínuos (a partir do M3).
- Sem sistemas contínuos ativos, o motor salta direto ao próximo trabalho. Testes provam hash idêntico ao modo tick a tick (`--no-skip`).
- Comandos sem tick explícito entram no próximo limite de tick; tick no passado é recusado.

## D-006 — Unidades inteiras no domínio
**Data:** 04/11/2026 · **Estado:** aceita

| Grandeza | Unidade |
|---|---|
| Parâmetros comportamentais | permille, 0–1000 |
| Ouro do MMORPG | cobre (`long`) |
| Dinheiro empresarial e orçamento das pessoas | centavos (`long`/`int`) |
| Distâncias no conteúdo | metros inteiros |
| Tempos no conteúdo | ms inteiros, convertidos para ticks (D-005) |

Sem `float`/`double` no estado autoritativo. Números de ponto flutuante só em relatórios e testes estatísticos.

## D-007 — Hash canônico de estado (SHA-256, formato v1)
**Data:** 04/11/2026 · **Estado:** aceita

`StateHasher` serializa campos em ordem fixa (little-endian; strings UTF-8 com tamanho) e calcula SHA-256. Usos: replay, comparação com e sem observação 3D, saves e divergências entre execuções. Todo estado novo do domínio precisa entrar no hash; ao mudar o formato, incrementar `FormatVersion`.

## D-008 — Conteúdo: JSON para troca, save SQLite como fonte da campanha, memória na execução
**Data:** 04/11/2026 · **Estado:** aceita (implementação quando o primeiro save existir)

**Contexto.** Pergunta do criador sobre o papel de JSON, SQLite e memória. A tabela da seção 14 do plano listava rascunhos como JSON.

**Decisão.**
1. JSON é o formato de autoria e troca: presets, importação e exportação.
2. Ao criar a campanha, as definições são copiadas para o SQLite do save, que passa a ser a fonte da verdade daquela campanha: versões publicadas (conteúdo ativo), criações e rascunhos. Editar o JSON do preset não altera campanhas existentes.
3. Na execução, a simulação usa `ContentDatabase` em memória. Não há acesso a banco dentro do tick.
4. Cada definição fica no SQLite como o próprio payload JSON (id, versão, estado, schemaVersion, hash), reaproveitando esquema, validador e migrações.

## D-009 — Arquivos de texto em UTF-8 sem BOM e convenção de idioma
**Data:** 04/11/2026 · **Estado:** aceita

- O parser do Godot rejeita `project.godot`/`.tscn` com BOM (verificado). O PowerShell 5.1 grava BOM com `-Encoding utf8`, então use `[IO.File]::WriteAllText` com `UTF8Encoding($false)` ou a ferramenta de escrita do Claude. Ver `.editorconfig`.
- Comentários de código: português sem acentos (ASCII). Textos exibidos ao jogador e relatórios: pt-BR com acentuação completa.

## D-010 — Atividades oferecidas pelos locais desde o esquema do M0
**Data:** 04/11/2026 · **Estado:** aceita

Seguindo o plano v1.4 (seção 4.4.1), cada região declara `activities` com tipo, requisitos (nível, grupo, classe, itens), criaturas e itens envolvidos, duração típica e risco base. O validador garante coerência: serviços só em cidade, combate nunca em cidade, emboscada só com PvP aberto, coleta só de recursos do local, boss só com criatura boss. Também avisa quando um local não oferece atividades. Os valores efetivos (recompensa, risco, tempo, benefício social) serão calculados no M2 a partir do estado do mundo.

## D-011 — Testes com xUnit 2.9.3
**Data:** 04/11/2026 · **Estado:** aceita

Template padrão do SDK 10. Testes estatísticos usam várias seeds e tolerâncias justificadas no próprio teste.
