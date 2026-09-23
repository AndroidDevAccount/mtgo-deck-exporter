using MtgoDeckExporter;

if (args.Length != 1) throw new ArgumentException("Pass a .dek file path.");
var deck = DekParser.Parse(args[0]);
var moxfield = DeckExporter.Export(deck, ExportFormat.Moxfield);
Console.WriteLine($"{deck.MainCount}|{deck.SideboardCount}|{deck.Cards.Count}");
Console.WriteLine(moxfield);
