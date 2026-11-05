# Ideia futura: percepção e caça espacial

**Status:** proposta do criador para o combate (Etapa 2, M3), registrada em 05/11/2026. Será detalhada quando chegarmos à implementação; não altera a etapa atual.

## Percepção do personagem

Cada personagem terá um **raio de percepção de 360°**, configurável, que informa sobre criaturas, personagens e acontecimentos próximos.

**Percepção é diferente de alcance de ataque.** Detectar um alvo permite avaliar uma ação, mas não significa conseguir atacá-lo imediatamente.

As decisões consideram o que foi percebido, os objetivos, a personalidade, a memória, as capacidades e o estado atual. Exemplos:
- reconhecer um rival e decidir atacar, evitar ou ignorar, conforme as regras de PvP;
- escolher criaturas próximas para caçar;
- atrair mais monstros quando estima que consegue enfrentar um grupo maior;
- fugir ao perceber que o encontro ficou perigoso.

A capacidade de enfrentar vários monstros será uma **estimativa do jogador simulado, sujeita a erros**, conforme conhecimento, experiência, recursos e força percebida dos adversários.

## Área de caça

A região sombreada de cada área de caça representa o **território das criaturas**. O marcador central identifica o local; os monstros existem individualmente, distribuídos pelo espaço.

Dentro dela haverá posições de surgimento, deslocamentos e possíveis patrulhas. Quantidade, densidade, agressividade e reaparecimento serão configuráveis.

Não é preciso aumentar todas as áreas agora. O tamanho será validado com distâncias reais, percepção, alcance e espaço para movimentação.

## Borda e perseguição

- Fora de combate, os monstros ficam dentro da área.
- Quando atraídos por um personagem, podem passar da borda por uma margem limitada.
- Ao atingir o limite de perseguição, desistem e voltam sozinhos ao território.
- A perseguição tem limite espacial e, se necessário, temporal, para impedir que sejam arrastados indefinidamente.
- A margem externa, as condições de desistência e o destino de retorno serão configuráveis.
- Zonas seguras e fronteiras sem conexão válida continuam sendo respeitadas.

Durante o retorno, a regra inicial proposta é interromper a agressão, impedir novas provocações e restaurar a vida. Isso evita que personagens usem a borda para causar dano sem risco. Os detalhes serão definidos no combate.

## Correspondência entre 2D e 3D

Posições, percepção, perseguição e combate pertencem à simulação. O mapa 2D e a futura observação 3D representam esses mesmos dados.

Abrir ou fechar uma região em 3D não altera os acontecimentos nem os resultados (regra do `CLAUDE.md`).

## Atividade de caça e seleção de alvos

O jogador escolhe uma **atividade de caça** ligada a uma área e viaja até ela. A seleção de cada monstro acontece ao executar essa atividade, depois de chegar ao local.

Durante a caça, a percepção permite localizar criaturas elegíveis, escolher alvos, movimentar-se, atrair inimigos e avaliar o perigo. Essas ações fazem parte da mesma atividade: cada monstro derrotado não exige uma nova escolha global de atividade.

A caça pode ter vários encontros seguidos. Os critérios para continuar ou encerrar consideram objetivos, criaturas disponíveis, recursos, risco e tempo de sessão.

Perceber uma criatura durante a viagem **não** inicia uma atividade de caça. Reagir a uma agressão é um comportamento separado e pode interromper o deslocamento por um tempo.

## "Matar monstros" é uma ação reutilizável

A tarefa define por que a ação está sendo executada:

| Tarefa atual | Etapas possíveis |
|---|---|
| Caçar | Ir à área de caça → procurar alvos → matar monstros → recolher o saque |
| Fazer missão | Aceitar → ir ao local → matar os monstros exigidos → entregar |
| Dungeon | Entrar → percorrer as salas → matar monstros → derrotar o chefe |
