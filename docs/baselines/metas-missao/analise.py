import csv, io, sys
from collections import Counter, defaultdict
d = sys.argv[1]
ends = list(csv.DictReader(io.open(d + '/ends.csv', encoding='utf-8')))
evs = list(csv.DictReader(io.open(d + '/events.csv', encoding='utf-8')))
TPH = 36000
pct = lambda a, b: f'{100*a/b:.1f}%' if b else '-'
out = []
P = out.append

# ---------- metas por tipo ----------
sets = Counter(); set_people = defaultdict(set)
for e in evs:
    if e['type'] == 'goal_set':
        t = e['detail'].split('|')[0]; sets[t] += 1; set_people[t].add((e['seed'], e['char']))
P('## 1. Metas por tipo (5 seeds, ocorrências e pessoas)\n')
P('| Tipo | Adotadas | Pessoas | Concluídas | Abandonadas (sem progresso) | Abandonadas (impaciência) | Pessoas com abandono |')
P('|---|---|---|---|---|---|---|')
for t in sorted(sets):
    E = [r for r in ends if r['type'] == t]
    c = sum(r['completed'] == '1' for r in E); a1 = sum(r['reason'] == 'no_progress' for r in E); a2 = sum(r['reason'] == 'impatience' for r in E)
    pa = len({(r['seed'], r['char']) for r in E if r['completed'] == '0'})
    P(f'| {t} | {sets[t]} | {len(set_people[t])} | {c} ({pct(c, len(E))}) | {a1} ({pct(a1, len(E))}) | {a2} ({pct(a2, len(E))}) | {pa} |')

# ---------- metas de missao ----------
Q = [r for r in ends if r['type'] == 'complete_quest']
A = [r for r in Q if r['completed'] == '0']
P(f'\n## 2. Metas de missão encerradas: {len(Q)} ({sum(r["completed"]=="1" for r in Q)} concluídas, {len(A)} abandonadas)\n')
for cat in ['main', 'secondary']:
    qc = [r for r in Q if r['cat'] == cat]; ac = [r for r in qc if r['completed'] == '0']
    P(f'- {cat}: {len(qc)} encerradas, {len(ac)} abandonadas ({pct(len(ac), len(qc))}); pessoas {len({(r["seed"], r["char"]) for r in qc})}, com abandono {len({(r["seed"], r["char"]) for r in ac})}')

def klass(r):
    if r['active'] == '0':
        return 'nao_aceita_vagas_cheias' if int(r['active_quests']) >= int(r['max_active']) else 'nao_aceita_com_vaga'
    if int(r['step_index']) >= int(r['steps']): return 'pronta_para_entregar'
    if int(r['step_minlevel']) > int(r['level']): return 'etapa_acima_do_nivel'
    if int(r['step_group']) > 1: return 'etapa_exige_grupo'
    if r['step_zone_known'] == '0' and r['step_hint'] == '0': return 'etapa_em_zona_desconhecida'
    if int(r['step_min']) > 0: return 'progresso_parcial_na_etapa'
    return 'etapa_disponivel_sem_avanco'
K = Counter(klass(r) for r in A)
P('\n## 3. Estado da missão no instante do abandono da meta\n')
P('| Estado | Abandonos | % | sem progresso | impaciência |')
P('|---|---|---|---|---|')
for k, v in K.most_common():
    P(f'| {k} | {v} | {pct(v, len(A))} | {sum(1 for r in A if klass(r)==k and r["reason"]=="no_progress")} | {sum(1 for r in A if klass(r)==k and r["reason"]=="impatience")} |')

# minutos parciais
part = [r for r in A if klass(r) == 'progresso_parcial_na_etapa']
if part:
    fr = sorted(int(r['step_min']) / max(1, int(r['step_need'])) for r in part)
    P(f'\nProgresso parcial: mediana {100*fr[len(fr)//2]:.0f}% da etapa feita no abandono; {sum(1 for x in fr if x>=0.5)} com metade ou mais.')
# tempo sem progresso x limite
rat = sorted(int(r['since_progress']) / max(1, int(r['stale'])) for r in A if r['reason'] == 'no_progress')
if rat: P(f'Sem progresso: tempo jogado desde o último avanço ÷ limite: mediana {rat[len(rat)//2]:.2f}.')

# ---------- continuou depois? ----------
byq = defaultdict(list)
for e in evs:
    if e['type'] in ('quest_step_completed', 'quest_completed', 'quest_accepted', 'goal_set'):
        q = e['detail'].split('#')[0] if e['type'] != 'goal_set' else (e['detail'].split('|')[1] if e['detail'].startswith('complete_quest') else None)
        if q: byq[(e['seed'], e['char'], q)].append((int(e['tick']), e['type']))
cont = Counter(); readopt = 0; finished_after = 0
for r in A:
    later = [x for x in byq[(r['seed'], r['char'], r['target'])] if x[0] > int(r['end_tick'])]
    if any(t == 'quest_step_completed' for _, t in later) or any(t == 'quest_completed' for _, t in later): cont[klass(r)] += 1
    if any(t == 'quest_completed' for _, t in later): finished_after += 1
    if any(t == 'goal_set' for _, t in later): readopt += 1
P(f'\n## 4. Depois do abandono\n')
P(f'- Continuaram avançando a mesma missão: {sum(cont.values())} de {len(A)} ({pct(sum(cont.values()), len(A))}); concluíram a missão depois: {finished_after} ({pct(finished_after, len(A))}).')
P(f'- Readotaram meta da mesma missão: {readopt} ({pct(readopt, len(A))}).')
P('- Por estado no abandono (continuaram / total): ' + ', '.join(f'{k} {cont[k]}/{K[k]}' for k in K))

# ---------- por nivel, paciencia, personalidade ----------
def band(x, edges):
    for i, e in enumerate(edges):
        if x < e: return f'<{e}'
    return f'>={edges[-1]}'
P('\n## 5. Taxa de abandono das metas de missão por grupo\n')
for name, key in [('nível', lambda r: band(int(r['level']), [5, 10, 15, 20, 25])), ('paciência', lambda r: band(int(r['patience']), [300, 500, 700])),
                  ('personalidade', lambda r: r['personality'] or '-'), ('perfil', lambda r: r['profile'])]:
    g = defaultdict(lambda: [0, 0])
    for r in Q:
        g[key(r)][0] += 1; g[key(r)][1] += r['completed'] == '0'
    P(f'- {name}: ' + ', '.join(f'{k} {pct(v[1], v[0])} de {v[0]}' for k, v in sorted(g.items())))

# ---------- objetivo da campanha ----------
camp_set = defaultdict(list); camp_ab = []
for e in evs:
    if e['type'] == 'objective_set' and e['detail'].startswith('campanha_principal'): camp_set[(e['seed'], e['char'])].append(int(e['tick']))
    if e['type'] == 'objective_abandoned' and e['detail'].startswith('campanha_principal'): camp_ab.append(e)
P(f'\n## 6. Objetivo "Concluir a campanha principal"\n')
P(f'- Pessoas que o tiveram (inicial ou renovado): {len(camp_set)}; abandonos: {len(camp_ab)}')
links_main = [r for r in Q if r['cat'] == 'main' and 'campanha_principal:advance' in r['links']]
la = [r for r in links_main if r['completed'] == '0']
P(f'- Metas de missão principal ligadas à campanha: {len(links_main)}, abandonadas {len(la)} ({pct(len(la), len(links_main))})')
P('- Estado da missão nos abandonos ligados: ' + ', '.join(f'{k} {v}' for k, v in Counter(klass(r) for r in la).most_common()))
print('\n'.join(out))
