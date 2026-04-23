using System.Windows;
using JetBrains.Annotations;

namespace Flow.Launcher.Plugin.Snippets;

public partial class SnippetEditWindows : Window
{
    private readonly SnippetManage _snippetManage;

    [CanBeNull] private SnippetModel _editModel;
    private bool _edit;

    public SnippetEditWindows(SnippetManage snippetManage, [CanBeNull] SnippetModel editModel = null)
    {
        _snippetManage = snippetManage;
        if (editModel != null)
        {
            _edit = true;
            _editModel = editModel;
        }

        InitializeComponent();
    }
}