using Godot;

namespace GodotClient.Ui;

/// <summary>Coluna de tabela: titulo, largura (0 = expande), texto, ordenacao, cor e tooltip por celula.</summary>
public sealed class Column<T>
{
    public string Title { get; init; } = "";
    public float Width { get; init; }
    public bool Right { get; init; }
    public Func<T, string> Text { get; init; } = _ => "";
    public Func<T, IComparable>? SortKey { get; init; }
    public Func<T, Color>? Color { get; init; }
    public Func<T, string>? Tooltip { get; init; }
    public bool Numeric { get; init; }
}

/// <summary>
/// Tabela densa de jogo de gestao: cabecalho condensado clicavel (ordena), linhas alternadas,
/// destaque ao passar o mouse, clique na linha e tooltips estruturados por celula.
/// O estado de ordenacao fica no objeto (sobrevive as atualizacoes ao vivo).
/// </summary>
public sealed class DataTable<T>
{
    private readonly List<Column<T>> _columns;
    private int _sortColumn;
    private bool _sortDesc;

    public DataTable(IEnumerable<Column<T>> columns, int sortColumn = 0, bool sortDesc = true)
    {
        _columns = columns.ToList();
        _sortColumn = sortColumn;
        _sortDesc = sortDesc;
    }

    public Action<T>? OnClick { get; set; }
    public Action? OnSortChanged { get; set; }

    public Control Build(IEnumerable<T> rows)
    {
        var col = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        col.AddThemeConstantOverride("separation", 0);

        var head = new PanelContainer();
        head.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0), BorderColor = UiTokens.BorderStrong, BorderWidthBottom = 1, ContentMarginLeft = 6, ContentMarginRight = 6, ContentMarginTop = 2, ContentMarginBottom = 4 });
        var hrow = new HBoxContainer();
        hrow.AddThemeConstantOverride("separation", 6);
        for (int i = 0; i < _columns.Count; i++)
        {
            var c = _columns[i];
            int index = i;
            string arrow = i == _sortColumn ? (_sortDesc ? " ▼" : " ▲") : "";
            var b = new Button { Text = c.Title + arrow, Flat = true, FocusMode = Control.FocusModeEnum.None, Alignment = c.Right ? HorizontalAlignment.Right : HorizontalAlignment.Left, ClipText = true };
            b.AddThemeFontOverride("font", UiFonts.Medium);
            b.AddThemeFontSizeOverride("font_size", 11);
            b.AddThemeColorOverride("font_color", i == _sortColumn ? UiTokens.AccentSoft : UiTokens.TextSecondary);
            b.AddThemeColorOverride("font_hover_color", UiTokens.TextPrimary);
            foreach (var s in new[] { "normal", "hover", "pressed", "hover_pressed" }) b.AddThemeStyleboxOverride(s, new StyleBoxEmpty());
            Size(b, c);
            b.Pressed += () =>
            {
                if (_sortColumn == index) _sortDesc = !_sortDesc;
                else { _sortColumn = index; _sortDesc = _columns[index].Numeric; }
                OnSortChanged?.Invoke();
            };
            hrow.AddChild(b);
        }
        head.AddChild(hrow);
        col.AddChild(head);

        var list = rows.ToList();
        var sc = _columns[Math.Clamp(_sortColumn, 0, _columns.Count - 1)];
        if (sc.SortKey != null)
            list = (_sortDesc ? list.OrderByDescending(sc.SortKey) : list.OrderBy(sc.SortKey)).ToList();
        if (list.Count == 0)
        {
            var empty = ThemeFactory.Text("Nada para mostrar.", UiTokens.FontSmall, UiTokens.TextDisabled);
            empty.CustomMinimumSize = new Vector2(0, 28);
            col.AddChild(empty);
        }
        int r = 0;
        foreach (var item in list)
        {
            var row = new Button { FocusMode = Control.FocusModeEnum.None, CustomMinimumSize = new Vector2(0, 26), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            var n = new StyleBoxFlat { BgColor = r++ % 2 == 1 ? UiTokens.RowAlt : new Color(0, 0, 0, 0) };
            var h = new StyleBoxFlat { BgColor = new Color(UiTokens.Accent, 0.10f), BorderColor = UiTokens.Accent, BorderWidthLeft = 2 };
            row.AddThemeStyleboxOverride("normal", n);
            row.AddThemeStyleboxOverride("hover", h);
            row.AddThemeStyleboxOverride("pressed", h);
            var cells = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Pass };
            cells.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            cells.OffsetLeft = 6; cells.OffsetRight = -6;
            cells.AddThemeConstantOverride("separation", 6);
            foreach (var c in _columns)
            {
                var cell = new TipLabel
                {
                    Text = c.Text(item), VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = c.Right ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                    TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis, ClipText = true,
                    TooltipText = c.Tooltip?.Invoke(item) ?? "",
                    MouseFilter = c.Tooltip != null ? Control.MouseFilterEnum.Pass : Control.MouseFilterEnum.Ignore,
                };
                cell.AddThemeFontSizeOverride("font_size", UiTokens.FontSmall);
                if (c.Numeric) cell.AddThemeFontOverride("font", UiFonts.Numbers);
                cell.AddThemeColorOverride("font_color", c.Color?.Invoke(item) ?? UiTokens.TextPrimary);
                Size(cell, c);
                cells.AddChild(cell);
            }
            row.AddChild(cells);
            var captured = item;
            row.Pressed += () => OnClick?.Invoke(captured);
            col.AddChild(row);
        }
        return col;
    }

    private static void Size(Control c, Column<T> col)
    {
        if (col.Width > 0) c.CustomMinimumSize = new Vector2(col.Width, 0);
        else c.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
    }
}
