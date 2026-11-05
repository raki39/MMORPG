"""Analise do experimento de dedicacao: le reports/exp-ded/{val,extra}/<V>/people.csv e escreve
reports/exp-ded/comparacao.md e CSVs auxiliares. Sem efeito no jogo (so leitura de relatorios)."""
import os, csv, json, math
import numpy as np
os.chdir(r'C:\Users\Pichau\OneDrive\Área de Trabalho\IdeiaJogo')
VERS = ['A', 'B0', 'B', 'C0', 'C']
DESC = {'A': 'atual (referência)', 'B0': 'triangular, valores equivalentes, sem dedicação compartilhada',
        'B': 'triangular, valores equivalentes, influência 0,45', 'C0': 'triangular, valores candidatos, sem dedicação compartilhada',
        'C': 'triangular, valores candidatos, influência 0,45'}
PROF = ['casual', 'social', 'explorador', 'comerciante', 'competitivo', 'hardcore', 'devoto']
PNAME = {p: p.capitalize() for p in PROF}
occ_def = json.load(open('Content/presets/inicial/occupations.json', encoding='utf-8'))
occ_list = occ_def['occupations'] if isinstance(occ_def, dict) else occ_def
ONAME = {o['id']: o['name'] for o in occ_list}
OUT = 'reports/exp-ded'

def load(v):
    rows = []
    for part in ('val',):
        with open(f'{OUT}/{part}/{v}/people.csv', encoding='utf-8') as f:
            for r in csv.DictReader(f):
                rows.append({'seed': int(r['seed']), 'id': int(r['person_id']), 'profile': r['profile'], 'occ': r['occupation'],
                             'reg': r['registered'] == '1', 'state': r['state'],
                             'imp': int(r['game_importance']), 'ses': int(r['session_minutes']), 'hab': int(r['habit']),
                             'est': float(r['expected_h_week']), 'h': float(r['played_h_week']),
                             'free': float(r['free_h_week']) if r['free_h_week'] != 'NaN' else float('nan')})
    return rows

D = {v: load(v) for v in VERS}
seeds = sorted({r['seed'] for r in D['A']})

def pct(a, q):
    a = np.asarray(a, dtype=float)
    return float(np.percentile(a, q * 100)) if len(a) else float('nan')

def f1(x): return '—' if x != x else f'{x:.1f}'.replace('.', ',')
def f0(x): return '—' if x != x else f'{x:.0f}'
def f2(x): return '—' if x != x else f'{x:.2f}'.replace('.', ',')

def ranks01(a):
    a = np.asarray(a, dtype=float)
    order = a.argsort(kind='mergesort')
    r = np.empty(len(a)); r[order] = np.arange(len(a))
    # empates: media dos postos
    _, inv, counts = np.unique(a, return_inverse=True, return_counts=True)
    sums = np.bincount(inv, weights=r)
    r = sums[inv] / counts[inv]
    return (r + 0.5) / len(a)

md = []
w = md.append
w('# Experimento de dedicação: comparação A, B0, B, C0 e C\n')
w('Experimento, não configuração adotada. O preset principal continua na versão A.\n')
w('## Configuração\n')
w(f'- **Pessoas por execução:** 1.000. **Duração:** 90 dias, com 7 de aquecimento (janela de 83 dias, 11,9 semanas).')
w(f'- **Seeds:** 1 a 5 por versão ({len(seeds)} execuções, {len(D["A"]):,} pessoas no total por versão), a validação padrão com as regras do cenário. Rodada rápida: o teste mais completo fica para depois dos próximos ajustes.'.replace(',', '.'))
w('- **Pareamento:** a mesma seed gera as mesmas pessoas em todas as versões; só os três atributos de dedicação mudam (o teste `FormatoTriangular_NaoMudaOsDemaisAtributos` confere isso em 3.000 pessoas).')
w('- **Horas reais:** tempo jogado na janela dividido pelas semanas da janela, a mesma conta da validação, para quem criou conta. **Estimadas:** estimativa semanal feita na criação da pessoa.')
w('- **Versões:**')
for v in VERS: w(f'  - **{v}:** {DESC[v]}.')
w('- **Conteúdo e código:** ver `mundos/conteudo.txt` (hash de cada `population.json`) e `versao.txt`.\n')

# ---------- 1. parametros ----------
w('## 1. Atributos de dedicação por perfil (p10 · mediana · p90)\n')
for key, label in (('imp', 'Importância do jogo'), ('ses', 'Sessão normal (min)'), ('hab', 'Hábito')):
    w(f'**{label}**\n')
    w('| Perfil | ' + ' | '.join(VERS) + ' |')
    w('|---|' + '---|' * len(VERS))
    for p in PROF:
        cells = []
        for v in VERS:
            a = [r[key] for r in D[v] if r['profile'] == p]
            cells.append(f'{f0(pct(a,.1))} · {f0(pct(a,.5))} · {f0(pct(a,.9))}')
        w(f'| {PNAME[p]} | ' + ' | '.join(cells) + ' |')
    w('')

# ---------- 2. correlacao e combinacoes ----------
w('## 2. Como os três atributos se combinam\n')
w('Postos calculados dentro de cada perfil (para não misturar a diferença entre perfis), depois juntados. "Alto" = terço superior do perfil; "baixo" = terço inferior. Com sorteios independentes, cada combinação esperada é 11,1%.\n')
w('| Versão | Correlação importância × sessão | importância × hábito | sessão × hábito | Importância alta e sessão curta | Importância alta e hábito baixo | Importância baixa e sessão longa |')
w('|---|---|---|---|---|---|---|')
combo_rows = []
for v in VERS:
    ri, rs, rh = [], [], []
    for p in PROF:
        g = [r for r in D[v] if r['profile'] == p]
        if not g: continue
        ri += list(ranks01([r['imp'] for r in g])); rs += list(ranks01([r['ses'] for r in g])); rh += list(ranks01([r['hab'] for r in g]))
    ri, rs, rh = map(np.array, (ri, rs, rh))
    c = lambda x, y: float(np.corrcoef(x, y)[0, 1])
    hl = np.mean((ri > 2/3) & (rs < 1/3)) * 100; hh = np.mean((ri > 2/3) & (rh < 1/3)) * 100; lh = np.mean((ri < 1/3) & (rs > 2/3)) * 100
    w(f'| {v} | {f2(c(ri,rs))} | {f2(c(ri,rh))} | {f2(c(rs,rh))} | {f1(hl)}% | {f1(hh)}% | {f1(lh)}% |')
w('')

# ---------- 3/4. horas ----------
def hours_table(field, title, reg_only):
    w(f'## {title}\n')
    w('p10 · mediana · p90 · p99 (máximo), em horas por semana. n = pessoas.\n')
    w('| Perfil | ' + ' | '.join(VERS) + ' |')
    w('|---|' + '---|' * len(VERS))
    for p in PROF:
        cells = []
        for v in VERS:
            a = [r[field] for r in D[v] if r['profile'] == p and (r['reg'] or not reg_only)]
            cells.append(f'{f1(pct(a,.1))} · **{f1(pct(a,.5))}** · {f1(pct(a,.9))} · {f1(pct(a,.99))} ({f1(max(a) if a else float("nan"))}) n={len(a)}')
        w(f'| {PNAME[p]} | ' + ' | '.join(cells) + ' |')
    w('')
hours_table('est', '3. Horas estimadas por perfil', False)
hours_table('h', '4. Horas reais por perfil (quem criou conta)', True)

# ---------- 5. diferenca pareada ----------
w('## 5. Diferença contra A, pareada por seed\n')
w('Para cada seed, a diferença entre a versão e A (mesmas pessoas, só a dedicação muda); depois a média das seeds com o intervalo de 95% (t de Student, n − 1 graus de liberdade). Mediana = diferença das medianas do perfil; média por pessoa = média das diferenças de cada pessoa com conta nas duas versões.\n')
w('| Perfil | Medida | ' + ' | '.join(VERS[1:]) + ' |')
w('|---|---|' + '---|' * (len(VERS) - 1))
T14 = {4: 2.776, 14: 2.145}[len(seeds) - 1]
idx = {v: {(r['seed'], r['id']): r for r in D[v]} for v in VERS}
paired_csv = [['perfil', 'versao', 'medida', 'media', 'ic95_meia_largura']]
for p in PROF:
    for meas in ('mediana', 'média por pessoa'):
        cells = []
        for v in VERS[1:]:
            diffs = []
            for s in seeds:
                ga = [r for r in D['A'] if r['seed'] == s and r['profile'] == p]
                if meas == 'mediana':
                    a = [r['h'] for r in ga if r['reg']]; b = [r['h'] for r in D[v] if r['seed'] == s and r['profile'] == p and r['reg']]
                    if a and b: diffs.append(pct(b, .5) - pct(a, .5))
                else:
                    pd = [idx[v][(s, r['id'])]['h'] - r['h'] for r in ga if r['reg'] and idx[v][(s, r['id'])]['reg']]
                    if pd: diffs.append(float(np.mean(pd)))
            m = float(np.mean(diffs)); hw = T14 * float(np.std(diffs, ddof=1)) / math.sqrt(len(diffs))
            sig = '' if abs(m) <= hw else ' ✱'
            cells.append(f'{"+" if m >= 0 else "−"}{f1(abs(m))} ± {f1(hw)}{sig}')
            paired_csv.append([p, v, meas, f'{m:.3f}', f'{hw:.3f}'])
        w(f'| {PNAME[p]} | {meas} | ' + ' | '.join(cells) + ' |')
w('\n✱ = o intervalo de 95% não inclui zero.\n')

# ---------- 6. perfil x ocupacao ----------
w('## 6. Horas reais por perfil × ocupação\n')
w('p10 · mediana · p90 · p99. O p99 só aparece com pelo menos 300 pessoas na célula (abaixo disso ele se apoia em menos de 3 pessoas). Todas as versões e o máximo estão em `perfil-ocupacao.csv`.\n')
occs = sorted({r['occ'] for r in D['A']}, key=lambda o: ONAME.get(o, o))
po_csv = [['versao', 'perfil', 'ocupacao', 'n', 'p10', 'mediana', 'p90', 'p99', 'maximo', 'tempo_livre_mediana_h']]
for v in VERS:
    for p in PROF:
        for o in occs:
            a = [r['h'] for r in D[v] if r['profile'] == p and r['occ'] == o and r['reg']]
            fr = [r['free'] for r in D[v] if r['profile'] == p and r['occ'] == o and r['reg'] and r['free'] == r['free']]
            if not a: continue
            po_csv.append([v, p, o, len(a), f'{pct(a,.1):.2f}', f'{pct(a,.5):.2f}', f'{pct(a,.9):.2f}', f'{pct(a,.99):.2f}', f'{max(a):.2f}', f'{pct(fr,.5):.2f}' if fr else ''])
for p in PROF:
    w(f'**{PNAME[p]}**\n')
    w('| Ocupação | n | Tempo livre (mediana, h/sem) | ' + ' | '.join(VERS) + ' |')
    w('|---|---|---|' + '---|' * len(VERS))
    for o in occs:
        base = [r for r in D['A'] if r['profile'] == p and r['occ'] == o and r['reg']]
        if not base: continue
        fr = [r['free'] for r in base if r['free'] == r['free']]
        cells = []
        for v in VERS:
            a = [r['h'] for r in D[v] if r['profile'] == p and r['occ'] == o and r['reg']]
            p99 = f1(pct(a, .99)) if len(a) >= 300 else '—'
            cells.append(f'{f1(pct(a,.1))} · **{f1(pct(a,.5))}** · {f1(pct(a,.9))} · {p99}')
        w(f'| {ONAME.get(o, o)} | {len(base)} | {f1(pct(fr,.5))} | ' + ' | '.join(cells) + ' |')
    w('')

# ---------- 7. teto ----------
w('## 7. Topo da distribuição e tempo livre\n')
w('Fração do tempo livre (agenda guardada) que virou jogo, por pessoa com conta: mediana, p90 e p99. Um teto escondido apareceria como muita gente parada num mesmo valor abaixo do tempo livre; o máximo observado sozinho não prova nem descarta isso. Também: horas reais dos 5% mais dedicados de cada perfil (maior soma dos postos dos três atributos), contra o tempo livre deles.\n')
w('| Perfil | Versão | Fração jogada (mediana · p90 · p99) | 5% mais dedicados: horas (mediana · p90) | Tempo livre deles (mediana) |')
w('|---|---|---|---|---|')
for p in PROF:
    for v in ('A', 'C'):
        g = [r for r in D[v] if r['profile'] == p and r['reg'] and r['free'] == r['free'] and r['free'] > 0]
        share = [min(1.5, r['h'] / r['free']) * 100 for r in g]
        score = ranks01([r['imp'] for r in g]) + ranks01([r['ses'] for r in g]) + ranks01([r['hab'] for r in g])
        cut = np.percentile(score, 95)
        top = [r for r, sc in zip(g, score) if sc >= cut]
        w(f'| {PNAME[p]} | {v} | {f0(pct(share,.5))}% · {f0(pct(share,.9))}% · {f0(pct(share,.99))}% | {f1(pct([r["h"] for r in top],.5))} · {f1(pct([r["h"] for r in top],.9))} | {f1(pct([r["free"] for r in top],.5))} |')
w('')

# ---------- 8. cadastro ----------
w('## 8. Quem criou conta\n')
w('| Versão | Pessoas | Com conta | Sem conta |')
w('|---|---|---|---|')
for v in VERS:
    n = len(D[v]); reg = sum(r['reg'] for r in D[v])
    w(f'| {v} | {n} | {reg} | {n - reg} |')
w('')

open(f'{OUT}/comparacao.md', 'w', encoding='utf-8', newline='').write('\n'.join(md))
with open(f'{OUT}/perfil-ocupacao.csv', 'w', encoding='utf-8', newline='') as f: csv.writer(f).writerows(po_csv)
with open(f'{OUT}/pareado.csv', 'w', encoding='utf-8', newline='') as f: csv.writer(f).writerows(paired_csv)
print('ok', len(md))
