using Godot;

namespace GodotClient.Ui;

/// <summary>
/// Submenu da barra superior (D-034): abre ao passar o mouse sobre um item do menu e leva
/// direto a uma aba ou lugar especifico (ex.: Jogadores › Conectados, Zonas › Vila Aurora).
/// </summary>
public partial class MenuDropdown : PanelContainer
{
    public readonly record struct Item(string Icon, string Label, string Hint, Action Action);

    public MenuDropdown(IReadOnlyList<Item> items, Action close)
    {
        MouseFilter = MouseFilterEnum.Stop;
        CustomMinimumSize = new Vector2(270, 0);
        var sb = new StyleBoxFlat
        {
            BgColor = new Color(UiTokens.CardTop, 0.985f), BorderColor = UiTokens.BorderStrong,
            ShadowColor = new Color(0, 0, 0, 0.55f), ShadowSize = 14, ShadowOffset = new Vector2(0, 6),
            ContentMarginLeft = 6, ContentMarginRight = 6, ContentMarginTop = 6, ContentMarginBottom = 6, AntiAliasing = true,
        };
        sb.SetBorderWidthAll(1);
        sb.BorderWidthTop = 2;
        sb.BorderColor = UiTokens.BorderStrong;
        sb.CornerRadiusBottomLeft = sb.CornerRadiusBottomRight = 5;
        AddThemeStyleboxOverride("panel", sb);
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 0);
        AddChild(col);
        col.AddChild(new ColorRect { Color = UiTokens.Accent, CustomMinimumSize = new Vector2(0, 2), MouseFilter = MouseFilterEnum.Ignore });
        foreach (var it in items)
        {
            var item = it;
            var b = ModalFrame.HoverRow(() => { close(); item.Action(); });
            b.CustomMinimumSize = new Vector2(0, 48);
            var r = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            r.SetAnchorsPreset(LayoutPreset.FullRect);
            r.OffsetLeft = 12; r.OffsetRight = -12;
            r.AddThemeConstantOverride("separation", 12);
            r.AddChild(new Icon(item.Icon, 16, UiTokens.AccentSoft));
            var t = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsVertical = SizeFlags.ShrinkCenter, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            t.AddThemeConstantOverride("separation", 0);
            t.AddChild(ThemeFactory.Text(item.Label, UiTokens.FontBody, UiTokens.TextPrimary, UiFonts.Medium));
            t.AddChild(ThemeFactory.Text(item.Hint, UiTokens.FontCaption, UiTokens.TextSecondary));
            r.AddChild(t);
            r.AddChild(new Icon("chevron-right", 14, UiTokens.TextDisabled));
            b.AddChild(r);
            col.AddChild(b);
        }
    }

    public override void _UnhandledKeyInput(InputEvent e)
    {
        if (e is InputEventKey { Pressed: true, Keycode: Key.Escape })
        {
            QueueFree();
            GetViewport().SetInputAsHandled();
        }
    }
}
