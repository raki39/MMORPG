"""Conferencias da R6: (1) deficit de quem trabalha e estuda; (2) contas da janela depois do turno."""
import csv, os, json, collections, statistics as st, datetime
BASE = r'C:\Users\Pichau\AppData\Local\Temp\claude\7e6c3e7f92c7c968\4e9bc49c-3ce5-4a0d-a5a1-a1a006a649c0\scratchpad\hor2'
ROOT = r'C:\Users\Pichau\OneDrive\Área de Trabalho\IdeiaJogo'
TPM, TPD = 600, 864000
PH = ['id', 'profile', 'occupation', 'residence', 'offset_min', 'preferred_hour', 'habitual_hour', 'bedtime_min', 'sleep_need_min', 'registered', 'chronotype', 'free_eve']
DH = ['person', 'local_day', 'free_day', 'sleep_start', 'wake', 'bed', 'night_shift', 'day_sleeper', 'first_block', 'slept', 'deficit', 'reason', 'first_start', 'free_minutes', 'sleep_shift', 'post_shift_windows']
def rows(path, header):
    r = csv.reader(open(path, encoding='utf-8')); next(r)
    return [dict(zip(header, x)) for x in r]
world = json.load(open(os.path.join(ROOT, 'Content/presets/inicial/world.json'), encoding='utf-8'))['world']
start = datetime.date.fromisoformat(world['startDate'][:10]) if 'startDate' in world else None
DOW = ['seg', 'ter', 'qua', 'qui', 'sex', 'sáb', 'dom']
def dow(day): return (start + datetime.timedelta(days=day)).weekday() if start else -1
rs = json.load(open(os.path.join(ROOT, 'Content/presets/inicial/population.json'), encoding='utf-8'))['routine']
out = []; w = out.append
def hm(m): m = int(round(m)) % 1440; return f'{m // 60:02d}:{m % 60:02d}'
for seed in (1, 2):
    d = os.path.join(BASE, f'depois_p1000_s{seed}')
    P = {int(r['id']): r for r in rows(os.path.join(d, 'persons.csv'), PH)}
    S = collections.defaultdict(list)
    for a, b, c in csv.reader(open(os.path.join(d, 'sessions.csv'), encoding='utf-8')):
        if a != 'person': S[int(a)].append((int(b), int(c)))
    days = collections.defaultdict(dict)
    for r in rows(os.path.join(d, 'days.csv'), DH): days[int(r['person'])][int(r['local_day'])] = r
    if seed == 1: data1 = (P, S, days)
    else: data2 = (P, S, days)

# ---------------- (1) trabalho + estudo ----------------
w('## 1. Déficit de quem trabalha e estuda à noite (trabalho_estudo; seeds 1 e 2; dias 7 a 27)\n')
by_dow = collections.defaultdict(lambda: {'n': 0, 'plan_short': [], 'play_cut': [], 'deficit': [], 'played_late': 0, 'slept': [], 'planned': []})
no_play_deficit, play_deficit = [], []
for P, S, days in (data1, data2):
    for pid, dd in days.items():
        pr = P[pid]
        if pr['occupation'] != 'trabalho_estudo': continue
        need = int(pr['sleep_need_min']); off = int(pr['offset_min'])
        for day, r in dd.items():
            if not (7 <= day < 28): continue
            ds = day * TPD - off * TPM
            planned_start = ds + int(r['sleep_start']) * TPM
            planned = int(r['wake']) - int(r['sleep_start'])
            slept = int(r['slept'])
            shift = int(r['sleep_shift'] or 0)
            # Jogou alem da hora de deitar planejada (sessao que termina depois do inicio planejado do sono)?
            late = any(a < planned_start < b for a, b in S.get(pid, []))
            x = by_dow[dow(day)]
            x['n'] += 1
            x['plan_short'].append(max(0, need - planned))
            x['play_cut'].append(max(0, planned + shift - slept))
            x['deficit'].append(int(r['deficit']))
            x['slept'].append(slept); x['planned'].append(planned)
            if late: x['played_late'] += 1
            (play_deficit if late else no_play_deficit).append(max(0, need - slept))
w('Por dia da semana (a noite que termina no dia indicado; minutos):\n')
w('| Dia | Noites | Sono planejado (média) | Falta pela agenda (média) | Cortado jogando (média) | Noites jogando depois de deitar | Déficit ao acordar (média) |')
w('|---|---|---|---|---|---|---|')
for k in sorted(by_dow):
    x = by_dow[k]
    w(f"| {DOW[k] if k >= 0 else '?'} | {x['n']} | {st.mean(x['planned']) / 60:.1f} h | {st.mean(x['plan_short']):.0f} | {st.mean(x['play_cut']):.1f} | {x['played_late'] * 100 / x['n']:.1f}% | {st.mean(x['deficit']):.0f} |")
tot_plan = sum(sum(x['plan_short']) for x in by_dow.values()); tot_play = sum(sum(x['play_cut']) for x in by_dow.values())
w(f'\nDo sono que faltou no total: {tot_plan * 100 / max(1, tot_plan + tot_play):.1f}% vem da agenda (obrigações) e {tot_play * 100 / max(1, tot_plan + tot_play):.1f}% de ficar jogando depois de deitar.')
w(f'Noites sem jogar depois de deitar: falta média {st.mean(no_play_deficit):.0f} min ({len(no_play_deficit)} noites); com: {st.mean(play_deficit) if play_deficit else 0:.0f} min ({len(play_deficit)} noites).')
w(f"Recuperação: manhã livre recupera até {rs.get('recoveryFreeNightMinutes')} min; noite normal, {rs.get('recoveryNormalNightMinutes', '?')} min (parâmetros da rotina).")

# ---------------- (2) janela depois do turno ----------------
w('\n## 2. Janela depois do turno da noite: contas do sono e do dia (seeds 1 e 2; dias 7 a 27)\n')
checked = played = mismatch = overlap = beyond = 0
shifts, cuts, deficits_after = [], [], []
for P, S, days in (data1, data2):
    for pid, dd in days.items():
        pr = P[pid]
        if pr['occupation'] != 'turno_noturno': continue
        need = int(pr['sleep_need_min']); off = int(pr['offset_min'])
        for day, r in dd.items():
            if r['night_shift'] != '1' or not (7 <= day < 27) or day + 1 not in dd: continue
            nxt = dd[day + 1]
            ds = day * TPD - off * TPM
            bed = int(r['bed'])                       # chegada do turno = inicio planejado do sono
            ws, we = ds + bed * TPM, ds + (bed + rs['postShiftWindowMinutes']) * TPM
            checked += 1
            sess = [(a, b) for a, b in S.get(pid, []) if ws <= a < we]
            if not sess: continue
            played += 1
            logout = max(b for a, b in sess)
            if logout > we: beyond += 1
            nds = (day + 1) * TPD - off * TPM
            planned_wake = nds + int(nxt['wake']) * TPM
            shift = int(nxt['sleep_shift'] or 0)
            real_wake = planned_wake + shift * TPM
            expected_slept = (real_wake - max(ws, logout)) // TPM
            if abs(expected_slept - int(nxt['slept'])) > 1: mismatch += 1
            # Nada pode cair numa obrigacao: o primeiro bloco do dia seguinte comeca depois do acordar real + preparo.
            fb = int(nxt['first_block'])
            if fb >= 0 and nds + fb * TPM < real_wake + rs['prepMinutes'] * TPM: overlap += 1
            shifts.append(shift); cuts.append(max(0, need - int(nxt['slept']))); deficits_after.append(int(nxt['deficit']))
w(f'- Dias de turno conferidos: {checked}; com jogo na janela depois do turno: {played} ({played * 100 / max(1, checked):.1f}%).')
w(f'- Sessões que passaram do fim da janela: {beyond}.')
w(f'- Sono contado = do fim do jogo (ou da chegada) até o acordar real, uma vez só: divergências {mismatch} de {played}.')
w(f'- Acordar real + preparo depois do início da obrigação seguinte: {overlap} de {played}.')
if played:
    w(f'- Sono deslocado depois de jogar: mediana {st.median(shifts):.0f} min; sono que ainda faltou: média {st.mean(cuts):.1f} min; déficit ao acordar: média {st.mean(deficits_after):.0f} min.')
print('\n'.join(out))
open(os.path.join(BASE, 'conferencias_r6.md'), 'w', encoding='utf-8', newline='').write('\n'.join(out) + '\n')
