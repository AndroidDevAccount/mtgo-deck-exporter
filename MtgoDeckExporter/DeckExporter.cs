using System.Text;

namespace MtgoDeckExporter;

public enum ExportFormat
{
    Moxfield,
    Archidekt,
    MTGGoldfish,
    Arena,
    PlainText,
    Csv
}

public static class DeckExporter
{
    public static string Export(Deck deck, ExportFormat format)
    {
        var main = deck.Cards.Where(c => !c.IsSideboard).ToList();
        var sideboard = deck.Cards.Where(c => c.IsSideboard).ToList();

        return format switch
        {
            ExportFormat.Arena => Sections(main, sideboard, "Deck", "Sideboard"),
            ExportFormat.PlainText => Plain(main, sideboard),
            ExportFormat.Csv => Csv(main, sideboard),
            _ => Sections(main, sideboard, null, "SIDEBOARD:")
        };
    }

    private static string Sections(List<DeckCard> main, List<DeckCard> sideboard, string? mainHeader, string sideHeader)
    {
        var lines = new List<string>();
        if (mainHeader is not null) lines.Add(mainHeader);
        lines.AddRange(main.Select(Line));
        if (sideboard.Count > 0)
        {
            lines.Add("");
            lines.Add(sideHeader);
            lines.AddRange(sideboard.Select(Line));
        }
        return string.Join(Environment.NewLine, lines);
    }

    private static string Plain(List<DeckCard> main, List<DeckCard> sideboard)
    {
        var lines = main.SelectMany(c => Enumerable.Repeat(c.Name, c.Quantity)).ToList();
        if (sideboard.Count > 0)
        {
            lines.Add("");
            lines.Add("SIDEBOARD:");
            lines.AddRange(sideboard.SelectMany(c => Enumerable.Repeat(c.Name, c.Quantity)));
        }
        return string.Join(Environment.NewLine, lines);
    }

    private static string Csv(List<DeckCard> main, List<DeckCard> sideboard)
    {
        static string Escape(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
        var lines = new[] { "Quantity,Card Name,Section" }.Concat(main.Concat(sideboard)
            .Select(c => $"{c.Quantity},{Escape(c.Name)},{(c.IsSideboard ? "Sideboard" : "Main Deck")}"));
        return string.Join(Environment.NewLine, lines);
    }

    private static string Line(DeckCard card) => $"{card.Quantity} {card.Name}";
}
