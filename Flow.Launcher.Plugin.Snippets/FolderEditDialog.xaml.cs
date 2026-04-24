using System.Windows;
using System.Windows.Input;
using Flow.Launcher.Plugin.Snippets.Model;
using Flow.Launcher.Plugin.Snippets.Util;
using JetBrains.Annotations;

namespace Flow.Launcher.Plugin.Snippets;

public partial class FolderEditDialog : Window
{
    private readonly PluginInitContext _context;
    private readonly SnippetManage _snippetManage;
    [CanBeNull] private readonly FolderModel _editModel;

    private bool _isEditMode;

    public string TitleName { get; set; }


    public FolderEditDialog(PluginInitContext context, SnippetManage snippetManage,
        [CanBeNull] FolderModel editModel = null)
    {
        _context = context;
        _snippetManage = snippetManage;

        if (editModel != null)
        {
            _isEditMode = true;
            _editModel = editModel;
            TitleName = _context.API.GetTranslation("snippets_plugin_edit");
        }
        else
        {
            _isEditMode = false;
            TitleName = _context.API.GetTranslation("snippets_plugin_add");
        }

        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        InitializeComponent();

        DataContext = this;
        Closed += (_, _) => { };
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        };

        _renderView();
    }

    private void _renderView()
    {
        if (_isEditMode && _editModel != null)
        {
            TbName.Text = _editModel.Name;
            TbCreateTime.Text = DateTimeUtil.FormatDateTime(_editModel.CreateTime);
            TbUpdateTime.Text = DateTimeUtil.FormatDateTime(_editModel.UpdateTime);
        }
        else
        {
            LabelCreateTime.Visibility = Visibility.Hidden;
            LabelUpdateTime.Visibility = Visibility.Hidden;
        }
    }


    private bool _checkFolderNameExist(string folderName, bool edit)
    {
        var fm = _snippetManage.GetFolder(folderName);
        if (fm != null)
        {
            var title = string.Format(
                _context.API.GetTranslation(edit ? "snippets_plugin_edit_failed" : "snippets_plugin_add_failed"),
                _context.API.GetTranslation("snippets_plugin_snippets_folder"));
            var subTitle = string.Format(_context.API.GetTranslation("snippets_plugin_name_already_exists"),
                folderName);

            _context.API.ShowMsgBox(subTitle, title, icon: MessageBoxImage.Error);

            return true;
        }

        return false;
    }

    private bool _doSave()
    {
        var name = TbName.Text.Trim();
        if (string.IsNullOrEmpty(name))
            return false;

        if (_isEditMode && _editModel != null)
        {
            if (string.Equals(name, _editModel.Name))
                return false;

            if (_checkFolderNameExist(name, true))
                return false;
            _snippetManage.UpdateFolderById(_editModel.Id, name);
        }
        else
        {
            if (_checkFolderNameExist(name, false))
                return false;
            _snippetManage.AddFolder(name);
        }

        return true;
    }

    private void SaveAndCloseButtonClick(object sender, RoutedEventArgs e)
    {
        if (_doSave())
        {
            DialogResult = true;
            Close();
        }
    }

    private void OnCancelButtonClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnCloseExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        Close();
    }
}