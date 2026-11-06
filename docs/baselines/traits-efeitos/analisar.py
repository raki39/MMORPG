"""P4c: efeitos das traits. Q0 = efeitos desligados, Q1 = ligados (mesmas pessoas e traits).
Por trait: portadores Q1 x Q0 nas medidas onde a trait age; controle: quem nao tem a trait."""
import os, csv, re, statistics as st, collections
os.chdir(r'C:\Users\Pichau\OneDrive\Área de Trabalho\IdeiaJogo')
TYPES = ['hunt', 'boss', 'quest', 'explore', 'gather', 'trade', 'socialize', 'train', 'repair', 'patrol', 'ambush']
def load(v): return {(r['seed'], r['person_id']): r for r in csv.DictReader(open(f'reports/p4c/val/{v}/people.csv', encoding='utf-8')) if r['registered'] == '1'}
A, B = load('Q0'), load('Q1')
keys = sorted(set(A) & set(B))
def traits(r): return [x for x in r['traits'].split('|') if x]
def stats(r): return dict((kv.split('=')[0], int(kv.split('=')[1])) for kv in r['quirk_stats'].split('|') if kv)
def f(r, k): return float(r[k]) if r[k] not in ('', 'nan', 'NaN') else 0.0
def share(r, ts):
    pl = int(r['min_played']); return 100 * sum(int(r['min_' + t]) for t in ts) / pl if pl else 0
def walk(r):
    pl = int(r['min_played']); return 100 * (pl - sum(int(r['min_' + t]) for t in TYPES)) / pl if pl else 0
def idle(r):
    pl = int(r['min_played']); return 100 * int(r['min_idle']) / pl if pl else 0
M = [  # (rotulo, funcao)
    ('horas/sem', lambda r: f(r, 'played_h_week')), ('nível', lambda r: f(r, 'level')),
    ('frustração', lambda r: f(r, 'frustration')), ('satisfação', lambda r: f(r, 'satisfaction')), ('engajamento', lambda r: f(r, 'engagement')),
    ('saídas com raiva', lambda r: f(r, 'rage_quits')), ('metas concluídas', lambda r: f(r, 'goals_completed')), ('metas abandonadas', lambda r: f(r, 'goals_abandoned')),
    ('conhecimento', lambda r: f(r, 'game_knowledge')), ('mecânica', lambda r: f(r, 'mechanical_skill')),
    ('zonas conhecidas', lambda r: f(r, 'known_regions')), ('pistas', lambda r: f(r, 'hints')), ('memórias', lambda r: f(r, 'memories')),
    ('voltou (vezes)', lambda r: f(r, 'times_returned')), ('desistiu', lambda r: 100.0 if r['state'] == 'Quit' else 0.0),
    ('andando %', walk), ('espera %', idle), ('explorar %', lambda r: share(r, ['explore'])), ('missão %', lambda r: share(r, ['quest'])),
    ('caça+chefe %', lambda r: share(r, ['hunt', 'boss'])), ('cidade %', lambda r: share(r, ['socialize', 'trade', 'train', 'repair'])),
]
RELEVANT = {
    'persistente': ['metas abandonadas', 'metas concluídas', 'frustração', 'horas/sem'],
    'impulsivo': ['metas concluídas', 'andando %', 'missão %', 'satisfação', 'horas/sem'],
    'colecionador': ['zonas conhecidas', 'explorar %', 'andando %', 'nível', 'satisfação'],
    'otimizador': ['nível', 'caça+chefe %', 'cidade %', 'satisfação', 'horas/sem'],
    'resiliente': ['frustração', 'saídas com raiva', 'memórias', 'engajamento', 'desistiu'],
    'pavio_curto': ['frustração', 'saídas com raiva', 'horas/sem', 'engajamento', 'desistiu'],
    'saudosista': ['memórias', 'voltou (vezes)', 'desistiu', 'satisfação', 'engajamento'],
    'rancoroso': ['satisfação', 'cidade %', 'caça+chefe %', 'andando %'],
    'estudioso': ['conhecimento', 'missão %', 'nível'],
    'esquecido': ['mecânica', 'conhecimento'],
    'memoria_firme': ['mecânica', 'conhecimento', 'satisfação'],
    'observador': ['pistas', 'zonas conhecidas', 'explorar %'],
    'indeciso': ['espera %', 'andando %', 'nível', 'horas/sem'],
    'maratonista': ['horas/sem', 'nível', 'satisfação', 'frustração'],
    'procrastinador': ['horas/sem', 'nível', 'satisfação'],
}
out = []; w = out.append
w('## Por traço: quem tem o traço, com efeitos (Q1) contra as mesmas pessoas sem efeitos (Q0)\n')
w('Média por pessoa; entre colchetes, a variação no grupo sem o traço (controle de interações).\n')
mi = {name: fn for name, fn in M}
for q, rel in RELEVANT.items():
    car = [k for k in keys if q in traits(B[k])]
    non = [k for k in keys if q not in traits(B[k])]
    if not car: continue
    act = collections.Counter()
    for k in car:
        for kk, vv in stats(B[k]).items():
            if kk.startswith(q): act[kk] += vv
    acts = ', '.join(f'{kk.replace(q + "_", "")} {vv / len(car):.1f}' for kk, vv in sorted(act.items()))
    w(f'### {q} (n = {len(car)})')
    w(f'- Atuações por pessoa: {acts or "nenhuma"}')
    parts = []
    for name in rel:
        fn = mi[name]
        a = st.mean(fn(A[k]) for k in car); b = st.mean(fn(B[k]) for k in car)
        ca = st.mean(fn(A[k]) for k in non); cb = st.mean(fn(B[k]) for k in non)
        parts.append(f'{name}: {a:.2f} → {b:.2f} [{cb - ca:+.2f}]')
    w('- ' + '; '.join(parts))
    w('')
# geral
w('## Todos (Q0 → Q1)\n')
w('- ' + '; '.join(f'{name}: {st.mean(fn(A[k]) for k in keys):.2f} → {st.mean(fn(B[k]) for k in keys):.2f}' for name, fn in M))
# validacao
def parse(p):
    d = {}
    for line in open(p, encoding='utf-8'):
        m = re.match(r'\|\s*(✅|❌|⚠️)[^|]*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|', line)
        if m: d[m.group(2)] = (m.group(1), m.group(3), m.group(4), m.group(5))
    return d
va, vb = parse('reports/p4c/val/Q0/validation.md'), parse('reports/p4c/val/Q1/validation.md')
w('\n## Validação (Q0 → Q1), só o que mudou\n')
for k in va:
    if k in vb and (va[k][0] != vb[k][0] or va[k][2] != vb[k][2]):
        w(f'- {k}: {va[k][0]} {va[k][2]} → {vb[k][0]} {vb[k][2]} (IC {vb[k][3]})')
for v in ('Q0', 'Q1'):
    for line in open(f'reports/p4c/val/{v}/validation.md', encoding='utf-8'):
        if line.startswith('### '): w(f'- {v}: {line[4:].strip()}')
print('\n'.join(out))
open('reports/p4c/analise.md', 'w', encoding='utf-8', newline='').write('# P4c — efeitos das traits: medição\n\n' + '\n'.join(out) + '\n')
