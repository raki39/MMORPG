"""P4a: (1) o Agressivo ainda responde a preferencias, metas e risco? (2) quem anda mais com as regras?
P0 (sem regras) x P1 (com regras), mesmas pessoas. Le reports/p4a/val/<V>/people.csv."""
import os, csv, collections
import numpy as np
os.chdir(r'C:\Users\Pichau\OneDrive\Área de Trabalho\IdeiaJogo')
TYPES = ['hunt', 'boss', 'quest', 'explore', 'gather', 'trade', 'socialize', 'train', 'repair', 'patrol', 'ambush']
def load(v):
    return {(r['seed'], r['person_id']): r for r in csv.DictReader(open(f'reports/p4a/val/{v}/people.csv', encoding='utf-8')) if r['registered'] == '1'}
A, B = load('P0'), load('P1')
keys = sorted(set(A) & set(B))
def share(r, ts):
    pl = int(r['min_played'])
    return 100 * sum(int(r['min_' + t]) for t in ts) / pl if pl else 0
def travel(r):
    pl = int(r['min_played'])
    return 100 * (pl - sum(int(r['min_' + t]) for t in TYPES)) / pl if pl else 0   # deslocamento + espera
out = []; w = out.append
# ---------- (2) deslocamento por personalidade ----------
w('== Andando ou esperando (deslocamento + espera), % do tempo conectado, ponderado por tempo ==')
w('personalidade      n    P0     P1    delta  | contribuicao ao aumento geral (pontos)')
tot0 = sum(int(A[k]['min_played']) for k in keys); tot1 = sum(int(B[k]['min_played']) for k in keys)
def wtravel(rows):
    pl = sum(int(r['min_played']) for r in rows)
    return 100 * sum(int(r['min_played']) - sum(int(r['min_' + t]) for t in TYPES) for r in rows) / pl if pl else 0
w(f'{"TODOS":16s} {len(keys):5d} {wtravel([A[k] for k in keys]):5.1f} {wtravel([B[k] for k in keys]):6.1f}')
pers = sorted(set(B[k]['personality_initial'] for k in keys))
for ps in pers:
    ks = [k for k in keys if B[k]['personality_initial'] == ps]
    a = [A[k] for k in ks]; b = [B[k] for k in ks]
    # contribuicao: travel minutes change share of total played
    trav0 = sum(int(r['min_played']) - sum(int(r['min_' + t]) for t in TYPES) for r in a)
    trav1 = sum(int(r['min_played']) - sum(int(r['min_' + t]) for t in TYPES) for r in b)
    contrib = 100 * trav1 / tot1 - 100 * trav0 / tot0
    w(f'{ps:16s} {len(ks):5d} {wtravel(a):5.1f} {wtravel(b):6.1f} {wtravel(b)-wtravel(a):+6.1f}  | {contrib:+.2f}')
idle0 = sum(int(A[k]['min_idle']) for k in keys) * 100 / tot0; idle1 = sum(int(B[k]['min_idle']) for k in keys) * 100 / tot1
w(f'espera (idle) geral: P0 {idle0:.1f}%  P1 {idle1:.1f}%')
for ps in ('agressivo', 'pacifico', 'reservado', 'inquieto', 'versatil', 'metodico', 'paciente', 'sociavel'):
    ks = [k for k in keys if B[k]['personality_initial'] == ps]
    i0 = sum(int(A[k]['min_idle']) for k in ks) * 100 / max(1, sum(int(A[k]['min_played']) for k in ks))
    i1 = sum(int(B[k]['min_idle']) for k in ks) * 100 / max(1, sum(int(B[k]['min_played']) for k in ks))
    w(f'   espera {ps:10s}: {i0:.1f}% -> {i1:.1f}%')

# ---------- (1) Agressivo ----------
ag = [k for k in keys if B[k]['personality_initial'] == 'agressivo']
w(f'\n== Agressivo (n={len(ag)}): distribuicao por pessoa do % de PvP (patrulha + emboscada) ==')
for nm, D in (('P0', A), ('P1', B)):
    v = np.array([share(D[k], ['patrol', 'ambush']) for k in ag])
    w(f'{nm}: p10 {np.percentile(v,10):.1f}  p25 {np.percentile(v,25):.1f}  mediana {np.median(v):.1f}  p75 {np.percentile(v,75):.1f}  p90 {np.percentile(v,90):.1f}  max {v.max():.1f}  | >50%: {np.mean(v>50)*100:.1f}%  >70%: {np.mean(v>70)*100:.1f}%  zero: {np.mean(v==0)*100:.1f}%')
    w(f'    patrulha {np.mean([share(D[k],["patrol"]) for k in ag]):.1f}%  emboscada {np.mean([share(D[k],["ambush"]) for k in ag]):.1f}% (media por pessoa)')
w('\nAgressivo: % de PvP por quartil de preferencia de PvP (P0 -> P1)')
pv = np.array([int(B[k]['pref_pvp']) for k in ag]); qs = np.percentile(pv, [25, 50, 75])
for i, (lo, hi) in enumerate(((-1, qs[0]), (qs[0], qs[1]), (qs[1], qs[2]), (qs[2], 2000))):
    ks = [k for k in ag if lo < int(B[k]['pref_pvp']) <= hi]
    w(f'  Q{i+1} pref_pvp ({lo:.0f},{hi:.0f}] n={len(ks):3d}: {np.mean([share(A[k],["patrol","ambush"]) for k in ks]):5.1f} -> {np.mean([share(B[k],["patrol","ambush"]) for k in ks]):5.1f}')
w('\nAgressivo: o tempo em cada tema ainda acompanha a preferencia do tema? (correlacao de Spearman por pessoa, P0 -> P1; e o mesmo para os nao agressivos em P1)')
def spear(x, y):
    rx = np.argsort(np.argsort(x)); ry = np.argsort(np.argsort(y)); return np.corrcoef(rx, ry)[0, 1]
others = [k for k in keys if B[k]['personality_initial'] not in ('agressivo',)]
for pref, ts in (('pvp', ['patrol', 'ambush']), ('pve', ['hunt', 'boss']), ('gathering', ['gather']), ('exploration', ['explore']), ('social', ['socialize']), ('trade', ['trade'])):
    x = [int(B[k]['pref_' + pref]) for k in ag]
    c0 = spear(x, [share(A[k], ts) for k in ag]); c1 = spear(x, [share(B[k], ts) for k in ag])
    xo = [int(B[k]['pref_' + pref]) for k in others]; co = spear(xo, [share(B[k], ts) for k in others])
    w(f'  {pref:12s}: agressivo {c0:+.2f} -> {c1:+.2f}   | demais (P1) {co:+.2f}')
w('\nAgressivo: tempo medio por tema (por pessoa), P0 -> P1, por quartil de preferencia de coleta')
gv = np.array([int(B[k]['pref_gathering']) for k in ag]); qg = np.percentile(gv, [50])
for nm, sel in (('coleta abaixo da mediana', lambda k: int(B[k]['pref_gathering']) <= qg[0]), ('coleta acima da mediana', lambda k: int(B[k]['pref_gathering']) > qg[0])):
    ks = [k for k in ag if sel(k)]
    w(f'  {nm:26s} n={len(ks)}: coleta {np.mean([share(A[k],["gather"]) for k in ks]):.1f} -> {np.mean([share(B[k],["gather"]) for k in ks]):.1f}; PvP {np.mean([share(A[k],["patrol","ambush"]) for k in ks]):.1f} -> {np.mean([share(B[k],["patrol","ambush"]) for k in ks]):.1f}')
w('\nAgressivo: progressao e metas (P0 -> P1)')
for nm, f in (('nivel mediano', lambda D: np.median([int(D[k]['level']) for k in ag])),
              ('horas/semana mediana', lambda D: np.median([float(D[k]['played_h_week']) for k in ag])),
              ('metas de desbloqueio concluidas/criadas', lambda D: f"{sum(int(D[k]['unlock_done']) for k in ag)}/{sum(int(D[k]['unlock_set']) for k in ag)}"),
              ('descoberta concluida/criada', lambda D: f"{sum(int(D[k]['discover_done']) for k in ag)}/{sum(int(D[k]['discover_set']) for k in ag)}"),
              ('missao % tempo', lambda D: round(np.mean([share(D[k], ['quest']) for k in ag]), 1)),
              ('horas ate o nivel alvo (mediana, quem chegou)', lambda D: np.median([float(D[k]['h_to_target']) for k in ag if D[k]['h_to_target'] not in ('', 'nan', 'NaN')]))):
    w(f'  {nm}: {f(A)} -> {f(B)}')
w('\nAgressivo: PvP por perfil (P1) e por nivel ao fim')
for prof in ('casual', 'regular', 'hardcore', 'devoto'):
    ks = [k for k in ag if B[k]['profile'] == prof]
    if ks: w(f'  {prof:9s} n={len(ks):3d}: {np.mean([share(A[k],["patrol","ambush"]) for k in ks]):.1f} -> {np.mean([share(B[k],["patrol","ambush"]) for k in ks]):.1f}')
for lo, hi in ((1, 10), (11, 20), (21, 30), (31, 99)):
    ks = [k for k in ag if lo <= int(B[k]['level']) <= hi]
    if ks: w(f'  nivel {lo}-{hi}: n={len(ks):3d}: {np.mean([share(B[k],["patrol","ambush"]) for k in ks]):.1f}')
print('\n'.join(out))
open(r'C:\Users\Pichau\AppData\Local\Temp\claude\7e6c3e7f92c7c968\4e9bc49c-3ce5-4a0d-a5a1-a1a006a649c0\scratchpad\p4a2_out.txt', 'w', encoding='utf-8').write('\n'.join(out))
