using Godot;
using GodotClient.Sim;

namespace GodotClient.Ui;

/// <summary>
/// Lista de uma aglomeracao no mapa (bolha clicada): titulo, busca e jogadores ordenados.
/// Fecha com Esc, com o X ou ao abrir uma ficha.
/// </summary>
public partial class ClusterPopover : PanelContainer
{
    private readonly ClientContext _ctx;
    private readonly IReadOnlyList<int> _people;
    private readonly VBoxContainer _list;
    private string _filter = "";

    public ClusterPopover(ClientContext ctx, string title, string subtitle, IReadOnlyList<int> people, Vector2 anchor, Vector2 viewport)
    {
        _ctx = ctx;
        _people = people.ToList();
        MouseFilter = MouseFilterEnum.Stop;
        var sb = ThemeFactory.Glass(16, 12);
        sb.BgColor = new Color(UiTokens.Surface, 0.97f);
        sb.BorderColor = new Color(UiTokens.Accent, 0.4f);
        AddThemeStyleboxOverride("panel", sb);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 10);
        AddChild(col);
        var head = new HBoxContainer();
        var titles = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        titles.AddThemeConstantOverride("separation", 0);
        titles.AddChild(ThemeFactory.Text(title, 17, UiTokens.TextPrimary, UiFonts.SemiBold));
        titles.AddChild(ThemeFactory.Text(subtitle, UiTokens.FontSmall, UiTokens.TextSecondary));
        head.AddChild(titles);
        var close = new Button { Text = "✕", FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(32, 32) };
        close.Pressed += QueueFree;
        head.AddChild(close);
        col.AddChild(head);

        var search = new LineEdit { PlaceholderText = "Buscar por nome, classe ou atividade…", ClearButtonEnabled = true };
        search.TextChanged += t => { _filter = t.Trim(); Rebuild(); };
        col.AddChild(search);

        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, CustomMinimumSize = new Vector2(360, Mathf.Min(360, 52 * _people.Count + 4)) };
        _list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _list.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(_list);
        col.AddChild(scroll);
        Rebuild();

        // Posiciona ao lado da bolha, sem sair da tela.
        var size = new Vector2(392, Mathf.Min(360, 52 * _people.Count + 4) + 130);
        var pos = anchor + new Vector2(28, -size.Y / 2f);
        if (pos.X + size.X > viewport.X - 12) pos.X = anchor.X - size.X - 28;
        pos.Y = Mathf.Clamp(pos.Y, 70, viewport.Y - size.Y - 12);
        Position = pos;
        Size = size;
    }

    private void Rebuild()
    {
        foreach (var c in _list.GetChildren()) c.QueueFree();
        var snap = _ctx.Snapshot();
        var rows = _people
            .Select(id => (Id: id, V: id < snap.ByPerson.Length ? snap.ByPerson[id] : null))
            .Where(x => x.V != null)
            .OrderByDescending(x => _ctx.Favorites.Contains(x.Id)).ThenByDescending(x => x.V!.Level).ThenBy(x => x.V!.Name, StringComparer.Ordinal);
        foreach (var (id, v) in rows)
        {
            string cls = _ctx.Content.Classes.TryGetValue(v!.ClassId, out var c) ? c.Name : v.ClassId;
            string hay = $"{v.Name} {_ctx.Persons[id - 1].Name} {cls} {StatusText.Of(v, _ctx.Content)}";
            if (_filter.Length > 0 && hay.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
            int pid = id;
            _list.AddChild(PlayerRow.Build(_ctx, id, v, () => { QueueFree(); _ctx.OpenPerson(pid); }));
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
