import io, os, csv, sys
from collections import Counter, defaultdict
os.chdir(r'C:\Users\Pichau\OneDrive\Área de Trabalho\IdeiaJogo')
base = sys.argv[1] if len(sys.argv) > 1 else 'docs/baselines/objetivos/O3'
OFFN, ONN = (sys.argv[2], sys.argv[3]) if len(sys.argv) > 3 else ('desligada', 'ligada')

def load(run):
    rows = list(csv.reader(io.open(f'{base}/{run}/people.csv', encoding='utf-8')))
    return rows[0], [dict(zip(rows[0], r)) for r in rows[1:]]
h, OFF = load(OFFN)
_, ON = load(ONN)
reg = lambda rows: [r for r in rows if r['registered'] == '1']
def f(x):
    try: return float(x)
    except: return float('nan')
def mean(xs):
    xs = [x for x in xs if x == x]
    return sum(xs) / len(xs) if xs else float('nan')
def hist(r):
    out = []
    for x in r['objective_history'].split('|'):
        if not x: continue
        change, rest = x.split(':', 1)
        oid, rest = rest.split('@', 1)
        day, reason = rest.split(':', 1) if ':' in rest else (rest, '')
        out.append((change, oid, int(day), reason))
    return out
def states(r):
    return [tuple(x.split(':')) for x in r['objective_states'].split('|') if x]
cap = 30
out = []
out.append('# O3: evolução desligada × ligada (5 seeds × 1.000 × 90 dias)\n')

# --- mudancas na rodada ligada
on = reg(ON); off = reg(OFF); n = len(on)
ch = Counter(); trig = Counter(); adopted = Counter(); gap_people = 0; gaps = 0
for r in on:
    hs = hist(r)
    for (c, oid, d, reason) in hs:
        if c == 'set' and reason == 'initial': continue
        ch[c] += 1
        if c in ('set', 'resumed', 'recovered', 'readopted'):
            adopted[oid] += 1
            trig[reason.split('+')[0].split(':')[0]] += 1
        if c == 'gap': trig['gap:' + reason.split(':')[0]] += 1
    g = int(r['objective_gaps']); gaps += g; gap_people += g > 0
out.append(f'## Mudanças (ligada, {n} pessoas)\n')
out.append('| Mudança | Total | Por 100 pessoas |')
out.append('|---|---|---|')
for k, v in ch.most_common():
    out.append(f'| {k} | {v} | {100*v/n:.1f} |')
out.append(f'\nPessoas com pelo menos uma falta de objetivo relevante: {gap_people} ({100*gap_people/n:.1f}%); faltas no total: {gaps}.\n')
out.append('| Motivo da renovação ou da falta | Vezes |')
out.append('|---|---|')
for k, v in trig.most_common():
    out.append(f'| {k} | {v} |')
out.append('\n| Objetivo adotado na renovação | Vezes |')
out.append('|---|---|')
for k, v in adopted.most_common():
    out.append(f'| {k} | {v} |')

# --- estado no fim, por grupo e no nivel maximo
def endstate(rows):
    s = {}
    rr = reg(rows); m = len(rr)
    s['pessoas'] = m
    s['ativos no fim (média)'] = mean([int(r['objectives_active_end']) for r in rr])
    s['% com algum ativo acionável no fim'] = 100 * sum(int(r['objectives_actionable_end']) > 0 for r in rr) / m
    s['% sem nenhum ativo no fim'] = 100 * sum(int(r['objectives_active_end']) == 0 for r in rr) / m
    s['objetivos alcançados/pessoa'] = mean([sum(1 for st in states(r) if st[1] in ('completed', 'held')) for r in rr])
    s['metas ligadas %'] = 100 * sum(int(r['goals_linked_set']) for r in rr) / max(1, sum(sum(int(kv.split(':')[1]) for kv in r['goals_by_type'].split('|') if kv) for r in rr))
    s['nível'] = mean([f(r['level']) for r in rr])
    s['satisfação'] = mean([f(r['satisfaction']) for r in rr])
    s['engajamento'] = mean([f(r['engagement']) for r in rr])
    s['h jogadas/semana'] = mean([f(r['played_h_week']) for r in rr])
    s['% desistiu'] = 100 * sum(r['state'] == 'Quit' for r in rr) / m
    played = sum(f(r['min_played']) for r in rr)
    for a in ['hunt', 'explore', 'gather', 'trade', 'socialize', 'patrol', 'quest']:
        s[f'% tempo {a}'] = 100 * sum(f(r['min_' + a]) for r in rr) / played
    return s
groups = [('all', 'todos'), ('actionable', 'acionáveis'), ('preparation', 'só preparação'), ('unavailable', 'indisponíveis'), ('cap', 'no nível máximo no fim')]
for g, name in groups:
    sel = (lambda r: True) if g == 'all' else (lambda r, g=g: int(r['level'] or 0) >= cap) if g == 'cap' else (lambda r, g=g: r['objective_group'] == g)
    a = endstate([r for r in OFF if sel(r)]); b = endstate([r for r in ON if sel(r)])
    out.append(f'\n## {name} (desligada {a["pessoas"]} · ligada {b["pessoas"]})\n')
    out.append('| Medida | Desligada | Ligada | Diferença |')
    out.append('|---|---|---|---|')
    for k in a:
        if k == 'pessoas': continue
        out.append(f'| {k} | {a[k]:.2f} | {b[k]:.2f} | {b[k]-a[k]:+.2f} |')

# --- no nivel maximo: o que ficou ativo
capon = [r for r in on if int(r['level'] or 0) >= cap]
act = Counter()
for r in capon:
    for st in states(r):
        if st[1] == 'active': act[st[0]] += 1
out.append(f'\n## No nível máximo (ligada, {len(capon)} pessoas): objetivos ativos no fim\n')
out.append('| Objetivo | Pessoas |')
out.append('|---|---|')
for k, v in act.most_common():
    out.append(f'| {k} | {v} |')
# abandonos e faltas por objetivo/motivo
ab = Counter(); gp = Counter(); gcap = 0; gp_people_need = 0
for r in on:
    hs = hist(r)
    for (c, oid, d, reason) in hs:
        if c == 'abandoned': ab[oid] += 1
        if c == 'gap': gp[reason.split(':')[0]] += 1
    if any(c == 'gap' for (c, _, _, _) in hs): gp_people_need += 1
    if int(r['level'] or 0) >= cap and any(c == 'gap' for (c, _, _, _) in hs): gcap += 1
out.append('\n## Abandonos por objetivo (ligada)\n')
out.append('| Objetivo | Abandonos |')
out.append('|---|---|')
for k, v in ab.most_common(): out.append(f'| {k} | {v} |')
out.append(f'\nFaltas por motivo: {dict(gp)}; pessoas com falta: {gp_people_need} ({100*gp_people_need/n:.1f}%); no nível máximo no fim e com falta: {gcap} de {len(capon)} ({100*gcap/max(1,len(capon)):.0f}%).')
print('\n'.join(out))
