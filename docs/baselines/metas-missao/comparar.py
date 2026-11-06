import csv, io, sys, json
from collections import Counter, defaultdict
A_DIR, B_DIR, CONTENT = sys.argv[1], sys.argv[2], sys.argv[3]
quests = {q['id']: q['category'] for q in json.load(io.open(CONTENT + '/quests.json', encoding='utf-8'))['quests']}

def klass(r):
    if r['active'] == '0':
        return 'não aceita (vagas cheias)' if int(r['active_quests']) >= int(r['max_active']) else 'não aceita (com vaga)'
    if int(r['step_index']) >= int(r['steps']): return 'pronta para entregar'
    if int(r['step_minlevel']) > int(r['level']): return 'etapa acima do nível'
    if int(r['step_group']) > 1: return 'etapa exige grupo'
    if r['step_zone_known'] == '0' and r['step_hint'] == '0': return 'etapa em zona desconhecida'
    if int(r['step_min']) > 0: return 'progresso parcial na etapa'
    return 'etapa disponível sem avanço'

def stats(d):
    ends = list(csv.DictReader(io.open(d + '/ends.csv', encoding='utf-8')))
    s = {}
    sets = Counter(); people = defaultdict(set); same = Counter(); abk = set(); setk = set()
    main_done = Counter(); camp_ab = 0; camp_done = 0; persons = set()
    later = defaultdict(list)
    for e in csv.DictReader(io.open(d + '/events.csv', encoding='utf-8')):
        t = e['type']
        if t == 'goal_set':
            ty = e['detail'].split('|')[0]; sets[ty] += 1; people[ty].add((e['seed'], e['char'])); setk.add((e['seed'], e['char'], e['tick'], e['detail']))
            if ty == 'complete_quest': later[(e['seed'], e['char'], e['detail'].split('|')[1])].append(int(e['tick']))
        elif t == 'goal_abandoned': abk.add((e['seed'], e['char'], e['tick'], e['detail']))
        elif t == 'quest_completed':
            if quests.get(e['detail']) == 'main': main_done[(e['seed'], e['char'])] += 1
            later[(e['seed'], e['char'], 'done:' + e['detail'])].append(int(e['tick']))
        elif t == 'objective_abandoned' and e['detail'].startswith('campanha'): camp_ab += 1
        elif t == 'objective_completed' and e['detail'].startswith('campanha'): camp_done += 1
        elif t == 'session_started': persons.add((e['seed'], e['char']))
    for (sd, ch, tk, det) in abk & setk: same[det.split('|')[0]] += 1
    Q = [r for r in ends if r['type'] == 'complete_quest']; A = [r for r in Q if r['completed'] == '0']
    s['metas de missão adotadas'] = sets['complete_quest']
    s['pessoas com meta de missão'] = len(people['complete_quest'])
    s['metas de missão: % concluídas'] = 100 * sum(r['completed'] == '1' for r in Q) / len(Q)
    s['metas de missão: % abandonadas'] = 100 * len(A) / len(Q)
    s['pessoas com abandono de meta de missão'] = len({(r['seed'], r['char']) for r in A})
    K = Counter(klass(r) for r in A)
    for k in ['não aceita (vagas cheias)', 'etapa disponível sem avanço', 'progresso parcial na etapa', 'não aceita (com vaga)', 'pronta para entregar']:
        s['abandonos: ' + k] = K[k]
    s['abandono + readoção no mesmo instante (missão)'] = same['complete_quest']
    s['abandono + readoção no mesmo instante (prática)'] = same['practice_activity']
    s['abandono + readoção no mesmo instante (descoberta)'] = same['discover_zone']
    for ty in ['practice_activity', 'discover_zone', 'reach_level', 'unlock_level']:
        E = [r for r in ends if r['type'] == ty]
        s[f'{ty}: % abandonadas'] = 100 * sum(r['completed'] == '0' for r in E) / max(1, len(E))
    s['missões principais concluídas (total)'] = sum(main_done.values())
    s['objetivo da campanha: abandonos'] = camp_ab
    s['objetivo da campanha: concluídos'] = camp_done
    return s

a, b = stats(A_DIR), stats(B_DIR)
print('| Medida | Antes | Depois |')
print('|---|---|---|')
for k in a:
    fa, fb = a[k], b[k]
    fmt = (lambda x: f'{x:.1f}') if isinstance(fa, float) else (lambda x: f'{x}')
    print(f'| {k} | {fmt(fa)} | {fmt(fb)} |')
