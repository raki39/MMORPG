// Marcador de pessoa no mapa (instancias de MultiMesh): esfera pequena com contorno escuro,
// sombreamento e brilho especular. A cor vem da instancia (perfil).
shader_type canvas_item;

void fragment() {
    vec2 p = UV * 2.0 - 1.0;
    float d = length(p);
    float aa = fwidth(d) * 1.5;
    float body = 1.0 - smoothstep(0.74 - aa, 0.74, d);
    float outline = 1.0 - smoothstep(1.0 - aa, 1.0, d);
    vec3 base = COLOR.rgb;
    // Sombreamento esferico com luz vindo de cima-esquerda.
    float light = clamp(1.0 - length(p - vec2(-0.30, -0.35)) * 0.75, 0.0, 1.0);
    vec3 shaded = base * (0.62 + 0.55 * light);
    float spec = 1.0 - smoothstep(0.0, 0.26, length(p - vec2(-0.28, -0.32)));
    shaded += vec3(spec * 0.45);
    vec3 col = mix(vec3(0.03, 0.04, 0.05), shaded, body);
    COLOR = vec4(col, outline * COLOR.a);
}
