# Gera GodotClient/Scripts/Ui/Icons.cs a partir de SVGs do lucide-static 0.469.0 (ISC).
# Uso: baixe os .svg em uma pasta (https://unpkg.com/lucide-static@0.469.0/icons/<nome>.svg)
#      e rode: python tools/gen_icons.py <pasta_svgs> GodotClient/Scripts/Ui/Icons.cs
import sys, os, re
src, out = sys.argv[1], sys.argv[2]
items = []
for f in sorted(os.listdir(src)):
    if not f.endswith('.svg'): continue
    t = open(os.path.join(src, f), encoding='utf-8').read()
    t = re.sub(r'<!--.*?-->', '', t, flags=re.S)
    inner = re.search(r'<svg[^>]*>(.*)</svg>', t, flags=re.S).group(1)
    inner = ' '.join(inner.split())
    items.append((f[:-4], inner.replace('"', '""')))
lines = ['using Godot;', '', 'namespace GodotClient.Ui;', '',
 '/// <summary>',
 '/// Icones Lucide (ISC, Assets/Icons/Lucide-LICENSE.txt) embutidos como SVG e rasterizados no',
 '/// tamanho exato de uso (nitidos em qualquer tamanho), brancos para tingir com Modulate.',
 '/// Arquivo gerado a partir de lucide-static 0.469.0; editar a lista e regenerar.',
 '/// </summary>',
 'public static class Icons', '{',
 '    private static readonly Dictionary<string, string> Svg = new(StringComparer.Ordinal)', '    {']
for n, inner in items:
    lines.append(f'        ["{n}"] = @"{inner}",')
lines += ['    };', '',
 '    private static readonly Dictionary<(string, int, int), Texture2D> Cache = new();', '',
 '    public static bool Has(string name) => Svg.ContainsKey(name) || GameIcons.Svg.ContainsKey(name);', '',
 '    /// <summary>Textura branca do icone com size px; traco visual de ~strokePx pixels.</summary>',
 '    public static Texture2D Get(string name, int size, float strokePx = 1.6f)',
 '    {',
 '        int key = Mathf.RoundToInt(strokePx * 10);',
 '        if (Cache.TryGetValue((name, size, key), out var tex)) return tex;',
 '        // Icones do jogo (GameIcons, "g-...") no mesmo traco; o resto e Lucide.',
    '        if (!Svg.TryGetValue(name, out var inner) && !GameIcons.Svg.TryGetValue(name, out inner)) inner = Svg["circle-alert"];',
 '        float stroke = strokePx * 24f / size;',
 '        string svg = $"<svg xmlns=\\"http://www.w3.org/2000/svg\\" width=\\"24\\" height=\\"24\\" viewBox=\\"0 0 24 24\\" fill=\\"none\\" stroke=\\"#ffffff\\" stroke-width=\\"{stroke.ToString(System.Globalization.CultureInfo.InvariantCulture)}\\" stroke-linecap=\\"round\\" stroke-linejoin=\\"round\\">{inner}</svg>";',
 '        var img = new Image();',
 '        img.LoadSvgFromString(svg, size / 24f);',
 '        tex = ImageTexture.CreateFromImage(img);',
 '        Cache[(name, size, key)] = tex;',
 '        return tex;',
 '    }', '}', '',
 '/// <summary>Icone como controle: tamanho exato, tingido, preso ao pixel.</summary>',
 'public partial class Icon : TextureRect',
 '{',
 '    public Icon(string name, int size, Color color, float strokePx = 1.6f)',
 '    {',
 '        Texture = Icons.Get(name, size, strokePx);',
 '        SelfModulate = color;',
 '        CustomMinimumSize = new Vector2(size, size);',
 '        StretchMode = StretchModeEnum.KeepCentered;',
 '        TextureFilter = TextureFilterEnum.Nearest;',
 '        SizeFlagsVertical = SizeFlags.ShrinkCenter;',
 '        MouseFilter = MouseFilterEnum.Ignore;',
 '    }',
 '}', '']
open(out, 'w', encoding='utf-8', newline='\n').write('\n'.join(lines))
print(len(items))
