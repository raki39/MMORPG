"""R6: antes x depois (mesmas pessoas), por ocupacao e hora local: sono, inicio das sessoes, presenca,
passagens trabalho <-> folga; controles: horas por semana e pico. Le hor2/{antes,depois}_p1000_s{1,2}."""
import csv, os, collections, statistics as st
BASE = r'C:\Users\Pichau\AppData\Local\Temp\claude\7e6c3e7f92c7c968\4e9bc49c-3ce5-4a0d-a5a1-a1a006a649c0\scratchpad\hor2'
TPM, TPD = 600, 864000
D0, D1 = 7, 28   # dias medidos (depois de uma semana de cadastro)
PH = ['id', 'profile', 'occupation', 'residence', 'offset_min', 'preferred_hour', 'habitual_hour', 'bedtime_min', 'sleep_need_min', 'registered', 'chronotype', 'free_eve']
DH = ['person', 'local_day', 'free_day', 'sleep_start', 'wake', 'bed', 'night_shift', 'day_sleeper', 'first_block', 'slept', 'deficit', 'reason', 'first_start', 'free_minutes', 'sleep_shift', 'post_shift_windows']
def rows(path, header):
    r = csv.reader(open(path, encoding='utf-8')); next(r)
    return [dict(zip(header, x)) for x in r]
def group(occ):
    if occ in ('sem_ocupacao', 'aposentado'): return 'sem obrigação'
    if occ == 'turno_noturno': return 'turno noturno'
    if occ in ('estudante_manha', 'universitario'): return 'estudo de manhã'
    if occ in ('estudante_tarde', 'meio_periodo_tarde', 'autonomo', 'escala_6x1'): return 'obrigação começa tarde'
    if occ == 'trabalho_estudo': return 'trabalho + estudo à noite'
    return 'trabalho diurno'
GROUPS = ['sem obrigação', 'trabalho diurno', 'estudo de manhã', 'obrigação começa tarde', 'trabalho + estudo à noite', 'turno noturno']
def load(tag, seed):
    d = os.path.join(BASE, f'{tag}_p1000_s{seed}')
    P = {int(r['id']): r for r in rows(os.path.join(d, 'persons.csv'), PH)}
    S = [(int(a), int(b), int(c)) for a, b, c in csv.reader(open(os.path.join(d, 'sessions.csv'), encoding='utf-8')) if a != 'person']
    Dy = rows(os.path.join(d, 'days.csv'), DH)
    return P, S, Dy
def hm(m): m = int(round(m)) % 1440; return f'{m // 60:02d}:{m % 60:02d}'
def q(a, x): a = sorted(a); return a[min(len(a) - 1, int(len(a) * x))] if a else 0
out = []; w = out.append
data = {(t, s): load(t, s) for t in ('antes', 'depois') for s in (1, 2)}

# 1) Presenca por hora local (% do tempo jogado do grupo em cada hora) e por hora do servidor
w('## 1. Tempo jogado por hora local (% do tempo do grupo; dias 7 a 27, seeds 1 e 2)\n')
w('Faixas: madrugada 0h–6h, manhã 6h–12h, tarde 12h–18h, noite 18h–24h.\n')
w('| Grupo | Pessoas | Madrugada | Manhã | Tarde | Noite | Horas/semana (mediana) |')
w('|---|---|---|---|---|---|---|')
hourly = {}
for g in GROUPS + ['todos']:
    for t in ('antes', 'depois'):
        mins = collections.Counter(); n = 0; weekly = []
        for s in (1, 2):
            P, S, _ = data[(t, s)]
            per = collections.Counter()
            for p, a, b in S:
                r = P[p]
                if g != 'todos' and group(r['occupation']) != g: continue
                off = int(r['offset_min'])
                a, b = max(a, D0 * TPD), min(b, D1 * TPD)
                tt = a - a % TPM
                while tt < b:
                    mins[((tt // TPM + off) % 1440) // 60] += 10; per[p] += 10; tt += 10 * TPM
            ids = [p for p, r in P.items() if r['registered'] == '1' and (g == 'todos' or group(r['occupation']) == g)]
            n += len(ids)
            weekly += [per.get(p, 0) / 60 / ((D1 - D0) / 7) for p in ids]
        tot = max(1, sum(mins.values()))
        hourly[(g, t)] = [mins.get(h, 0) * 100 / tot for h in range(24)]
        band = lambda h0, h1: sum(mins.get(h, 0) for h in range(h0, h1)) * 100 / tot
        w(f'| {g} ({t}) | {n} | {band(0, 6):.1f}% | {band(6, 12):.1f}% | {band(12, 18):.1f}% | {band(18, 24):.1f}% | {st.median(weekly) if weekly else 0:.1f} |')
w('\n**Hora a hora (todos, % do tempo jogado):**\n')
w('| Hora local | ' + ' | '.join(f'{h}h' for h in range(24)) + ' |')
w('|---|' + '---|' * 24)
for t in ('antes', 'depois'):
    w(f'| {t} | ' + ' | '.join(f'{x:.1f}' for x in hourly[('todos', t)]) + ' |')

# 2) Conectados por hora do servidor e pico
w('\n## 2. Conectados por hora do servidor (média dos dias 14 a 27, 1.000 pessoas)\n')
w('| Seed | Versão | 02h | 03h | 05h | 08h | 10h | 14h | 20h | 21h | Pico/cadastrados |')
w('|---|---|---|---|---|---|---|---|---|---|---|')
for s in (1, 2):
    for t in ('antes', 'depois'):
        P, S, _ = data[(t, s)]
        reg = sum(1 for r in P.values() if r['registered'] == '1')
        def at(h, mm=0): return st.mean(sum(1 for _, a, b in S if a <= d * TPD + (h * 60 + mm) * TPM < b) for d in range(14, 28))
        hours = {h: at(h) for h in range(24)}
        w(f'| {s} | {t} | ' + ' | '.join(f'{hours[h]:.1f}' for h in (2, 3, 5, 8, 10, 14, 20, 21)) + f' | {max(hours.values()) * 100 / reg:.1f}% |')

# 3) Sono por grupo: dia de obrigacao x dia livre (planejado) e real (dormiu, deficit, deslocado)
w('\n## 3. Sono por grupo (hora local; dias 7 a 27)\n')
w('Dormir e acordar planejados pela rotina; "dormiu" é o real, contando quem ficou acordado jogando.\n')
w('| Grupo | Versão | Dia | Dormir p10 · mediana · p90 | Acordar p10 · mediana · p90 | Dormiu (mediana) | Déficit médio | Sono deslocado (dias) |')
w('|---|---|---|---|---|---|---|---|')
for g in GROUPS:
    for t in ('antes', 'depois'):
        for kind in ('obrigação', 'livre'):
            beds, wakes, slept, deficit, shifted, n = [], [], [], [], 0, 0
            for s in (1, 2):
                P, _, Dy = data[(t, s)]
                for r in Dy:
                    pr = P[int(r['person'])]
                    if group(pr['occupation']) != g or not (D0 <= int(r['local_day']) < D1): continue
                    free = r['free_day'] == '1'
                    if (kind == 'livre') != free: continue
                    beds.append(int(r['sleep_start'])); wakes.append(int(r['wake']))
                    slept.append(int(r['slept'])); deficit.append(int(r['deficit'])); n += 1
                    if r.get('sleep_shift') and int(r['sleep_shift']) > 0: shifted += 1
            if n == 0: continue
            w(f'| {g} | {t} | {kind} | {hm(q(beds, .1))} · {hm(q(beds, .5))} · {hm(q(beds, .9))} | {hm(q(wakes, .1))} · {hm(q(wakes, .5))} · {hm(q(wakes, .9))} | {st.median(slept) / 60:.1f} h | {st.mean(deficit):.0f} min | {shifted * 100 / n:.1f}% |')

# 4) Passagens trabalho <-> folga (dormir na noite que antecede o dia)
w('\n## 4. Passagens entre trabalho e folga (quem tem obrigação; hora de dormir na véspera, mediana)\n')
w('| Grupo | Versão | Véspera de folga (dia de obrigação → livre) | Véspera de obrigação (livre → obrigação) | Entre dias de obrigação |')
w('|---|---|---|---|---|')
for g in GROUPS[1:]:
    for t in ('antes', 'depois'):
        res = collections.defaultdict(list)
        for s in (1, 2):
            P, _, Dy = data[(t, s)]
            by = collections.defaultdict(dict)
            for r in Dy: by[int(r['person'])][int(r['local_day'])] = r
            for pid, days in by.items():
                if group(P[pid]['occupation']) != g: continue
                for d, r in days.items():
                    if not (D0 <= d < D1) or d - 1 not in days: continue
                    prev, cur = days[d - 1]['free_day'] == '1', r['free_day'] == '1'
                    key = 'w2f' if (not prev and cur) else 'f2w' if (prev and not cur) else 'w2w' if (not prev and not cur) else 'f2f'
                    res[key].append(int(r['sleep_start']))
        f = lambda k: hm(st.median(res[k])) if res[k] else '—'
        w(f'| {g} | {t} | {f("w2f")} | {f("f2w")} | {f("w2w")} |')

# 5) Inicio das sessoes por hora local (todos)
w('\n## 5. Início das sessões por hora local (todos, % das sessões; dias 7 a 27)\n')
w('| Versão | ' + ' | '.join(f'{h}h' for h in range(24)) + ' |')
w('|---|' + '---|' * 24)
for t in ('antes', 'depois'):
    c = collections.Counter(); n = 0
    for s in (1, 2):
        P, S, _ = data[(t, s)]
        for p, a, b in S:
            if D0 * TPD <= a < D1 * TPD:
                c[((a // TPM + int(P[p]['offset_min'])) % 1440) // 60] += 1; n += 1
    w(f'| {t} | ' + ' | '.join(f'{c.get(h, 0) * 100 / n:.1f}' for h in range(24)) + ' |')

# 6) Geracao (depois): cronotipo, preferido, hora natural e vespera de folga
P, _, _ = data[('depois', 1)]
Pa, _, _ = data[('antes', 1)]
w('\n## 6. Geração (seed 1, 1.000 pessoas)\n')
pref_a = collections.Counter(int(r['preferred_hour']) for r in Pa.values()); pref_d = collections.Counter(int(r['preferred_hour']) for r in P.values())
w('Horário preferido (hora local; 24 a 28 = 0h a 4h):\n')
w('| Versão | ' + ' | '.join(f'{h}' for h in range(7, 29)) + ' |'); w('|---|' + '---|' * 22)
for t, c in (('antes', pref_a), ('depois', pref_d)):
    w(f'| {t} | ' + ' | '.join(str(c.get(h, 0)) for h in range(7, 29)) + ' |')
bed_a = [int(r['bedtime_min']) for r in Pa.values()]; bed_d = [int(r['bedtime_min']) for r in P.values()]
w(f'\nHora natural de dormir (já adaptada às obrigações): antes p10 {hm(q(bed_a, .1))}, mediana {hm(q(bed_a, .5))}, p90 {hm(q(bed_a, .9))}, máx. {hm(max(bed_a))}; depois p10 {hm(q(bed_d, .1))}, mediana {hm(q(bed_d, .5))}, p90 {hm(q(bed_d, .9))}, máx. {hm(max(bed_d))}.')
for g in ('sem obrigação', 'trabalho diurno'):
    b = [int(r['bedtime_min']) for r in P.values() if group(r['occupation']) == g]
    w(f'- {g}, depois: p10 {hm(q(b, .1))}, mediana {hm(q(b, .5))}, p90 {hm(q(b, .9))}, depois das 02h {sum(1 for x in b if x >= 1560) * 100 / len(b):.0f}%.')
fe = [int(r['free_eve']) for r in P.values()]
w(f'- Véspera de folga: {sum(1 for x in fe if x > 0) * 100 / len(fe):.0f}% deslocam; entre quem desloca, mediana {st.median([x for x in fe if x > 0]):.0f} min.')
# coerencia: cronotipo x preferido x dormir
ch = [(int(r['chronotype']), int(r['preferred_hour']), int(r['bedtime_min'])) for r in P.values() if group(r['occupation']) == 'sem obrigação']
def spear(x, y):
    import numpy as np
    rx = np.argsort(np.argsort(x)); ry = np.argsort(np.argsort(y)); return float(np.corrcoef(rx, ry)[0, 1])
allc = [(int(r['chronotype']), int(r['preferred_hour']), int(r['bedtime_min'])) for r in P.values()]
w(f'- Correlação de postos com o cronotipo (todos): horário preferido {spear([a for a, _, _ in allc], [b for _, b, _ in allc]):+.2f}; hora natural de dormir (sem obrigação) {spear([a for a, _, _ in ch], [c for _, _, c in ch]):+.2f}.')
print('\n'.join(out))
open(os.path.join(BASE, 'analise_r6.md'), 'w', encoding='utf-8', newline='').write('\n'.join(out) + '\n')
