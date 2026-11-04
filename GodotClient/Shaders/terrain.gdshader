// Terreno procedural do mapa estrategico (apresentacao pura; nao decide nada da simulacao).
// Relevo = soma de gaussianas nas regioes + ruido fbm com oitavas rotacionadas (sem
// artefatos alinhados aos eixos). Sombreamento por gradiente, curvas de nivel sobre o
// relevo suave, litoral com espuma, mar animado, nuvens e ciclo dia/noite.
shader_type canvas_item;

uniform vec2 viewport_size = vec2(1920.0, 1080.0);
uniform vec2 pan = vec2(0.0);
uniform float zoom = 1.0;
uniform int region_count = 0;
// x, y (coordenadas do mapa), sigma (alcance do relevo), indice do bioma
uniform vec4 regions[16];
uniform float daylight : hint_range(0.0, 1.0) = 1.0;
uniform float anim_time = 0.0;

const float COAST = 0.42;
const mat2 ROT = mat2(vec2(0.80, 0.60), vec2(-0.60, 0.80));

vec3 biome_color(int i) {
    if (i == 0) return vec3(0.36, 0.40, 0.25);   // planicie: oliva
    if (i == 1) return vec3(0.17, 0.30, 0.21);   // floresta: verde profundo
    if (i == 2) return vec3(0.37, 0.33, 0.28);   // caverna: rocha
    if (i == 3) return vec3(0.29, 0.27, 0.32);   // ruinas: cinza arroxeado
    return vec3(0.32, 0.33, 0.30);
}

// Hash sem seno (D. Hoskins): estavel na GPU, sem blocos nem cortes retos.
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

float gauss_sum(vec2 m) {
    float h = 0.0;
    for (int i = 0; i < 16; i++) {
        if (i >= region_count) break;
        vec4 r = regions[i];
        float d = length(m - r.xy);
        h += exp(-(d * d) / (2.0 * r.z * r.z));
    }
    return h;
}

// Relevo base (litoral irregular).
float base_height(vec2 m) {
    return gauss_sum(m) + (fbm(m * 0.009, 4) - 0.5) * 0.62;
}

vec3 land_color(vec2 m) {
    vec3 c = vec3(0.0);
    float wsum = 0.0;
    for (int i = 0; i < 16; i++) {
        if (i >= region_count) break;
        vec4 r = regions[i];
        float d = length(m - r.xy);
        float s = r.z * 0.75;
        float w = exp(-(d * d) / (2.0 * s * s)) + 1e-5;
        c += biome_color(int(r.w)) * w;
        wsum += w;
    }
    return c / max(wsum, 1e-5);
}

void fragment() {
    vec2 px = UV * viewport_size;
    vec2 m = (px - pan) / zoom;

    float hb = base_height(m);
    // Detalhe de relevo (so para luz e textura, nao para contornos).
    float detail = (fbm(m * 0.04, 4) - 0.5) * 0.10;
    float h = hb + detail;

    float e = 1.5 / zoom;
    float hx = base_height(m + vec2(e, 0.0)) + (fbm((m + vec2(e, 0.0)) * 0.04, 4) - 0.5) * 0.10;
    float hy = base_height(m + vec2(0.0, e)) + (fbm((m + vec2(0.0, e)) * 0.04, 4) - 0.5) * 0.10;
    vec3 n = normalize(vec3((h - hx) * 60.0 * zoom, (h - hy) * 60.0 * zoom, 1.0));
    vec3 light_dir = normalize(vec3(-0.55, -0.65, 0.70));
    float shade = clamp(dot(n, light_dir), 0.0, 1.0);

    // Mar
    float sea_t = smoothstep(COAST - 0.40, COAST, hb);
    vec3 sea = mix(vec3(0.026, 0.050, 0.075), vec3(0.055, 0.125, 0.165), sea_t);
    float waves = fbm(m * 0.03 + vec2(anim_time * 0.03, anim_time * 0.018), 3);
    sea += (waves - 0.5) * 0.035;
    float swell = sin((hb - COAST) * 95.0 - anim_time * 1.1) * 0.5 + 0.5;
    sea += vec3(0.05, 0.09, 0.10) * smoothstep(COAST - 0.07, COAST, hb) * swell * 0.30;

    // Terra
    float grain = fbm(m * 0.11, 3) * 0.55 + fbm(m * 0.42, 2) * 0.45;
    vec3 land = land_color(m) * (0.80 + 0.40 * grain);
    float elevation = clamp((hb - COAST) * 1.4, 0.0, 1.0);
    land = mix(land, land * 1.18 + vec3(0.025), elevation * 0.45);
    land *= 0.48 + 0.80 * shade;

    // Curvas de nivel sobre um relevo bem suave (sem riscos), mais marcadas a cada 4 niveis.
    float hc = gauss_sum(m) + (fbm(m * 0.009, 2) - 0.5) * 0.62;
    float lev = (hc - COAST) * 9.0;
    float lw = fwidth(lev);
    float line = 1.0 - clamp(abs(fract(lev - 0.5) - 0.5) / max(lw, 1e-4), 0.0, 1.0);
    float major = 1.0 - clamp(abs(fract(lev / 4.0 - 0.5) - 0.5) * 4.0 / max(lw, 1e-4), 0.0, 1.0);
    land = mix(land, land * 0.72, line * 0.40);
    land = mix(land, land * 0.55, major * 0.35);

    float land_mask = smoothstep(COAST - 0.004, COAST + 0.004, hb);
    vec3 col = mix(sea, land, land_mask);

    // Areia e espuma na costa.
    float shore = 1.0 - smoothstep(0.0, 0.03, abs(hb - COAST));
    col = mix(col, vec3(0.62, 0.56, 0.42), shore * 0.45);
    float foam = (1.0 - smoothstep(0.0, 0.012, abs(hb - (COAST - 0.035 - 0.012 * sin(anim_time * 0.8))))) * (1.0 - land_mask);
    col += vec3(0.10, 0.13, 0.14) * foam;

    // Graticula so no mar, muito discreta.
    vec2 g = m / 120.0;
    vec2 gw = fwidth(g);
    vec2 gl = abs(fract(g - 0.5) - 0.5) / max(gw, vec2(1e-4));
    float grid = 1.0 - clamp(min(gl.x, gl.y), 0.0, 1.0);
    col += vec3(0.025, 0.035, 0.045) * grid * (1.0 - land_mask);

    // Sombra de nuvens.
    float cl = fbm(m * 0.004 + vec2(anim_time * 0.006, anim_time * 0.003), 4);
    col *= 1.0 - smoothstep(0.52, 0.80, cl) * 0.25;

    // Dia e noite.
    vec3 night = col * vec3(0.30, 0.36, 0.58);
    col = mix(night, col, daylight);

    // Vinheta.
    vec2 uv = UV - 0.5;
    float vig = smoothstep(0.95, 0.30, length(uv * vec2(1.0, 1.15)));
    col *= mix(0.50, 1.0, vig);

    COLOR = vec4(col, 1.0);
}
