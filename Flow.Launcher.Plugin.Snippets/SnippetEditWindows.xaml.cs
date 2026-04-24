using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Flow.Launcher.Plugin.Snippets.Model;
using ICSharpCode.AvalonEdit.Highlighting;
using JetBrains.Annotations;

namespace Flow.Launcher.Plugin.Snippets;

public partial class SnippetEditWindows : Window
{
    private readonly SnippetManage _snippetManage;
    private readonly List<FolderModel> _folders;

    [CanBeNull] private SnippetModel _editModel;
    private bool _edit;

    public SnippetEditWindows(SnippetManage snippetManage, [CanBeNull] SnippetModel editModel = null)
    {
        _snippetManage = snippetManage;
        _folders = snippetManage.ListFolders();

        if (editModel != null)
        {
            _edit = true;
            _editModel = editModel;
        }

        InitializeComponent();
        _initView();
    }

    private void _initView()
    {
        // Syntax
        CbSyntax.Items.Add("(None)");
        CbSyntax.SelectedIndex = 0;
        foreach (var syntax in _getSyntaxList())
            CbSyntax.Items.Add(syntax);

        // Folder
        CbFolder.Items.Add("(None)");
        CbFolder.SelectedIndex = 0;
        foreach (var folder in _folders)
            CbFolder.Items.Add(folder.Name);

        // Load edit model
        if (_editModel != null)
        {
            TbKey.Text = _editModel.Name;
            TbKey.IsEnabled = false;
            Editor.Text = _editModel.Value;
            BtnFavorite.IsChecked = _editModel.Faviorites == 1;

            if (!string.IsNullOrEmpty(_editModel.Syntax))
            {
                var idx = _getSyntaxList().IndexOf(_editModel.Syntax);
                if (idx >= 0) CbSyntax.SelectedIndex = idx + 1;
            }

            if (_editModel.FolderId.HasValue)
            {
                var folder = _folders.FirstOrDefault(f => f.Id == _editModel.FolderId.Value);
                if (folder != null)
                    CbFolder.SelectedItem = folder.Name;
            }
        }
        else
        {
            Editor.Text = "";
        }
    }

    private static List<string> _getSyntaxList() => new()
    {
        "Text", "C#", "Java", "JavaScript", "Python", "HTML", "XML", "JSON", "SQL", "CSS", "Go", "Rust", "TypeScript",
        "Markdown"
    };

    private static readonly Dictionary<string, string> SyntaxHighlightingMap = new()
    {
        { "C#", "C#" },
        { "Java", "Java" },
        { "JavaScript", "JavaScript" },
        { "Python", "Python" },
        { "HTML", "HTML" },
        { "XML", "XML" },
        { "JSON", "JSON" },
        { "SQL", "TSQL" },
        { "CSS", "CSS" },
        { "Go", "Go" },
        { "Rust", "Rust" },
        { "TypeScript", "TypeScript" },
        { "Markdown", "MarkDown" },
    };

    private void OnSyntaxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Editor == null || CbSyntax == null) return;

        var selected = CbSyntax.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(selected) || selected == "(None)" || selected == "Text")
        {
            Editor.SyntaxHighlighting = null;
            return;
        }

        var highlightingName = SyntaxHighlightingMap.GetValueOrDefault(selected, selected);
        Editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition(highlightingName);
    }

    private void OnSaveButtonClick(object sender, RoutedEventArgs e)
    {
        var name = TbKey.Text.Trim();
        var value = Editor.Text;

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(value))
        {
            MessageBox.Show("Name and Value are required.");
            return;
        }

        var favorites = BtnFavorite.IsChecked;
        var syntax = CbSyntax.SelectedIndex > 0 ? CbSyntax.SelectedItem?.ToString() : null;
        long? folderId = null;
        if (CbFolder.SelectedIndex > 0)
        {
            var folder = _folders.ElementAtOrDefault(CbFolder.SelectedIndex - 1);
            if (folder != null) folderId = folder.Id;
        }

        if (_edit && _editModel != null)
        {
            _snippetManage.UpdateSnippetById(_editModel.Id, name: name, value: value, syntax: syntax,
                folderId: folderId, favorites: favorites);
        }
        else
        {
            _snippetManage.Add(name, value, syntax, favorites, folderId);
        }

        DialogResult = true;
        Close();
    }

    private void OnCancelButtonClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}