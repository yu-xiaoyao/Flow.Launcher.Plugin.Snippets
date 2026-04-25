using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Flow.Launcher.Plugin.Snippets.Model;
using Flow.Launcher.Plugin.Snippets.Util;
using ICSharpCode.AvalonEdit.Highlighting;
using JetBrains.Annotations;

namespace Flow.Launcher.Plugin.Snippets;

public partial class SnippetEditWindows : Window
{
    private readonly PluginInitContext _context;
    private readonly SnippetManage _snippetManage;

    public List<FolderModel> Folders { get; set; }

    [CanBeNull] private SnippetModel _editModel;

    public SnippetEditWindows(PluginInitContext context, SnippetManage snippetManage,
        [CanBeNull] SnippetModel editModel = null)
    {
        _context = context;
        _snippetManage = snippetManage;

        if (editModel != null)
        {
            _editModel = editModel;
        }

        InitializeComponent();

        _loadPluginImage();

        Folders = snippetManage.ListFolders();

        _initView();
    }

    private void _loadPluginImage()
    {
        var ico = Utils.LoadPluginIcon(_context);
        if (ico == null) return;
        Icon = ico;
        IconImage.Source = ico;
    }

    private void _initView()
    {
        BtnSaveOrUpdate.Content =
            _context.API.GetTranslation(_editModel != null ? "snippets_plugin_update" : "snippets_plugin_save");

        // Syntax
        CbSyntax.Items.Add("(None)");
        CbSyntax.SelectedIndex = 0;
        foreach (var syntax in _getSyntaxList())
            CbSyntax.Items.Add(syntax);

        // Folder
        CbFolder.Items.Add("(None)");
        CbFolder.SelectedIndex = 0;
        foreach (var folder in Folders)
            CbFolder.Items.Add(folder);

        // Load edit model
        if (_editModel != null)
        {
            TbSnippetName.Text = _editModel.Name;
            Editor.Text = _editModel.Value;
            BtnFavorite.IsChecked = Utils.IntToBool(_editModel.Faviorites);

            if (!string.IsNullOrEmpty(_editModel.Syntax))
            {
                var idx = _getSyntaxList().IndexOf(_editModel.Syntax);
                if (idx >= 0) CbSyntax.SelectedIndex = idx + 1;
            }

            if (_editModel.FolderId.HasValue)
            {
                var folder = Folders.FirstOrDefault(f => f.Id == _editModel.FolderId.Value);
                if (folder != null)
                    CbFolder.SelectedItem = folder;
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
        var name = TbSnippetName.Text.Trim();
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
            var folder = Folders.ElementAtOrDefault(CbFolder.SelectedIndex - 1);
            if (folder != null) folderId = folder.Id;
        }

        if (_editModel != null)
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

    private void OnCloseExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        Close();
    }


    #region Window Custom TitleBar

    private void OnMinimizeButtonClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnMaximizeRestoreButtonClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState switch
        {
            WindowState.Maximized => WindowState.Normal,
            _ => WindowState.Maximized
        };
    }

    private void OnCloseButtonClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void RefreshMaximizeRestoreButton()
    {
        if (WindowState == WindowState.Maximized)
        {
            MaximizeButton.Visibility = Visibility.Hidden;
            RestoreButton.Visibility = Visibility.Visible;
        }
        else
        {
            MaximizeButton.Visibility = Visibility.Visible;
            RestoreButton.Visibility = Visibility.Hidden;
        }
    }

    #endregion
}