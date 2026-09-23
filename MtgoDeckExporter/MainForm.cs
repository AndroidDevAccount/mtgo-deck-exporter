using System.Drawing;

namespace MtgoDeckExporter;

public sealed class MainForm : Form
{
    private readonly Label _fileLabel = new() { AutoEllipsis = true, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Label _countLabel = new() { AutoSize = true, ForeColor = Color.DimGray };
    private readonly ComboBox _format = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
    private readonly TextBox _preview = new() { Multiline = true, ScrollBars = ScrollBars.Both, WordWrap = false, Dock = DockStyle.Fill, Font = new Font("Consolas", 10.5f), AcceptsTab = true };
    private Deck? _deck;
    private string? _currentPath;

    public MainForm(string? initialPath)
    {
        Text = "MTGO Deck Exporter";
        MinimumSize = new Size(680, 500);
        Size = new Size(820, 650);
        StartPosition = FormStartPosition.CenterScreen;
        AllowDrop = true;

        _format.DataSource = Enum.GetValues<ExportFormat>();
        _format.SelectedItem = ExportFormat.Moxfield;
        _format.SelectedIndexChanged += (_, _) => RefreshPreview();

        var open = Button("Open .dek…", (_, _) => OpenDeck());
        var copy = Button("Copy", (_, _) => CopyPreview());
        var save = Button("Export…", (_, _) => SaveExport());
        var associate = Button("Associate .dek files", (_, _) => Associate());

        var top = new TableLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(10, 8, 10, 4), ColumnCount = 3 };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        top.Controls.Add(open, 0, 0);
        top.Controls.Add(_fileLabel, 1, 0);
        top.Controls.Add(associate, 2, 0);

        var controls = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(10, 8, 10, 4), FlowDirection = FlowDirection.LeftToRight };
        controls.Controls.Add(new Label { Text = "Export format:", AutoSize = true, Margin = new Padding(0, 7, 4, 0) });
        controls.Controls.Add(_format);
        controls.Controls.Add(copy);
        controls.Controls.Add(save);
        controls.Controls.Add(_countLabel);

        var hint = new Label { Text = "Open a file or drop a .dek file anywhere in this window.", Dock = DockStyle.Bottom, Height = 28, Padding = new Padding(10, 5, 0, 0), ForeColor = Color.DimGray };
        Controls.Add(_preview);
        Controls.Add(hint);
        Controls.Add(controls);
        Controls.Add(top);

        DragEnter += (_, e) => e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true ? DragDropEffects.Copy : DragDropEffects.None;
        DragDrop += (_, e) =>
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files) LoadDeck(files[0]);
        };
        Shown += (_, _) => { if (initialPath is not null) LoadDeck(initialPath); };
    }

    private static Button Button(string text, EventHandler action)
    {
        var button = new Button { Text = text, AutoSize = true, Height = 30 };
        button.Click += action;
        return button;
    }

    private void OpenDeck()
    {
        using var dialog = new OpenFileDialog { Filter = "MTGO deck files (*.dek)|*.dek|All files (*.*)|*.*", Title = "Open MTGO deck" };
        if (dialog.ShowDialog(this) == DialogResult.OK) LoadDeck(dialog.FileName);
    }

    private void LoadDeck(string path)
    {
        try
        {
            _deck = DekParser.Parse(path);
            _currentPath = path;
            _fileLabel.Text = Path.GetFileName(path);
            _fileLabel.Tag = path;
            _countLabel.Text = $"   {_deck.MainCount} main / {_deck.SideboardCount} sideboard";
            Text = $"{_deck.Name} — MTGO Deck Exporter";
            RefreshPreview();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not read this deck.\n\n{ex.Message}", "Invalid deck file", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RefreshPreview()
    {
        if (_deck is not null && _format.SelectedItem is ExportFormat format)
            _preview.Text = DeckExporter.Export(_deck, format);
    }

    private void CopyPreview()
    {
        if (string.IsNullOrWhiteSpace(_preview.Text)) return;
        Clipboard.SetText(_preview.Text);
        _countLabel.Text = "   Copied to clipboard";
    }

    private void SaveExport()
    {
        if (_deck is null || _format.SelectedItem is not ExportFormat format) return;
        var extension = format == ExportFormat.Csv ? "csv" : "txt";
        using var dialog = new SaveFileDialog
        {
            Filter = format == ExportFormat.Csv ? "CSV file (*.csv)|*.csv" : "Text file (*.txt)|*.txt",
            FileName = $"{Path.GetFileNameWithoutExtension(_currentPath)}.{extension}"
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            File.WriteAllText(dialog.FileName, _preview.Text);
    }

    private void Associate()
    {
        try
        {
            FileAssociation.Register();
            MessageBox.Show(this, ".dek files are now associated with MTGO Deck Exporter for your Windows account.", "Association complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Windows could not create the association.\n\n{ex.Message}", "Association failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
