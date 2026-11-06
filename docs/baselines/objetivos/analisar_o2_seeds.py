import io, os, csv, sys, json
os.chdir(r'C:\Users\Pichau\OneDrive\Área de Trabalho\IdeiaJogo')
base = sys.argv[1] if len(sys.argv) > 1 else 'docs/baselines/objetivos/O2'
def load(run):
    rows = list(csv.reader(io.open(f'{base}/{run}/people.csv', encoding='utf-8')))
    return [dict(zip(rows[0], r)) for r in rows[1:]]
OFF, ON = load('desligada'), load('ligada')
def gt(r): return sum(int(kv.split(':')[1]) for kv in r['goals_by_type'].split('|') if kv)
def metrics(rows):
    reg = [r for r in rows if r['registered'] == '1']
    tot = sum(gt(r) for r in reg); lk = sum(int(r['goals_linked_set']) for r in reg)
    played = sum(float(r['min_played']) for r in reg)
    return {
        '% metas ligadas': 100 * lk / tot,
        'nível': sum(float(r['level']) for r in reg) / len(reg),
        'satisfação': sum(float(r['satisfaction']) for r in reg) / len(reg),
        '% tempo socializar': 100 * sum(float(r['min_socialize']) for r in reg) / played,
        '% tempo caça': 100 * sum(float(r['min_hunt']) for r in reg) / played,
    }
print('| Medida | ' + ' | '.join(f'seed {s}' for s in range(1, 6)) + ' |')
print('|---|' + '---|' * 5)
rows = {}
for s in range(1, 6):
    a = metrics([r for r in OFF if r['seed'] == str(s)]); b = metrics([r for r in ON if r['seed'] == str(s)])
    for k in a: rows.setdefault(k, []).append(b[k] - a[k])
for k, v in rows.items():
    print(f'| {k} | ' + ' | '.join(f'{x:+.2f}' for x in v) + ' |')
# conhecer o mundo: quem tem e quem concluiu
has = [r for r in ON if r['registered'] == '1' and 'conhecer_o_mundo:' in r['objectives']]
done = [r for r in has if 'conhecer_o_mundo@' in r['objectives_completed']]
days = sorted(int(x.split('@')[1]) for r in done for x in r['objectives_completed'].split('|') if x.startswith('conhecer_o_mundo@'))
print(f'\nconhecer_o_mundo: {len(done)} de {len(has)} ({100*len(done)/len(has):.0f}%) concluíram; dia mediano {days[len(days)//2]}')
for oid in ['nivel_maximo', 'campanha_principal']:
    h = [r for r in ON if r['registered'] == '1' and oid + ':' in r['objectives']]
    d = [r for r in h if oid + '@' in r['objectives_completed']]
    ds = sorted(int(x.split('@')[1]) for r in d for x in r['objectives_completed'].split('|') if x.startswith(oid + '@'))
    print(f'{oid}: {len(d)} de {len(h)} ({100*len(d)/len(h):.0f}%) concluíram; dia mediano {ds[len(ds)//2] if ds else "-"}')
# validacao: maiores diferencas
va = list(csv.reader(io.open(f'{base}/desligada/validation.csv', encoding='utf-8'))); vb = list(csv.reader(io.open(f'{base}/ligada/validation.csv', encoding='utf-8')))
print('\nvalidation.csv cabeçalho:', va[0][:8])
