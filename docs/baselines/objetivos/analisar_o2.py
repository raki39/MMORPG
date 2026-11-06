import io, os, csv, sys
from collections import Counter, defaultdict
os.chdir(r'C:\Users\Pichau\OneDrive\Área de Trabalho\IdeiaJogo')
base = sys.argv[1] if len(sys.argv) > 1 else 'docs/baselines/objetivos/O2'

def load(run):
    rows = list(csv.reader(io.open(f'{base}/{run}/people.csv', encoding='utf-8')))
    h = rows[0]
    return h, [dict(zip(h, r)) for r in rows[1:]]

h, OFF = load('desligada')
_, ON = load('ligada')
assert len(OFF) == len(ON)
key = lambda r: (r['seed'], r['person_id'])
offm = {key(r): r for r in OFF}
# mesmas pessoas e mesmos objetivos nas duas rodadas
assert all(offm[key(r)]['objectives'] == r['objectives'] for r in ON), 'objetivos diferentes entre as rodadas'

acts = [c[4:] for c in h if c.startswith('min_') and c not in ('min_idle', 'min_played')]
GROUPS = [('actionable', 'acionáveis'), ('preparation', 'só preparação'), ('unavailable', 'indisponíveis'), ('all', 'todos')]

def f(x):
    try: return float(x)
    except: return float('nan')

def mean(xs):
    xs = [x for x in xs if x == x]
    return sum(xs) / len(xs) if xs else float('nan')

def gtypes(r):
    return {kv.split(':')[0]: int(kv.split(':')[1]) for kv in r['goals_by_type'].split('|') if kv}

def summary(rows):
    reg = [r for r in rows if r['registered'] == '1']
    n = len(reg)
    s = {}
    s['n'] = n
    gs = sum(int(r['goals_completed']) + int(r['goals_abandoned']) for r in reg)
    s['metas adotadas/pessoa'] = mean([sum(gtypes(r).values()) for r in reg])
    s['metas ligadas/pessoa'] = mean([int(r['goals_linked_set']) for r in reg])
    tot = sum(sum(gtypes(r).values()) for r in reg)
    lk = sum(int(r['goals_linked_set']) for r in reg)
    s['% metas ligadas'] = 100 * lk / tot if tot else 0
    s['% metas com preparação'] = 100 * sum(int(r['goals_prep_set']) for r in reg) / tot if tot else 0
    ld = sum(int(r['goals_linked_done']) for r in reg); la = sum(int(r['goals_linked_abandoned']) for r in reg)
    s['ligadas: % concluídas'] = 100 * ld / lk if lk else 0
    s['ligadas: % abandonadas'] = 100 * la / lk if lk else 0
    ud = sum(int(r['goals_completed']) for r in reg) - ld; ua = sum(int(r['goals_abandoned']) for r in reg) - la
    s['independentes: % concluídas'] = 100 * ud / (tot - lk) if tot - lk else 0
    s['independentes: % abandonadas'] = 100 * ua / (tot - lk) if tot - lk else 0
    for t in ['reach_level', 'unlock_level', 'discover_zone', 'complete_quest', 'practice_activity']:
        s[f'metas {t}/pessoa'] = mean([gtypes(r).get(t, 0) for r in reg])
    s['nível'] = mean([f(r['level']) for r in reg])
    s['h jogadas/semana'] = mean([f(r['played_h_week']) for r in reg])
    s['satisfação'] = mean([f(r['satisfaction']) for r in reg])
    s['engajamento'] = mean([f(r['engagement']) for r in reg])
    s['% desistiu'] = 100 * sum(r['state'] == 'Quit' for r in reg) / n if n else 0
    s['zonas conhecidas'] = mean([f(r['known_regions']) for r in reg])
    s['objetivos concluídos/100 pessoas'] = 100 * mean([len([x for x in r['objectives_completed'].split('|') if x]) for r in reg])
    played = sum(f(r['min_played']) for r in reg)
    for a in acts:
        s[f'% tempo {a}'] = 100 * sum(f(r['min_' + a]) for r in reg) / played if played else 0
    s['% tempo andando/esperando'] = 100 * (played - sum(sum(f(r['min_' + a]) for a in acts) for r in reg)) / played if played else 0
    return s

out = []
for g, name in GROUPS:
    a = summary([r for r in OFF if g == 'all' or r['objective_group'] == g])
    b = summary([r for r in ON if g == 'all' or r['objective_group'] == g])
    out.append(f'\n## {name} (n = {a["n"]})\n')
    out.append('| Medida | Desligada | Ligada | Diferença |')
    out.append('|---|---|---|---|')
    for k in a:
        if k == 'n': continue
        d = b[k] - a[k]
        out.append(f'| {k} | {a[k]:.2f} | {b[k]:.2f} | {d:+.2f} |')

# objetivos concluidos por id
def comp(rows):
    c = Counter()
    for r in rows:
        for x in r['objectives_completed'].split('|'):
            if x: c[x.split('@')[0]] += 1
    return c
co, cn = comp(OFF), comp(ON)
out.append('\n## Objetivos concluídos (pessoas)\n')
out.append('| Objetivo | Desligada | Ligada |')
out.append('|---|---|---|')
for k in sorted(set(co) | set(cn)):
    out.append(f'| {k} | {co[k]} | {cn[k]} |')

# metas ligadas por objetivo (ligada)
def linked(rows):
    c = defaultdict(lambda: [0, 0, 0])
    for r in rows:
        for x in r['objective_links'].split('|'):
            if not x: continue
            oid, nums = x.split(':')
            a, b, d = map(int, nums.split('/'))
            c[oid][0] += a; c[oid][1] += b; c[oid][2] += d
    return c
lo, ln = linked(OFF), linked(ON)
out.append('\n## Metas ligadas por objetivo (adotadas / concluídas / abandonadas)\n')
out.append('| Objetivo | Desligada | Ligada |')
out.append('|---|---|---|')
for k in sorted(set(lo) | set(ln)):
    out.append(f'| {k} | {"/".join(map(str, lo[k]))} | {"/".join(map(str, ln[k]))} |')
print('\n'.join(out))
