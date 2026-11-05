"""Analise da P4a (personalidade principal): P0 (classificada, sem regras) x P1 (com regras),
mesmas pessoas; e contra o preset anterior (V3b, D-055). Le reports/p4a/val/<V>/."""
import os, csv, collections
import numpy as np
os.chdir(r'C:\Users\Pichau\OneDrive\Área de Trabalho\IdeiaJogo')
TYPES = ['hunt', 'boss', 'quest', 'explore', 'gather', 'trade', 'socialize', 'train', 'repair', 'patrol', 'ambush']
GROUPS = [('PvP (patrulha e emboscada)', ['patrol', 'ambush']), ('Caça e chefe', ['hunt', 'boss']), ('Missão (visitas)', ['quest']),
          ('Exploração', ['explore']), ('Coleta', ['gather']), ('Cidade', ['trade', 'socialize', 'train', 'repair'])]
NAMES = {'sociavel': 'Sociável', 'reservado': 'Reservado', 'agressivo': 'Agressivo', 'pacifico': 'Pacífico', 'paciente': 'Paciente',
         'inquieto': 'Inquieto', 'leal': 'Leal', 'independente': 'Independente', 'cauteloso': 'Cauteloso', 'destemido': 'Destemido',
         'versatil': 'Versátil', 'metodico': 'Metódico', 'equilibrado': 'Equilibrado'}
ORDER = list(NAMES)
def load(path): return {(r['seed'], r['person_id']): r for r in csv.DictReader(open(path, encoding='utf-8')) if r['registered'] == '1'}
P0 = load('reports/p4a/val/P0/people.csv'); P1 = load('reports/p4a/val/P1/people.csv')
OLD = load('docs/baselines/progressao-inicial/V3b/people.csv')
def tot(r): return sum(int(r['min_' + t]) for t in TYPES)
def share(rows, ts):
    a = sum(tot(r) for r in rows); return sum(sum(int(r['min_' + t]) for t in ts) for r in rows) * 100 / max(1, a)
def f1(x): return f'{x:.1f}'.replace('.', ',')
def med(a): return float(np.median(a)) if a else float('nan')
def rules(v):
    out = {}
    for line in open(f'reports/p4a/val/{v}/validation.md' if v != 'V3b' else 'docs/baselines/progressao-inicial/V3b/validation.md', encoding='utf-8'):
        if line.startswith('### '): out.setdefault('_s', []).append(line.strip('# \n'))
        if line.startswith('| ') and ('✅' in line or '❌' in line or '⚠' in line):
            c = [x.strip() for x in line.split('|')]; out[c[2].split(' (')[0]] = (c[1], c[4])
    return out
R = {v: rules(v) for v in ('V3b', 'P0', 'P1')}

md = []; w = md.append
w('# P4a — Personalidade principal: medição\n')
w('Validação padrão: seeds 1 a 5 × 1.000 pessoas × 90 dias.\n')
w('- **V3b:** preset anterior (D-055).')
w('- **P0:** personalidade classificada, sem as regras de comportamento. Paciência, Lealdade e Aversão a risco já no formato triangular equivalente.')
w('- **P1:** personalidade com as regras (preset).\n')
w('P0 e P1 têm as mesmas pessoas e as mesmas personalidades: a classificação não depende das regras. Contra V3b, as pessoas mudam nos três atributos convertidos.\n')

w('## Distribuição (P1, quem tem conta)\n')
ini = collections.Counter(r['personality_initial'] for r in P1.values()); fin = collections.Counter(r['personality'] for r in P1.values())
w('| Personalidade | Na criação | No fim (90 dias) |'); w('|---|---|---|')
for k in ORDER: w(f'| {NAMES[k]} | {ini[k]*100/len(P1):.1f}% | {fin[k]*100/len(P1):.1f}% |')
ch = [r for r in P1.values() if int(r['personality_changes']) > 0]
trans = collections.Counter((r['personality_initial'], r['personality']) for r in ch)
w(f'\n**Mudanças em 90 dias:** {len(ch)} de {len(P1)} pessoas ({len(ch)*100/len(P1):.2f}%).' + (' Transições: ' + ', '.join(f'{NAMES.get(a,a)} → {NAMES.get(b,b)} {n}' for (a, b), n in trans.most_common(10)) + '.' if ch else ''))
w('')

w('## Comportamento por personalidade (parcela do tempo em atividades; P0 sem regras → P1 com regras)\n')
w('| Personalidade | n | ' + ' | '.join(g for g, _ in GROUPS) + ' | Nível (mediana) | Horas/semana (mediana) |')
w('|---|---|' + '---|' * len(GROUPS) + '---|---|')
for k in ORDER:
    keys = [key for key, r in P1.items() if r['personality_initial'] == k and key in P0]
    a = [P0[key] for key in keys]; b = [P1[key] for key in keys]
    cells = [f'{f1(share(a, ts))} → {f1(share(b, ts))}' for _, ts in GROUPS]
    lv = f'{med([int(r["level"]) for r in a]):.0f} → {med([int(r["level"]) for r in b]):.0f}'
    hr = f'{f1(med([float(r["played_h_week"]) for r in a]))} → {f1(med([float(r["played_h_week"]) for r in b]))}'
    w(f'| {NAMES[k]} | {len(keys)} | ' + ' | '.join(cells) + f' | {lv} | {hr} |')
w('')

w('## Regras e metas da validação\n')
w('| Medida | V3b (antes) | P0 | P1 |'); w('|---|---|---|---|')
for k in [k for k in R['P1'] if k != '_s']:
    def cell(v):
        if k not in R[v]: return '—'
        s, val = R[v][k]; return f'{val} {"✅" if "✅" in s else ("⚠️" if "⚠" in s else "❌")}'
    w(f'| {k} | {cell("V3b")} | {cell("P0")} | {cell("P1")} |')
w('')
for v in ('V3b', 'P0', 'P1'): w(f'- {v}: ' + '; '.join(x for x in R[v].get('_s', []) if x.startswith(('Regras', 'Provis', 'Dependem'))))
w('')

w('## Horas por perfil (mediana por pessoa)\n')
w('| Perfil | V3b | P0 | P1 |'); w('|---|---|---|---|')
for p in ('casual', 'regular', 'hardcore', 'devoto'):
    w(f'| {p.capitalize()} | ' + ' | '.join(f1(med([float(r["played_h_week"]) for r in D.values() if r["profile"] == p])) for D in (OLD, P0, P1)) + ' |')
w('')
open('reports/p4a/analise.md', 'w', encoding='utf-8', newline='').write('\n'.join(md))
print('ok')
