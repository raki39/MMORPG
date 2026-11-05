"""Analise da populacao nova (Perfil x Preferencias x atributos) contra a referencia A.
Le reports/pp/val/people.csv (+ validation.md) e docs/baselines/exp-dedicacao/A/people.csv.
Escreve reports/pp/analise.md e CSVs. So leitura de relatorios."""
import os, csv, json, math, re, sys
import numpy as np
os.chdir(r'C:\Users\Pichau\OneDrive\Área de Trabalho\IdeiaJogo')
NEW = sys.argv[1] if len(sys.argv) > 1 else 'reports/pp/val'
OUT = sys.argv[2] if len(sys.argv) > 2 else 'reports/pp'
REF = 'docs/baselines/exp-dedicacao/A/people.csv'
occ_def = json.load(open('Content/presets/inicial/occupations.json', encoding='utf-8'))['occupations']
ONAME = {o['id']: o['name'] for o in occ_def}
PREFS = ['pve', 'pvp', 'crafting', 'trade', 'gathering', 'exploration', 'social']
PNAME = {'pve': 'PvE', 'pvp': 'PvP', 'crafting': 'Crafting', 'trade': 'Comércio', 'gathering': 'Coleta', 'exploration': 'Exploração', 'social': 'Social'}
PROF = ['casual', 'regular', 'hardcore', 'devoto']
VOC = ('social', 'explorador', 'comerciante', 'competitivo')

def load(path):
    rows = []
    with open(path, encoding='utf-8') as f:
        for r in csv.DictReader(f): rows.append(r)
    return rows
N = load(f'{NEW}/people.csv'); A = load(REF)
def num(r, k): return float(r[k])
def pct(a, q): return float(np.percentile(np.asarray(a, float), q * 100)) if len(a) else float('nan')
def f1(x): return '—' if x != x else f'{x:.1f}'.replace('.', ',')
def f0(x): return '—' if x != x else f'{x:.0f}'
def f2(x): return '—' if x != x else f'{x:.2f}'.replace('.', ',')
def ranks01(a):
    a = np.asarray(a, float); order = a.argsort(kind='mergesort'); r = np.empty(len(a)); r[order] = np.arange(len(a))
    _, inv, cnt = np.unique(a, return_inverse=True, return_counts=True); r = np.bincount(inv, weights=r)[inv] / cnt[inv]
    return (r + 0.5) / len(a)
def spearman(x, y): return float(np.corrcoef(ranks01(x), ranks01(y))[0, 1])
def hrow(a): return f'{f1(pct(a,.1))} · **{f1(pct(a,.5))}** · {f1(pct(a,.9))} · {f1(pct(a,.99))} ({f1(max(a) if a else float("nan"))})'

md = []; w = md.append
seeds = sorted({r['seed'] for r in N})
w('# Perfil × Preferências × atributos: medição da população nova\n')
w(f'Medição antes de adotar o preset. Execuções: seeds {", ".join(seeds)} × 1.000 pessoas × 90 dias (7 de aquecimento, janela de 83 dias), a validação padrão. Referência: versão A de `docs/baselines/exp-dedicacao/` (mesmas seeds e tamanho).\n')

# Pareamento
idxA = {(r['seed'], r['person_id']): r for r in A}
same_occ = sum(1 for r in N if (r['seed'], r['person_id']) in idxA and idxA[(r['seed'], r['person_id'])]['occupation'] == r['occupation'])
w(f'**Pareamento com A:** {same_occ} de {len(N)} pessoas têm a mesma ocupação de A ({same_occ*100/len(N):.1f}%); nome e localidade seguem o mesmo fluxo. As diferenças vêm do devoto, que tem ocupações próprias, e do sorteio de perfil com os pesos novos.\n')

# 1. Perfis: atributos de dedicacao
w('## 1. Atributos de dedicação por perfil (p10 · mediana · p90)\n')
w('| Perfil | n | Importância | Sessão normal (min) | Hábito |')
w('|---|---|---|---|---|')
for p in PROF:
    g = [r for r in N if r['profile'] == p]
    cells = [f"{f0(pct([num(r,k) for r in g],.1))} · {f0(pct([num(r,k) for r in g],.5))} · {f0(pct([num(r,k) for r in g],.9))}" for k in ('game_importance', 'session_minutes', 'habit')]
    w(f'| {p.capitalize()} | {len(g)} | ' + ' | '.join(cells) + ' |')
w('')

# 2. Horas
def hours_table(field, title):
    w(f'## {title}\n')
    w('p10 · **mediana** · p90 · p99 (máximo), em horas por semana. Na referência A, "Vocações" junta social, explorador, comerciante e competitivo.\n')
    w('| Perfil | n | Nova | Referência A |')
    w('|---|---|---|---|')
    for p in PROF:
        a = [num(r, field) for r in N if r['profile'] == p and r['registered'] == '1']
        ref = [num(r, field) for r in A if (r['profile'] in VOC if p == 'regular' else r['profile'] == p) and r['registered'] == '1']
        lab = p.capitalize() + (' (A: vocações)' if p == 'regular' else '')
        w(f'| {lab} | {len(a)} | {hrow(a)} | {hrow(ref)} |')
    allN = [num(r, field) for r in N if r['registered'] == '1']; allA = [num(r, field) for r in A if r['registered'] == '1']
    w(f'| **População** | {len(allN)} | {hrow(allN)} | {hrow(allA)} |')
    w('')
hours_table('expected_h_week', '2. Horas estimadas (na criação)')
hours_table('played_h_week', '3. Horas reais')

# Sobreposicao
w('**Sobreposição (horas reais):**')
def q(p, qq): return pct([num(r, 'played_h_week') for r in N if r['profile'] == p], qq)
w(f'- p90 do casual {f1(q("casual",.9))} h contra mediana do regular {f1(q("regular",.5))} h;')
w(f'- p10 do hardcore {f1(q("hardcore",.1))} h contra mediana do regular {f1(q("regular",.5))} h e p90 do regular {f1(q("regular",.9))} h;')
w(f'- p10 do devoto {f1(q("devoto",.1))} h contra p90 do hardcore {f1(q("hardcore",.9))} h.\n')

# 4. Perfil x ocupacao
w('## 4. Horas reais por perfil × ocupação\n')
w('p10 · mediana · p90; p99 só com 300 pessoas ou mais na célula. Todas as células e o máximo em `perfil-ocupacao.csv`.\n')
occs = sorted({r['occupation'] for r in N}, key=lambda o: ONAME.get(o, o))
po = [['perfil', 'ocupacao', 'n', 'p10', 'mediana', 'p90', 'p99', 'maximo', 'tempo_livre_mediana_h']]
w('| Ocupação | Tempo livre (mediana) | ' + ' | '.join(p.capitalize() for p in PROF) + ' |')
w('|---|---|' + '---|' * len(PROF))
for o in occs:
    fr = [num(r, 'free_h_week') for r in N if r['occupation'] == o and r['free_h_week'] != 'NaN']
    cells = []
    for p in PROF:
        a = [num(r, 'played_h_week') for r in N if r['profile'] == p and r['occupation'] == o and r['registered'] == '1']
        if not a: cells.append('—'); continue
        p99 = f1(pct(a, .99)) if len(a) >= 300 else '—'
        cells.append(f'{f1(pct(a,.1))} · **{f1(pct(a,.5))}** · {f1(pct(a,.9))} (n={len(a)})')
        po.append([p, o, len(a), f'{pct(a,.1):.2f}', f'{pct(a,.5):.2f}', f'{pct(a,.9):.2f}', f'{pct(a,.99):.2f}' if len(a) >= 300 else '', f'{max(a):.2f}', f'{pct(fr,.5):.2f}'])
    w(f'| {ONAME.get(o, o)} | {f1(pct(fr,.5))} | ' + ' | '.join(cells) + ' |')
w('')

# 5. Preferencias: variedade
w('## 5. Preferências: distribuição e variedade\n')
w('| Preferência | p10 | mediana | p90 | forte (≥ 700) | predominante |')
w('|---|---|---|---|---|---|')
top = {}
for r in N:
    vals = {k: int(r['pref_' + k]) for k in PREFS}
    t = max(PREFS, key=lambda k: (vals[k], -PREFS.index(k))); top[t] = top.get(t, 0) + 1
for k in PREFS:
    a = [int(r['pref_' + k]) for r in N]
    w(f'| {PNAME[k]} | {f0(pct(a,.1))} | {f0(pct(a,.5))} | {f0(pct(a,.9))} | {sum(1 for x in a if x >= 700)*100/len(a):.1f}% | {top.get(k,0)*100/len(N):.1f}% |')
cnt = {}
for r in N:
    s = sum(1 for k in PREFS if int(r['pref_' + k]) >= 700); cnt[min(s, 4)] = cnt.get(min(s, 4), 0) + 1
w('\nPreferências fortes por pessoa: ' + ' · '.join(f'{"4 ou mais" if k == 4 else k}: {v*100/len(N):.0f}%' for k, v in sorted(cnt.items())) + '.\n')
w('**Independência do perfil** (mediana de cada preferência por perfil):\n')
w('| Perfil | ' + ' | '.join(PNAME[k] for k in PREFS) + ' |')
w('|---|' + '---|' * len(PREFS))
for p in PROF:
    g = [r for r in N if r['profile'] == p]
    w(f'| {p.capitalize()} | ' + ' | '.join(f0(pct([int(r["pref_" + k]) for r in g], .5)) for k in PREFS) + ' |')
w('')
w('**Ligações (correlação de postos medida):**\n')
pairs = [('pref_social', 't_sociability', 'Social × Sociabilidade (0,35)'), ('pref_pvp', 't_aggressiveness', 'PvP × Agressividade (0,30)'),
         ('pref_exploration', 't_variety', 'Exploração × Variedade (0,30)'), ('pref_pve', 't_pve', 'Preferência PvE × especialidade PvE (0,50)'),
         ('pref_social', 't_socializing', 'Social × especialidade Socializar (0,50)'), ('pref_pvp', 't_competitiveness', 'PvP × Competitividade (sem ligação)'),
         ('t_competitiveness', 't_aggressiveness', 'Competitividade × Agressividade (sem ligação)'), ('pref_pve', 'game_importance', 'PvE × importância do jogo (sem ligação)'),
         ('pref_crafting', 'pref_trade', 'Crafting × Comércio (sem ligação)'), ('pref_gathering', 'pref_trade', 'Coleta × Comércio (sem ligação)')]
w('| Par | Correlação |'); w('|---|---|')
for a, b, lab in pairs: w(f'| {lab} | {f2(spearman([num(r,a) for r in N], [num(r,b) for r in N]))} |')
w('')

# 6. Influencia nas decisoes
w('## 6. Influência real das preferências nas escolhas\n')
w('Parcela do tempo em atividades de cada tipo (toda a campanha), pelo terço da preferência correspondente. Se a preferência pesa, o terço de cima passa mais tempo no tipo; se fosse obrigação, o de baixo não faria nada dele.\n')
groups = [('pve', ['hunt', 'boss'], 'Caça e chefe'), ('gathering', ['gather'], 'Coleta'), ('trade', ['trade'], 'Comércio'),
          ('exploration', ['explore'], 'Exploração'), ('social', ['socialize'], 'Taverna'), ('pvp', ['patrol', 'ambush'], 'Patrulha e emboscada'),
          ('crafting', ['gather', 'trade'], 'Coleta e comércio, pelo Crafting (deve ficar igual)')]
TYPES = ['hunt', 'boss', 'quest', 'explore', 'gather', 'trade', 'socialize', 'train', 'repair', 'patrol', 'ambush']
act = [r for r in N if sum(int(r['min_' + t]) for t in TYPES) >= 60]
w('| Tipo | Preferência | Terço de baixo | Terço do meio | Terço de cima | Correlação (pessoa) |')
w('|---|---|---|---|---|---|')
for pref, types, lab in groups:
    share = [sum(int(r['min_' + t]) for t in types) / sum(int(r['min_' + t]) for t in TYPES) * 100 for r in act]
    pv = [int(r['pref_' + pref]) for r in act]
    rk = ranks01(pv)
    lo = [s for s, x in zip(share, rk) if x < 1/3]; mi = [s for s, x in zip(share, rk) if 1/3 <= x < 2/3]; hi = [s for s, x in zip(share, rk) if x >= 2/3]
    w(f'| {lab} | {PNAME[pref]} | {f1(np.mean(lo))}% | {f1(np.mean(mi))}% | {f1(np.mean(hi))}% | {f2(spearman(pv, share))} |')
w('')
w('**Especialidade (reforço mínimo):** o fator "especialidade" vale no máximo `teto × peso da afinidade` = 50 × 450 / 1000 = **22 pontos**, contra até **450 pontos** do gosto (preferência): no máximo 5% do termo de afinidade. Comparar tempo de caça por nível de especialidade não isola o reforço, porque a especialidade cresce com a prática e está ligada à preferência (0,5).\n')

# 6b. Mistura de atividades (validacao)
def shares(path):
    out = {}
    for line in open(path, encoding='utf-8'):
        if line.startswith('| ') and 'Tempo ' in line and '%' in line:
            cols = [c.strip() for c in line.split('|')]
            out[cols[2].split(' (')[0]] = cols[4]
    return out
sa = shares('docs/baselines/exp-dedicacao/A/validation.md'); sn = shares(f'{NEW}/validation.md')
w('**Mistura de atividades** (parcela do tempo conectado, média das seeds; faixa provisória da validação):\n')
w('| Medida | Referência A | Nova |'); w('|---|---|---|')
for k in sn: w(f'| {k} | {sa.get(k, "—")}% | {sn[k]}% |')
w('')

# 7. Competitividade
w('## 7. Competitividade e agressividade\n')
w('| Medida | Correlação com Competitividade | Correlação com Agressividade |')
w('|---|---|---|')
lv = [num(r, 'level') for r in N if r['registered'] == '1']
comp = [num(r, 't_competitiveness') for r in N if r['registered'] == '1']; agg = [num(r, 't_aggressiveness') for r in N if r['registered'] == '1']
hrs = [num(r, 'played_h_week') for r in N if r['registered'] == '1']
w(f'| Nível ao fim | {f2(spearman(comp, lv))} | {f2(spearman(agg, lv))} |')
w(f'| Horas por semana | {f2(spearman(comp, hrs))} | {f2(spearman(agg, hrs))} |')
amb = [(int(r['min_ambush']) + int(r['min_patrol'])) for r in N if r['registered'] == '1']
w(f'| Minutos de patrulha e emboscada | {f2(spearman(comp, amb))} | {f2(spearman(agg, amb))} |')
w('')

os.makedirs(OUT, exist_ok=True)
open(f'{OUT}/analise.md', 'w', encoding='utf-8', newline='').write('\n'.join(md))
with open(f'{OUT}/perfil-ocupacao.csv', 'w', encoding='utf-8', newline='') as f: csv.writer(f).writerows(po)
print('ok')
