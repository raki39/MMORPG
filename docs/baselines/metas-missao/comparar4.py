import csv, io, sys, json, re
from collections import Counter, defaultdict
DIAG, REP, CONTENT = sys.argv[1], sys.argv[2], sys.argv[3]
VARS = ['base', 'v1', 'v12', 'v123']
quests = {q['id']: q['category'] for q in json.load(io.open(CONTENT + '/quests.json', encoding='utf-8'))['quests']}

def klass(r):
    if r['active'] == '0':
        return 'nao_aceita_vagas_cheias' if int(r['active_quests']) >= int(r['max_active']) else 'nao_aceita_com_vaga'
    if int(r['step_index']) >= int(r['steps']): return 'pronta_para_entregar'
    if int(r['step_min']) > 0: return 'progresso_parcial'
    return 'etapa_disponivel_sem_avanco'

def diag(v):
    d = f'{DIAG}/d066_{v}'
    ends = list(csv.DictReader(io.open(d + '/ends.csv', encoding='utf-8')))
    s = {}
    Q = [r for r in ends if r['type'] == 'complete_quest']; A = [r for r in Q if r['completed'] == '0']
    s['metas de missão encerradas'] = len(Q)
    s['metas de missão: % concluídas'] = 100 * (len(Q) - len(A)) / len(Q)
    s['metas de missão: abandonadas sem progresso'] = sum(r['reason'] == 'no_progress' for r in A)
    s['metas de missão: abandonadas por impaciência'] = sum(r['reason'] == 'impatience' for r in A)
    s['metas de missão: encerradas por impedimento'] = sum(r['reason'] == 'blocked' for r in A)
    K = Counter(klass(r) for r in A if r['reason'] != 'blocked')
    for k in ['nao_aceita_vagas_cheias', 'etapa_disponivel_sem_avanco', 'progresso_parcial', 'pronta_para_entregar']:
        s['  abandono por estado: ' + k] = K[k]
    pairs = Counter((r['seed'], r['char'], r['type'], r['target']) for r in ends if r['completed'] == '0' and r['reason'] != 'blocked')
    s['ciclos: mesmo alvo abandonado 2+ vezes (pares)'] = sum(1 for v_ in pairs.values() if v_ >= 2)
    s['ciclos: mesmo alvo abandonado 3+ vezes (pares)'] = sum(1 for v_ in pairs.values() if v_ >= 3)
    s['ciclos: maior repetição'] = max(pairs.values()) if pairs else 0
    for ty in ['practice_activity', 'discover_zone', 'reach_level', 'unlock_level']:
        E = [r for r in ends if r['type'] == ty]
        s[f'{ty}: % abandonadas'] = 100 * sum(r['completed'] == '0' for r in E) / max(1, len(E))
    ev = Counter(); drops = Counter(); main_done = 0; sec_done = 0; daily_done = 0; obj = Counter()
    for e in csv.DictReader(io.open(d + '/events.csv', encoding='utf-8')):
        t = e['type']
        if t == 'quest_completed':
            c = quests.get(e['detail']); main_done += c == 'main'; sec_done += c == 'secondary'; daily_done += c == 'daily'
        elif t == 'quest_dropped':
            ev['dropped'] += 1; drops[quests.get(e['detail'])] += 1
        elif t == 'quest_expired': ev['expired'] += 1
        elif t.startswith('objective_') and t != 'objective_set':
            obj[(t, e['causes'].split('=')[0])] += 1
        elif t == 'objective_completed' and e['detail'].startswith('campanha'): pass
    s['missões principais concluídas'] = main_done
    s['missões secundárias concluídas'] = sec_done
    s['missões diárias concluídas'] = daily_done
    s['missões largadas (troca de prioridade)'] = ev['dropped']
    s['  largadas por categoria'] = ' '.join(f'{k}:{v_}' for k, v_ in sorted(drops.items(), key=lambda x: str(x[0])))
    s['diárias expiradas no reset'] = ev['expired']
    for (t, why), n in sorted(obj.items()):
        if t in ('objective_paused', 'objective_abandoned'): s[f'{t} ({why})'] = n
    camp = sum(n for (t, why), n in obj.items() if t == 'objective_completed')
    s['objetivos concluídos (todos)'] = camp
    return s

def people(v):
    rows = list(csv.reader(io.open(f'{REP}/{v}/people.csv', encoding='utf-8')))
    R = [dict(zip(rows[0], r)) for r in rows[1:] if dict(zip(rows[0], r))['registered'] == '1']
    m = lambda k: sum(float(r[k]) for r in R) / len(R)
    pl = sum(float(r['min_played']) for r in R)
    s = {'nível': m('level'), 'satisfação': m('satisfaction'), 'frustração': m('frustration'), 'engajamento': m('engagement'),
         'h jogadas/semana': m('played_h_week'), 'metas abandonadas/pessoa': m('goals_abandoned'),
         '% desistiu do jogo': 100 * sum(r['state'] == 'Quit' for r in R) / len(R)}
    for a in ['quest', 'hunt', 'socialize']: s[f'% tempo {a}'] = 100 * sum(float(r['min_' + a]) for r in R) / pl
    md = io.open(f'{REP}/{v}/validation.md', encoding='utf-8').read()
    s['validação'] = ' · '.join(re.findall(r'### (Regras do cenário: \d+ de \d+|Provisórias: \d+ de \d+)', md))
    return s

res = {v: {**diag(v), **people(v)} for v in VARS}
keys = list(res['base'].keys())
for v in VARS[1:]:
    for k in res[v]:
        if k not in keys: keys.append(k)
print('| Medida | Base (tudo desligado) | +1 diário | +1+2 metas | +1+2+3 objetivos |')
print('|---|---|---|---|---|')
for k in keys:
    vals = []
    for v in VARS:
        x = res[v].get(k, 0)
        vals.append(f'{x:.1f}' if isinstance(x, float) else str(x))
    print(f'| {k} | ' + ' | '.join(vals) + ' |')
