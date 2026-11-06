import csv, io, sys
from collections import defaultdict, Counter
d = sys.argv[1]
TPD, TPH = 864000, 36000
ends = list(csv.DictReader(io.open(d + '/ends.csv', encoding='utf-8')))
ev = defaultdict(list); person_of = {}
for r in ends: person_of[r['char']] = r['person']
rows = list(csv.DictReader(io.open(d + '/events.csv', encoding='utf-8')))
for e in rows:
    ev[e['char']].append(e)
def when(t): t = int(t); return f'd{t // TPD:02d} {(t % TPD) // TPH:02d}h'
def show(ch, quests, title, limit=40):
    print(f'\n### {title} (personagem {ch})')
    pid = '-' + person_of.get(ch, '0')
    items = [e for e in ev[ch] + ev[pid] if (
        (e['type'] in ('goal_set', 'goal_completed', 'goal_abandoned') and e['detail'].startswith('complete_quest') and e['detail'].split('|')[1] in quests)
        or (e['type'] in ('quest_accepted', 'quest_step_completed', 'quest_completed') and e['detail'].split('#')[0] in quests)
        or e['type'] in ('objective_abandoned',) or (e['type'] == 'level_up' and int(e['detail'].split()[-1]) % 5 == 0)
        or (e['type'] == 'objective_set' and 'campanha' in e['detail']))]
    items.sort(key=lambda e: int(e['tick']))
    for e in items[:limit]:
        extra = e['causes'].split('|')[0] if e['type'] == 'goal_abandoned' else ''
        print(f'  {when(e["tick"])}  {e["type"]:22s} {e["detail"]} {extra}')
    E = [r for r in ends if r['char'] == ch and r['type'] == 'complete_quest' and r['target'] in quests and r['completed'] == '0']
    for r in E[:4]:
        print(f'    abandono em {when(r["end_tick"])}: nível {r["level"]}, missão ativa={r["active"]}, etapa {r["step_index"]}/{r["steps"]} ({r["step_min"]}/{r["step_need"]} min) em {r["step_region"]}/{r["step_act"]}; vagas {r["active_quests"]}/{r["max_active"]}: {r["slots"]}')

# (a) abandonou o objetivo da campanha
ab = [e for e in rows if e['type'] == 'objective_abandoned' and e['detail'].startswith('campanha')]
if ab:
    ch = ab[0]['char']
    qs = Counter(r['target'] for r in ends if r['char'] == ch and r['type'] == 'complete_quest')
    show(ch, set(q for q, _ in qs.most_common(2)), 'A: abandonou o objetivo "Concluir a campanha principal"')
# (b) missao principal antiga de nivel baixo, readotada varias vezes
cand = Counter((r['char'], r['target']) for r in ends if r['type'] == 'complete_quest' and r['completed'] == '0' and r['active'] == '1' and int(r['step_min']) == 0 and int(r['level']) >= 20)
if cand:
    (ch, q), n = cand.most_common(1)[0]
    show(ch, {q}, f'B: missão antiga, meta readotada e abandonada {n} vezes')
# (c) progresso parcial ignorado e missao concluida depois
part = [r for r in ends if r['type'] == 'complete_quest' and r['completed'] == '0' and r['active'] == '1' and int(r['step_min']) * 2 >= int(r['step_need']) > 0]
for r in part:
    later = [e for e in ev[r['char']] if e['type'] == 'quest_completed' and e['detail'] == r['target'] and int(e['tick']) > int(r['end_tick'])]
    if later:
        show(r['char'], {r['target']}, 'C: meta abandonada com a etapa quase feita; a missão foi concluída depois', 16)
        break
