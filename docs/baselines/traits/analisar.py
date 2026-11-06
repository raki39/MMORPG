"""P4b: (1) P3 (traits ligadas) e identica a P2 (sem traits) na simulacao? (2) distribuicao das traits."""
import os, csv, collections, json
os.chdir(r'C:\Users\Pichau\OneDrive\Área de Trabalho\IdeiaJogo')
out = []; w = out.append
# (1) igualdade
a = open('reports/p4a/val/P2/validation.csv', encoding='utf-8').read()
b = open('reports/p4b/val/P3/validation.csv', encoding='utf-8').read()
w(f'validation.csv identico (P2 x P3): {a == b}')
pa = list(csv.DictReader(open('reports/p4a/val/P2/people.csv', encoding='utf-8')))
pb = list(csv.DictReader(open('reports/p4b/val/P3/people.csv', encoding='utf-8')))
cols = [c for c in pa[0].keys()]
diff = sum(1 for x, y in zip(pa, pb) if any(x[c] != y[c] for c in cols))
w(f'people.csv: {len(pa)} x {len(pb)} linhas; linhas com alguma coluna comum diferente: {diff}')
# (2) distribuicao (todas as pessoas geradas, com ou sem conta)
cat = {t['id']: t for t in json.load(open('Content/presets/inicial/traits.json', encoding='utf-8'))['traits']}
people = pb
n = len(people)
counts = collections.Counter(len([x for x in r['traits'].split('|') if x]) for r in people)
w(f'\n== Quantidade de traits por pessoa (n = {n}, 5 seeds x 1.000) ==')
table = [8, 25, 50, 10, 4, 2, 1]
w('| Traits | Tabela | Medido |'); w('|---|---|---|')
for k in range(7): w(f'| {k} | {table[k]}% | {counts.get(k, 0) * 100 / n:.1f}% |')
allq = [x for r in people for x in r['traits'].split('|') if x]
neg = sum(1 for x in allq if cat[x]['polarity'] == 'negative')
w(f'\nTraits sorteadas: {len(allq)}; negativas {neg * 100 / len(allq):.1f}% (chance por vaga: 40%)')
two = [r for r in people if len([x for x in r['traits'].split('|') if x]) == 2]
def pol(r): return [cat[x]['polarity'] for x in r['traits'].split('|') if x]
w(f'Com 2 traits: so positivas {sum(1 for r in two if pol(r).count("positive") == 2) * 100 / len(two):.1f}%, uma de cada {sum(1 for r in two if pol(r).count("positive") == 1) * 100 / len(two):.1f}%, so negativas {sum(1 for r in two if pol(r).count("negative") == 2) * 100 / len(two):.1f}% (esperado 36, 48 e 16%)')
freq = collections.Counter(allq)
w('\n== Frequencia de cada trait (% das pessoas) ==')
w('| Trait | +/- | Sistema | % |'); w('|---|---|---|---|')
for tid, c in sorted(freq.items(), key=lambda kv: -kv[1]):
    t = cat[tid]
    w(f"| {t['name']} | {'+' if t['polarity'] == 'positive' else '-'} | {t.get('system', '') or 'pessoa'} | {c * 100 / n:.1f} |")
# coerencia: taxa por personalidade e perfil
def rate(tid, f):
    g = [r for r in people if f(r)]
    return sum(1 for r in g if tid in r['traits'].split('|')) * 100 / max(1, len(g))
w('\n== Coerencia (taxa da trait no grupo, %) ==')
for tid, groups in (
    ('pavio_curto', [('Agressivo', lambda r: r['personality_initial'] == 'agressivo'), ('Pacifico', lambda r: r['personality_initial'] == 'pacifico'), ('todos', lambda r: True)]),
    ('provocador', [('Agressivo', lambda r: r['personality_initial'] == 'agressivo'), ('Pacifico (excluido)', lambda r: r['personality_initial'] == 'pacifico'), ('todos', lambda r: True)]),
    ('maratonista', [('Devoto', lambda r: r['profile'] == 'devoto'), ('Hardcore', lambda r: r['profile'] == 'hardcore'), ('Regular', lambda r: r['profile'] == 'regular'), ('Casual', lambda r: r['profile'] == 'casual')]),
    ('lobo_solitario', [('Reservado', lambda r: r['personality_initial'] == 'reservado'), ('Sociavel (excluido)', lambda r: r['personality_initial'] == 'sociavel'), ('todos', lambda r: True)]),
    ('observador', [('pref. Exploracao >= 700', lambda r: int(r['pref_exploration']) >= 700), ('<= 300', lambda r: int(r['pref_exploration']) <= 300)]),
    ('otimizador', [('Competitividade >= 700', lambda r: int(r['t_competitiveness']) >= 700), ('<= 300', lambda r: int(r['t_competitiveness']) <= 300)]),
    ('persistente', [('Paciente', lambda r: r['personality_initial'] == 'paciente'), ('Leal (excluido)', lambda r: r['personality_initial'] == 'leal'), ('Inquieto (excluido)', lambda r: r['personality_initial'] == 'inquieto'), ('todos', lambda r: True)]),
):
    w(f"- {cat[tid]['name']}: " + '; '.join(f'{name} {rate(tid, f):.1f}' for name, f in groups))
# exclusoes
bad = 0
for r in people:
    ids = [x for x in r['traits'].split('|') if x]
    for x in ids:
        t = cat[x]
        if r['personality_initial'] in t.get('excludesPersonalities', []): bad += 1
        for y in ids:
            if y != x and y in t.get('excludes', []): bad += 1
w(f'\nCombinacoes excluidas encontradas: {bad}')
print('\n'.join(out))
os.makedirs('reports/p4b', exist_ok=True)
open('reports/p4b/analise.md', 'w', encoding='utf-8', newline='').write('# P4b — traits: medição\n\n' + '\n'.join(out) + '\n')
