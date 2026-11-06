# Personagem (Etapa 1)

Decisões no D-061 (escopo reduzido pelo criador). Base no D-040: estatísticas diretas e morte sem perda.

## 1. Pessoa × personagem

| | Pessoa (quem joga) | Personagem (com o que joga) |
|---|---|---|
| Guarda | Perfil, Preferências, personalidade, capacidades, Conduta, rotina, traits, humor e memória | Classe, nível, XP, estatísticas, vida e recurso atuais |
| Papel | Decide e, no combate, executa: as capacidades influem nas decisões e na execução | Fornece os números |

- As capacidades da pessoa nunca entram nas estatísticas base do personagem.
- **Um personagem por pessoa.** Personagens alternativos ficam para uma etapa posterior.
- **Adiado para depois do combate inicial:**
  - instâncias de item, inventário e equipamento funcional;
  - carteira;
  - aplicação material das recompensas.

  As recompensas de missão continuam **previstas** na interface e não são concedidas em silêncio.
- O primeiro combate usa só as estatísticas da classe e do nível, sem equipamento.

## 2. Estatísticas

**Fórmula:** estatística = base da classe + crescimento por nível × (nível − 1), só com inteiros.
- O nível usado é limitado de 1 ao nível máximo da progressão (`progression.json`, hoje 30).
- Equipamento e efeitos temporários entram depois, como novas parcelas da decomposição.

| Chave | Na tela | Unidade |
|---|---|---|
| `health` | Vida | pontos |
| `resource` | nome do recurso da classe (Fúria, Mana…) | pontos |
| `physicalAttack` | Ataque físico | pontos |
| `magicAttack` | Ataque mágico | pontos |
| `physicalDefense` | Defesa física | pontos |
| `magicDefense` | Defesa mágica | pontos |
| `critChancePermille` | Chance de crítico | milésimos (50 = 5%) |
| `critMultiplierPermille` | Multiplicador de dano crítico | milésimos do dano normal (1500 = 150%) |
| `moveSpeed` | Velocidade de movimento | unidades de mapa por minuto (1 unidade = 10 m; 15 = 9 km/h) |

### Limites (`combat.json`)

- **Chance de crítico:** de 0 a 750 (75%).
- **Multiplicador:** de 1000 (sem bônus) a 5000 (500%).
- **Velocidade:** no mínimo 1.
- **Demais estatísticas:** no mínimo 0.

O validador recusa uma **base** fora dos limites. O crescimento por nível pode passar deles, e então o valor efetivo é cortado no cálculo. A ficha marca o corte.

### Recurso

A classe define o **tipo** (a regra) e o **nome** exibido.

| Tipo | Fora de combate | No combate (Etapa 2) |
|---|---|---|
| `mana` | cheio | gasta e regenera |
| `rage` (fúria) | vazio | gerada ao causar e receber dano |
| `energy` | cheio | reserva pequena que regenera rápido |

Na criação do personagem e a cada nível, a vida fica cheia e o recurso segue a regra do tipo. O combate é que passará a gastar e recuperar.

### Velocidade de movimento

- Saiu da pessoa (`population.json`, `behavior.walkSpeed`) e foi para o personagem.
- Os presets usam 15 em todas as classes, sem crescimento por nível. É o valor de antes, então o deslocamento atual fica igual.
- A velocidade é configurável por classe.
- No mapa, o tempo de caminhada entre zonas usa a velocidade da classe mais lenta.

### Crítico nas habilidades

- Uma habilidade só pode ser crítica quando o efeito declara `canCrit`. Isso só vale para **dano e cura diretos**.
- Não critam: dano e cura periódicos, escudos, buffs, controle (enraizar) e ameaça.
- **Presets:** Golpe Heroico, Bola de Fogo (o impacto), Lança de Gelo (o dano), Explosão Arcana, Punição e Cura.
- A resolução do crítico entra no combate (Etapa 2). Nesta etapa a marcação só é declarada.

### Habilidades

- A classe se liga às habilidades **pela própria lista** (`abilityIds`).
- O `classId` da habilidade é só informativo. Uma classe nova pode usar habilidades de outra.
- Nenhuma regra do código depende do nome ou do id de uma classe.
- **Ícone:** o ícone da classe vem do conteúdo. Se faltar, ou se o ícone não existir, a interface usa um genérico pelo papel.

### Classes de teste (valores candidatos)

| Nível 1 → 10 → 30 | Guerreiro | Mago | Sacerdote |
|---|---|---|---|
| Vida | 220 → 436 → 916 | 130 → 238 → 478 | 150 → 276 → 556 |
| Recurso | Fúria 100 | Mana 200 → 362 → 722 | Mana 220 → 400 → 800 |
| Ataque físico | 18 → 45 → 105 | 4 | 5 |
| Ataque mágico | 4 | 22 → 58 → 138 | 20 → 47 → 107 |
| Defesa física | 40 → 76 → 156 | 10 → 19 → 39 | 14 → 32 → 72 |
| Defesa mágica | 10 → 19 → 39 | 20 → 38 → 78 | 24 → 51 → 111 |
| Chance de crítico | 5% | 7% | 4% |
| Multiplicador crítico | 150% | 150% | 150% |
| Velocidade | 15 | 15 | 15 |

## 3. Índice de poder (candidato)

**Para que serve:**
- comparar personagens na interface;
- estimar rapidamente a ameaça e a dificuldade;
- apoiar a percepção (Etapa 2).

Não substitui as fórmulas do combate.

**De onde sai:** é calculado das estatísticas **efetivas**. Hoje são a classe e o nível; depois entram o equipamento e outros modificadores, sem mudar a fórmula.

```
Ataque        = max(AF, AM) + min(AF, AM) × 25%
Crítico       = 1 + chance × (multiplicador − 1)
Ofensiva      = Ataque × Crítico ÷ 22
Defesa média  = DF × 50% + DM × 50%
Sobrevivência = Vida × (100 + Defesa média) ÷ 100 ÷ 200
Poder         = 100 × √(Ofensiva × Sobrevivência)
```

- **Por que média geométrica:** num duelo, quem vence é quem derruba o outro antes. Isso é dano por tempo × tempo até cair. A raiz faz o poder crescer perto do linear com o nível.
- **Ataque secundário:** conta só 25%. Ninguém usa ataque físico e mágico inteiros ao mesmo tempo, então somar os dois inflaria classes híbridas.
- **Defesa:** usa a mesma escala que a mitigação do combate, defesa ÷ (defesa + 100). Com 100 de defesa média, a vida efetiva dobra.
- **Referência:** quem tem 22 de ataque, 200 de vida efetiva e nenhum crítico tem poder 100.
- **Configuração:** todos os pesos e referências ficam em `combat.json` (`powerIndex`).

| Poder (nível 1 → 10 → 30) | Guerreiro | Mago | Sacerdote |
|---|---|---|---|
| Poder | 110 → 262 → 668 | 90 → 206 → 498 | 94 → 209 → 517 |
| Ofensiva × Sobrevivência no nível 30 | 4,94 × 9,04 | 6,54 × 3,79 | 5,02 × 5,32 |

**Nominal × condição:**
- O **poder nominal** não olha a vida nem o recurso atuais.
- A **condição** fica à parte: vida e recurso atuais sobre o máximo.
- **Estimativa no momento** (para a ameaça, depois do combate): poder × √(vida atual ÷ vida máxima). Com metade da vida, fica em cerca de 71% do poder.
- O recurso não entra na estimativa, porque a fúria começa vazia por regra, não por fraqueza.

**Limites conhecidos do candidato** (aprovado assim pelo criador, sem ser balanceamento definitivo):
- **Não representa** as habilidades, a cura, a proteção de aliados, o alcance nem o desempenho real em combate.
  - O Sacerdote aparece abaixo do que vale num grupo.
  - A escala de cada habilidade (Bola de Fogo escala 120% do ataque mágico), o custo e a recarga ficam de fora.
- **A defesa é uma média:** contra um adversário só de dano físico, a defesa física vale mais do que a fórmula mostra.
- **A estimativa pela vida atual é provisória.** Ela não deve virar o único critério das decisões futuras, como ameaça, fuga ou escolha de alvo.
- **"Ser forte" não é o índice:** os objetivos pessoais não reduzem força ao índice de poder.
- Pesos, valores de crítico e limites são candidatos configuráveis.

## 4. Próximas partes

- **E1b:** página Classes, com criar, editar e duplicar, e conteúdo personalizado persistido. Os presets ficam preservados e os ids, estáveis.
- **E1c:** aba "Personagem", com classe, nível, XP, vida, recurso, estatísticas e a decomposição do cálculo. Inventário e Banco não aparecem como prontos.

## 5. Medição

Validação padrão (5 seeds × 1.000 × 90 dias), comparada à última rodada da P4d (`reports/p4d/val/A1`):
- `validation.csv` e `people.csv` são idênticos;
- os presets preservam a progressão e o deslocamento atuais.

O hash do estado muda (versão 28), porque a vida e o recurso atuais entraram nele.
