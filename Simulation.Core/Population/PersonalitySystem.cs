using Simulation.Core.Content;

namespace Simulation.Core.Population;

/// <summary>
/// Personalidade principal (P4a): uma por jogador, derivada dos seis atributos de personalidade
/// pela posicao da pessoa na distribuicao do PRESET (nao da populacao do servidor). Vence o
/// atributo mais extremo (alto ou baixo); quem nao chega ao limite de destaque e Equilibrado.
/// Muda raramente: so por mudanca relevante e persistente nesses atributos (margem e confirmacao).
/// As regras de comportamento de cada personalidade ficam em PersonalityRules.
/// </summary>
public static class PersonalitySystem
{
    /// <summary>Id da personalidade sem destaque (sem regra propria).</summary>
    public const string Balanced = "equilibrado";

    /// <summary>Atributos de personalidade que definem a personalidade principal (ordem fixa para desempate).</summary>
    public static readonly string[] Attributes = { "sociability", "aggressiveness", "patience", "loyalty", "riskAversion", "variety" };

    /// <summary>
    /// Posicao (permille, 0..1000) de um valor na distribuicao triangular do preset: a acumulada
    /// F(valor). Inteiros apenas.
    /// </summary>
    public static int Position(TraitRange r, int value)
    {
        if (!r.IsTriangular) return 500;
        long a = r.Min!.Value, c = r.Mode!.Value, b = r.Max!.Value, x = value;
        if (b <= a) return 500;
        if (x <= a) return 0;
        if (x >= b) return 1000;
        if (x <= c)
            return c == a ? 0 : (int)((x - a) * (x - a) * 1000 / ((b - a) * (c - a)));
        return b == c ? 1000 : (int)(1000 - (b - x) * (b - x) * 1000 / ((b - a) * (b - c)));
    }

    /// <summary>Destaque de uma personalidade (permille): quanto a posicao se afasta do meio na direcao dela; pode ser negativo.</summary>
    public static int Score(PersonalityTypeDefinition type, int position) =>
        type.Direction == PersonalityDirections.High ? (position - 500) * 2 : (500 - position) * 2;

    /// <summary>Destaque de cada tipo para os atributos dados (na ordem dos tipos no conteudo).</summary>
    public static List<(PersonalityTypeDefinition Type, int Score)> Scores(ContentDatabase c, PersonTraits t)
    {
        var list = new List<(PersonalityTypeDefinition, int)>();
        var ranges = c.PopulationTraits;
        foreach (var type in c.Personality.Types)
            list.Add((type, Score(type, Position(Population.PopulationGenerator.TraitRangeOf(ranges, type.Attribute), t.Get(type.Attribute)))));
        return list;
    }

    /// <summary>
    /// Classificacao sem historico (criacao): o tipo de maior destaque, se chegar ao limite;
    /// senao Equilibrado. Empate exato: a ordem do conteudo (fixa).
    /// </summary>
    public static string Classify(ContentDatabase c, PersonTraits t)
    {
        var s = c.Personality;
        if (!s.Enabled || s.Types.Count == 0) return "";
        var best = Best(Scores(c, t));
        return best.Type != null && best.Score >= s.ThresholdPermille ? best.Type.Id : Balanced;
    }

    private static (PersonalityTypeDefinition? Type, int Score) Best(List<(PersonalityTypeDefinition Type, int Score)> scores)
    {
        (PersonalityTypeDefinition? Type, int Score) best = (null, int.MinValue);
        foreach (var x in scores) if (x.Score > best.Score) best = x;   // estrito: empate fica com o primeiro
        return best;
    }

    /// <summary>
    /// Para onde a personalidade quer ir agora (com histerese), ou a atual se deve ficar:
    /// - de um tipo para outro: o outro precisa de destaque no limite e acima do atual + margem;
    /// - de um tipo para Equilibrado: o atual cai abaixo de limite - margem e nenhum outro chega a limite + margem;
    /// - de Equilibrado para um tipo: o tipo precisa de limite + margem.
    /// A faixa [limite - margem, limite + margem] evita oscilacao nos dois sentidos.
    /// </summary>
    public static string Target(ContentDatabase c, string current, PersonTraits t)
    {
        var s = c.Personality;
        var scores = Scores(c, t);
        int thr = s.ThresholdPermille, m = s.ChangeMarginPermille;
        if (current == Balanced || current.Length == 0)
        {
            var best = Best(scores);
            return best.Type != null && best.Score >= thr + m ? best.Type.Id : Balanced;
        }
        int own = scores.Where(x => x.Type.Id == current).Select(x => x.Score).DefaultIfEmpty(int.MinValue).First();
        (PersonalityTypeDefinition? Type, int Score) other = (null, int.MinValue);
        foreach (var x in scores)
            if (x.Type.Id != current && x.Score > other.Score) other = x;
        if (other.Type != null && other.Score >= Math.Max(thr, own + m)) return other.Type.Id;
        if (own < thr - m && (other.Type == null || other.Score < thr + m)) return Balanced;
        return current;
    }

    /// <summary>
    /// Conferencia diaria: a mudanca so acontece se o alvo se mantiver por ConfirmDays dias
    /// seguidos; se ele se desfaz, a contagem recomeca. Registra anterior, nova e o atributo.
    /// Devolve a mudanca aplicada, ou null.
    /// </summary>
    public static PersonalityChange? OnDaily(ContentDatabase c, Person p, long day)
    {
        var s = c.Personality;
        if (!s.Enabled || p.PersonalityId.Length == 0) return null;
        string target = Target(c, p.PersonalityId, p.Traits);
        if (target == p.PersonalityId)
        {
            p.PersonalityPending = "";
            return null;
        }
        if (p.PersonalityPending != target)
        {
            p.PersonalityPending = target;
            p.PersonalityPendingSince = day;
            return null;
        }
        if (day - p.PersonalityPendingSince < s.ConfirmDays) return null;
        // O atributo que motivou: o do novo tipo (ou, ao virar Equilibrado, o do tipo que saiu).
        var motive = c.Personality.Types.FirstOrDefault(x => x.Id == (target == Balanced ? p.PersonalityId : target));
        string attr = motive?.Attribute ?? "";
        int value = attr.Length > 0 ? p.Traits.Get(attr) : 0;
        int pos = attr.Length > 0 ? Position(Population.PopulationGenerator.TraitRangeOf(c.PopulationTraits, attr), value) : 500;
        var change = new PersonalityChange(day, p.PersonalityId, target, attr, value, pos);
        p.PersonalityHistoryList.Add(change);
        p.PersonalityId = target;
        p.PersonalityPending = "";
        return change;
    }
}

/// <summary>Mudanca da personalidade principal: dia, anterior, nova e o atributo que motivou (valor e posicao).</summary>
public sealed record PersonalityChange(long Day, string From, string To, string Attribute, int Value, int PositionPermille);
