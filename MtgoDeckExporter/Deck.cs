using System.Xml.Linq;

namespace MtgoDeckExporter;

public sealed record DeckCard(string Name, int Quantity, bool IsSideboard);

public sealed class Deck
{
    public string Name { get; init; } = "Untitled deck";
    public IReadOnlyList<DeckCard> Cards { get; init; } = [];
    public int MainCount => Cards.Where(c => !c.IsSideboard).Sum(c => c.Quantity);
    public int SideboardCount => Cards.Where(c => c.IsSideboard).Sum(c => c.Quantity);
}

public static class DekParser
{
    public static Deck Parse(string path)
    {
        using var stream = File.OpenRead(path);
        var document = XDocument.Load(stream, LoadOptions.None);
        var cards = document.Descendants()
            .Where(e => e.Name.LocalName.Equals("Cards", StringComparison.OrdinalIgnoreCase) ||
                        e.Name.LocalName.Equals("Card", StringComparison.OrdinalIgnoreCase))
            .Select(ParseCard)
            .Where(c => c is not null)
            .Cast<DeckCard>()
            .GroupBy(c => new CardGroupKey(c.Name, c.IsSideboard), CardGroupKeyComparer.Instance)
            .Select(g => new DeckCard(g.First().Name, g.Sum(c => c.Quantity), g.Key.IsSideboard))
            .OrderBy(c => c.IsSideboard)
            .ThenBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        if (cards.Count == 0)
            throw new InvalidDataException("This file does not contain any recognizable MTGO card entries.");

        var deckName = document.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals("DeckName", StringComparison.OrdinalIgnoreCase))?.Value;

        return new Deck
        {
            Name = string.IsNullOrWhiteSpace(deckName) ? Path.GetFileNameWithoutExtension(path) : deckName.Trim(),
            Cards = cards
        };
    }

    private static DeckCard? ParseCard(XElement element)
    {
        string? Attr(params string[] names) => element.Attributes()
            .FirstOrDefault(a => names.Contains(a.Name.LocalName, StringComparer.OrdinalIgnoreCase))?.Value;

        var name = Attr("Name", "CardName") ?? element.Elements()
            .FirstOrDefault(e => e.Name.LocalName.Equals("Name", StringComparison.OrdinalIgnoreCase))?.Value;
        if (string.IsNullOrWhiteSpace(name)) return null;

        var quantityText = Attr("Quantity", "Qty", "Count");
        var quantity = int.TryParse(quantityText, out var parsed) && parsed > 0 ? parsed : 1;
        var sideboardText = Attr("Sideboard", "IsSideboard");
        var isSideboard = bool.TryParse(sideboardText, out var sideboard) && sideboard;
        if (!isSideboard && string.Equals(Attr("Zone"), "Sideboard", StringComparison.OrdinalIgnoreCase))
            isSideboard = true;

        return new DeckCard(name.Trim(), quantity, isSideboard);
    }

    private sealed record CardGroupKey(string Name, bool IsSideboard);

    private sealed class CardGroupKeyComparer : IEqualityComparer<CardGroupKey>
    {
        public static readonly CardGroupKeyComparer Instance = new();

        public bool Equals(CardGroupKey? x, CardGroupKey? y) => x is not null && y is not null &&
            x.IsSideboard == y.IsSideboard && StringComparer.OrdinalIgnoreCase.Equals(x.Name, y.Name);
        public int GetHashCode(CardGroupKey obj) => HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name), obj.IsSideboard);
    }
}
