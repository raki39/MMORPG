import os, json, shutil, hashlib
os.chdir(r'C:\Users\Pichau\OneDrive\Área de Trabalho\IdeiaJogo')
SRC = 'Content/presets/inicial'
OUT = 'reports/pe/mundos'
VARS = {'V0': (False, False), 'V1': (True, False), 'V2': (False, True), 'V3': (True, True)}
lines = []
for name, (early, goals) in VARS.items():
    dst = os.path.join(OUT, name)
    if os.path.exists(dst): shutil.rmtree(dst)
    shutil.copytree(SRC, dst)
    p = os.path.join(dst, 'population.json')
    s = open(p, encoding='utf-8').read()
    s = s.replace('"earlyProgression": { "enabled": true,', '"earlyProgression": { "enabled": %s,' % ('true' if early else 'false'))
    s = s.replace('"unlockEnabled": true,', '"unlockEnabled": %s,' % ('true' if goals else 'false'))
    s = s.replace('"contentDiscoveryEnabled": true,', '"contentDiscoveryEnabled": %s,' % ('true' if goals else 'false'))
    d = json.loads(s)
    assert d['earlyProgression']['enabled'] == early and d['goals']['unlockEnabled'] == goals and d['goals']['contentDiscoveryEnabled'] == goals
    open(p, 'w', encoding='utf-8', newline='').write(s)
    lines.append(f'{name} early={early} desbloqueio_e_descoberta={goals} sha256 {hashlib.sha256(s.encode("utf-8")).hexdigest()}')
open(os.path.join(OUT, 'conteudo.txt'), 'w', encoding='utf-8', newline='').write('\n'.join(lines) + '\n')
print('\n'.join(lines))
