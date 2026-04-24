using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Flow.Launcher.Plugin.Snippets.Model;
using JetBrains.Annotations;

namespace Flow.Launcher.Plugin.Snippets;

public partial class FolderEditDialog : Window, INotifyPropertyChanged
{
    private readonly PluginInitContext _context;
    private readonly SnippetManage _snippetManage;
    [CanBeNull] private readonly FolderModel _editModel;

    private bool _isEditMode;

    public event PropertyChangedEventHandler PropertyChanged;

    private string _titleName;

    public string TitleName
    {
        get => _titleName;
        set
        {
            _titleName = value;
            OnPropertyChanged(nameof(TitleName));
        }
    }

    public FolderEditDialog(PluginInitContext context, SnippetManage snippetManage,
        [CanBeNull] FolderModel editModel = null)
    {
        _context = context;
        _snippetManage = snippetManage;

        if (editModel != null)
        {
            _isEditMode = true;
            _editModel = editModel;
        }

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
        TitleName = _isEditMode
            ? _context.API.GetTranslation("snippets_plugin_edit_folder")
            : _context.API.GetTranslation("snippets_plugin_add_folder");

        if (_isEditMode && _editModel != null)
        {
            TbId.Text = _editModel.Id.ToString();
            TbName.Text = _editModel.Name;
            TbOrderNum.Text = _editModel.OrderNum.ToString();
            TbCreateTime.Text = _editModel.CreateTime.ToString("yyyy-MM-dd HH:mm:ss");
            TbUpdateTime.Text = _editModel.UpdateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }
        else
        {
            TbId.Text = "-";
            TbName.Text = "";
            TbOrderNum.Text = "0";
            TbCreateTime.Text = "-";
            TbUpdateTime.Text = "-";
        }
    }

    private bool _doSave()
    {
        var name = TbName.Text.Trim();
        if (string.IsNullOrEmpty(name))
            return false;

        var orderResult = long.TryParse(TbOrderNum.Text.Trim(), out var orderNum);
        if (!orderResult)
            orderNum = 0;

        if (_isEditMode && _editModel != null)
        {
            return _snippetManage.UpdateFolderById(_editModel.Id, name, orderNum);
        }
        else
        {
            return _snippetManage.AddFolder(name, orderNum);
        }
    }

    private void SaveAndCloseButtonClick(object sender, RoutedEventArgs e)
    {
        if (_doSave())
        {
            DialogResult = true;
            Close();
        }
        else
        {
            _context.API.ShowMsgError(
                _context.API.GetTranslation("snippets_plugin_error"),
                _context.API.GetTranslation("snippets_plugin_add_failed"));
        }
    }

    private void OnCancelButtonClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var input = e.Text.Trim();
        if (!string.IsNullOrEmpty(input))
        {
            if ("-".Equals(input))
                return;

            var isNum = long.TryParse(input, out _);
            if (!isNum)
                e.Handled = true;
        }
    }

    private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var text = (string)e.DataObject.GetData(typeof(string));
            if (text != null)
            {
                var input = text.Trim();
                if (!string.IsNullOrEmpty(input))
                {
                    var isNum = long.TryParse(input, out _);
                    if (!isNum)
                        e.CancelCommand();
                }
            }
        }
        else
        {
            e.CancelCommand();
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}