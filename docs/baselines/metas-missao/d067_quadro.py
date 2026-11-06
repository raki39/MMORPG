"""Visitas ao quadro e destino das diarias, a partir de events.csv do diagnostico (D-066).
Uso: python quadro.py <pasta com events.csv> <pasta do conteudo>
Uma visita vai do activity_chosen no quadro ate o proximo activity_chosen ou session_ended do
personagem. Os resultados (entrega, aceite, troca) saem no fim da acao no quadro (4 a 6 min).
"""
import csv, io, sys, json
from collections import Counter, defaultdict

D, CONTENT = sys.argv[1], sys.argv[2]
TPM = 600                       # ticks por minuto (100 ms)
DAY = 1440 * TPM
qj = json.load(io.open(CONTENT + '/quests.json', encoding='utf-8'))
CAT = {q['id']: q['category'] for q in qj['quests']}
STEP = {q['id']: [(s.get('regionId', ''), s.get('activityId', '')) for s in q['steps']] for q in qj['quests']}
BOARDS = {b['regionId'] + '/' + b['activityId'] for b in qj['boards']}

class Ch:
    __slots__ = ('visit', 'dailies', 'expired', 'session', 'sess_dailies', 'last_visit', 'active')
    def __init__(s):
        s.visit = None; s.dailies = {}; s.expired = {}; s.session = None; s.sess_dailies = []; s.last_visit = None; s.active = {}

chars = defaultdict(Ch)
V = Counter()                    # contagens de visitas
travel = Counter()               # minutos de viagem por finalidade
visits = []                      # (finalidade, resultado, viagem)
daily_inst = []                  # instancias de diaria aceitas
reaccept = Counter()
same_board_gap = Counter()

def close_visit(c, key, end_tick, by):
    v = c.visit
    c.visit = None
    if v is None: return
    res_tick = v['res_tick']
    if res_tick is not None: mins = (res_tick - v['tick']) / TPM - 5
    elif by == 'activity': mins = (end_tick - v['tick']) / TPM - 5
    else: mins = None
    acc = v['acc']; dl = v['del']; dr = v['drop']
    cats = Counter(CAT.get(q) for q in acc)
    if by == 'session' and res_tick is None:
        out = 'interrompida (sessao acabou antes)'
    elif not acc and not dl:
        out = 'sem resultado'
    elif dl and acc: out = 'entregou e aceitou'
    elif dl: out = 'so entregou'
    elif dr: out = 'troca (largou para aceitar)'
    else: out = 'so aceitou'
    only_daily = bool(acc) and not dl and all(CAT.get(q) == 'daily' for q in acc)
    v.update(out=out, mins=mins, only_daily=only_daily, cats=cats)
    visits.append(v)
    if c.last_visit is not None and c.last_visit['board'] == v['board'] and c.last_visit['session'] == v['session']:
        gap = (v['tick'] - c.last_visit['end']) / TPM
        same_board_gap[(c.last_visit['out'], out, 'ate 30 min' if gap <= 30 else 'mais de 30 min')] += 1
    v['end'] = end_tick
    c.last_visit = v

P2C = {}
for r in csv.DictReader(io.open(D + '/ends.csv', encoding='utf-8')): P2C[(r['seed'], int(r['person']))] = int(r['char'])
n = 0; unmapped = 0
with io.open(D + '/events.csv', encoding='utf-8') as f:
    for e in csv.DictReader(f):
        n += 1
        cid = int(e['char'])
        if cid < 0:
            cid = P2C.get((e['seed'], -cid), 0)
            if cid == 0: unmapped += 1; continue
        if cid == 0: continue
        key = (e['seed'], cid)
        c = chars[key]
        t = e['type']; tick = int(e['tick']); det = e['detail']
        if t == 'session_started':
            c.session = tick; c.sess_dailies = []
        elif t == 'session_ended':
            close_visit(c, key, tick, 'session')
            for d in c.sess_dailies: d['sess_end'] = tick
            c.sess_dailies = []
        elif t == 'activity_chosen':
            close_visit(c, key, tick, 'activity')
            # trabalho nas diarias ativas
            for q, d in c.dailies.items():
                if STEP[q] and det == STEP[q][0][0] + '/' + STEP[q][0][1]: d['worked'] = True
            if det in BOARDS:
                causes = e['causes']
                purpose = 'entregar' if 'quest_deliver=' in causes else ('aceitar' if 'quest_offer=' in causes else 'outro')
                c.visit = dict(tick=tick, board=det, purpose=purpose, res_tick=None, acc=[], **{'del': []}, drop=[], session=c.session)
        elif t == 'quest_completed':
            if c.visit is not None:
                c.visit['del'].append(det)
                if c.visit['res_tick'] is None: c.visit['res_tick'] = tick
            if det in c.dailies:
                d = c.dailies.pop(det); d['fim'] = 'entregue'; d['end'] = tick
        elif t == 'quest_accepted':
            vis = c.visit
            if vis is not None:
                vis['acc'].append(det)
                if vis['res_tick'] is None: vis['res_tick'] = tick
            if CAT.get(det) == 'daily':
                prev = c.expired.pop(det, None)
                d = dict(q=det, tick=tick, worked=False, step=False, fim='ativa', end=None, sess_end=None,
                         to_reset=(DAY - tick % DAY) / TPM, purpose=vis['purpose'] if vis else '?',
                         visit_had_delivery=bool(vis and vis['del']), reaccept=prev)
                c.dailies[det] = d; daily_inst.append(d); c.sess_dailies.append(d)
                if prev is not None:
                    reaccept[('anterior com progresso' if prev['worked'] or prev['step'] else 'anterior sem progresso',
                              'visita para entregar' if vis and vis['del'] else 'visita so para aceitar')] += 1
        elif t == 'quest_dropped':
            if c.visit is not None:
                c.visit['drop'].append(det)
                if c.visit['res_tick'] is None: c.visit['res_tick'] = tick
            if det in c.dailies:
                d = c.dailies.pop(det); d['fim'] = 'largada'; d['end'] = tick
        elif t == 'quest_step_completed':
            q = det.split('#')[0]
            if q in c.dailies: c.dailies[q]['step'] = True; c.dailies[q]['worked'] = True
        elif t == 'quest_expired':
            if det in c.dailies:
                d = c.dailies.pop(det); d['fim'] = 'expirou'; d['end'] = tick
                c.expired[det] = d

print(f'eventos lidos: {n:,}; de pessoas sem personagem no ends: {unmapped:,}')
pl = Counter(); tm = Counter()
for v in visits:
    pl[(v['purpose'], v['out'])] += 1
    if v['mins'] is not None: tm[(v['purpose'], v['out'])] += max(0, v['mins'])
print('\n## Visitas ao quadro por finalidade e resultado')
print('| finalidade | resultado | visitas | viagem (h) | viagem media (min) |')
print('|---|---|---|---|---|')
for k in sorted(pl, key=lambda k: (k[0], -pl[k])):
    print(f'| {k[0]} | {k[1]} | {pl[k]:,} | {tm[k]/60:,.0f} | {tm[k]/max(1,pl[k]):.1f} |')
tot = sum(tm.values())
print(f'total de viagem ao quadro: {tot/60:,.0f} h em {len(visits):,} visitas')

od = [v for v in visits if v['only_daily']]
print(f"\nvisitas que so aceitaram diarias: {len(od):,} (viagem {sum(max(0,v['mins'] or 0) for v in od)/60:,.0f} h); finalidade: {dict(Counter(v['purpose'] for v in od))}")
acc_in_deliver = Counter()
for v in visits:
    if v['del']: acc_in_deliver['com entrega'] += 1; acc_in_deliver['com entrega e aceite'] += bool(v['acc'])
print('visitas com entrega que tambem aceitaram:', dict(acc_in_deliver))

print('\n## Mesmo quadro de novo na mesma sessao (visita anterior -> esta)')
for k, x in same_board_gap.most_common(20): print(f'  {k}: {x:,}')

print('\n## Diarias aceitas')
fim = Counter(d['fim'] for d in daily_inst)
print('destino:', dict(fim), 'total', len(daily_inst))
ex = [d for d in daily_inst if d['fim'] == 'expirou']
cls = Counter('etapa feita, nao entregue' if d['step'] else ('trabalhou sem concluir' if d['worked'] else 'nenhuma etapa executada') for d in ex)
print('expiradas:', dict(cls))
def bucket(m):
    return 'ate 1 h' if m <= 60 else 'ate 3 h' if m <= 180 else 'ate 8 h' if m <= 480 else 'mais de 8 h'
print('expiradas sem etapa: tempo ate o reset no aceite:', dict(Counter(bucket(d['to_reset']) for d in ex if not d['worked'])))
def sess_left(d):
    if d['sess_end'] is None: return 'sessao sem fim registrado'
    m = (d['sess_end'] - d['tick']) / TPM
    return 'sessao acaba em ate 30 min' if m <= 30 else 'ate 60 min' if m <= 60 else 'mais de 60 min'
print('expiradas sem etapa: sessao restante no aceite:', dict(Counter(sess_left(d) for d in ex if not d['worked'])))
print('expiradas sem etapa: visita do aceite:', dict(Counter(('com entrega' if d['visit_had_delivery'] else 'so aceite', d['purpose']) for d in ex if not d['worked'])))
print('entregues: visita do aceite:', dict(Counter(('com entrega' if d['visit_had_delivery'] else 'so aceite', d['purpose']) for d in daily_inst if d['fim'] == 'entregue')))
print('\n## Reaceite da mesma diaria depois de expirar')
for k, x in sorted(reaccept.items()): print(f'  {k}: {x:,}')
re_inst = [d for d in daily_inst if d['reaccept'] is not None]
print('destino das reaceitas:', dict(Counter(d['fim'] for d in re_inst)))
chain = Counter()
for d in re_inst:
    p = d['reaccept']
    chain[(('com progresso' if (p['worked'] or p['step']) else 'sem progresso'), d['fim'])] += 1
print('anterior -> destino da reaceita:', dict(chain))
# ciclos aceitar -> expirar sem progresso -> aceitar de novo, por personagem e diaria
streak = Counter()
for d in daily_inst:
    k = 0; p = d
    while p['reaccept'] is not None and not (p['reaccept']['worked'] or p['reaccept']['step']):
        k += 1; p = p['reaccept']
    streak[k] += 1
print('cadeias de expiracoes sem progresso antes do aceite:', dict(sorted(streak.items())))
