"""P4d: aquisicao e perda. A0 = aquisicao desligada (= efeitos da P4c), A1 = ligada; mesmas pessoas."""
import os, csv, re, collections, statistics as st, json
os.chdir(r'C:\Users\Pichau\OneDrive\Área de Trabalho\IdeiaJogo')
def load(v): return {(r['seed'], r['person_id']): r for r in csv.DictReader(open(f'reports/p4d/val/{v}/people.csv', encoding='utf-8')) if r['registered'] == '1'}
A, B = load('A0'), load('A1')
keys = sorted(set(A) & set(B))
cat = {t['id']: t for t in json.load(open('Content/presets/inicial/traits.json', encoding='utf-8'))['traits']}
out = []; w = out.append
same = open('reports/p4d/val/A0/validation.csv', encoding='utf-8').read() == open('reports/p4c/val/Q1/validation.csv', encoding='utf-8').read()
w(f'- A0 (aquisição desligada) igual à rodada da P4c com os efeitos ligados: {same}')
def tr(r): return [x for x in r['traits'].split('|') if x]
def hist(r): return [h for h in r['quirk_history'].split('|') if h]
def ev(r):
    d = {}
    for item in r['quirk_evidence'].split('|'):
        if not item: continue
        parts = item.split(':'); d[parts[0]] = {p[:2] if p[:2] in ('op', 'ef') else p[0]: int(p[2:] if p[:2] in ('op', 'ef') else p[1:]) for p in parts[1:]}
    return d
n = len(keys)
w(f'- Pessoas com conta: {n}; dias jogados por pessoa em 90 dias: mediana {st.median(int(B[k]["play_days"]) for k in keys):.0f}')
for q in ('persistente', 'rancoroso'):
    initial_holders = [k for k in keys if q in tr(A[k])]
    changes = [(k, h) for k in keys for h in hist(B[k]) if h.startswith(q + ':')]
    acq = [c for c in changes if ':acquired@' in c[1]]
    lost = [c for c in changes if ':lost@' in c[1]]
    lost_initial = [c for c in lost if c[1].endswith(':innate')]
    w(f'\n### {cat[q]["name"]}')
    w(f'- Com a trait inicial: {len(initial_holders)} ({len(initial_holders) * 100 / n:.1f}%)')
    w(f'- Adquiriram: {len(acq)} ({len(acq) * 100 / n:.2f}% das pessoas); perderam: {len(lost)} (iniciais: {len(lost_initial)})')
    # oscilacao: adquiriu e perdeu (ou o contrario) a mesma trait
    per = collections.defaultdict(list)
    for k, h in changes: per[k].append(h)
    w(f'- Mudaram duas vezes ou mais: {sum(1 for v in per.values() if len(v) >= 2)}')
    # evidencia: oportunidades, a favor, contra e so pelo efeito, por grupo (tem a trait no fim x nao tem)
    for label, group in (('tem a trait (fim)', [k for k in keys if q in tr(B[k])]), ('não tem', [k for k in keys if q not in tr(B[k])])):
        e = [ev(B[k]).get(q, {}) for k in group]
        if not group: continue
        tot = lambda key: sum(x.get(key, 0) for x in e)
        w(f'- {label} ({len(group)}): oportunidades {tot("op") / len(group):.2f} por pessoa; a favor {tot("a") / len(group):.2f}; contra {tot("c") / len(group):.2f}; só pelo efeito {tot("ef") / len(group):.2f}; pessoas com alguma oportunidade {sum(1 for x in e if x.get("op", 0) > 0) * 100 / len(group):.1f}%')
    if acq:
        days = [int(h.split('@')[1].split(':')[0]) for _, h in acq]
        w(f'- Dia jogado da aquisição: mediana {st.median(days):.0f} (mín. {min(days)}, máx. {max(days)})')
    # exclusoes depois das mudancas
    bad = 0
    for k in keys:
        ids = tr(B[k])
        for x in ids:
            t = cat[x]
            if B[k]['personality'] in t.get('excludesPersonalities', []): bad += 1
            for y in ids:
                if y != x and y in t.get('excludes', []): bad += 1
    w(f'- Combinações excluídas depois das mudanças: {bad}')
# validacao
def parse(p):
    d = {}
    for line in open(p, encoding='utf-8'):
        m = re.match(r'\|\s*(✅|❌|⚠️)[^|]*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|', line)
        if m: d[m.group(2)] = (m.group(1), m.group(3), m.group(4), m.group(5))
    return d
va, vb = parse('reports/p4d/val/A0/validation.md'), parse('reports/p4d/val/A1/validation.md')
w('\n## Validação (A0 → A1), só o que mudou\n')
diff = [f'- {k}: {va[k][0]} {va[k][2]} → {vb[k][0]} {vb[k][2]}' for k in va if k in vb and (va[k][0] != vb[k][0] or va[k][2] != vb[k][2])]
w('\n'.join(diff) if diff else '- nada mudou')
print('\n'.join(out))
open('reports/p4d/analise.md', 'w', encoding='utf-8', newline='').write('# P4d — aquisição e perda: medição\n\n' + '\n'.join(out) + '\n')
