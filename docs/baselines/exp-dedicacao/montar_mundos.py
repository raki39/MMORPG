"""Monta os mundos do experimento de dedicacao em reports/exp-ded/mundos/<versao>/.

A  = preset atual, sem mudanca (referencia)
B0 = triangular equivalente aos valores atuais, sem dedicacao compartilhada
B  = triangular equivalente aos valores atuais, influencia 450
C0 = triangular com os valores candidatos, sem dedicacao compartilhada
C  = triangular com os valores candidatos, influencia 450
"""
import os, json, shutil, hashlib
os.chdir(r'C:\Users\Pichau\OneDrive\Área de Trabalho\IdeiaJogo')
SRC = 'Content/presets/inicial'
OUT = 'reports/exp-ded/mundos'
DED = {'sessionMinutes': (10, 720), 'gameImportance': (0, 1000), 'habit': (0, 1000)}
VOC = ('social', 'explorador', 'comerciante', 'competitivo')
CAND = {
    'casual':   {'gameImportance': (150, 300, 800), 'sessionMinutes': (30, 70, 240), 'habit': (100, 350, 850)},
    'vocacoes': {'gameImportance': (250, 600, 950), 'sessionMinutes': (50, 130, 330), 'habit': (200, 600, 950)},
    'hardcore': {'gameImportance': (500, 900, 1000), 'sessionMinutes': (90, 260, 420), 'habit': (400, 850, 1000)},
    'devoto':   {'gameImportance': (800, 970, 1000), 'sessionMinutes': (180, 360, 600), 'habit': (650, 950, 1000)},
}

def equivalent(t, lo, hi):
    h = t['spread'] * 816 // 1000          # mesma conta do teste: desvio padrao igual ao atual
    return {'min': max(lo, t['mean'] - h), 'mode': t['mean'], 'max': min(hi, t['mean'] + h)}

def build(name, kind, influence):
    dst = os.path.join(OUT, name)
    if os.path.exists(dst): shutil.rmtree(dst)
    shutil.copytree(SRC, dst)
    if kind == 'A': return dst
    p = os.path.join(dst, 'population.json')
    d = json.load(open(p, encoding='utf-8'))
    for pr in d['profiles']:
        for key, (lo, hi) in DED.items():
            if kind == 'B':
                pr[key] = equivalent(pr[key], lo, hi)
            else:
                group = 'vocacoes' if pr['id'] in VOC else pr['id']
                a, c, b = CAND[group][key]
                pr[key] = {'min': a, 'mode': c, 'max': b}
    if influence > 0: d['dedication'] = {'influencePermille': influence}
    open(p, 'w', encoding='utf-8', newline='').write(json.dumps(d, ensure_ascii=False, indent=2))
    return dst

worlds = {'A': build('A', 'A', 0), 'B0': build('B0', 'B', 0), 'B': build('B', 'B', 450),
          'C0': build('C0', 'C', 0), 'C': build('C', 'C', 450)}

# Metas sem as regras do cenario: as seeds extras so servem para dados por pessoa (sem rodada neutra).
t = json.load(open(os.path.join(SRC, 'targets.json'), encoding='utf-8'))
kinds = sorted({x.get('kind') for x in t['targets']}, key=str)
t['targets'] = [x for x in t['targets'] if x.get('kind') != 'scenario']
open('reports/exp-ded/metas-sem-cenario.json', 'w', encoding='utf-8', newline='').write(json.dumps(t, ensure_ascii=False, indent=2))

with open('reports/exp-ded/mundos/conteudo.txt', 'w', encoding='utf-8', newline='') as f:
    for k, v in worlds.items():
        h = hashlib.sha256(open(os.path.join(v, 'population.json'), 'rb').read()).hexdigest()
        f.write(f'{k} population.json sha256 {h}\n')
print('ok', kinds, len(t['targets']))
print(open('reports/exp-ded/mundos/conteudo.txt', encoding='utf-8').read())
