"""Variantes do conteudo para a comparacao pareada do D-067 (mesmas pessoas, so as chaves mudam)."""
import io, os, shutil
SRC = r'C:\Users\Pichau\OneDrive\Área de Trabalho\IdeiaJogo\Content\presets\inicial'
OUT = os.path.dirname(os.path.abspath(__file__))
V = {
    #        visita coerente, bloqueio de nivel, aceite avaliado, prazo da diaria pronta (min)
    'base': (False, False, False, 0),
    'v1':   (True,  True,  False, 0),
    'v12':  (True,  True,  True,  0),
    'v123': (True,  True,  True,  30),
}
def sub(s, old, new):
    if s.count(old) != 1: raise SystemExit(f'achei {s.count(old)}x: {old}')
    return s.replace(old, new, 1)
b = lambda x: 'true' if x else 'false'
for name, (cv, lb, ac, dl) in V.items():
    d = os.path.join(OUT, name, 'inicial')
    if os.path.exists(d): shutil.rmtree(d)
    shutil.copytree(SRC, d)
    q = io.open(os.path.join(d, 'quests.json'), encoding='utf-8').read()
    q = sub(q, '"consistentVisits": true', f'"consistentVisits": {b(cv)}')
    q = sub(q, '"acceptance": {\n        "enabled": true', '"acceptance": {\n        "enabled": ' + b(ac))
    q = sub(q, '"deliverDeadlineMarginMinutes": 30', f'"deliverDeadlineMarginMinutes": {dl}')
    io.open(os.path.join(d, 'quests.json'), 'w', encoding='utf-8', newline='').write(q)
    p = io.open(os.path.join(d, 'population.json'), encoding='utf-8').read()
    p = sub(p, '"levelBlockAnyTarget": true', f'"levelBlockAnyTarget": {b(lb)}')
    io.open(os.path.join(d, 'population.json'), 'w', encoding='utf-8', newline='').write(p)
    print(name, d)
