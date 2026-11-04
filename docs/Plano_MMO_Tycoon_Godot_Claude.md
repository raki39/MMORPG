# Plano de desenvolvimento — MMORPG Tycoon + simulação de comunidade

**Versão:** 1.5 · **Data:** 04/11/2026 · **Engine decidida:** Godot 4 (.NET) / C#

**Finalidade:** documento inicial de produto, simulação, arquitetura, interface e execução para desenvolver o jogo com Claude CLI. O nome comercial permanece em aberto.

Este documento consolida as preferências do criador. Números, fórmulas, conteúdo de exemplo e metas sugeridas são hipóteses iniciais de implementação, sujeitas a testes. Nenhum limite de população ou desempenho está comprovado nesta fase.

**Documento principal:** este plano é a referência única para implementação. O conteúdo complementar foi consolidado nas seções correspondentes; `PLANO_DO_JOGO.md` passa a ser referência histórica e não deve orientar tarefas concorrentes. O arquivo antigo não é movido ou excluído por esta revisão.

**Decisão de arquitetura:** construir uma simulação testável em C# puro, executável pelo terminal, com o Godot 4 como cliente gráfico operado sem editor. A janela mostra e controla a simulação através de consultas e comandos.

## 1. Visão do jogo

Um jogo de gestão para um jogador, inicialmente offline, que combina a criação de um MMORPG com a profundidade de acompanhamento de Football Manager. O jogador é o criador e administrador do MMORPG e da empresa que o opera. Os usuários desse MMORPG são pessoas simuladas individualmente.

O prazer central está em criar conteúdo, observar uma comunidade se adaptar, investigar suas histórias e tomar decisões com consequências. Um jogador desconhecido pode virar líder de uma guilda; uma região recém-lançada pode gerar uma disputa econômica; uma alteração de classe pode mudar a composição dos grupos e o mercado.

A população não será uma decoração para indicadores. Dinheiro, inventários, relações, objetivos, combates e transações terão estado próprio e continuidade.

### 1.1 Pilares obrigatórios

1. **Individualidade:** cada pessoa possui identidade, características, disponibilidade, memória e objetivos. Todos os jogadores permanecem indivíduos. É permitido reduzir a frequência de processamento, agendar eventos e simplificar representação visual. Não substituir silenciosamente indivíduos por percentuais genéricos em sistemas que deveriam acompanhar suas ações.
2. **Causalidade:** indicadores e acontecimentos derivam das regras e eventos da simulação.
3. **Continuidade:** observar uma região em 3D não cria nem altera resultados por si só.
4. **Autoria:** conteúdo personalizável por predefinições e editores acessíveis.
5. **Gestão conectada:** empresa, conteúdo e comunidade afetam uns aos outros.
6. **Legibilidade:** o jogador consegue investigar por que um resultado aconteceu.
7. **UX profissional:** organização, consistência e interatividade integram o produto desde o começo.
8. **Escalabilidade medida:** otimizações preservam a identidade e são justificadas por medições.

### 1.2 Experiência desejada

Criar uma mina, publicar a atualização, acompanhar jogadores chegando, observar comércio de minério, ver grupos se formando e descobrir uma guilda disputando seu acesso. Depois, investigar se a região está divertida, lucrativa, perigosa ou dominada demais.

O jogador alterna entre a visão estratégica do mundo, a observação local em 3D, fichas individuais e relatórios. Todos mostram o mesmo estado do jogo.

## 2. Decisões e limites iniciais

| Tema | Diretriz |
|---|---|
| Engine | Godot 4 com C# (.NET); fixar versão após validar o ambiente local |
| Uso do editor | O criador não abre nem usa o editor do Godot em nenhum momento do projeto. Projeto, cenas, importação, testes e exportação são feitos por arquivos de texto, código e linha de comando |
| Plataforma inicial | Windows desktop, teclado e mouse |
| Operação | Single-player offline; nenhuma dependência de serviços para simular jogadores |
| Visual | 3D estilizado com câmera de cima; personagens montados com blocos e formas estilizadas, gerados por código (seção 7.3) |
| Visão global | Mapa 2D interativo com regiões, população, fluxos e filtros |
| Visão local | Região 3D observável, com deslocamento, combate e magias reais da simulação |
| Construção | Mundo predefinido editável e opção de começar do zero |
| Conteúdo | Classes, habilidades, itens, criaturas, bosses, quests, cidades e regiões editáveis |
| Publicação | Conteúdo em rascunho entra no mundo ativo através de atualizações |
| PvP | Regras por área; cidades sempre sem PvP |
| Economia | Ouro do MMORPG separado do dinheiro empresarial fictício |
| Interface | Português do Brasil; estrutura preparada para localização posterior |
| Escala | Começar com população pequena e medir antes de expandir |

Multiplayer real, integração real com Twitch, dinheiro real, pagamentos e usuários humanos não fazem parte do escopo inicial. Patrocínios, lives, salários e monetização existem como sistemas internos do tycoon.

Temporadas são expansão futura. Pesquisas e progressão empresarial entram após o ciclo básico estar validado. A visão completa continua sendo o destino do projeto; a ordem de implementação reduz riscos.

## 3. Ciclo de jogo e modos

### 3.1 Ciclo principal

1. Observar o mundo e os indicadores da empresa.
2. Investigar oportunidades, reclamações e problemas.
3. Criar conteúdo ou ajustar regras em rascunho.
4. Planejar uma atualização e alocar equipe.
5. Desenvolver, testar e publicar.
6. Acompanhar adoção, comportamento, economia e satisfação.
7. Reinvestir a receita e ampliar o MMORPG.

### 3.2 Modos

**Gestão:** orçamento, salários, infraestrutura, reputação, capacidade de produção e progressão limitam o crescimento.

**Sandbox:** permite experimentar com restrições configuráveis, mantendo as mesmas regras de simulação. Opções como dinheiro ilimitado e publicação instantânea devem ser explícitas.

**Mundo inicial:** cidade segura, áreas de níveis diferentes, classes, itens, criaturas e atividades suficientes para a primeira sessão.

**Mundo vazio:** assistente orienta a criar cidade inicial, ponto de entrada, uma classe utilizável, conteúdo acessível e recompensas. Validar os requisitos antes de abrir o servidor simulado; permitir construir livremente enquanto estiver fechado.

### 3.3 Ritmo e decisões de gestão

| Ciclo | Duração simulada | Decisões |
|---|---|---|
| Curto | Horas a um dia | Investigar incidentes, acompanhar grupos, ajustar preços autorizados e responder a alertas |
| Médio | Semana | Planejar patches, alocar equipe, contratar e avaliar marketing e balanceamento |
| Longo | Meses | Expandir infraestrutura, pesquisar, rever monetização e preparar temporadas futuras |

Antes do M4, definir e validar: o roteiro dos primeiros 30 minutos; decisões semanais; sinais de sucesso em receita, população, satisfação e reputação; falhas possíveis e alertas antecipados; condição de derrota no modo Gestão; metas intermediárias e progressão. Essas escolhas permanecem abertas até testadas, especialmente falência e encerramento do servidor.

Cada sistema deve permitir chegar a uma explicação útil de um acontecimento em até três cliques a partir do alerta ou indicador relacionado. Validar esse percurso com dados reais, sem transformar a regra em limite para toda investigação avançada.

## 4. Modelo da população

### 4.1 Pessoa, conta e personagem

Separar os conceitos desde o modelo de dados:

- **Pessoa:** disponibilidade, personalidade, habilidade, preferências, orçamento de entretenimento e relações.
- **Conta:** adesão ao MMORPG, assinatura, gastos, punições, satisfação e estado de atividade.
- **Personagem:** classe, nível, posição, equipamentos, inventário, ouro e progressão.

Inicialmente, uma pessoa terá uma conta e um personagem ativo. Personagens alternativos podem ser acrescentados depois.

### 4.2 Ficha individual

| Grupo | Parâmetros sugeridos | Uso |
|---|---|---|
| Perfil | Afinidade com PvE, PvP, comércio, exploração e socialização | Escolha de atividades |
| Personalidade | Sociabilidade, agressividade, paciência, lealdade e aversão ao risco | Decisões e relações |
| Capacidade | Habilidade mecânica, conhecimento e coordenação | Combate e aprendizado |
| Rotina | Calendário, frequência, horário preferido e duração disponível | Sessões |
| Finanças | Orçamento de lazer, disposição a pagar e sensibilidade a vantagens pagas | Monetização |
| Estado | Satisfação, fadiga, frustração, engajamento e intenção de retorno | Retenção |
| Conduta | Propensão a trapaça, oportunismo e tolerância a punições | Incidentes |
| Memória | Experiências marcantes e resultados recentes | Preferências e decisões futuras |

Proposta de convenção: parâmetros comportamentais normalizados entre 0 e 1; atributos de combate com unidades próprias. Persistir valores e documentar suas escalas. Evitar misturar nível, personalidade e habilidade em um único atributo.

Distribuições devem gerar diversidade coerente e correlações controladas. Nem todo competitivo é agressivo; nem todo jogador frequente é habilidoso. Predefinições de perfil ajudam a configurar populações, mas não tornam seus membros idênticos.

### 4.3 Calendário e sessões

O relógio simulado possui data, dia da semana e horário. Um calendário configurável marca feriados e eventos. Fins de semana e feriados alteram disponibilidade conforme o perfil individual.

Estados mínimos: potencial interessado, registrado, desconectado, conectado, inativo, desistente e banido. Uma pessoa pode retornar após uma atualização ou convite.

Ao iniciar sessão, definir tempo disponível, objetivos e compromissos. Engajamento elevado e tendência a prolongar sessões podem modificar a duração, respeitando limites do modelo. Evitar um único parâmetro de “vício” explicando toda a rotina.

O fim de uma sessão precisa de regras: tentar concluir a atividade atual, não entrar em uma raid longa perto de sair, desconectar após tolerância configurável e registrar eventual prejuízo social.

### 4.3.1 Tempo e calendário

Manter um relógio de campanha independente da taxa de quadros. Definir explicitamente a conversão entre tempo real e tempo simulado e exibi-la nas configurações.

Proposta de controles: pausa, 1x, 2x, 5x e 10x. A aceleração aumenta o tempo processado, preservando a ordem das regras. Se a máquina não acompanhar, informar a velocidade efetiva; não descartar acontecimentos silenciosamente.

Usar calendários e fusos simulados coerentes para horários de sessão. Começar com uma região de público configurável e calendário simples de feriados. Fins de semana e feriados aumentam ou redistribuem disponibilidade conforme cada perfil, sem obrigar todos a jogar mais.

Abrir menus comuns não deve pausar sem indicação. Edições críticas podem oferecer pausa explícita. A pausa precisa congelar a simulação, mantendo a interface utilizável.

### 4.4 Decisão por utilidade

Para cada atividade elegível, calcular uma utilidade inicial:

`U(a) = wR·recompensa + wD·diversão + wS·benefício_social + wO·avanço_objetivo − wP·risco − wT·tempo − wC·custo − wF·frustração`

Todos os termos devem ser normalizados antes da soma. Os pesos pertencem ao perfil individual. Recompensa considera utilidade pessoal de XP, ouro, equipamento e reputação.

Restringir opções pelo que a pessoa conhece e pelo que consegue fazer. Uma pessoa não possui conhecimento perfeito de todos os preços, bosses e jogadores.

Manter a decisão por um compromisso mínimo e reavaliar por eventos: objetivo concluído, ataque, convite, falta de recurso, morte ou mudança relevante. Pequena aleatoriedade com semente reproduzível gera variedade. Uma margem de troca evita oscilar continuamente entre atividades semelhantes.

A ficha deve explicar a decisão com os fatores efetivamente calculados: “Escolheu a mina: precisa de minério, dois amigos estão lá e o risco percebido é baixo”.

### 4.4.1 Atividades oferecidas pelos locais

As atividades elegíveis vêm dos locais do mundo. Cada cidade, área, ponto de interesse e boss declara as atividades que oferece (modelo de affordances ou objetos inteligentes). A pessoa simulada não conhece de antemão todas as ações possíveis: ela consulta as atividades dos locais que conhece e consegue alcançar, e a fórmula de utilidade pontua apenas essas opções.

| Local (exemplo) | Atividades oferecidas |
|---|---|
| Cidade | Comprar e vender no mercado, consertar equipamento, pegar e entregar quests, treinar, socializar na taverna, procurar grupo |
| Floresta (nível 5–10) | Caçar lobos, coletar couro e ervas, quest de entrega |
| Mina Velha | Coletar minério, caçar goblins |
| Covil do boss | Enfrentar o boss |
| Área PvP | Caçar com risco maior, patrulhar, emboscar |

Cada atividade é uma definição de conteúdo em JSON, ligada a um local, com:

- **Tipo**, a partir de um catálogo de tipos implementados no Core (caçar, coletar, comerciar, quest, boss, social, treinar, viajar, PvP).
- **Requisitos:** nível, classe, tamanho de grupo, itens, reputação ou quest prévia.
- **Rendimentos esperados:** XP, ouro, itens, materiais, reputação, diversão e benefício social por perfil.
- **Capacidade e duração típica**, quando aplicável.

Os termos da utilidade são calculados com o estado atual do mundo, não com valores fixos:

- **Recompensa** cai com a competição: muitos jogadores caçando o mesmo grupo de monstros reduzem o rendimento de cada um, conforme densidade e respawn reais.
- **Risco** sobe com PKs ativos, mortes recentes conhecidas, regras de PvP e full loot do local.
- **Tempo** inclui a viagem estimada até o local.
- **Benefício social** sobe quando amigos ou membros da guilda estão lá.
- **Conhecimento:** a pessoa usa o que sabe ou ouviu (memória, amigos, guilda), não o valor exato do mundo. Locais novos são descobertos por exploração, anúncios de patch e conversas.

**Consequências de projeto:**

- Conteúdo novo se integra sozinho: ao publicar um patch com um local novo, suas atividades passam a ser consideradas por quem fica sabendo dele, sem código específico para a IA.
- Desempenho: cada pessoa avalia algumas dezenas de opções conhecidas e próximas, não o mundo inteiro. Índices por região e por tipo de atividade aceleram a consulta.
- Explicabilidade: a ficha mostra a atividade escolhida, o local e os termos calculados, e pode listar as alternativas descartadas e por quê.
- Novos tipos de atividade exigem implementação e testes no Core antes de aparecerem no editor, como os efeitos de magia.

### 4.5 Memória e aprendizado

Registrar acontecimentos relevantes com intensidade, contexto e tempo. Frustrações recentes perdem peso gradualmente; conquistas e rivalidades importantes podem durar mais. Manter resumos e eventos marcantes em vez de um histórico ilimitado em memória.

Conhecimento sobre rotas, perigos e mecânicas pode melhorar com experiência. Isso precisa produzir diferenças observáveis sem exigir modelos de linguagem durante a execução.

## 5. Relações, grupos e guildas

Neutralidade é a condição inicial. Relações são direcionais: A pode confiar em B mais do que B confia em A. Guardar apenas vínculos relevantes, evitando uma matriz de todos contra todos.

Uma interação altera afinidade, confiança e ressentimento conforme contexto, compatibilidade e resultado. Caçar junto com divisão justa favorece amizade; abandono, roubo e PK recorrente favorecem rivalidade. Probabilidades usam esses fatores e um intervalo temporal definido; não repetir uma chance por frame.

### 5.1 Grupos

Formação por objetivo, proximidade, papéis, disponibilidade e relações. Definir líder, regras de loot, duração e condições de saída. O grupo coordena deslocamento e seleção de conteúdo; cada membro mantém suas próprias limitações.

### 5.2 Guildas

Guildas surgem quando existem vínculos, liderança e interesse comum suficientes. Têm identidade, membros, papéis, tesouro, objetivos, reputação e política de recrutamento. Saídas derivam de descontentamento, falta de atividade, conflitos e oportunidades.

A primeira versão pode usar regras simples de fundação e adesão. Alianças, cisões, diplomacia extensa e guerras complexas ficam para marcos posteriores.

### 5.3 Territórios

Somente áreas marcadas como disputáveis aceitam controle de guilda. Definir requisito de disputa, janela, vitória, duração da posse e benefícios. Evitar crescimento irreversível de uma guilda: custos, logística e contestação devem ser ajustáveis.

Cidades permanecem sem PvP mesmo que sua administração ou influência possa futuramente se relacionar com territórios.

## 6. Mundo e autoria de conteúdo

O mapa estratégico representa regiões conectadas e rotas. Cada região possui limites, bioma, acessos, faixa de nível, recursos, serviços, capacidade e regras locais. Custo de viagem e distância influenciam escolhas e logística.

### 6.1 Predefinições e editores

| Conteúdo | Modelos iniciais | Campos principais |
|---|---|---|
| Cidade | Vila, entreposto, capital | Serviços, guardas, capacidade, custos e aparência |
| Área | Floresta, mina, ruínas | Níveis, recursos, densidade, perigo e PvP |
| Classe | Tanque, dano, suporte | Atributos, progressão, recursos e habilidades |
| Magia | Dano, cura, controle, proteção | Alcance, custo, duração, recarga e efeitos |
| Criatura | Corpo a corpo, distância, suporte | Atributos, comportamento, respawn e loot |
| Boss | Solo, grupo, raid | Fases, habilidades, requisitos e recompensas |
| Item | Arma, armadura, material, consumível | Atributos, categoria, raridade e circulação |
| Quest | Caça, coleta, entrega, exploração | Condições, objetivos, recompensas e repetição |

Cidades, áreas, pontos de interesse e bosses têm no editor uma lista de atividades oferecidas (seção 4.4.1), criada a partir dos tipos disponíveis, com requisitos e rendimentos. Os modelos iniciais já vêm com atividades típicas, e o validador avisa quando um local publicado não oferece nenhuma atividade.

Os editores apresentam campos compreensíveis, unidades, limites e consequências previstas. Oferecer duplicar, desfazer, refazer, pesquisar e visualizar dependências. Valores inválidos impedem publicação e apontam o campo exato.

### 6.2 Regras locais

Cada área define PvP permitido, condições de agressão, consequências de PK e regra de morte. Full loot é uma opção explícita da área: estabelecer o que cai, quem pode recolher, proteção temporária, prazo do corpo e itens excluídos.

Travessias de fronteira precisam resolver agressões em curso sem permitir dano dentro de cidades. A primeira regra sugerida é negar dano PvP quando o alvo está em zona segura, além de validar a posição no instante da resolução.

### 6.3 Conteúdo versionado

Cada definição tem ID estável e versão. Rascunhos não alteram o mundo ativo. Uma atualização aplica um conjunto validado de mudanças de forma atômica num limite seguro de processamento.

Para cada mudança, definir impacto nos objetos existentes: um nerf de estatística pode atualizar equipamentos existentes; uma nova tabela de drop afeta futuras recompensas; remover uma classe exige migração explícita. Não apagar referências ativas silenciosamente.

### 6.4 Conteúdo como dados e efeitos combináveis

Definições de classes, magias, itens, criaturas, bosses, regiões, cidades e quests são dados com esquema versionado e validação de campos e referências. JSON é o formato autoral dos presets e o formato de importação e exportação; dentro de uma campanha, as mesmas definições ficam no save SQLite (ver seção 14, “Armazenamento”). Não criar uma classe C# específica para cada magia ou criatura. O editor modifica esses dados; novos comportamentos exigem novas primitivas testadas no Core.

| Efeito | Parâmetros |
|---|---|
| Dano / cura | Valor, tipo e escala por atributo |
| Dano / cura periódicos | Valor por pulso, intervalo e duração |
| Atordoar, silenciar, enraizar | Duração e condições |
| Buff / debuff | Atributo, magnitude, duração e acúmulo |
| Ameaça | Quantidade gerada ou reduzida |
| Escudo | Absorção, duração e prioridade |
| Área | Raio, filtro e máximo de alvos |

Exemplo ilustrativo, sujeito ao esquema definitivo:

```json
{
  "id": "bola_de_fogo",
  "version": 1,
  "classId": "mago",
  "manaCost": 30,
  "range": 30,
  "castTimeMs": 2000,
  "cooldownMs": 6000,
  "effects": [
    { "type": "damage", "value": 120, "element": "fire", "intelligenceScalePermille": 1200 },
    { "type": "damageOverTime", "valuePerPulse": 10, "intervalMs": 1000, "durationMs": 6000 }
  ],
  "visualId": "vfx_fireball"
}
```

Tempos autorais convertem para unidades inteiras da simulação com política explícita de arredondamento. O identificador visual referencia recursos do cliente e não controla a resolução da magia. Invocação e teleporte só aparecem no editor após existir suporte testado no Core. Presets, conteúdo importado e rascunhos usam o mesmo esquema e passam pelas mesmas validações, venham de arquivo JSON ou do save.

## 7. Combate e observação 3D

Combate por alvo, inspirado em MMORPGs como WoW: ataque automático, habilidades com recarga, custo de recurso, tempo de lançamento, alcance, ameaça, cura e papéis complementares.

### 7.1 Modelo mínimo

- Vida, recurso, defesa, dano, velocidade e alcance.
- Seleção de alvo e deslocamento até posição válida.
- Preparação, início, resolução, interrupção e cancelamento de habilidade.
- Efeitos temporários com regras de duração e acúmulo.
- Ameaça para criaturas, critérios de troca de alvo e retorno ao ponto de origem.
- Morte, atribuição de participação, loot, XP e reaparecimento.
- Reação de aliados, fuga e uso de consumíveis.

Fórmula inicial proposta: `dano = max(0, potência_base + escala_atributo) × mitigação × modificadores`. Definir a função de mitigação e os limites em um catálogo de regras; não espalhar constantes por scripts.

Habilidade simulada influencia tempo de reação, seleção de habilidades e execução de mecânicas. Não conceder automaticamente dano invisível extra só porque o perfil é “habilidoso”.

### 7.2 Mesma simulação, diferentes representações

A câmera observa estado autoritativo. Abrir a região cria representações visuais com os IDs existentes e lê os eventos recentes. Uma animação não concede dano; ela representa uma ação já controlada pelo sistema de combate.

Renderização interpola posições entre atualizações. Eventos possuem instante, origem, alvo, habilidade e resultado. Diferenciar lançamento, impacto, cura, interrupção e morte visualmente.

Na visão local, permitir câmera livre limitada, selecionar personagem, seguir grupo, inspecionar alvo e alternar indicadores. Efeitos devem ser legíveis em multidões, com redução de partículas e prioridade para personagens selecionados.

O combate usa as mesmas regras e precisão temporal com região aberta ou fechada. Simplificações futuras só entram após demonstrar que não introduzem vantagem dependente da observação.

### 7.3 Direção visual da região

O gráfico não precisa ser realista. O objetivo é ver claramente o que está acontecendo e ter a sensação de estar no mundo.

**Câmera e estilo.** 3D simplificado visto de cima, com câmera inclinada no estilo isométrico: zoom, arrastar, girar e seguir personagem. Sem câmera livre em terceira pessoa.

**Personagens.** Montados com blocos chanfrados e formas estilizadas, em proporções caricatas (cabeça grande, corpo compacto), na linha do visual de MMORPG Tycoon 2. Mais refinados que primitivas brutas: cantos arredondados, peças afinadas, rosto simples com olhos.

- Gerados por código: malhas procedurais no cliente (SurfaceTool/ArrayMesh) ou scripts Python do Blender executados em segundo plano, exportando `.glb`. Nenhuma modelagem manual.
- Corpo feito de peças separadas (cabeça, tronco, braços, pernas, arma, escudo), animadas proceduralmente: andar, atacar, lançar magia, ser atingido, morrer, comemorar.
- Equipamento visível: arma, armadura e chapéu refletem os itens reais do inventário. Quando alguém compra uma espada melhor no mercado, ela aparece no personagem.
- Variação individual determinística a partir do ID da pessoa (altura, tom de pele, cabelo, cores): cada jogador simulado tem aparência própria e reconhecível em qualquer sessão.
- Silhueta por classe (guerreiro com escudo, mago com chapéu pontudo, sacerdote com auréola) e monstros com formas distintas.
- Se no futuro um pacote de modelos prontos for preferido, a troca afeta só o cliente visual.

**Sombreamento.** Toon suave com contorno, sombras em tempo real e brilho (glow) para magias. Paleta consistente com a HUD.

**Clareza em combate.**

- Cores com significado fixo: inimigo/PK vermelho, aliado azul, boss dourado, cura verde.
- Círculo de seleção no chão, barras de vida e recurso, barra de lançamento de magia.
- Projéteis com rastro de partículas, áreas de efeito marcadas no chão, números de dano e cura subindo.
- Linhas finas opcionais ligando atacante e alvo em lutas de grupo.
- Nome e guilda ao passar o mouse ou com zoom próximo.

**Sensação de presença.**

- Som: impactos, magias, burburinho da cidade e música ambiente por região (sons livres de licença ou gerados por código).
- Ciclo de dia e noite seguindo o relógio da simulação.
- Vida ambiente: pássaros, folhas, fumaça, tochas.
- Peso nos momentos grandes: tremor de câmera em golpes fortes de boss, flash em mortes, partículas em loot raro.
- Balões de fala e emotes derivados do log de eventos (drop raro, morte para PK, vitória sobre boss).

Todos esses elementos representam estado e eventos do Core. Nenhum efeito visual decide resultado.

## 8. Economia do MMORPG

Ouro, itens e materiais circulam entre personagens, guildas e NPCs. Todo evento econômico registra origem, destino, quantidade, motivo e instante. Usar unidades monetárias inteiras para evitar erros de arredondamento.

### 8.1 Fluxos

**Fontes de ouro:** recompensas de quests, criaturas e compras por NPCs, conforme as regras escolhidas.

**Consumo de ouro:** reparos, serviços, viagens, taxas e outros pagamentos retirados da circulação.

**Fontes de itens:** loot, coleta, fabricação e incidentes de duplicação explicitamente registrados.

**Consumo de itens:** poções, fabricação, destruição e desgaste conforme política. Full loot transfere propriedade; não destrói automaticamente o item.

### 8.2 Mercado

Distinguir preço de referência, preço anunciado e preço efetivamente negociado. O criador pode definir uma referência fixa ou automática; vendas entre jogadores continuam dependentes de compradores e vendedores.

Uma política de preço realmente fixo exige uma contraparte ou restrição: por exemplo, NPC com estoque e orçamento definidos. Exibir essa diferença no editor.

Vendedores estimam valor e urgência; compradores avaliam utilidade, alternativas e saldo. Anúncios expiram e podem ser revisados. Uma compra transfere ouro e propriedade atomicamente, cobrando a taxa uma única vez. Não permitir compra dupla do mesmo item.

O preço automático inicial pode combinar utilidade, raridade, custo de fabricação e referências existentes. Depois, usar negócios realizados para atualizar estimativas. Item sem vendas deve aparecer como “sem histórico”, sem preço de mercado inventado.

### 8.3 Indicadores

Oferta listada, estoque, volume vendido, preço mediano, dispersão, liquidez, ouro emitido/retirado, concentração de riqueza e cesta de preços. Exibir janela e amostra. Comparar inflação usando uma cesta definida e informar mudanças na composição.

Raridade e drop influenciam oferta; demanda depende de utilidade, progressão, substitutos e riqueza. Um item raro e inútil pode continuar barato.

## 9. Empresa, monetização e aquisição

O dinheiro empresarial é fictício e independente do ouro. Registrar receitas, despesas, compromissos e saldo projetado. O jogador precisa entender custo mensal e tempo estimado de sobrevivência financeira.

### 9.1 Monetização

Modelos planejados: assinatura, cosméticos, expansões e vantagens pagas configuráveis. Implementar um modelo inicial simples antes dos demais. Preferência proposta para o primeiro ciclo: assinatura configurável, com possibilidade de teste gratuito.

Conversão e cancelamento dependem de orçamento individual, satisfação, preço, amigos e percepção de justiça. Gastos debitam o orçamento de entretenimento simulado e entram na receita empresarial. Não criar receita apenas por aumentar a contagem de usuários.

### 9.2 Marketing e lives

Campanhas têm público, alcance, custo, duração e conversão. Pessoas alcançadas tornam-se potenciais interessadas e avaliam o MMORPG. Marketing aumenta descoberta; retenção depende da experiência.

Lives e patrocínios são canais simulados com audiência, compatibilidade e credibilidade. Evitar contar a mesma pessoa como nova aquisição repetidamente. Boca a boca depende de satisfação e relações.

### 9.3 Infraestrutura

Servidores simulados têm capacidade, custo e confiabilidade. Sobrecarga altera latência e experiência dentro do modelo do MMORPG. Diferenciar isso do desempenho real do aplicativo: contratar um servidor no tycoon não aumenta a capacidade do computador do usuário.

### 9.4 Equipe e atualizações

Desenvolvedores possuem especialidade, produtividade, salário e experiência. GMs, suporte e segurança têm funções próprias. Capacidade efetiva inclui comunicação e coordenação; dobrar equipe não deve reduzir todos os prazos pela metade.

Proposta de trabalho: cada mudança gera esforço; a equipe produz progresso por período; qualidade de testes e pressão de prazo modificam risco de incidentes. A interface mostra estimativa como intervalo quando houver incerteza.

Fluxo: rascunho → planejamento → desenvolvimento → testes → pronto → publicado. Mudanças de escopo atualizam previsão de entrega. Permitir cancelar com custo já incorrido e preservar o rascunho.

## 10. Bots, trapaça e moderação

Incidentes são mecânicas controladas da simulação. Não implementar vulnerabilidades reais no aplicativo.

- Bots podem prolongar atividade e repetir rotinas produtivas, gerando sinais observáveis.
- Trapaças de combate possuem efeitos limitados e rastreáveis no modelo.
- Duplicação cria instâncias identificáveis e um lançamento econômico de origem irregular.
- Denúncias são evidências imperfeitas, com falsos positivos e atrasos.
- GMs investigam e aplicam ações; segurança melhora detecção; desenvolvimento corrige a causa técnica simulada.

A punição pode remover acesso e tratar bens conforme política. Estornar duplicações exige decidir como lidar com compradores inocentes; não remover ouro ou itens arbitrariamente sem registro.

As telas de gestão mostram suspeita e evidência disponível. Uma visão de diagnóstico pode revelar a verdade interna para testes; não confundir essa visão com a informação normalmente disponível ao jogador.

## 11. Feedback e balanceamento

Feedback nasce de experiências individuais. Templates de texto usam eventos e parâmetros reais: derrota recorrente, falta de conteúdo, dificuldade, monetização ou atendimento. Não gerar reclamações desconectadas do estado.

Separar percepção e medição. Uma classe pode ser frustrante sem dominar vitórias; sua popularidade pode concentrar jogadores experientes.

### 11.1 Painel de classes

- População, adoção, abandono e progressão por classe.
- Dano, cura, sobrevivência e participação por contexto.
- PvP individual, grupos e bosses separadamente.
- Filtros de nível, equipamento, habilidade, período e versão.
- Tamanho da amostra, dados ausentes e casos insuficientes.
- Comparação antes/depois com exposição explícita a outras mudanças.

Não chamar correlação de efeito comprovado de um patch. Incluir taxa bruta e cortes comparáveis. Mostrar “sem dados” quando o denominador for zero.

Buffs e nerfs passam pelo sistema de atualizações. Acompanhar adaptação gradual: jogadores podem continuar usando uma classe antiga por investimento e amizades.

## 12. Direção de HUD e UX — requisito central

**Objetivo:** aparência de um jogo de gestão profissional, moderno e agradável. Interface densa o suficiente para investigar uma comunidade, com hierarquia e espaço para leitura. A primeira versão jogável já deve transmitir essa direção.

### 12.1 Linguagem visual

Tema escuro sóbrio com superfícies distinguíveis, tipografia clara e poucas cores de destaque. Mapa e acontecimentos recebem o foco. Evitar excesso de bordas, brilhos, gradientes, caixas aninhadas e efeitos que disputem atenção.

Proposta inicial de tokens, a validar em tela:

| Token | Valor inicial | Aplicação |
|---|---|---|
| Fundo | #10151D | Área geral |
| Superfície | #192230 | Painéis |
| Superfície elevada | #223044 | Menus e inspeção |
| Texto principal | #EAF0F7 | Informação principal |
| Texto secundário | #AAB8C9 | Contexto |
| Destaque | #57B8FF | Seleção e ação |
| Sucesso | #69C994 | Estado positivo |
| Atenção | #E8BB65 | Avisos |
| Perigo | #EC7881 | Problemas e ações críticas |
| Espaçamento | 4, 8, 12, 16, 24, 32 px | Ritmo consistente |
| Corpo | 14–16 px de referência | Leitura desktop |
| Alvos de interação | 36–44 px de referência | Botões e controles |

Esses valores não certificam acessibilidade. Verificar contraste, legibilidade e escala na implementação. Números tabulares nas métricas; unidades visíveis; nomes longos com tratamento consistente.

### 12.2 Estrutura da HUD

**Barra superior:** data/hora, pausa, velocidades, estado de processamento, caixa empresarial e alertas prioritários. Distinguir usuários cadastrados, conectados e personagens visíveis.

**Navegação lateral:** Visão geral, Mundo, Comunidade, Guildas, Economia, Conteúdo, Atualizações, Empresa e Relatórios. Agrupar Segurança dentro da gestão da comunidade inicialmente para reduzir navegação.

**Área central:** mapa ou tela de trabalho com título, contexto e filtros relevantes.

**Painel de inspeção à direita:** informação do objeto selecionado, atividade, motivos e ações relacionadas. Deve poder recolher e redimensionar dentro de limites.

**Faixa de eventos recolhível:** acontecimentos com importância, região e entidades clicáveis. Agrupar repetições; alertas rotineiros não devem interromper o jogo.

Evitar que modais escondam continuamente o mundo. Usar painel lateral para inspeção, tela dedicada para edição complexa e modal apenas para decisões curtas.

### 12.3 Consistência de componentes

Criar uma biblioteca de componentes: botões primário/secundário/perigoso, campos, seletor, slider com valor digitável, abas, tabelas, cards de métricas, tooltip, alerta, toast, modal e painel lateral.

Todos possuem estados normal, hover, foco, selecionado, desabilitado e erro quando aplicável. Não depender apenas de cor. Aplicar a mesma regra de seleção e navegação em todas as telas.

Formulários mostram unidade, faixa válida e validação próxima do campo. Editores numéricos permitem digitar precisamente; sliders servem como apoio. Indicar alterações não publicadas e oferecer comparação com a versão ativa.

### 12.4 Mapa interativo

Pan, zoom, seleção, busca por região e centralização em entidade. Camadas de população, níveis, mortes, PK, recursos, mercado e influência de guildas. Legenda e escala sempre disponíveis para a camada ativa.

Ao aproximar, revelar detalhes progressivamente. Agrupar marcadores em visão distante; não esconder um personagem selecionado. Ao clicar numa região, mostrar resumo e ação “Observar em 3D”. Voltar preserva zoom, filtros e seleção.

### 12.5 Gráficos dinâmicos

Gráficos devem ajudar a tomar decisões, com título, unidade, intervalo e legenda. Usar linhas para séries temporais, barras para comparação e distribuições para variação. Evitar 3D decorativo nos gráficos.

Interações obrigatórias para os gráficos centrais:

- Tooltip com instante, valor, amostra e contexto.
- Filtro de período e segmento.
- Ligar/desligar séries pela legenda.
- Marcas de publicação de patches e incidentes.
- Seleção de uma faixa para detalhar os dados relacionados.
- Acesso à tabela correspondente.
- Estado vazio e dados insuficientes claros.

Gráficos são atualizados em cadência própria; não reconstruir toda a interface a cada tick. Atualizações não devem roubar foco nem alterar a linha que o usuário está lendo.

### 12.6 Tabelas e investigação

Busca, ordenação, filtros e colunas apropriadas. Virtualização em listas extensas. Clicar em um jogador abre sua ficha; clicar na região centraliza o mapa; clicar no item abre mercado e origem.

Preservar filtros ao voltar. Histórico de navegação permite investigar jogador → guilda → região → incidente e retornar sem recomeçar.

### 12.7 Ficha de jogador

Cabeçalho com nome, classe, nível, guilda e estado. Abas: Resumo, Equipamento, Objetivos, Relações, Histórico e Finanças. Mostrar atividade atual, motivo da escolha, satisfação e eventos marcantes. Botões para seguir em 3D, localizar no mapa e acompanhar como favorito.

A ficha precisa tornar alguém memorável, permitindo reconhecer conquistas e conflitos ao longo do tempo.

### 12.8 Responsividade e acessibilidade

Projetar primeiro para 1920×1080 e verificar 1366×768. Escala de interface configurável; menus com limites e rolagem explícita. Nenhum controle essencial fora da tela.

Suportar teclado, foco visível, atalhos descobríveis, redução de movimento e distinções além de cor. Proposta: espaço pausa, Esc fecha camada atual, teclas numéricas controlam velocidade quando não houver campo em edição.

Transições curtas e discretas. Não animar reorganizações que atrapalhem leitura. Formatar valores em pt-BR com unidades e tooltip para valores completos abreviados.

### 12.9 Fluxos prioritários

1. Alerta de PK → abrir região → observar combate → inspecionar agressor → avaliar regra local.
2. Preço subindo → abrir item → comparar oferta e vendas → localizar origem → preparar mudança de drop.
3. Reclamação de classe → abrir relatório filtrado → comparar segmentos → editar habilidade → incluir no patch.
4. Criar área por preset → ajustar parâmetros → visualizar → validar → planejar publicação.

### 12.10 Critérios de aceitação visual

Sem sobreposição, corte indevido, campos desalinhados ou espaçamento arbitrário nas resoluções de referência. Componentes usam tokens comuns. Ações principais são identificáveis. Estados vazios, carregamento, erro e seleção existem. Navegação preserva contexto. Gráficos levam a dados verificáveis. Revisão visual por capturas deve ocorrer em cada marco de interface.

## 13. Arquitetura proposta

As escolhas abaixo são diretrizes de projeto. Confirmar APIs, versões e compatibilidade na documentação oficial no momento da implementação.

### 13.1 Camadas

| Camada | Responsabilidade | Restrição |
|---|---|---|
| Domínio | Entidades, regras e invariantes | Sem dependência de cenas ou interface |
| Simulação | Relógio, decisões, combate, economia e eventos | Funciona sem renderização |
| Aplicação | Comandos, consultas, publicação e save/load | Valida operações |
| Apresentação | HUD, mapa 2D, cenas 3D e efeitos | Não altera estado diretamente |
| Infraestrutura | Arquivos, persistência, métricas e integração com o Godot | Acesso mediado por interfaces |

Não criar um nó com lógica completa e `_Process` para cada pessoa. Representações visuais existem para entidades observadas, com reutilização de objetos e controle de detalhe.

### 13.2 Projetos e dependências obrigatórias

| Projeto / pasta | Conteúdo | Dependência do Godot |
|---|---|---|
| `Simulation.Core/` | Domínio, simulação, comandos, consultas e contratos de persistência | Nenhuma |
| `Simulation.Tests/` | Testes automatizados com xUnit ou NUnit | Nenhuma |
| `Simulation.Runner/` | CLI, cenários, relatórios, comparação e composição da infraestrutura | Nenhuma |
| `Simulation.Infrastructure/` | JSON, SQLite, saves, migrações e exportação | Nenhuma |
| `Content/` | JSON, esquemas e presets versionáveis | Nenhuma |
| `GodotClient/` | Projeto Godot: mapa, 3D, HUD, editores do jogo e adaptadores visuais | Sim |
| `docs/` | Plano, backlog, decisões e benchmarks | Não se aplica |

O Core é uma biblioteca C# pura compilável por `.csproj` fora do Godot. Não usa a API GodotSharp (`Node`, `GD`, `Vector3` do Godot, `RandomNumberGenerator`, `_Process`). Usar tipos próprios ou `System.Numerics`. Escolher o target framework igual ou compatível com o .NET usado pela versão do Godot selecionada.

Separação por projetos é obrigatória. O `.csproj` do GodotClient (Godot.NET.Sdk) referencia Simulation.Core e Simulation.Infrastructure por `ProjectReference`, com fonte única. Não copiar fontes nem manter duas implementações. Uma solução `.sln` na raiz reúne todos os projetos.

O Godot lê snapshots e envia comandos; a aplicação valida e aplica no próximo limite de tick previsto. SQLite e carregamento JSON ficam nos adaptadores, não dentro das regras do Core.

### 13.3 Relógio, determinismo e fidelidade em camadas

A mesma versão do simulador, conteúdo, configuração, semente e sequência de comandos produz o mesmo estado de domínio no ambiente suportado. Determinismo é requisito desde o M0. Registrar as versões e não prometer identidade binária entre plataformas ou runtimes antes de verificá-la.

- Tick lógico fixo, independente de tempo real, FPS e câmera. Proposta inicial de resolução: 100 ms; calibrar antes de estabilizar contratos. Sistemas de frequência menor são agendados sobre esse relógio.
- Geradores pseudoaleatórios de algoritmo e versão definidos, com fluxos derivados da semente por subsistema e, quando necessário, entidade. Persistir seus estados. Fluxos separados reduzem interferências acidentais; mudanças de eventos ainda podem propagar efeitos legítimos.
- Nunca usar relógio do computador ou RNG do Godot para decisões do domínio.
- Iteração e desempate estáveis; ordenar explicitamente quando a ordem de dicionários ou conjuntos afetar o resultado.
- Ouro e valores críticos de combate usam inteiros ou ponto fixo, com escala, arredondamento e limites de overflow documentados.
- Comandos gravam payload, tick aplicado, sequência e versão. Checkpoints e hashes canônicos permitem detectar divergências em replay.

| Camada | Estado | Processamento |
|---|---|---|
| Observada | Região aberta em 3D | Estado individual autoritativo, visualização contínua, animações, efeitos e interpolação |
| Ativa | Pessoas online fora da região aberta | Estado individual, mesmas regras; agenda por eventos, consultas locais e representação espacial compacta |
| Offline | Pessoas desconectadas | Sem atualização contínua do personagem; login e mudanças relevantes são eventos agendados |

**Compatibilização obrigatória:** a câmera não pode elevar a precisão autoritativa apenas da região observada. Usar combate a 10 Hz quando observado e 1 Hz quando oculto, ou trocar navegação abstrata por navmesh que altera alcance, chegadas e encontros, violaria o pilar de continuidade. Frequências diferentes são permitidas para apresentação e sistemas distintos, ou com saltos por eventos equivalentes ao mesmo relógio lógico, independentemente da câmera.

Representação abstrata de rotas, zona e distância deve manter informação suficiente para determinar encontros, alcance e tempo de chegada. Materializar posições coerentes ao abrir 3D, preservando IDs, alvos, recursos e ações pendentes. Pathfinding de apresentação pode suavizar movimento, mas não decide impactos, viagens ou colisões autoritativas. Se uma simplificação não preserva resultados, ela precisa ser redesenhada antes de entrar.

Mercado processa lotes em instantes definidos; relações reagem a eventos; empresa fecha períodos diários ou semanais; decisões mudam por objetivo, ataque, morte, convite ou revisão agendada. Um jogador offline ainda pode receber consequências externas, como venda de anúncio ou punição, sem executar uma rotina contínua.

Tempo simulado, cadência de cálculo e velocidade de reprodução são distintos. Informar velocidade efetiva quando a máquina não acompanha; nunca descartar acontecimentos para sustentar a velocidade pedida.

### 13.4 Paralelismo e dados

Começar com regras testáveis e estruturas compactas. Threads do .NET, estruturas compactas e processamento em lote são as opções para pontos medidos. O Core continua testável fora do Godot.

Separar leitura, cálculo e aplicação. Paralelismo deve produzir resultados aplicados em ordem determinística, com reduções estáveis e RNG particionado. Não aceitar quebra de determinismo como otimização. Testar a equivalência com o modo sequencial antes de habilitar.

A UI recebe snapshots ou consultas de leitura e envia comandos. Relatórios usam agregações incrementais. Preferir arrays, IDs e estruturas compactas onde o benchmark justificar, sem complexidade antecipada.

### 13.5 Relações e espaço

Índices por região e célula ajudam a encontrar candidatos próximos. Relações usam grafo esparso. Procura de grupo e mercado trabalham com conjuntos filtrados e limites de candidatos. Não comparar todos com todos a cada decisão.

### 13.6 Runner de linha de comando

O runner executa o mesmo Core sem Godot, o mais rápido possível, para verificar regras, balanceamento e desempenho. Rodar semanas em pouco tempo é objetivo de otimização, não capacidade já comprovada.

Contrato de CLI a implementar:

```bash
dotnet test
dotnet run --project Simulation.Runner -- --world Content/presets/inicial --days 30 --seed 42 --out reports/teste01
```

Exportar CSV e resumo Markdown com metadados: versão do simulador/conteúdo, configuração, seed, população, duração e tempos por sistema. Métricas incluem online, aquisição, abandono, fontes e consumos de ouro, cesta de inflação quando calculável, preços médios e medianos negociados, volume, combate segmentado, guildas, relações e finanças empresariais. Marcar métricas ainda indisponíveis, sem preencher com números fictícios.

Adicionar modo de comparação entre configurações base e candidata, com snapshot inicial comum, seeds pareadas, comandos de patch em ticks definidos e relatório de diferenças. A mesma seed ajuda a comparar, mas não prova causalidade isolada nem elimina divergências de trajetórias; repetir em várias seeds e controlar as demais mudanças.

Regressões de balanceamento usam cenários, amostras mínimas e limites justificados. Um limite como 60% de vitórias só vira teste após definir classe, adversários, equipamento, habilidade, número de confrontos e tolerância. Não impor um teto universal com base em poucas lutas.

### 13.7 Cliente Godot sem editor

O criador não usa o editor do Godot. Todo o cliente é mantido como texto e operado pela linha de comando.

- Cenas em `.tscn` escritas como texto ou montadas por código; preferir criar nós por código quando a cena for dinâmica.
- Modelos `.glb`, sons e fontes colocados em pastas do projeto e importados com `godot --headless --import`.
- Exportação para Windows com `godot --headless --export-release "Windows Desktop" build/MMOTycoon.exe`, usando `export_presets.cfg` versionado e modelos de exportação instalados por arquivo.
- Testes do cliente, quando houver, executados em modo headless. Regras continuam testadas em Simulation.Tests.
- O criador apenas executa o jogo e envia capturas de tela quando algo visual precisar de ajuste. Nunca pedir que ele abra o editor; se algo parecer exigir o editor, encontrar a alternativa por arquivo ou código.

**Regras de desempenho do cliente:**

- Simulação em thread separada; a tela recebe snapshots e eventos.
- Não criar um nó por jogador no mapa 2D: desenhar populações com MultiMesh, desenho customizado ou RenderingServer.
- Minimizar chamadas entre C# e a engine por frame: atualizar posições e instâncias em lote.
- Personagens observados com detalhe visual por distância (menos peças e animações simples ao longe), sem alterar a simulação.
- Tabelas grandes virtualizadas: criar apenas as linhas visíveis.
- Pool de objetos visuais para personagens e efeitos que entram e saem da região.

## 14. Dados, persistência e rastreabilidade

Separar definições de conteúdo das instâncias da campanha. Uma definição de espada descreve o tipo de item; uma instância representa a espada que pertence a um personagem.

| Entidade | Dados principais |
| --- | --- |
| Pessoa simulada | ID, nome, perfil, horários, orçamento, satisfação, relações, histórico |
| Conta | Estado, assinatura, gastos, punições, data de entrada |
| Personagem | Classe, nível, experiência, atributos, posição, inventário, objetivos |
| Guilda | Membros, liderança, tesouro, objetivos, relações e territórios |
| Região e área | Geometria, conexões, bioma, regras, recursos e pontos de interesse |
| Cidade | Serviços, capacidade, melhorias e acesso |
| Definição de conteúdo | ID estável, versão, atributos, referências e estado de publicação |
| Item instanciado | Tipo, quantidade ou ID individual, proprietário, origem e alterações |
| Atualização | Alterações, equipe, trabalho restante, custo, testes e publicação |
| Transação | Partes, item, quantidade, preço, taxas e instante |
| Incidente | Tipo, evidências, impacto, responsáveis, investigação e resolução |
| Evento | Tempo, envolvidos, tipo, resultado e vínculos causais |

Usar IDs estáveis; nomes editáveis não podem ser chaves de relacionamento. A primeira versão pode limitar cada pessoa a um personagem, mas preservar a separação conceitual para personagens alternativos futuros.

IDs estáveis para pessoas, personagens, itens, guildas, regiões, patches e eventos. Diferenciar definição de item e instância: uma espada concreta tem proprietário e origem próprios; materiais empilháveis podem usar lotes rastreáveis.

### Armazenamento: JSON, memória e SQLite

Cada formato tem um papel diferente, e o conteúdo percorre um ciclo definido.

| Dados | Onde fica | Papel |
|---|---|---|
| Presets de mundo (ex.: `Content/presets/inicial`) | Arquivos JSON versionados no repositório | Ponto de partida de novas campanhas; autoria e comparação pelo git |
| Conteúdo exportado ou a importar | Arquivos JSON | Troca e compartilhamento entre campanhas e pessoas |
| Conteúdo da campanha: versões publicadas (conteúdo ativo), criações e rascunhos | Save SQLite da campanha | Fonte da verdade da campanha |
| Estado vivo da simulação e conteúdo ativo em uso | Memória C# | Regras e processamento rápido |
| Eventos e estatísticas | SQLite, no save | Investigação, séries e relatórios |

**Ciclo do conteúdo:**

1. **Nova campanha:** o preset escolhido é lido do JSON, validado e copiado para o save SQLite. Daí em diante a campanha não lê mais o preset; editar o arquivo do preset não altera campanhas existentes.
2. **Criação e edição:** o editor do jogo grava criações e rascunhos no save. Rascunhos nunca afetam o mundo ativo.
3. **Publicação:** uma atualização valida o conjunto de rascunhos, grava uma nova versão publicada no save e troca o conteúdo ativo em memória de forma atômica, num limite de tick.
4. **Execução:** a simulação usa apenas o conteúdo em memória; o banco não é consultado dentro do tick.
5. **Exportação e importação:** qualquer definição ou conjunto pode ser exportado para JSON e importado em outra campanha, passando pelas mesmas validações.

Cada definição é guardada no SQLite como o próprio payload JSON (ID estável, versão, estado rascunho/publicado, versão do esquema e hash). Assim, arquivo e save compartilham o mesmo esquema, o mesmo validador e as mesmas migrações.

Nenhuma consulta ao banco ocorre dentro do tick. Acumular eventos em buffers limitados e gravar em lotes por quantidade ou intervalo. I/O e consultas de relatórios acontecem fora do processamento autoritativo; mostrar o tick de referência e atraso de dados. Aplicar contrapressão explícita se necessário, sem descartar transações ou eventos essenciais.

Save inclui versão do esquema, relógio, agenda, estado dos RNGs, população, conteúdo da campanha (versões publicadas, criações e rascunhos), empresa, mercados, relações, comandos e eventos necessários. Como o conteúdo é copiado para o save no início da campanha, editar arquivos JSON externos nunca altera um save silenciosamente.

Gravar snapshots e seu ponto de corte de eventos em transação SQLite. Em caso de falha, conservar o último snapshot confirmado e descartar lotes incompletos na recuperação. Uma cópia exportada deve ser consistente mesmo com journal/WAL; não copiar apenas o arquivo aberto indiscriminadamente. Backups ou exportações podem usar arquivo temporário e substituição segura. Armazenar agregações por período, classe e região para evitar recalcular todo o histórico.

**SQLite no cliente Godot:** o Godot .NET usa pacotes NuGet normalmente, então o mesmo adaptador SQLite do runner deve funcionar no cliente. Validar antes do primeiro save pelo jogo com um teste mínimo de gravar e ler no executável Windows exportado, confirmando que a biblioteca nativa do SQLite acompanha a exportação. Registrar a escolha no registro de decisões. Isso não bloqueia o M0, que roda fora do Godot.

Carregar valida referências, versões e integridade. Migrações explícitas permitem evoluir o formato. Documentar compatibilidade prometida por marco, sem garantir compatibilidade eterna prematuramente.

Log de eventos estruturado suporta investigação; usar snapshots e retenção para impedir crescimento ilimitado. Histórico agregado permanece acessível mesmo quando detalhes antigos forem compactados, com a limitação indicada.

### Eventos causais e camada narrativa

Eventos relevantes persistem ID, tick, tipo, envolvidos, resultado, importância, versão e causas calculadas. Causas apontam fatores e eventos de origem; pesos explicam a decisão do modelo e não são prova de causalidade estatística.

```json
{
  "eventId": 7001,
  "tick": 1834200,
  "type": "guild_left",
  "playerId": 318,
  "guildId": 12,
  "importance": "high",
  "causes": [
    { "factor": "unfair_loot", "sourceEventId": 6990, "weightPermille": 600 },
    { "factor": "low_leader_affinity", "weightPermille": 400 }
  ]
}
```

Persistir mortes, drops raros, negócios, decisões marcantes, mudanças de guilda e incidentes. Não manter cada golpe para sempre no log narrativo. Eventos transitórios de combate continuam disponíveis em buffer para VFX; replay detalhado usa checkpoint, comandos e RNG, ou captura diagnóstica limitada. Separar esses canais impede perder fidelidade visual ou criar um banco ilimitado.

A mesma evidência alimenta explicações de fichas, caixa de entrada do gestor, notícias do servidor, posts de fórum e comentários de streamers. Templates respeitam conhecimento dos personagens e a informação disponível ao gestor. Prioridade e agrupamento limitam ruído. Gravação em lote ocorre fora do tick.

### 14.1 Invariantes obrigatórias

- Item não possui dois proprietários simultâneos.
- Transferência comum não cria ouro nem quantidade de itens.
- Fontes, consumos e duplicações são registrados separadamente.
- Morte e recompensa são resolvidas uma única vez.
- Cidade não aceita dano PvP.
- Conteúdo em rascunho não afeta partidas ativas.
- Abrir/fechar região não altera resultado da simulação.
- Carregar um save não repete uma transação já concluída.

## 15. Escopo da primeira versão integrada

Conteúdo de teste proposto: uma cidade segura, duas áreas de caça, uma área opcional de PvP e um boss de grupo. Três classes: guerreiro, mago e sacerdote. Catálogo pequeno de equipamentos, materiais e consumíveis com progressão curta.

Proposta de conteúdo para a primeira campanha integrada: 3 classes, 12 habilidades no total, 6 tipos de monstro, 1 boss, 24 definições de item e 8 quests por templates. São metas ajustáveis de autoria, não restrições do editor final.

O início da campanha deve mostrar aquisição progressiva de jogadores. Disponibilizar cenário de teste separado com população já existente para validar rapidamente combate, mercado e desempenho.

O preset é criado pelo mesmo sistema de dados usado pelo editor. Não implementar um mundo especial que o usuário não consegue reproduzir ou alterar.

População inicial: 100 pessoas simuladas para validar comportamento. Aumentar depois para cenários de carga de 1.000, 5.000 e 10.000 cadastrados, medindo conectados e em combate separadamente. Esses números são cenários de teste, não promessas de capacidade.

A versão integrada contém sessões, decisão explicável, combate, loot, inventário, mercado, relações básicas, grupos, guilda simples, mapa 2D, região 3D, save/load, uma monetização, custos empresariais e um patch com prazo.

Full loot, incidentes e territórios entram na sequência após esse núcleo; devem estar previstos nos modelos e permanecer claramente identificados como futuros até funcionarem.

## 16. Roadmap com entregas verificáveis

| Marco | Entrega | Critério de conclusão |
|---|---|---|
| M0 — Fundação | Core C# puro, testes, RNG, relógio, conteúdo JSON mínimo e runner | Compilar sem Godot, executar cenário reproduzível e exportar CSV/Markdown com metadados |
| M1 — HUD e mapa | Tokens e shell simples com mapa e inspeção | Revisar o padrão visual cedo, sem construir todos os menus; fixtures identificadas e conexão mínima ao Core |
| M2 — População | Sessões, atributos, objetivos, atividades oferecidas pelos locais e escolhas | 100 pessoas entram/saem, escolhem atividades entre as oferecidas pelos locais que conhecem e exibem motivos reais |
| M3 — Combate | Movimento, habilidades, morte e loot | Um grupo luta contra criaturas e boss com estado persistente |
| M4 — Observação 3D | Personagens estilizados por código, magias, efeitos e câmera de cima (seção 7.3) | Observar e deixar de observar preserva os resultados do cenário. Teste de estresse visual: algumas centenas de personagens animados em combate numa região, com FPS medido e registrado |
| M5 — Economia | Inventário, anúncios, compras e métricas | Negócios transferem bens/saldos e conservação é verificada |
| M6 — Social | Relações, grupos, guilda e PvP | Eventos concretos alteram vínculos; cidades continuam seguras |
| M7 — Empresa e patches | Receita, despesas, equipe e publicação | Mudança preparada é produzida e publicada, com impacto mensurável |
| M8 — Versão integrada | Ciclo completo e relatórios | Sessão de gestão completa com save/load e revisão visual |
| M9 — Expansão | Full loot, incidentes e territórios | Cada novo sistema altera comportamento e possui ferramentas de investigação |
| M10 — Profundidade | Pesquisas, canais de aquisição e conteúdo | Crescimento e progressão testados em campanhas maiores |
| Posterior | Temporadas e sistemas avançados | Projetados após validar persistência e equilíbrio de longo prazo |

O roteiro M0–M10 permanece como referência; a numeração de sete etapas mencionada no material complementar não o substitui. Antes do M4, validar as decisões de ritmo da seção 3.3. O teste causal completo de patch e mercado ocorre no M7–M8, após os sistemas necessários existirem.

Marcos podem intercalar interface e regras. M1 pode usar fixtures claramente marcadas; cada tela deve migrar para dados reais conforme o respectivo sistema ficar pronto. Não entregar uma coleção de menus como se fosse o ciclo integrado.

Não fixar calendário de semanas antes de medir o trabalho no ambiente e a capacidade de iteração com o Claude.

## 17. Verificação e desempenho

### 17.1 Testes relevantes

Testes de regras para transferências, loot, morte, recargas, proteção das cidades e aplicação de patches. Cenários de integração para sessão completa, mercado, save/load e guildas. Usar cenários com semente e decisões observáveis.

Teste central: executar o mesmo cenário com e sem observação 3D e comparar estado de domínio. Outro teste: salvar no meio de atividades, carregar e comparar a continuação suportada.

Testes estatísticos devem usar várias sementes e tolerâncias justificadas. Não exigir o mesmo resultado social de toda campanha.

### 17.2 Cenários de produto

1. Aumentar drop via patch: medir oferta, preço, compras e mudança de atividade, reconhecendo efeitos que podem variar por contexto.
2. Fortalecer classe: medir uso e desempenho por segmentos, sem pressupor adoção instantânea.
3. Criar região PvP: observar escolhas diferentes por aversão ao risco.
4. Produzir escassez: verificar substituição, preços e desistência de compras.
5. Incidente de duplicação: rastrear oferta irregular, investigação e resposta.
6. Marketing intenso sem capacidade: observar entrada, sobrecarga simulada e retenção.
7. Remover uma fonte importante de recursos e verificar rotas e escolhas alternativas, sem inventar estoque.
8. Criar zona full loot e conferir propriedade dos itens antes e depois de uma morte.
9. Fazer amigos entrarem em horários compatíveis e verificar formação de grupo conforme objetivos.
10. Salvar durante uma atividade e carregar, preservando inventário, saldo, objetivo e eventos pendentes.
11. Executar o mesmo cenário com e sem visão 3D e comparar o estado simulado.
12. Abrir relatórios e alterar filtros enquanto a simulação roda, sem travar a interface ou mostrar populações incompatíveis.

### 17.3 Perfil de desempenho

Referência desejada de validação: Ryzen 7 5700X, 32 GB RAM e RTX 3060 12 GB. Medir no hardware disponível e registrar diferenças; não declarar resultado sem execução.

Meta de experiência proposta: interface responsiva e observação próxima de 60 FPS em 1080p na carga escolhida. É um objetivo, sujeito a benchmark.

Registrar tempo de frame mediano e percentis, tempo por sistema, memória, alocações, tamanho do save, tempo de carga e atraso de simulação. Benchmarks indicam população cadastrada, conectada, combates ativos, região observada, velocidade e duração.

Evitar travamentos longos ao abrir relatórios. Agregar dados incrementalmente, limitar pontos visuais e virtualizar listas. Aumentar população apenas quando o cenário atual tiver margem medida.

Meta inicial proposta para o runner: simular um dia em menos de 30 segundos na velocidade máxima, na carga e hardware registrados. Medir execução headless separada da observação 3D; informar se inclui gravação e relatórios. A meta não garante semanas em segundos. Quando não for atingida, investigar o gargalo antes de ampliar a população.

Manter os cenários de 100 → 1.000 → 5.000 → 10.000 cadastrados da seção 15 e sempre declarar o pico online e combates simultâneos. Não adotar os números alternativos do complemento como compromisso de lançamento.

Nunca reduzir a população ou pular transações escondidamente para atingir FPS. Limitações devem ser configuráveis e informadas.



Servidores múltiplos podem futuramente ter estados e RNGs separados, com agenda coordenada e agregação empresarial. Somente a apresentação do servidor observado recebe detalhe visual adicional; os demais continuam processando acontecimentos e custos de forma individual e consistente.

## 18. Riscos e respostas de projeto

| Risco | Resposta |
|---|---|
| Muitos sistemas incompletos | Fechar o ciclo integrado antes de expandir |
| Economia artificial | Inventários, orçamento e transações reais da simulação |
| Custo de relações quadrático | Grafo esparso e candidatos locais |
| Combate muda com a câmera | Autoridade única e comparação automática de cenários |
| UI fica inconsistente | Tokens, componentes comuns e revisão visual por marco |
| Relatórios enganam | Amostras, segmentação, unidades e dados ausentes explícitos |
| Mundo vazio é trabalhoso | Presets, assistente e validação de conteúdo mínimo |
| Patches quebram saves | IDs estáveis, versionamento e migração explícita |
| Campanhas ficam opacas | Motivos de decisão, histórico e vínculos clicáveis |
| Desenvolvimento vira refatoração permanente | Mudanças pequenas, critérios de conclusão e decisões registradas |

## 19. Processo de trabalho com Claude CLI

O repositório deve conter este plano, um `CLAUDE.md` com regras operacionais, um backlog por marco e um registro de decisões. Este documento não cria esses arquivos automaticamente; orienta a preparação deles na implementação.

### 19.1 Regras para o Claude

1. Ler o estado atual do repositório antes de editar.
2. Trabalhar em incrementos pequenos que compilam e podem ser avaliados.
3. Preservar os pilares e distinguir requisitos de hipóteses.
4. Não construir todos os sistemas de uma vez.
5. Não esconder lógica de domínio em animações, telas ou componentes visuais.
6. Não substituir a simulação individual por números agregados decorativos.
7. Não adicionar chamadas a modelos de linguagem no loop da população.
8. Não adicionar bibliotecas e frameworks sem necessidade concreta.
9. Usar documentação oficial atual ao escolher versão, pacote ou API.
10. Registrar decisões que afetam persistência, performance e continuidade.
11. Não alegar testes, desempenho ou revisão visual que não executou.
12. Mostrar o que mudou, como verificar e o que ainda falta.
13. Manter a UI consistente; novas telas reutilizam componentes e tokens.
14. Distinguir dados de demonstração, estimativas e resultados reais.
15. Ao fechar um marco, atualizar backlog e instruções de execução.
16. Nunca usar a API do Godot no domínio e na simulação. Compilar Core e testes pelo terminal.
17. Toda regra nova de simulação vem com teste relevante em Simulation.Tests.
18. Rodar dotnet test e cenários do runner pertinentes antes de concluir; registrar comandos e resultados.
19. Eventos relevantes registram causas. Manter seed, ordem estável e comandos reproduzíveis.
20. Trabalhar uma tarefa por vez, mantendo cada entrega revisável e com commit no repositório quando disponível e autorizado pelo fluxo do projeto.
21. O criador não usa o editor do Godot. Cenas, importação, testes e exportação são feitos por arquivos de texto, código e comandos `godot --headless`.

### 19.2 Definição de pronto

A entrega compila no ambiente definido, possui comportamento verificável, mantém invariantes pertinentes e não introduz erros conhecidos sem registro. A interface correspondente tem estados vazios, erro e seleção e foi revisada nas resoluções definidas. Documentação acompanha mudanças de regras e formato de save.

Se uma verificação exigir ver o jogo rodando ou hardware indisponível ao CLI, fornecer ao criador o comando exato para executar e o que observar ou capturar em tela, e indicar a pendência. Não marcar a entrega como validada apenas por leitura de código.

### 19.3 Primeira tarefa sugerida

Inspecionar o repositório e as ferramentas .NET; confirmar compatibilidade de target framework com a versão .NET do Godot escolhida. Criar Simulation.Core, Simulation.Tests, Simulation.Runner e Simulation.Infrastructure (inicialmente só com o carregamento e a validação do conteúdo JSON), conteúdo JSON mínimo, relógio fixo, RNG determinístico e um cenário headless pequeno que exporta resumo e CSV. Registrar versões e comandos de execução. O M0 não depende de abrir o Editor.

Em seguida, iniciar o shell simples do M1 com o padrão visual da seção 12, mapa selecionável e painel lateral. Antes do M1, fazer um teste de viabilidade: projeto Godot criado só por terminal, referenciando o Core, exibindo o mapa com a população do cenário se movendo, exportado como executável Windows, sem uso do editor.

### 19.4 Contexto fixo do Claude

Manter este plano como referência única de design, preferencialmente em `docs/design.md` no repositório, sem cópias divergentes. `CLAUDE.md` na raiz resume arquitetura, regras, comandos e marco atual; backlog e registro de decisões apontam para o plano.

Conteúdo mínimo sugerido, a adaptar ao ambiente validado:

```markdown
# MMORPG Tycoon

## Arquitetura
- Simulation.Core: C# puro, sem API do Godot (Node, GD, RandomNumberGenerator).
- Simulation.Infrastructure: carregamento de JSON, SQLite, saves e exportação; o Core não depende dela.
- Simulation.Runner: CLI headless que compõe Core e Infrastructure.
- GodotClient: visualização, interface e comandos para o Core; referencia Core e Infrastructure por ProjectReference.
- O criador NUNCA usa o editor do Godot: cenas em texto ou por código; importação e exportação com godot --headless.
- Presets em Content/*.json (também formato de importação/exportação); na campanha, conteúdo ativo, criações e rascunhos ficam no save SQLite; a execução usa memória.
- SQLite fora do tick; eventos em lote; decisões e mudanças importantes com causas.

## Regras
- Seed e tick fixo; ordem de execução estável; comandos registrados.
- Toda regra nova de simulação tem teste relevante em Simulation.Tests.
- Abrir ou fechar 3D não muda o resultado autoritativo.
- Nada de um nó por jogador no mapa; atualizações visuais em lote.
- HUD consistente com os tokens e fluxos do plano.

## Verificação
- dotnet test
- dotnet run --project Simulation.Runner -- --world Content/presets/inicial --days 30 --seed 42 --out reports/teste01
- godot --headless --path GodotClient --import
- godot --headless --path GodotClient --export-release "Windows Desktop" ../build/MMOTycoon.exe

## Marco atual
- M0: Core, testes, Infrastructure (JSON) e runner headless; consultar docs/design.md.
```

Pedir tarefas com comportamento mensurável. Exemplo: implementar calendário de login e testar se um perfil trabalhador configurado para preferir 19h–23h concentra a proporção esperada de sessões nessa faixa em dias úteis. A proporção pertence ao cenário e precisa de amostra e tolerância, não é uma regra universal da população.

## 20. Prompt inicial para colar no Claude CLI

> Estamos criando um jogo single-player offline em Godot 4 com C# (.NET), inspirado em MMORPG Tycoon 2 e Football Manager. Leia integralmente este plano antes de implementar. O jogador cria e administra um MMORPG e sua empresa; cada usuário desse MMORPG é uma pessoa simulada individualmente.
>
> Preserve a separação entre pessoa, conta e personagem, entre ouro e dinheiro empresarial, e entre conteúdo em rascunho e conteúdo publicado. A simulação deve funcionar sem câmera. A região 3D apenas representa os acontecimentos e não pode mudar os resultados por ser observada.
>
> A HUD e a UX são prioridade desde o início: profissionais, modernas, legíveis, dinâmicas e consistentes. Use tokens de espaçamento, tipografia e cor, componentes reutilizáveis, mapa interativo, inspeção contextual, navegação com histórico e gráficos ligados a dados verificáveis. Não deixe a qualidade visual para o final.
>
> Comece pelo M0 headless: Simulation.Core em C# puro sem API do Godot, Simulation.Tests, Simulation.Runner e Simulation.Infrastructure para carregar o conteúdo JSON. Presets e trocas de conteúdo são JSON; dentro da campanha, o conteúdo fica no save SQLite; estado vivo fica em memória; persistência e análise usam adaptadores SQLite fora do tick. Implemente relógio fixo, RNGs derivados da seed, comandos registrados e ordem estável. Entregue dotnet test e um cenário pelo runner com CSV e resumo Markdown antes de depender do cliente. Confirme compatibilidade .NET/Godot e documente versões com fontes oficiais. Prepare CLAUDE.md, backlog e registro de decisões.
>
> Eu não vou usar o editor do Godot em nenhum momento. Tudo no cliente deve ser feito por arquivos de texto, código e comandos godot --headless; quando precisar que eu veja algo, me diga o comando para rodar o jogo e o que capturar em tela.
>
> Antes do M1, faça o teste de viabilidade do cliente Godot descrito na seção 19.3. Depois avance ao shell simples do M1: tokens, mapa selecionável e painel lateral, com fixtures claramente identificadas. Preserve a HUD cedo, sem construir todos os menus antes das regras. Fidelidade em camadas pode reduzir custo visual e usar agendamento equivalente, mas nunca alterar combate ou encontros porque a câmera mudou.
>
> Não implemente todos os sistemas agora. Não prometa capacidade populacional sem benchmark. Não use um Update completo por pessoa nem chamadas de IA por jogador. Documente hipóteses numéricas e mantenha testes relevantes de regras. Ao terminar, informe mudanças, instruções para executar, verificações realizadas e pendências que exigem que eu rode o jogo. Continue seguindo os critérios de conclusão deste plano.

### 20.1 Registro desta revisão

**v1.5 — 04/11/2026:** papéis de JSON, SQLite e memória esclarecidos (seções 6.4 e 14, CLAUDE.md e prompt inicial): JSON para presets, importação e exportação; save SQLite como fonte da verdade da campanha, incluindo conteúdo ativo, criações e rascunhos; execução em memória. Ciclo do conteúdo descrito em cinco etapas.

**v1.4 — 04/11/2026:** adicionada a seção 4.4.1: atividades oferecidas pelos locais (affordances), com requisitos, rendimentos e termos de utilidade dependentes do estado do mundo. Editores (6.1) e M2 atualizados.

**v1.3 — 04/11/2026:** engine trocada de Unity para Godot 4 com C# (.NET), por decisão do criador de não usar editor em nenhum momento. Adicionadas a seção 7.3 (direção visual: câmera de cima, personagens de blocos estilizados gerados por código, clareza e presença) e a seção 13.7 (cliente Godot sem editor e regras de desempenho). Projeto do cliente renomeado para GodotClient; teste de viabilidade antes do M1 e teste de estresse visual no M4; CLAUDE.md e prompt inicial atualizados.

**v1.2 — 04/11/2026:** Simulation.Infrastructure incluída na primeira tarefa, no CLAUDE.md e no prompt inicial, já que o M0 precisa carregar conteúdo JSON. Adicionado aviso sobre validação do SQLite dentro da Unity antes do primeiro save pelo jogo.

**v1.1 — 04/11/2026:** consolidação do complemento de arquitetura e dos trechos de design fornecidos. Núcleo C# puro e runner obrigatórios; determinismo reforçado; JSON/memória/SQLite definidos; eventos causais e narrativa integrados; fidelidade em camadas conciliada com invariância da câmera; calendário, preset, cenários, metas de benchmark e instruções ao Claude atualizados. Preservados HUD cedo, roadmap M0–M10 e cenários populacionais do plano principal.

A precisão autoritativa não varia pela observação; efeitos transitórios foram separados do histórico persistente; comparação pareada de patches não foi tratada como prova automática de causalidade. Essas resoluções evitam contradições no material consolidado.

## 21. Decisões ainda abertas

- Nome e identidade visual definitiva do jogo e do mundo inicial.
- Versão exata do Godot 4 (.NET), renderizador e versões de pacotes, após validar o ambiente.
- Manter personagens gerados por código ou trocar por pacote de modelos prontos, decidido após o M4.
- Formato do mapa e nível de liberdade da edição do terreno.
- Duração do dia simulado e velocidades sustentáveis.
- Escala alvo após os benchmarks iniciais.
- Regras detalhadas de morte, vinculação de itens e full loot.
- Modelo inicial de preços da assinatura e orçamento empresarial.
- Condições específicas de disputa territorial e pesquisas.

Essas decisões não impedem preparar a fundação e o protótipo de interface. Registrar escolhas conforme o marco precisar delas e preservar o histórico das justificativas.
