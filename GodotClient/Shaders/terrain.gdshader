// Terreno do mapa estrategico (apresentacao pura, D-028). O mundo e uma grade hexagonal de
// LOTES do tamanho de uma zona: cada lote ocupado e uma zona; lotes livres aparecem como
// terra nao desenvolvida (expansao futura). Fronteiras organicas por deformacao de ruido
// (desligada perto dos portoes), relevo por zona a partir de parametros do conteudo
// (forma, altitude, aspereza, semente), mar ao redor da grade, nuvens e ciclo dia/noite.
shader_type canvas_item;

uniform vec2 viewport_size = vec2(1920.0, 1080.0);
uniform vec2 pan = vec2(0.0);
uniform float zoom = 1.0;
uniform vec4 grid = vec4(0.0, 0.0, 230.0, 200.0);   // origem x, origem y, passo x, passo y
uniform ivec2 grid_dims = ivec2(1, 1);               // colunas, linhas
uniform float zone_radius = 120.0;
uniform sampler2D slot_zone : filter_nearest;        // r*255: 0 = mar, 1 = lote livre, 2+ = indice da zona + 2
uniform vec4 zone_terrain[64];   // forma do relevo, altitude, aspereza, semente
uniform vec4 zone_info[64];      // bioma, pvp (0/1/2), morte (0/1/2), cidade (0/1)
uniform vec4 zone_tint[64];      // rgb da camada, a = intensidade
uniform vec2 gates[128];
uniform int gate_count = 0;
uniform int selected_zone = -1;
uniform int hovered_zone = -1;
uniform int hovered_slot = -1;
uniform float daylight : hint_range(0.0, 1.0) = 1.0;
uniform float anim_time = 0.0;

const mat2 ROT = mat2(vec2(0.80, 0.60), vec2(-0.60, 0.80));

float hash(vec2 p) {
    vec3 p3 = fract(vec3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return fract((p3.x + p3.y) * p3.z);
}

float vnoise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    vec2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
    return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), u.x),
               mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), u.x), u.y);
}

float fbm(vec2 p, int octaves) {
    float v = 0.0;
    float a = 0.5;
    for (int i = 0; i < 6; i++) {
        if (i >= octaves) break;
        v += a * vnoise(p);
        p = ROT * p * 2.02 + vec2(11.7, 5.3);
        a *= 0.5;
    }
    return v;
}

vec3 biome_color(int i) {
    if (i == 0) return vec3(0.40, 0.44, 0.27);   // planicie
    if (i == 1) return vec3(0.17, 0.31, 0.20);   // floresta
    if (i == 2) return vec3(0.40, 0.37, 0.33);   // caverna / rocha
    if (i == 3) return vec3(0.32, 0.29, 0.35);   // ruinas
    if (i == 4) return vec3(0.24, 0.31, 0.23);   // pantano
    if (i == 5) return vec3(0.74, 0.78, 0.83);   // neve
    if (i == 6) return vec3(0.26, 0.19, 0.17);   // vulcanico
    if (i == 7) return vec3(0.21, 0.17, 0.25);   // sombrio
    return vec3(0.33, 0.34, 0.31);
}

vec2 slot_center(int col, int row) {
    float off = (row % 2 == 1) ? grid.z * 0.5 : 0.0;
    return vec2(grid.x + float(col) * grid.z + off, grid.y + float(row) * grid.w);
}

bool in_grid(int col, int row) {
    return col >= 0 && row >= 0 && col < grid_dims.x && row < grid_dims.y;
}

// Valor do lote: 0 mar (ou fora da grade), 1 terra livre, 2+ zona.
int slot_val(int col, int row) {
    if (!in_grid(col, row)) return 0;
    return int(texelFetch(slot_zone, ivec2(col, row), 0).r * 255.0 + 0.5);
}

// Dois lotes mais proximos (inclui mar e fora da grade, para o litoral) e o lote de terra
// mais proximo. Distancias em unidades de mapa.
void nearest_slots(vec2 m, out ivec2 s1, out float d1, out float d2, out float d_in) {
    d1 = 1e9; d2 = 1e9; d_in = 1e9; s1 = ivec2(-99);
    int r0 = int(round((m.y - grid.y) / grid.w));
    for (int dr = -1; dr <= 1; dr++) {
        int row = r0 + dr;
        float off = (abs(row) % 2 == 1) ? grid.z * 0.5 : 0.0;
        int c0 = int(round((m.x - grid.x - off) / grid.z));
        for (int dc = -1; dc <= 1; dc++) {
            int col = c0 + dc;
            vec2 c = vec2(grid.x + float(col) * grid.z + off, grid.y + float(row) * grid.w);
            float d = length(m - c);
            if (d < d1) { d2 = d1; d1 = d; s1 = ivec2(col, row); }
            else if (d < d2) { d2 = d; }
            if (slot_val(col, row) >= 1) d_in = min(d_in, d);
        }
    }
}

// Relevo da zona (0..~1.2). Formas: 0 planicie, 1 colinas, 2 montanhas, 3 vale, 4 planalto,
// 5 brejo, 6 terras vulcanicas. Nada e centrado no meio da zona.
float relief(vec4 t, vec2 m, out float water, out float crack) {
    water = 0.0; crack = 0.0;
    int kind = int(t.x + 0.5);
    float elev = t.y;
    float rough = t.z;
    vec2 p = m + vec2(t.w * 37.17, t.w * 91.73);
    float f = 0.0045 + 0.0085 * rough;
    float h;
    if (kind == 0) {
        h = 0.22 * fbm(p * f, 3);
    } else if (kind == 1) {
        h = 0.65 * fbm(p * f * 1.3, 4);
    } else if (kind == 2) {
        float n = fbm(p * f, 4);
        float ridge = pow(1.0 - abs(2.0 * n - 1.0), 2.0);
        h = ridge * 0.95 + 0.25 * fbm(p * f * 2.7, 3);
    } else if (kind == 3) {
        float n = fbm(p * 0.0065, 3);
        float river = smoothstep(0.0, 0.10, abs(n - 0.5));
        water = 1.0 - smoothstep(0.0, 0.018, abs(n - 0.5));
        h = 0.45 * river + 0.25 * fbm(p * f, 3);
    } else if (kind == 4) {
        float n = fbm(p * f * 0.9, 4);
        h = smoothstep(0.44, 0.52, n) * 0.65 + 0.20 * n;
    } else if (kind == 5) {
        float n = fbm(p * 0.020, 3);
        water = 1.0 - smoothstep(0.38, 0.43, n);
        h = 0.10 * fbm(p * f, 3);
    } else {
        float n = fbm(p * f * 1.5, 5);
        h = 0.85 * abs(n - 0.5) * 2.0 + 0.1;
        crack = 1.0 - smoothstep(0.0, 0.025, abs(fbm(p * 0.035, 3) - 0.5));
    }
    return h * (0.30 + 0.95 * elev);
}

void fragment() {
    vec2 px = UV * viewport_size;
    vec2 m = (px - pan) / zoom;

    // Fronteiras organicas: deformacao por ruido, desligada perto dos portoes (as estradas
    // atravessam a fronteira no ponto certo).
    float gd = 1e9;
    for (int i = 0; i < 128; i++) {
        if (i >= gate_count) break;
        gd = min(gd, length(m - gates[i]));
    }
    vec2 warp = vec2(fbm(m * 0.0085 + vec2(3.1, 7.7), 3), fbm(m * 0.0085 + vec2(9.2, 1.3), 3)) - 0.5;
    vec2 mw = m + warp * 80.0 * smoothstep(6.0, 34.0, gd);

    ivec2 s; float d1; float d2; float d_in;
    nearest_slots(mw, s, d1, d2, d_in);
    int sv = slot_val(s.x, s.y);
    bool inside = sv >= 1;
    int zi = sv - 2;
    int slot_index = inside ? s.y * grid_dims.x + s.x : -1;
    float edge = (d2 - d1) / zone_radius;

    // Litoral organico em volta dos lotes de terra (a mascara da forma a ilha).
    float lv = 1.12 + (fbm(m * 0.011, 4) - 0.5) * 0.38 - d_in / zone_radius;
    if (inside) lv = max(lv, 0.10);
    float land_mask = smoothstep(-0.006, 0.006, lv);

    // Relevo: parametros da zona (lote livre: colinas baixas com semente do lote).
    vec4 tz = zi >= 0 ? zone_terrain[zi] : vec4(1.0, 0.25, 0.4, float(slot_index) * 7.0 + 3.0);
    float water; float crack;
    float h = relief(tz, m, water, crack);
    // Normal por diferencas finitas em unidades de mapa (derivadas de tela ficavam em blocos).
    float e = max(0.8, 1.5 / zoom);
    float w2; float c2;
    float sx = (relief(tz, m + vec2(e, 0.0), w2, c2) - h) / e;
    float sy = (relief(tz, m + vec2(0.0, e), w2, c2) - h) / e;
    h += (fbm(m * 0.06, 3) - 0.5) * 0.05;
    vec3 n = normalize(vec3(-sx * 70.0, -sy * 70.0, 1.0));
    float shade = clamp(dot(n, normalize(vec3(-0.55, -0.65, 0.70))), 0.0, 1.0);

    // Mar.
    float sea_t = smoothstep(-0.45, 0.0, lv);
    vec3 sea = mix(vec3(0.026, 0.050, 0.075), vec3(0.055, 0.125, 0.165), sea_t);
    sea += (fbm(m * 0.03 + vec2(anim_time * 0.03, anim_time * 0.018), 3) - 0.5) * 0.035;
    float swell = sin(lv * 80.0 - anim_time * 1.1) * 0.5 + 0.5;
    sea += vec3(0.05, 0.09, 0.10) * smoothstep(-0.08, 0.0, lv) * swell * 0.30;

    // Terra.
    int biome = zi >= 0 ? int(zone_info[zi].x + 0.5) : 0;
    vec3 base = zi >= 0 ? biome_color(biome) : vec3(0.27, 0.28, 0.24);
    float grain = fbm(m * 0.11, 3) * 0.55 + fbm(m * 0.42, 2) * 0.45;
    vec3 land = base * (0.80 + 0.40 * grain);
    // Altitude clareia (rocha) e, em montanhas altas ou neve, cobre de branco.
    land = mix(land, vec3(0.52, 0.50, 0.47), smoothstep(0.75, 1.15, h) * 0.55);
    // Neve: so no bioma de neve (mais nas partes altas) ou em montanhas muito altas.
    float snow = biome == 5 ? 0.35 + smoothstep(0.25, 0.75, h) * 0.55
               : smoothstep(1.02, 1.20, h) * step(0.85, tz.y);
    land = mix(land, vec3(0.86, 0.89, 0.93), clamp(snow, 0.0, 1.0));
    land *= 0.55 + 0.75 * shade;
    // Agua de rios e pocas.
    land = mix(land, vec3(0.07, 0.16, 0.19) * (0.8 + 0.4 * shade), water * 0.85);
    // Fendas de lava.
    land = mix(land, vec3(1.0, 0.42, 0.10) * (0.7 + 0.3 * sin(anim_time * 2.0 + m.x * 0.05)), crack * (biome == 6 ? 0.85 : 0.0));

    // Curvas de nivel suaves, mais visiveis em relevo alto.
    float lev = h * 9.0;
    float lw = fwidth(lev);
    float contour = 1.0 - clamp(abs(fract(lev - 0.5) - 0.5) / max(lw, 1e-4), 0.0, 1.0);
    land = mix(land, land * 0.80, contour * 0.22 * smoothstep(0.1, 0.6, tz.y));

    // Lote livre: terra nao desenvolvida, com nevoa.
    if (zi < 0 && inside) {
        float lum = dot(land, vec3(0.30, 0.59, 0.11));
        land = mix(land, vec3(lum) * vec3(0.94, 0.97, 1.0), 0.55) * 0.82;
        float fog = fbm(m * 0.006 + vec2(anim_time * 0.010, -anim_time * 0.006), 4);
        land = mix(land, vec3(0.16, 0.18, 0.21), smoothstep(0.35, 0.75, fog) * 0.45);
        if (slot_index == hovered_slot) land = mix(land * 1.25, vec3(0.91, 0.71, 0.31), (1.0 - smoothstep(0.0, 0.10, edge)) * 0.35);
    }

    // Camada ativa (lotacao, perigo, nivel...).
    if (zi >= 0) land = mix(land, zone_tint[zi].rgb * (0.55 + 0.45 * shade), zone_tint[zi].a);

    // Regra de morte: hachura diagonal (tela).
    if (zi >= 0) {
        float diag = fract((px.x + px.y) / 15.0);
        float hatch = smoothstep(0.30, 0.42, diag) * (1.0 - smoothstep(0.58, 0.70, diag));
        float death = zone_info[zi].z;
        if (death > 1.5) {
            land = mix(land, vec3(0.80, 0.14, 0.14), hatch * 0.32);
            land = mix(land, vec3(0.85, 0.15, 0.15), (1.0 - smoothstep(0.0, 0.16, edge)) * 0.45);
        } else if (death > 0.5) {
            land = mix(land, vec3(0.93, 0.55, 0.18), hatch * 0.15);
            land = mix(land, vec3(0.93, 0.55, 0.18), (1.0 - smoothstep(0.0, 0.10, edge)) * 0.22);
        }
    }

    // Fronteiras: zona com faixa escura e filete claro; lote livre com tracejado discreto.
    float ew = fwidth(edge);
    float border = 1.0 - smoothstep(0.0, ew * 2.2, edge);
    if (zi >= 0) {
        float inner = 1.0 - smoothstep(0.0, 0.10, edge);
        land *= 1.0 - inner * 0.22;
        land = mix(land, vec3(0.05, 0.05, 0.06), border * 0.85);
        float rim = (1.0 - smoothstep(ew * 2.2, ew * 4.0, edge)) * (1.0 - border);
        land += vec3(0.10, 0.09, 0.07) * rim;
    } else if (inside) {
        float dash = step(0.5, fract((mw.x * 0.7 + mw.y * 0.7) / 14.0));
        land = mix(land, vec3(0.62, 0.62, 0.58), border * dash * 0.35);
    }

    // Hover e selecao.
    if (zi >= 0 && zi == hovered_zone) land *= 1.07;
    if (zi >= 0 && zi == selected_zone) {
        land *= 1.12;
        float glow = 1.0 - smoothstep(0.0, 0.12, edge);
        float pulse = 0.75 + 0.25 * sin(anim_time * 3.0);
        land = mix(land, vec3(0.91, 0.71, 0.31), glow * 0.55 * pulse);
    }

    vec3 col = mix(sea, land, land_mask);

    // Areia e espuma.
    float shore = 1.0 - smoothstep(0.0, 0.03, abs(lv));
    col = mix(col, vec3(0.62, 0.56, 0.42), shore * 0.45);
    float foam = (1.0 - smoothstep(0.0, 0.012, abs(lv + 0.035 + 0.012 * sin(anim_time * 0.8)))) * (1.0 - land_mask);
    col += vec3(0.10, 0.13, 0.14) * foam;

    // Nuvens.
    float cl = fbm(m * 0.004 + vec2(anim_time * 0.006, anim_time * 0.003), 4);
    col *= 1.0 - smoothstep(0.52, 0.80, cl) * 0.22;

    // Dia e noite.
    col = mix(col * vec3(0.52, 0.58, 0.80), col, daylight);

    // Vinheta.
    vec2 uv = UV - 0.5;
    col *= mix(0.50, 1.0, smoothstep(0.95, 0.30, length(uv * vec2(1.0, 1.15))));

    COLOR = vec4(col, 1.0);
}
