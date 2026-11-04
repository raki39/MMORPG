// Terreno do mapa estrategico em ZONAS (apresentacao pura). Cada pixel pertence a zona
// de centro mais proximo (escala unica => celulas que se encaixam, estilo Albion), com
// fronteira nitida, litoral organico, relevo sombreado, tinta da camada ativa, destaque
// da zona selecionada, mar animado, nuvens e ciclo dia/noite.
shader_type canvas_item;

uniform vec2 viewport_size = vec2(1920.0, 1080.0);
uniform vec2 pan = vec2(0.0);
uniform float zoom = 1.0;
uniform int zone_count = 0;
uniform vec4 zones[16];        // x, y, raio, bioma
uniform vec4 zone_tint[16];    // rgb da camada, a = intensidade
uniform vec4 zone_rules[16];   // x = pvp (0 nao, 1 consensual, 2 aberto), y = morte (0 sem, 1 parcial, 2 full loot)
uniform int selected_zone = -1;
uniform int hovered_zone = -1;
uniform float daylight : hint_range(0.0, 1.0) = 1.0;
uniform float anim_time = 0.0;

const mat2 ROT = mat2(vec2(0.80, 0.60), vec2(-0.60, 0.80));

vec3 biome_color(int i) {
    if (i == 0) return vec3(0.38, 0.42, 0.26);   // planicie
    if (i == 1) return vec3(0.18, 0.32, 0.22);   // floresta
    if (i == 2) return vec3(0.38, 0.34, 0.29);   // caverna / montanha
    if (i == 3) return vec3(0.31, 0.28, 0.34);   // ruinas
    return vec3(0.33, 0.34, 0.31);
}

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

// Duas zonas mais proximas (distancia normalizada pelo raio: 1 = borda nominal).
void nearest_zones(vec2 m, out int i1, out float d1, out float d2) {
    i1 = -1;
    d1 = 1e9;
    d2 = 1e9;
    for (int i = 0; i < 16; i++) {
        if (i >= zone_count) break;
        float d = length(m - zones[i].xy) / zones[i].z;
        if (d < d1) { d2 = d1; d1 = d; i1 = i; }
        else if (d < d2) { d2 = d; }
    }
}

float land_value(vec2 m, float d1) {
    // > 0 dentro da terra. Litoral organico em torno do conjunto de zonas.
    return 1.10 + (fbm(m * 0.011, 4) - 0.5) * 0.38 - d1;
}

void fragment() {
    vec2 px = UV * viewport_size;
    vec2 m = (px - pan) / zoom;

    int zi; float d1; float d2;
    nearest_zones(m, zi, d1, d2);
    float lv = land_value(m, d1);
    float land_mask = smoothstep(-0.006, 0.006, lv);

    // Relevo para sombreamento.
    float detail = (fbm(m * 0.045, 4) - 0.5) * 0.10;
    float h = lv * 0.9 + detail;
    float e = 1.5 / zoom;
    int zx; float dx1; float dx2; nearest_zones(m + vec2(e, 0.0), zx, dx1, dx2);
    int zy; float dy1; float dy2; nearest_zones(m + vec2(0.0, e), zy, dy1, dy2);
    float hx = land_value(m + vec2(e, 0.0), dx1) * 0.9 + (fbm((m + vec2(e, 0.0)) * 0.045, 4) - 0.5) * 0.10;
    float hy = land_value(m + vec2(0.0, e), dy1) * 0.9 + (fbm((m + vec2(0.0, e)) * 0.045, 4) - 0.5) * 0.10;
    vec3 n = normalize(vec3((h - hx) * 45.0 * zoom, (h - hy) * 45.0 * zoom, 1.0));
    float shade = clamp(dot(n, normalize(vec3(-0.55, -0.65, 0.70))), 0.0, 1.0);

    // Mar.
    float sea_t = smoothstep(-0.45, 0.0, lv);
    vec3 sea = mix(vec3(0.026, 0.050, 0.075), vec3(0.055, 0.125, 0.165), sea_t);
    sea += (fbm(m * 0.03 + vec2(anim_time * 0.03, anim_time * 0.018), 3) - 0.5) * 0.035;
    float swell = sin(lv * 80.0 - anim_time * 1.1) * 0.5 + 0.5;
    sea += vec3(0.05, 0.09, 0.10) * smoothstep(-0.08, 0.0, lv) * swell * 0.30;

    // Terra: bioma da zona, textura e relevo.
    vec3 base = zi >= 0 ? biome_color(int(zones[zi].w)) : vec3(0.3);
    float grain = fbm(m * 0.11, 3) * 0.55 + fbm(m * 0.42, 2) * 0.45;
    vec3 land = base * (0.80 + 0.40 * grain);
    land *= 0.52 + 0.78 * shade;

    // Curvas de nivel suaves.
    float lev = lv * 7.0;
    float lw = fwidth(lev);
    float contour = 1.0 - clamp(abs(fract(lev - 0.5) - 0.5) / max(lw, 1e-4), 0.0, 1.0);
    land = mix(land, land * 0.78, contour * 0.30);

    // Camada ativa (lotacao, perigo, nivel...).
    if (zi >= 0) land = mix(land, zone_tint[zi].rgb * (0.55 + 0.45 * shade), zone_tint[zi].a);

    // Regra de morte: hachura diagonal (tela) — full loot vermelho forte, parcial laranja sutil.
    float edge = d2 - d1;
    if (zi >= 0) {
        float diag = fract((px.x + px.y) / 15.0);
        float hatch = smoothstep(0.30, 0.42, diag) * (1.0 - smoothstep(0.58, 0.70, diag));
        float death = zone_rules[zi].y;
        if (death > 1.5) {
            land = mix(land, vec3(0.80, 0.14, 0.14), hatch * 0.32);
            land = mix(land, vec3(0.85, 0.15, 0.15), (1.0 - smoothstep(0.0, 0.16, edge)) * 0.45);
        } else if (death > 0.5) {
            land = mix(land, vec3(0.93, 0.55, 0.18), hatch * 0.15);
            land = mix(land, vec3(0.93, 0.55, 0.18), (1.0 - smoothstep(0.0, 0.10, edge)) * 0.22);
        }
    }

    // Fronteiras entre zonas: faixa escura com filete claro, e sombra interna.
    float ew = fwidth(edge);
    float border = 1.0 - smoothstep(0.0, ew * 2.2, edge);
    float inner = 1.0 - smoothstep(0.0, 0.10, edge);
    land *= 1.0 - inner * 0.22;
    land = mix(land, vec3(0.05, 0.05, 0.06), border * 0.85);
    float rim = (1.0 - smoothstep(ew * 2.2, ew * 4.0, edge)) * (1.0 - border);
    land += vec3(0.10, 0.09, 0.07) * rim;

    // Hover e selecao.
    if (zi == hovered_zone) land *= 1.07;
    if (zi == selected_zone) {
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
