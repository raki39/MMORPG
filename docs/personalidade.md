# Personalidade principal (P4a)

Cada jogador tem **uma** personalidade principal: a identidade central dele. Ela tem uma regra de comportamento própria. As **traits** (P4b em diante) são os traços, um sistema separado.

Personalidade e traits também não se confundem com:
- **Perfil:** dedicação;
- **Preferências:** do que a pessoa gosta;
- **Especialidades:** familiaridade desenvolvida;
- **atributos numéricos:** efeitos graduais, sempre ativos.

Tudo fica em `population.json`, seção `personality`, e é validado.

## 1. Como a personalidade é definida

- **Seis atributos** de personalidade definem os 12 tipos, um para cada extremo:

  | Atributo | Alto | Baixo |
  |---|---|---|
  | Sociabilidade | Sociável | Reservado |
  | Agressividade | Agressivo | Pacífico |
  | Paciência | Paciente | Inquieto |
  | Lealdade | Leal | Independente |
  | Aversão a risco | Cauteloso | Destemido |
  | Variedade | Versátil | Metódico |

- **Régua:** a posição da pessoa em cada atributo é calculada na **distribuição do preset** (a acumulada da triangular do conteúdo, de 0 a 1000). Não depende de quem está no servidor; entrar ou sair gente não muda a classificação de ninguém.
  - Para isso, os seis atributos ficam no formato triangular. Paciência, Lealdade e Aversão a risco foram convertidos para a triangular equivalente (mesmo centro e mesmo desvio, como no D-053).
- **Destaque** de um tipo = a distância da posição até o meio, na direção do tipo, de −1000 a 1000. Por exemplo, Pacífico = (500 − posição na agressividade) × 2.
- **Classificação:** vence o tipo de maior destaque, se ele chegar ao limite (`thresholdPermille` = 700, isto é, estar nos 15% de um extremo). Senão, a pessoa é **Equilibrado**.
- **Extremo em vários atributos:** só o mais extremo vira personalidade. Os outros extremos continuam agindo pelo efeito gradual dos atributos.
  - Exemplo: alguém nos 3% mais pacíficos e nos 1% mais cautelosos é Cauteloso, e continua pouco agressivo pelo atributo.
  - Empate exato (o mesmo destaque, em milésimos) fica com a ordem fixa da lista. É raro, mas possível.
- **Distribuição resultante** (12 mil pessoas): Equilibrado 11%; cada uma das 12 personalidades entre 7,2% e 7,8%. Nenhuma é forçada a ser mais comum.
  - Antes, a régua era o meio da escala de 1 a 20. Ela dava 33% de Pacíficos e nenhum Agressivo, porque a agressividade da população é baixa por natureza.

## 2. Mudança (rara)

- **Só os seis atributos de personalidade contam.** Capacidades, especialidades, preferências, competitividade e humor não mudam a personalidade diretamente.
- **Avaliação diária**, com histerese (limite L = 700, margem M = 100):

  | De | Para | Condição |
  |---|---|---|
  | um tipo | outro tipo | o outro chega ao limite **e** passa o atual + M |
  | um tipo | Equilibrado | o atual cai abaixo de L − M (600) **e** nenhum outro chega a L + M (800) |
  | Equilibrado | um tipo | o tipo chega a L + M (800) |
  | — | (fica como está) | nos demais casos |

  - **Faixa de proteção:** entre 600 e 800 ninguém entra nem sai de Equilibrado. Quem oscila em volta do limite (700) não alterna nos dois sentidos.
- **Confirmação:** o mesmo alvo precisa se manter por `confirmDays` = 14 dias seguidos. Se ele se desfaz antes, a contagem recomeça.
- **Registro:** evento `personality_changed` e histórico na pessoa, com a personalidade anterior, a nova e o atributo que motivou a troca (valor e posição). Ao virar Equilibrado, o atributo registrado é o do tipo que saiu.
- **Na prática, hoje:** só Sociabilidade (tempo na taverna) e Aversão a risco (enfrentar risco) evoluem, e devagar. A mudança fica rara de propósito, e o mecanismo está pronto para os sistemas que mexerem nesses atributos.

## 3. Regras próprias

As regras são **categóricas**: "nunca", "sempre", "começa por". Elas aparecem no detalhe da decisão como o fator **"Personalidade"**, separado dos efeitos graduais dos atributos, que continuam como estavam. Isso evita aplicar o mesmo efeito duas vezes.

**Garantias:**
- **Alternativa válida:** nenhuma regra tira opções. O "nunca" é uma penalidade grande (1000), então a opção só vence se não houver outra válida.
- **Limites:** disponibilidade, sono, obrigações e validade das ações continuam valendo, porque as regras só mudam a escolha entre opções já válidas dentro da sessão.

| Personalidade | Regra | Onde age |
|---|---|---|
| Pacífico | nunca inicia PvP (emboscada, patrulha); evita zona de PvP aberto (−300), salvo se uma missão ou meta pedir | nota |
| Agressivo | +150 em emboscada e patrulha; ignora metade da penalidade de lotação na caça e na coleta | nota |
| Sociável | +120 onde há 3 ou mais pessoas, na primeira decisão da sessão e nos últimos 45 min | nota |
| Reservado | nunca entra em atividade com lotação a partir de 80% da capacidade | nota |
| Paciente | acontecimentos não derrubam o plano da sessão; +100 para etapa de missão em andamento | plano e nota |
| Inquieto | −250 para repetir a mesma atividade depois de 2 vezes seguidas | nota |
| Leal | não abandona meta por impaciência | metas |
| Independente | só aceita missões com etapa de um tema que ele prefere (a partir de 400) | quadro de missões |
| Cauteloso | nunca entra em conteúdo com desafio acima do nível + 1 | nota |
| Destemido | o risco da primeira faixa acima do nível não pesa (devolvido no fator) | nota |
| Versátil | −150 para começar a sessão pela mesma atividade da sessão anterior | nota |
| Metódico | +150 para começar a sessão pela mesma atividade da sessão anterior | nota |
| Equilibrado | sem regra nem bônus | — |

**Depois** (com os sistemas):
- **Pacífico na Etapa 3:** não inicia PvP, só se defende ou foge.
- **Agressivo na Etapa 3:** inicia confrontos e revida.
- **Grupos (M6):** Sociável aceita convites; Reservado evita grupos grandes; Leal não troca de guilda; Independente sai de grupo com facilidade.

## 4. Medição

Validação padrão (5 seeds × 1.000 pessoas × 90 dias). **P0** = personalidade classificada, sem regras; **P1** = com regras (preset). P0 e P1 têm as mesmas pessoas. Resultados em `baselines/personalidade/`.

- **Distribuição (quem tem conta):** Equilibrado 11,7%; as 12 personalidades entre 6,8% e 7,8%.
- **Mudanças em 90 dias:** nenhuma em 5.000 pessoas. É o esperado, porque hoje só Sociabilidade e Aversão a risco evoluem, e devagar (seção 2).
- **Rotina:** igual. Regras do cenário 7 de 9 nas duas versões; dias, sessões e horas por perfil iguais; pico online 28,3%.
- **Efeito das regras** (parcela do tempo, P0 → P1):

  | Personalidade | Mudança principal |
  |---|---|
  | Pacífico | PvP 2,2% → 0,0% |
  | Agressivo | PvP 8,7% → 31,2% (patrulha 15,5%, emboscada 7,4%) |
  | Reservado | PvP 5,2% → 3,6% (lugares cheios) |
  | Sociável | cidade 49,1% → 50,4% |
  | Paciente | missões 2,1% → 3,2% |
  | Inquieto | exploração 11,9% → 15,1%; cidade 31,7% → 25,7% |
  | Metódico | cidade 30,2% → 32,1% (começa onde parou) |
  | Equilibrado | sem mudança |

- **Agressivo:** o efeito é forte, mas ele continua respondendo:
  - **à preferência de PvP:** 8,7% no quartil mais baixo, 43,8% no mais alto;
  - **ao risco:** 31,9% com pouca aversão, 12,7% com muita;
  - **às outras preferências:** as correlações entre preferência e tempo no tema ficam iguais às dos demais;
  - **às metas e ao nível:** os do nível 1 a 10 não fazem PvP.
  - O bônus de 150 fica como valor candidato.
- **Andando ou esperando:** de 19,4% para 20,3%, acima do limite provisório de 20%.
  - Vem do Inquieto (troca de lugar depois de 2 ciclos), do Paciente (etapas de missão em outras zonas) e do Versátil (começa a sessão em outro lugar).
  - Agressivo, Pacífico e Metódico passam a andar menos.
  - Não é ciclo vazio: no Inquieto, a distribuição inteira se desloca, e nível e horas não mudam.
  - **Decisão do criador:** não enfraquecer as personalidades para a meta passar. A divergência fica registrada em `validacao.md`.
- **Correção durante a medição:** o Independente ia ao quadro de missões sem aceitar nada (30% do tempo em missões). Agora o quadro só conta como opção quando tem uma missão que ele aceitaria, e a meta "concluir missão" segue o mesmo filtro. Depois da correção: 2,2%.
