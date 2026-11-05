"""Analise da progressao inicial e das metas de desbloqueio (D-055): V0 (tudo desligado), V1 (so
progressao inicial), V2 (so desbloqueio e descoberta), V3 (tudo ligado). Le reports/pe/val/<V>/."""
import os, csv, statistics as st
import numpy as np
os.chdir(r'C:\Users\Pichau\OneDrive\Área de Trabalho\IdeiaJogo')
VERS = ['V0', 'V1', 'V2', 'V3']
DESC = {'V0': 'tudo desligado (deve reproduzir o D-054)', 'V1': 'só progressão inicial', 'V2': 'só desbloqueio e descoberta', 'V3': 'tudo ligado (preset)'}
PROF = ['casual', 'regular', 'hardcore', 'devoto']
PREFS = ['pve', 'pvp', 'crafting', 'trade', 'gathering', 'exploration', 'social']
PNAME = {'pve': 'PvE', 'pvp': 'PvP', 'crafting': 'Crafting', 'trade': 'Comércio', 'gathering': 'Coleta', 'exploration': 'Exploração', 'social': 'Social'}
TYPES = ['hunt', 'boss', 'quest', 'explore', 'gather', 'trade', 'socialize', 'train', 'repair', 'patrol', 'ambush']
GROUPS = [('Caça e chefe', ['hunt', 'boss']), ('Missão', ['quest']), ('Exploração', ['explore']), ('Coleta', ['gather']),
          ('Cidade', ['trade', 'socialize', 'train', 'repair']), ('PvP', ['patrol', 'ambush'])]

def load(v): return [r for r in csv.DictReader(open(f'reports/pe/val/{v}/people.csv', encoding='utf-8')) if r['registered'] == '1']
D = {v: load(v) for v in VERS}
def f1(x): return '—' if x is None or x != x else f'{x:.1f}'.replace('.', ',')
def pct(a, q): return float(np.percentile(np.asarray(a, float), q * 100)) if a else float('nan')
def top(r): return max(PREFS, key=lambda k: (int(r['pref_' + k]), -PREFS.index(k)))
def mins(r, ts, pre=False):
    if pre: return sum(int(r['pre_min_' + t] or 0) for t in ts)
    return sum(int(r['min_' + t]) for t in ts)
def rules(v):
    out = {}
    for line in open(f'reports/pe/val/{v}/validation.md', encoding='utf-8'):
        if line.startswith('### '): out.setdefault('_secoes', []).append(line.strip('# \n'))
        if line.startswith('| ') and ('✅' in line or '❌' in line or '⚠' in line):
            c = [x.strip() for x in line.split('|')]
            out[c[2].split(' (')[0]] = (c[1], c[4])
    return out
R = {v: rules(v) for v in VERS}
target = int(D['V3'][0]['target_level'])

md = []; w = md.append
w('# Progressão inicial, metas de desbloqueio e descoberta (D-055): medição\n')
w(f'Validação padrão: seeds 1 a 5 × 1.000 pessoas × 90 dias por versão (mesmas pessoas). Nível-alvo da progressão inicial: {target}.\n')
for v in VERS: w(f'- **{v}:** {DESC[v]}.')
w('')

# Regras
w('## Regras e metas da validação\n')
w('| Medida | ' + ' | '.join(VERS) + ' |'); w('|---|' + '---|' * len(VERS))
for line in [s for s in R['V0'].get('_secoes', []) if s.startswith(('Regras', 'Provis', 'Dependem'))]:
    pass
keys = [k for k in R['V0'] if k != '_secoes']
for k in keys:
    w(f'| {k} | ' + ' | '.join(f'{R[v][k][1]} {"✅" if "✅" in R[v][k][0] else ("⚠️" if "⚠" in R[v][k][0] else "❌")}' if k in R[v] else '—' for v in VERS) + ' |')
w('')
for v in VERS: w(f'- {v}: ' + '; '.join(s for s in R[v].get('_secoes', []) if s.startswith(('Regras', 'Provis', 'Dependem'))))
w('')

# Tempo ate o alvo
w(f'## Tempo de jogo até o nível {target} (horas conectadas; mediana · p90; parcela que chegou)\n')
def reach(rows):
    h = [float(r['h_to_target']) for r in rows if r['h_to_target']]
    return f'{f1(pct(h,.5))} · {f1(pct(h,.9))} ({len(h)*100/max(1,len(rows)):.0f}%)'
w('| Grupo | ' + ' | '.join(VERS) + ' |'); w('|---|' + '---|' * len(VERS))
for p in PROF: w(f'| Perfil {p.capitalize()} | ' + ' | '.join(reach([r for r in D[v] if r['profile'] == p]) for v in VERS) + ' |')
for k in PREFS: w(f'| Prefere {PNAME[k]} | ' + ' | '.join(reach([r for r in D[v] if top(r) == k]) for v in VERS) + ' |')
w(f'| **Todos** | ' + ' | '.join(reach(D[v]) for v in VERS) + ' |')
w('')

# Atividades antes e depois
w(f'## Atividades antes e depois do nível {target} (parcela do tempo em atividades; quem chegou ao alvo)\n')
w('A foto do "antes" é do fim do dia em que a pessoa chegou ao nível-alvo (inclui o resto daquele dia).\n')
w('| Grupo de atividade | ' + ' | '.join(f'{v} antes · depois' for v in VERS) + ' |'); w('|---|' + '---|' * len(VERS))
for name, ts in GROUPS:
    cells = []
    for v in VERS:
        rr = [r for r in D[v] if r['h_to_target']]
        pre_tot = sum(mins(r, TYPES, True) for r in rr); post_tot = sum(mins(r, TYPES) - mins(r, TYPES, True) for r in rr)
        pre = sum(mins(r, ts, True) for r in rr) * 100 / max(1, pre_tot); post = sum(mins(r, ts) - mins(r, ts, True) for r in rr) * 100 / max(1, post_tot)
        cells.append(f'{f1(pre)}% · {f1(post)}%')
    w(f'| {name} | ' + ' | '.join(cells) + ' |')
w('')
w('**Por preferência predominante** (caça e chefe antes · depois do alvo; V0 → V3):\n')
w('| Prefere | V0 | V1 | V3 |'); w('|---|---|---|---|')
for k in PREFS:
    cells = []
    for v in ('V0', 'V1', 'V3'):
        rr = [r for r in D[v] if r['h_to_target'] and top(r) == k]
        pre_tot = sum(mins(r, TYPES, True) for r in rr); post_tot = sum(mins(r, TYPES) - mins(r, TYPES, True) for r in rr)
        pre = sum(mins(r, ['hunt', 'boss'], True) for r in rr) * 100 / max(1, pre_tot)
        post = sum(mins(r, ['hunt', 'boss']) - mins(r, ['hunt', 'boss'], True) for r in rr) * 100 / max(1, post_tot)
        cells.append(f'{f1(pre)}% · {f1(post)}%')
    w(f'| {PNAME[k]} | ' + ' | '.join(cells) + ' |')
w('')

# Travados
w('## Personagens sem caminho de progressão?\n')
w(f'Com conta, 20 h ou mais em atividades e ainda abaixo do nível {target} no fim (90 dias); e abaixo do nível 5.\n')
w('| Versão | Abaixo do alvo com 20 h+ | Abaixo do nível 5 com 20 h+ | Preferência predominante dos que ficaram abaixo do alvo |'); w('|---|---|---|---|')
for v in VERS:
    act = [r for r in D[v] if mins(r, TYPES) >= 1200]
    below = [r for r in act if int(r['level']) < target]; low = [r for r in act if int(r['level']) < 5]
    cnt = {}
    for r in below: cnt[top(r)] = cnt.get(top(r), 0) + 1
    w(f'| {v} | {len(below)} de {len(act)} ({len(below)*100/max(1,len(act)):.1f}%) | {len(low)} | ' + ', '.join(f'{PNAME[k]} {n}' for k, n in sorted(cnt.items(), key=lambda x: -x[1])) + ' |')
w('')

# Metas
w('## Metas de desbloqueio e descoberta\n')
w('| Versão | Desbloqueio: adotadas · cumpridas | Descoberta: adotadas · cumpridas | Descobertas motivadas por outra preferência (não Exploração) |'); w('|---|---|---|---|')
for v in VERS:
    s = lambda k: sum(int(r[k]) for r in D[v])
    w(f'| {v} | {s("unlock_set")} · {s("unlock_done")} | {s("discover_set")} · {s("discover_done")} | {s("discover_set_non_exploration")} |')
w('')

# Horas por perfil
w('## Horas reais por semana (mediana por pessoa, campanha)\n')
w('| Perfil | ' + ' | '.join(VERS) + ' |'); w('|---|' + '---|' * len(VERS))
for p in PROF:
    w(f'| {p.capitalize()} | ' + ' | '.join(f1(pct([float(r["played_h_week"]) for r in D[v] if r["profile"] == p], .5)) for v in VERS) + ' |')
w('')

# Nivel final por preferencia
w('## Nível no fim por preferência predominante (mediana)\n')
w('| Prefere | ' + ' | '.join(VERS) + ' |'); w('|---|' + '---|' * len(VERS))
for k in PREFS:
    w(f'| {PNAME[k]} | ' + ' | '.join(f1(pct([int(r["level"]) for r in D[v] if top(r) == k], .5)) for v in VERS) + ' |')
w('')

open('reports/pe/analise.md', 'w', encoding='utf-8', newline='').write('\n'.join(md))
print('ok')
