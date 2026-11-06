"""Comparacao pareada do D-067 (mesmas pessoas; so as chaves mudam).
Uso: python comparar_d067.py <pasta metas> <pasta reports> <pasta do conteudo>"""
import csv, io, sys, json, re
from collections import Counter, defaultdict
M, REP, CONTENT = sys.argv[1], sys.argv[2], sys.argv[3]
VARS = [('base', f'{M}/d066_v123', f'{REP}/d066/v123'), ('v1', f'{M}/d067_v1', f'{REP}/d067/v1'),
        ('v12', f'{M}/d067_v12', f'{REP}/d067/v12'), ('v123', f'{M}/d067_v123', f'{REP}/d067/v123')]
if len(sys.argv) > 4 and sys.argv[4] == 'd067a':   # D-067a: D-067 final x descoberta pelo requisito
    VARS = [('D-067', f'{M}/d067_v123', f'{REP}/d067/v123'), ('D-067a: descoberta', f'{M}/d067_v1234z', f'{REP}/d067/v1234z'),
            ('D-067a: + compromisso estavel (preset)', f'{M}/d067_v1234', f'{REP}/d067/v1234')]
TPM = 600
qj = json.load(io.open(CONTENT + '/quests.json', encoding='utf-8'))
CAT = {q['id']: q['category'] for q in qj['quests']}
STEP = {q['id']: (q['steps'][0]['regionId'] + '/' + q['steps'][0]['activityId']) if q['steps'] else '' for q in qj['quests']}
BOARDS = {b['regionId'] + '/' + b['activityId'] for b in qj['boards']}
rj = json.load(io.open(CONTENT + '/regions.json', encoding='utf-8'))
ATYPE = {}
for r in rj['regions']:
    for a in r.get('activities', []): ATYPE[r['id'] + '/' + a['id']] = a['type']
    for ar in r.get('areas', []):
        for a in ar.get('activities', []): ATYPE[r['id'] + '/' + a['id']] = a['type']
THEME = {q: ({'hunt': 'hunt', 'gather': 'gather'}.get(ATYPE.get(k), 'outro')) for q, k in STEP.items()}
MOTIVES = ('goal', 'reward', 'campaign', 'unlocks', 'practice_goal', 'taste')

def diag(d):
    ends = list(csv.DictReader(io.open(d + '/ends.csv', encoding='utf-8')))
    P2C = {(r['seed'], int(r['person'])): int(r['char']) for r in ends}
    s = {}
    st = defaultdict(lambda: {'visit': None, 'dailies': {}, 'expired': {}, 'last': None, 'sess': None})
    V = Counter(); T = Counter(); only_daily = [0, 0.0]; useless_chain = 0
    sec_theme = Counter(); reconsidered = Counter(); empty_ev = Counter()
    D = Counter(); reacc = Counter(); motive = Counter(); same_zone = 0; swap = 0; done = Counter(); dropped = Counter()
    level_up_ticks = defaultdict(list)
    def close(c, tick, by):
        nonlocal useless_chain
        v = c['visit']; c['visit'] = None
        if v is None: return
        if v['res'] is not None: mins = (v['res'] - v['tick']) / TPM - 5
        elif by == 'act': mins = (tick - v['tick']) / TPM - 5
        else: mins = None
        if by == 'sess' and v['res'] is None: out = 'interrompida'
        elif not v['acc'] and not v['del']: out = 'sem resultado'
        elif v['del'] and v['acc']: out = 'entregou e aceitou'
        elif v['del']: out = 'so entregou'
        elif v['drop']: out = 'troca'
        else: out = 'so aceitou'
        V[(v['purpose'], out)] += 1
        if mins is not None:
            T[v['purpose']] += max(0, mins)
            if out == 'troca' or (v['drop'] and v['purpose'] == 'aceitar'): T['liberar vaga'] += max(0, mins)
        if out == 'sem resultado' and c['last'] == ('sem resultado', v['board'], v['sess']): useless_chain += 1
        c['last'] = (out, v['board'], v['sess'])
        if v['acc'] and not v['del'] and all(CAT.get(q) == 'daily' for q in v['acc']):
            only_daily[0] += 1; only_daily[1] += max(0, mins or 0)
    for e in csv.DictReader(io.open(d + '/events.csv', encoding='utf-8')):
        cid = int(e['char'])
        if cid < 0: cid = P2C.get((e['seed'], -cid), 0)
        if cid == 0: continue
        key = (e['seed'], cid); c = st[key]; t = e['type']; tick = int(e['tick']); det = e['detail']
        if t == 'session_started': c['sess'] = tick
        elif t == 'session_ended': close(c, tick, 'sess')
        elif t == 'activity_chosen':
            close(c, tick, 'act')
            for q, x in c['dailies'].items():
                if det == STEP[q]: x['worked'] = True
            if det in BOARDS:
                cs = e['causes']
                purpose = 'entregar' if 'quest_deliver=' in cs else 'aceitar' if 'quest_offer=' in cs else 'outro'
                c['visit'] = {'tick': tick, 'board': det, 'purpose': purpose, 'res': None, 'acc': [], 'del': [], 'drop': [], 'sess': c['sess']}
        elif t == 'quest_completed':
            done[CAT.get(det)] += 1
            if CAT.get(det) == 'secondary': sec_theme[THEME.get(det, 'outro')] += 1
            if c['visit']: c['visit']['del'].append(det); c['visit']['res'] = c['visit']['res'] or tick
            if det in c['dailies']: c['dailies'].pop(det); D['entregue'] += 1
        elif t == 'quest_accepted':
            if c['visit']: c['visit']['acc'].append(det); c['visit']['res'] = c['visit']['res'] or tick
            fs = [x.split('=')[0] for x in e['causes'].split('|')]
            m = next((x for x in fs if x in MOTIVES), 'sem motivo registrado')
            motive[(CAT.get(det), m)] += 1
            same_zone += 'on_the_way' in fs; swap += 'priority_swap' in fs
            if CAT.get(det) == 'daily':
                D['aceita'] += 1
                prev = c['expired'].pop(det, None)
                if prev is not None: reacc['reaceita apos expirar ' + ('com progresso' if prev else 'intocada')] += 1
                c['dailies'][det] = {'worked': False, 'step': False}
        elif t == 'quest_dropped':
            dropped[CAT.get(det)] += 1
            if c['visit']: c['visit']['drop'].append(det); c['visit']['res'] = c['visit']['res'] or tick
            if det in c['dailies']: c['dailies'].pop(det); D['largada'] += 1
        elif t == 'quest_step_completed':
            q = det.split('#')[0]
            if q in c['dailies']: c['dailies'][q]['step'] = True; c['dailies'][q]['worked'] = True
        elif t == 'quest_expired':
            if det in c['dailies']:
                x = c['dailies'].pop(det)
                k = 'etapa feita, nao entregue' if x['step'] else 'trabalhou sem concluir' if x['worked'] else 'nenhuma etapa executada'
                D['expirou: ' + k] += 1
                c['expired'][det] = x['worked'] or x['step']
        elif t == 'level_up': level_up_ticks[key].append(tick)
        elif t == 'goal_reconsidered': reconsidered[e['causes'].split('=')[0]] += 1
        elif t == 'board_visit_empty':
            empty_ev['visitas com motivo registrado'] += 1
            for x in e['causes'].split('|'): empty_ev[x.split('=')[0]] += 1
    tot_v = sum(V.values())
    s['visitas ao quadro'] = tot_v
    for k in [('aceitar', 'so aceitou'), ('aceitar', 'troca'), ('aceitar', 'sem resultado'), ('aceitar', 'interrompida'),
              ('entregar', 'entregou e aceitou'), ('entregar', 'so entregou'), ('entregar', 'interrompida')]:
        s[f'  {k[0]}: {k[1]}'] = V[k]
    s['  visitas sem resultado logo apos outra sem resultado'] = useless_chain
    s['  sem resultado com motivo registrado (board_visit_empty)'] = empty_ev['visitas com motivo registrado']
    for k in ('no_slot', 'no_longer_offered', 'already_taken', 'no_time_before_reset', 'less_interest', 'expired_untouched'):
        if empty_ev[k]: s[f'    motivo: {k}'] = empty_ev[k]
    s['viagem ao quadro (h)'] = round((T['aceitar'] + T['entregar'] + T['outro']) / 60)
    s['  para aceitar (h)'] = round(T['aceitar'] / 60)
    s['  para entregar (h)'] = round(T['entregar'] / 60)
    s['  das para aceitar: com troca (liberar vaga) (h)'] = round(T['liberar vaga'] / 60)
    s['visitas que so aceitaram diarias'] = only_daily[0]
    s['  viagem delas (h)'] = round(only_daily[1] / 60)
    s['diarias aceitas'] = D['aceita']
    s['  entregues'] = D['entregue']
    s['  expiradas sem etapa executada'] = D['expirou: nenhuma etapa executada']
    s['  expiradas com a etapa feita (nao entregues)'] = D['expirou: etapa feita, nao entregue']
    s['  expiradas com trabalho parcial'] = D['expirou: trabalhou sem concluir']
    s['  largadas (troca)'] = D['largada']
    s['reaceite da mesma diaria apos expirar intocada'] = reacc['reaceita apos expirar intocada']
    s['reaceite apos expirar com progresso'] = reacc['reaceita apos expirar com progresso']
    for cat in ('main', 'secondary', 'daily'): s[f'missoes {cat} concluidas'] = done[cat]
    for th in ('hunt', 'gather', 'outro'): s[f'  secundarias concluidas, etapa de {th}'] = sec_theme[th]
    s['missoes largadas por troca'] = sum(dropped.values())
    tot_acc = sum(motive.values())
    for m in MOTIVES + ('sem motivo registrado',):
        n = sum(v for (cat, mm), v in motive.items() if mm == m)
        if n: s[f'aceites por motivo: {m}'] = f'{n} ({100*n/tot_acc:.0f}%)'
    s['aceites aproveitando a viagem de outra missao'] = same_zone
    # metas
    Q = [r for r in ends if r['type'] == 'complete_quest']
    s['metas de missao: % concluidas'] = round(100 * sum(r['completed'] == '1' for r in Q) / max(1, len(Q)), 1)
    ab = [r for r in ends if r['completed'] == '0' and r['reason'] != 'blocked']
    pairs = Counter((r['seed'], r['char'], r['type'], r['target']) for r in ab)
    s['mesmo alvo abandonado 3+ vezes (pares)'] = sum(1 for v in pairs.values() if v >= 3)
    s['  por tipo'] = ' '.join(f'{k}:{v}' for k, v in sorted(Counter(k[2] for k, v in pairs.items() if v >= 3).items()))
    # alcancar nivel readotada sem subir de nivel entre o abandono e a readocao
    rl = defaultdict(list)
    for r in ends:
        if r['type'] == 'reach_level': rl[(r['seed'], int(r['char']))].append(r)
    same = 0
    for key, lst in rl.items():
        lst.sort(key=lambda r: int(r['set_tick']))
        for a, b in zip(lst, lst[1:]):
            if a['completed'] == '0' and not any(int(a['end_tick']) < t <= int(b['set_tick']) for t in level_up_ticks[key]): same += 1
    s['meta de nivel readotada sem subir de nivel'] = same
    Z = [r for r in ends if r['type'] == 'discover_zone']
    s['metas de descobrir zona encerradas'] = len(Z)
    s['  abandonadas'] = sum(r['completed'] == '0' for r in Z)
    s['  concluidas'] = sum(r['completed'] == '1' for r in Z)
    for k in ('level_requirement', 'route', 'knowledge', 'quest_on_the_way'): s[f'descoberta reconsiderada: {k}'] = reconsidered[k]
    return s

def people(r):
    rows = list(csv.reader(io.open(r + '/people.csv', encoding='utf-8')))
    R = [dict(zip(rows[0], x)) for x in rows[1:]]
    R = [x for x in R if x['registered'] == '1']
    m = lambda k: sum(float(x[k]) for x in R) / len(R)
    pl = sum(float(x['min_played']) for x in R)
    s = {'nivel em 90 dias': round(m('level'), 1), 'satisfacao': round(m('satisfaction')), 'frustracao': round(m('frustration'), 1),
         'engajamento': round(m('engagement')), 'h jogadas/semana': round(m('played_h_week'), 2), 'metas abandonadas/pessoa': round(m('goals_abandoned'), 2),
         '% desistiu do jogo': round(100 * sum(x['state'] == 'Quit' for x in R) / len(R), 1)}
    for a in ('quest', 'hunt', 'gather', 'explore', 'socialize', 'trade', 'idle'): s[f'% tempo {a}'] = round(100 * sum(float(x['min_' + a]) for x in R) / pl, 1)
    md = io.open(r + '/validation.md', encoding='utf-8').read()
    w = re.search(r'Tempo andando ou esperando \| [^|]+\| ([\d,]+)', md)
    s['% andando ou esperando'] = w.group(1) if w else '?'
    q = re.search(r'Quests concluídas por personagem \(média\) \| [^|]+\| ([\d,]+)', md)
    s['missoes concluidas por personagem'] = q.group(1) if q else '?'
    x = re.search(r'XP vindo de quests \| [^|]+\| ([\d,]+)', md)
    s['% XP vindo de missoes'] = x.group(1) if x else '?'
    s['validacao'] = ' · '.join(re.findall(r'### (Regras do cenário: \d+ de \d+|Provisórias: \d+ de \d+)', md))
    return s

res = {}
for name, d, r in VARS:
    res[name] = {**diag(d), **people(r)}
    print(name, 'ok', file=sys.stderr)
keys = []
for v in res.values():
    for k in v:
        if k not in keys: keys.append(k)
print('| Medida | ' + ' | '.join(n for n, _, _ in VARS) + ' |')
print('|---' * (len(VARS) + 1) + '|')
for k in keys:
    print(f'| {k} | ' + ' | '.join(str(res[n].get(k, '—')) for n, _, _ in VARS) + ' |')
