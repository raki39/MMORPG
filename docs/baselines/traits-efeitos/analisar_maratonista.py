"""Maratonista: duracao das sessoes, maratonas e compensacao no dia seguinte (Q0 sem efeitos x Q1 com
todos os efeitos), mesmas pessoas; seeds 1 e 2 x 1.000 x 28 dias, dias 7 a 27."""
import csv, os, collections, statistics as st
BASE = r'C:\Users\Pichau\AppData\Local\Temp\claude\7e6c3e7f92c7c968\4e9bc49c-3ce5-4a0d-a5a1-a1a006a649c0\scratchpad\ses'
TPM, TPD = 600, 864000
def load(v, s):
    d = os.path.join(BASE, f'{v}_s{s}')
    P = {int(r['id']): r for r in csv.DictReader(open(os.path.join(d, 'persons.csv'), encoding='utf-8'))}
    S = [r for r in csv.DictReader(open(os.path.join(d, 'sessions.csv'), encoding='utf-8')) if r['kind'] == 'normal']
    return P, S
out = []; w = out.append
res = {}
for v in ('Q0', 'Q1'):
    for grp in ('maratonista', 'demais'):
        lens, marathons, sessions, persons = [], 0, 0, set()
        daymin = collections.defaultdict(int)   # (seed, person, localday) -> minutos
        mar_days = set()
        for s in (1, 2):
            P, S = load(v, s)
            for r in S:
                pid = int(r['person']); pr = P[pid]
                is_m = 'maratonista' in pr['traits'].split('|')
                if (grp == 'maratonista') != is_m: continue
                a, b = int(r['start']), int(r['end'])
                if not (7 * TPD <= a < 28 * TPD): continue
                m = (b - a) // TPM
                lens.append(m); sessions += 1; persons.add((s, pid))
                ld = (a // TPM + int(pr['offset_min'])) // 1440
                daymin[(s, pid, ld)] += m
                if m >= 2 * int(pr['session_min']): marathons += 1; mar_days.add((s, pid, ld))
        after = [daymin.get((s, pid, ld + 1), 0) for (s, pid, ld) in mar_days]
        days_played = [x for x in daymin.values()]
        res[(v, grp)] = dict(n=len(persons), sessions=sessions, med=st.median(lens), mean=st.mean(lens), p90=sorted(lens)[int(len(lens) * .9)],
                             mar=marathons, mar_pp=marathons / max(1, len(persons)), mar_share=marathons * 100 / max(1, sessions),
                             day=st.mean(days_played), after=st.mean(after) if after else 0, n_after=len(after))
w('| Grupo | Versão | Pessoas | Sessões normais | Duração mediana · média · p90 (min) | Maratonas (≥ 2× a sessão normal) | Minutos num dia jogado | Minutos no dia seguinte a uma maratona |')
w('|---|---|---|---|---|---|---|---|')
for grp in ('maratonista', 'demais'):
    for v in ('Q0', 'Q1'):
        r = res[(v, grp)]
        w(f"| {grp} | {v} | {r['n']} | {r['sessions']} | {r['med']:.0f} · {r['mean']:.0f} · {r['p90']} | {r['mar']} ({r['mar_pp']:.2f} por pessoa; {r['mar_share']:.1f}% das sessões) | {r['day']:.0f} | {r['after']:.0f} ({r['n_after']} dias) |")
print('\n'.join(out))
open(os.path.join(BASE, 'maratonista.md'), 'w', encoding='utf-8', newline='').write('\n'.join(out) + '\n')
