import csv, io, sys, bisect
from collections import Counter, defaultdict
d = sys.argv[1]; SEED = sys.argv[2] if len(sys.argv) > 2 else '1'
ends = [r for r in csv.DictReader(io.open(d + '/ends.csv', encoding='utf-8')) if r['seed'] == SEED]
acts = defaultdict(list); steps_other = defaultdict(list); sess = defaultdict(list)
for e in csv.DictReader(io.open(d + '/events.csv', encoding='utf-8')):
    if e['seed'] != SEED: continue
    k = e['char']
    if e['type'] == 'activity_chosen': acts[k].append((int(e['tick']), e['detail'], e['causes']))
    elif e['type'] == 'quest_step_completed': steps_other[k].append((int(e['tick']), e['detail'].split('#')[0]))
    elif e['type'] == 'session_started': sess[k].append(int(e['tick']))

def klass(r):
    if r['active'] == '0':
        return 'nao_aceita_vagas_cheias' if int(r['active_quests']) >= int(r['max_active']) else 'nao_aceita_com_vaga'
    if int(r['step_index']) >= int(r['steps']): return 'pronta_para_entregar'
    if int(r['step_minlevel']) > int(r['level']): return 'etapa_acima_do_nivel'
    if int(r['step_group']) > 1: return 'etapa_exige_grupo'
    if r['step_zone_known'] == '0' and r['step_hint'] == '0': return 'etapa_em_zona_desconhecida'
    if int(r['step_min']) > 0: return 'progresso_parcial_na_etapa'
    return 'etapa_disponivel_sem_avanco'

A = [r for r in ends if r['type'] == 'complete_quest' and r['completed'] == '0' and klass(r) == 'etapa_disponivel_sem_avanco']
chose_step = 0; zero = 0; nchoices = []; other_steps = 0; reasons = Counter(); top_other = Counter(); sessions = []
for r in A:
    k = r['char']; t0, t1 = int(r['set_tick']), int(r['end_tick'])
    target = r['step_region'] + '/' + r['step_act']
    ch = [x for x in acts[k] if t0 <= x[0] <= t1]
    nchoices.append(len(ch))
    if any(x[1] == target for x in ch): chose_step += 1
    if not ch: zero += 1
    if any(t0 <= t <= t1 and q != r['target'] for t, q in steps_other[k]): other_steps += 1
    for x in ch:
        if x[1] != target and x[2]: reasons[x[2].split('|')[0].split('=')[0]] += 1
        if x[1] != target: top_other[x[1].split('/')[1] if '/' in x[1] else x[1]] += 1
    sessions.append(sum(1 for s in sess[k] if t0 <= s <= t1))
n = len(A)
nchoices.sort(); sessions.sort()
print(f'seed {SEED}: etapa disponivel sem avanco = {n}')
print(f'- escolheram a atividade da etapa ao menos uma vez durante a meta: {chose_step} ({100*chose_step/n:.0f}%)')
print(f'- nenhuma escolha de atividade durante a meta: {zero}; escolhas por meta (mediana) {nchoices[n//2]}; sessoes (mediana) {sessions[n//2]}')
print(f'- avancaram outra missao durante a meta: {other_steps} ({100*other_steps/n:.0f}%)')
print('- principal motivo das outras escolhas: ' + ', '.join(f'{k} {v}' for k, v in reasons.most_common(8)))
print('- atividades escolhidas no lugar: ' + ', '.join(f'{k} {v}' for k, v in top_other.most_common(10)))
# a etapa: quais atividades/zonas
print('- etapas paradas mais comuns: ' + ', '.join(f'{k} {v}' for k, v in Counter(r['step_region'] + '/' + r['step_act'] for r in A).most_common(8)))
print('- missoes: ' + ', '.join(f'{k} {v}' for k, v in Counter(r['target'] for r in A).most_common(8)))
