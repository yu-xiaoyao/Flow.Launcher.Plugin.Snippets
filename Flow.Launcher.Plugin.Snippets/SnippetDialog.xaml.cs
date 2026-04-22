using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using JetBrains.Annotations;

namespace Flow.Launcher.Plugin.Snippets;

public partial class SnippetDialog : Window, INotifyPropertyChanged
{
    private static SnippetDialog _dialog;

    public static void ShowDialog(IPublicAPI publicApi, SnippetManage snippetManage,
        [CanBeNull] SnippetModel selectSm = null, [CanBeNull] Window parent = null)
    {
        if (_dialog == null)
        {
            _dialog = new SnippetDialog(publicApi, snippetManage, selectSm);
            if (parent != null) _dialog.Owner = parent;
            _dialog.Show();
        }
        else
        {
            _dialog._selectSm = selectSm;
            if (parent != null) _dialog.Owner = parent;
            _dialog.Activate();
        }
    }

    private IPublicAPI _publicAPI;

    private SnippetManage _snippetManage;

    // for edit
    [CanBeNull] private SnippetModel _selectSm;

    // public string TitleName { get; set; }
    public event PropertyChangedEventHandler PropertyChanged;

    private string _titleName;

    /// <summary>
    /// bind for TitleName
    /// </summary>
    public string TitleName
    {
        get => _titleName;
        set
        {
            _titleName = value;
            OnPropertyChanged(nameof(TitleName));
        }
    }


    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public SnippetDialog(IPublicAPI publicApi,
        SnippetManage snippetManage,
        [CanBeNull] SnippetModel selectSm = null)
    {
        _publicAPI = publicApi;
        _snippetManage = snippetManage;
        _selectSm = selectSm;

        InitializeComponent();
        DataContext = this;
        Closed += (sender, args) => { _dialog = null; };
        PreviewKeyDown += (sender, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        };

        Activated += (sender, args) => { _renderView(); };
    }

    private void _renderView()
    {
        TitleName = _publicAPI.GetTranslation(_selectSm != null
            ? "snippets_plugin_edit_snippet"
            : "snippets_plugin_add");

        if (_selectSm != null)
        {
            TbKey.IsEnabled = false;
            TbKey.Text = _selectSm.Key;
            TbValue.Text = _selectSm.Value;
            TbScore.Text = $"{_selectSm.Score}";
        }
        else
        {
            TbKey.IsEnabled = true;
            TbKey.Text = "";
            TbValue.Text = "";
            TbScore.Text = "0";
        }
    }

    private bool _doSave()
    {
        var key = TbKey.Text.Trim();
        var value = TbValue.Text;
        var scoreResult = int.TryParse(TbScore.Text, out var score);
        if (!scoreResult)
            score = 0; // make default for save

        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
            return false;

        if (_selectSm == null)
        {
            // ADD
            var sm = _snippetManage.GetByKey(key);
            if (sm != null)
            {
                _publicAPI.ShowMsgError(
                    _publicAPI.GetTranslation("snippets_plugin_add_failed"),
                    _publicAPI.GetTranslation("snippets_plugin_add_failed_cause")
                );
                return false;
            }

            _snippetManage.Add(key, value, score: score);
        }
        else
        {
            // update
            var result = _snippetManage.UpdateByKey(_selectSm.Key, value: value, score: score);

            return result;
        }

        return true;
    }

    private void SaveAndCloseButtonClick(object sender, RoutedEventArgs e)
    {
        if (_doSave())
            Close();
        else
        {
            _publicAPI.ShowMsgError("FAILED");
        }
    }

    private void SaveButtonClient(object sender, RoutedEventArgs e)
    {
        _doSave();
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
            {
                return;
            }

            var isNum = int.TryParse(input, out var score);
            if (!isNum)
            {
                e.Handled = true;
            }
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
                    var isNum = int.TryParse(input, out var score);
                    if (!isNum)
                    {
                        e.CancelCommand();
                    }
                }
            }
        }
        else
        {
            e.CancelCommand();
        }
    }
}