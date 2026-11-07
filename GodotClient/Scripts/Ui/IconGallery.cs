using Godot;

namespace GodotClient.Ui;

/// <summary>
/// Galeria dos icones do jogo (D-068, etapa 4), so para conferencia (capturas com --page=icons): cada icone na
/// moldura (64 px), no tamanho de linha (16 px) e com o nome.
/// </summary>
public partial class IconGallery : ModalFrame
{
    public IconGallery() : base(UiTokens.Accent)
    {
        Art.AddChild(new EmblemBox("g-cidade", UiTokens.Accent) { CustomMinimumSize = new Vector2(76, 76) });
        Header.AddChild(TitleText("Ícones do jogo"));
        Header.AddChild(Info("", "Desenhados no traço do Lucide · moldura na cor do tipo", UiTokens.TextSecondary, UiTokens.TextSecondary));
        Breadcrumb = "Conferência";
        AddTab("Todos", Build);
    }

    private static Color ColorOf(string name) => name switch
    {
        "g-guerreiro" or "g-tanque" => PlayerModal.RoleColor(Simulation.Core.Content.ClassRoles.Tank),
        "g-mago" or "g-dano" => PlayerModal.RoleColor(Simulation.Core.Content.ClassRoles.Damage),
        "g-sacerdote" or "g-suporte" => PlayerModal.RoleColor(Simulation.Core.Content.ClassRoles.Support),
        "g-cidade" or "g-principal" => UiTokens.Accent,
        "g-diaria" => UiTokens.Success,
        "g-secundaria" => UiTokens.Info,
        _ when name.StartsWith("g-") => GalleryArea(name),
        _ => UiTokens.TextLabel,
    };

    private static Color GalleryArea(string name)
    {
        foreach (var kind in new[] { "shop", "forge", "tavern", "town_square", "training_ground", "hunting_ground", "ore_vein", "herb_patch", "crystal_node", "landmark", "dungeon", "boss_lair", "chokepoint", "hideout" })
            if (GameIcons.Area(kind) == name) return AreaPalette.Color(kind);
        return name switch
        {
            "g-floresta" => UiTokens.BiomeColor("floresta").Lightened(0.3f),
            "g-caverna" => UiTokens.BiomeColor("caverna").Lightened(0.3f),
            "g-neve" => UiTokens.BiomeColor("neve").Lightened(0.3f),
            "g-pantano" => UiTokens.BiomeColor("pantano").Lightened(0.3f),
            "g-ruinas" => UiTokens.BiomeColor("ruinas").Lightened(0.3f),
            "g-sombrio" => UiTokens.BiomeColor("sombrio").Lightened(0.3f),
            "g-vulcanico" => UiTokens.BiomeColor("vulcanico").Lightened(0.3f),
            "g-planicie" => UiTokens.BiomeColor("planicie").Lightened(0.3f),
            _ => UiTokens.AccentSoft,
        };
    }

    private Control Build()
    {
        var page = Column(12);
        foreach (var (group, names) in GameIcons.Groups)
        {
            var flow = new HFlowContainer();
            flow.AddThemeConstantOverride("h_separation", 18);
            flow.AddThemeConstantOverride("v_separation", 14);
            foreach (var n in names)
            {
                var c = Column(6);
                c.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
                c.CustomMinimumSize = new Vector2(96, 0);
                var row = Row(10, new EmblemBox(n, ColorOf(n)) { CustomMinimumSize = new Vector2(64, 64) }, new Icon(n, 16, ColorOf(n)), new Icon(n, 24, UiTokens.TextPrimary));
                c.AddChild(row);
                c.AddChild(ThemeFactory.Text(n[2..], UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Medium));
                flow.AddChild(c);
            }
            page.AddChild(Card(group, flow));
        }
        return page;
    }
}
