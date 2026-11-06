import io, os, csv, json
from collections import Counter
os.chdir(r'C:\Users\Pichau\OneDrive\Área de Trabalho\IdeiaJogo')
cat = {o['id']: o for o in json.load(io.open('Content/presets/inicial/objectives.json', encoding='utf-8'))['objectives']}
NOW = {'max_level', 'main_campaign', 'all_zones'}
B = list(csv.reader(io.open('docs/baselines/objetivos/O1/people.csv', encoding='utf-8')))
h = B[0]; ix = {k: i for i, k in enumerate(h)}
rows = [r for r in B[1:] if r[ix['registered']] == '1']
def objs(r): return [x.split(':') for x in r[ix['objectives']].split('|') if x]
def has(r, oid): return any(o[0] == oid for o in objs(r))
def thirds(key):
    s = sorted(rows, key=lambda r: int(r[ix[key]])); t = len(s) // 3
    return s[:t], s[-t:]
def rate(g, oid): return sum(has(r, oid) for r in g) / len(g)
for key, oid in [('pref_exploration', 'conhecer_o_mundo'), ('pref_pvp', 'pvp_competitivo'), ('pref_social', 'entrar_guilda'),
                 ('pref_crafting', 'melhor_fabricante'), ('pref_trade', 'grande_fortuna'), ('t_competitiveness', 'mais_forte'),
                 ('t_competitiveness', 'grande_comerciante'), ('pref_pve', 'grandes_desafios_pve')]:
    lo, hi = thirds(key)
    print(f'{oid:22s} por {key:18s}: terço baixo {rate(lo, oid):5.1%}  alto {rate(hi, oid):5.1%}')
print()
for prof in ['casual', 'regular', 'hardcore', 'devoto']:
    g = [r for r in rows if r[ix['profile']] == prof]
    gens = [o for r in g for o in objs(r) if o[2].startswith('g')]
    hard = sum(cat[o[0]]['difficulty'] >= 850 for o in gens) / len(gens)
    avg = sum(cat[o[0]]['difficulty'] for o in gens) / len(gens)
    nowp = sum(any(cat[o[0]]['criterion'] in NOW for o in objs(r) if o[2].startswith('g')) for r in g) / len(g)
    print(f'{prof:9s} n={len(g):4d} dificuldade média dos gerais {avg:5.0f}; gerais de fim de jogo (>=850) {hard:5.1%}; com geral que pode avançar agora {nowp:5.1%}')
allnow = sum(any(cat[o[0]]['criterion'] in NOW for o in objs(r)) for r in rows) / len(rows)
gnow = sum(any(cat[o[0]]['criterion'] in NOW for o in objs(r) if o[2].startswith('g')) for r in rows) / len(rows)
print(f'\ncom algum objetivo que pode avançar agora: {allnow:.1%}; com geral que pode avançar agora: {gnow:.1%}')
both = sum(has(r, 'entrar_guilda') and has(r, 'fundar_guilda') for r in rows) / len(rows)
print(f'entrar e fundar guilda juntos: {both:.1%}')
