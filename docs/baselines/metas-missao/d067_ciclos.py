"""(1) Diarias com etapa feita que expiraram: havia sessao para entregar antes do reset?
(2) Os pares com 3+ abandonos: o que mudou entre um ciclo e o seguinte.
Uso: python ciclos.py <pasta com events.csv e ends.csv> <pasta do conteudo>"""
import csv, io, sys, json
from collections import Counter, defaultdict
D, CONTENT = sys.argv[1], sys.argv[2]
TPM = 600; DAY = 1440 * TPM
qj = json.load(io.open(CONTENT + '/quests.json', encoding='utf-8'))
CAT = {q['id']: q['category'] for q in qj['quests']}
BOARDS = {b['regionId'] + '/' + b['activityId']: b['id'] for b in qj['boards']}
QB = {q['id']: q['boardId'] for q in qj['quests']}
rj = json.load(io.open(CONTENT + '/regions.json', encoding='utf-8'))
ATYPE = {}
def walk(r):
    for a in r.get('activities', []): ATYPE[r['id'] + '/' + a['id']] = a['type']
    for ar in r.get('areas', []):
        for a in ar.get('activities', []): ATYPE[r['id'] + '/' + a['id']] = a['type']
for r in rj['regions']: walk(r)

ends = list(csv.DictReader(io.open(D + '/ends.csv', encoding='utf-8')))
P2C = {(r['seed'], int(r['person'])): int(r['char']) for r in ends}
ab = [r for r in ends if r['completed'] == '0' and r['reason'] != 'blocked']
g = defaultdict(list)
for r in ab: g[(r['seed'], int(r['char']), r['type'], r['target'])].append(r)
rep = {k: sorted(v, key=lambda r: int(r['end_tick'])) for k, v in g.items() if len(v) >= 3}
want = defaultdict(list)          # (seed, char) -> pares
for k in rep: want[(k[0], k[1])].append(k)

ready = []                        # diarias com etapa feita: (tick da etapa, quest)
per = defaultdict(list)           # eventos dos personagens dos pares
cur_ready = defaultdict(dict)
sess = defaultdict(list)          # (seed,char) -> [(inicio, fim)]
open_s = {}
for e in csv.DictReader(io.open(D + '/events.csv', encoding='utf-8')):
    cid = int(e['char'])
    if cid < 0: cid = P2C.get((e['seed'], -cid), 0)
    if cid == 0: continue
    key = (e['seed'], cid); t = e['type']; tick = int(e['tick']); det = e['detail']
    if t == 'session_started': open_s[key] = tick
    elif t == 'session_ended':
        sess[key].append((open_s.pop(key, tick), tick))
    if key in want: per[key].append((tick, t, det, e['causes']))
    if t == 'quest_step_completed':
        q = det.split('#')[0]
        if CAT.get(q) == 'daily': cur_ready[key][q] = dict(q=q, step=tick, at_board=None)
    elif t == 'quest_completed' and det in cur_ready[key]: cur_ready[key].pop(det)
    elif t == 'quest_expired' and det in cur_ready[key]:
        d = cur_ready[key].pop(det); d['key'] = key; d['expired'] = tick; ready.append(d)

print('## (1) Diarias com a etapa feita que expiraram sem entrega:', len(ready))
cls = Counter(); left = Counter()
for d in ready:
    ss = sess[d['key']]
    cur = next((s for s in ss if s[0] <= d['step'] <= s[1]), None)
    rest = (cur[1] - d['step']) / TPM if cur else None
    later = any(d['step'] < s[0] < d['expired'] for s in ss)
    reset = d['expired']
    if rest is None: c = 'sem sessao registrada'
    elif rest <= 15: c = 'sessao acabou em ate 15 min'
    elif rest <= 40: c = 'sessao acabou em 15 a 40 min'
    else: c = 'sobrou mais de 40 min de sessao'
    cls[(c, 'teve outra sessao antes do reset' if later else 'sem outra sessao antes do reset')] += 1
for k, v in sorted(cls.items(), key=lambda x: -x[1]): print(f'  {k}: {v:,}')

print('\n## (2) Pares com 3+ abandonos:', len(rep), dict(Counter(k[2] for k in rep)))
summary = Counter(); examples = defaultdict(list)
for k, v in rep.items():
    evs = per[(k[0], k[1])]
    for a, b in zip(v, v[1:]):
        lo, hi = int(a['end_tick']), int(b['set_tick'])
        mid = [x for x in evs if lo < x[0] <= hi]
        lv = sum(1 for x in mid if x[1] == 'level_up')
        obj = sum(1 for x in mid if x[1] == 'objective_set')
        prac = sum(1 for x in mid if x[1] == 'activity_chosen' and ATYPE.get(x[2]) == k[3]) if k[2] == 'practice_activity' else 0
        days = (hi - lo) / DAY
        if k[2] == 'practice_activity': why = 'voltou a praticar' if prac else ('subiu de nivel' if lv else 'nada registrado')
        elif k[2] == 'reach_level': why = 'subiu de nivel' if lv else 'nada registrado'
        elif k[2] == 'discover_zone': why = 'subiu de nivel' if lv else ('objetivo adotado' if obj else 'nada registrado')
        else: why = 'missao avancou' if any(x[1] in ('quest_step_completed', 'quest_accepted') and x[2].split('#')[0] == k[3] for x in mid) else 'nada registrado'
        summary[(k[2], why)] += 1
        examples[(k[2], why)].append((k, a, b, days, lv, prac))
for kk, v in sorted(summary.items()): print(f'  {kk}: {v}')
# duracao das metas abandonadas e motivo
for ty in ('practice_activity', 'reach_level', 'discover_zone'):
    rows = [r for k, v in rep.items() if k[2] == ty for r in v]
    durs = sorted((int(r['end_tick']) - int(r['set_tick'])) / DAY for r in rows)
    print(f"  {ty}: abandonos {len(rows)}; motivos {dict(Counter(r['reason'] for r in rows))}; dias ate largar (mediana) {durs[len(durs)//2]:.1f}; alvos {dict(Counter(k[3] for k in rep if k[2]==ty).most_common(6))}")
print('\nExemplos:')
for kk in sorted(examples):
    for k, a, b, days, lv, prac in examples[kk][:2]:
        print(f'  {kk} {k[3]}: largou dia {int(a["end_tick"])/DAY:.1f} (nivel {a["level"]}, {a["reason"]}) -> readotou dia {int(b["set_tick"])/DAY:.1f} ({days:.1f} d depois; subiu {lv} niveis; escolheu a atividade {prac}x) -> largou dia {int(b["end_tick"])/DAY:.1f} (nivel {b["level"]}, {b["reason"]})')

print('\n## praticar atividade: escolhas da atividade entre largar e readotar, e dias de intervalo')
pc = Counter(); gaps = []
for (ty, why), lst in examples.items():
    if ty != 'practice_activity': continue
    for k, a, b, days, lv, prac in lst:
        pc['1 escolha' if prac == 1 else '2 a 3' if prac <= 3 else '4 ou mais' if prac else '0'] += 1
        gaps.append(days)
gaps.sort()
print(' escolhas:', dict(pc), '; intervalo (dias) mediana', round(gaps[len(gaps)//2], 1), 'p10', round(gaps[len(gaps)//10], 1))
print('\n## alcancar nivel readotada sem subir de nivel: intervalo (dias)')
g2 = sorted(days for (ty, why), lst in examples.items() if ty == 'reach_level' and why == 'nada registrado' for k, a, b, days, lv, prac in lst)
print(' n', len(g2), 'mediana', round(g2[len(g2)//2], 2), 'p75', round(g2[len(g2)*3//4], 2))
