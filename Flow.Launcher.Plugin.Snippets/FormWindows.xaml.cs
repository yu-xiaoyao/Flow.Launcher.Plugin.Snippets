using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Flow.Launcher.Plugin.Snippets.Util;
using JetBrains.Annotations;

namespace Flow.Launcher.Plugin.Snippets;

public partial class FormWindows : Window
{
    private static FormWindows _instance;

    public static void ShowWindows(IPublicAPI publicApi, SnippetManage snippetManage,
        [CanBeNull] SnippetModel selectSm = null)
    {
        if (_instance == null)
        {
            _instance = new FormWindows(publicApi, snippetManage, selectSm);
            _instance.Show();
        }
        else
        {
            _instance._selectSm = selectSm;
            _instance.Activate();
        }
    }


    private IPublicAPI _publicAPI;

    private SnippetManage _snippetManage;

    // for edit
    [CanBeNull] private SnippetModel _selectSm;

    private readonly ObservableCollection<SnippetModel> _snippetsSource = new();

    public FormWindows(IPublicAPI publicApi,
        SnippetManage snippetManage,
        [CanBeNull] SnippetModel selectSm = null)
    {
        _publicAPI = publicApi;
        _snippetManage = snippetManage;
        _selectSm = selectSm;

        InitializeComponent();

        Activated += (sender, args) => { _reload(); };

        Closed += (sender, args) => { _instance = null; };
        PreviewKeyDown += (sender, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        };


        BtnClose.Content = _publicAPI.GetTranslation("snippets_plugin_close");
        BtnSave.Content = _publicAPI.GetTranslation("snippets_plugin_save");

        ComboBoxFilterType.SelectedIndex = 0; // default key

        DataGrid.ItemsSource = _snippetsSource;

        DataContext = this;
        AddContextMenu();
    }

    private void _reload()
    {
        _loadData();
        var findIdx = _findBySelectData(_selectSm?.Name);
        if (findIdx != -1)
            DataGrid.SelectedIndex = findIdx;
    }

    private int _findBySelectData([CanBeNull] string findKey)
    {
        if (findKey == null) return -1;

        var findIdx = -1;
        for (var i = 0; i < _snippetsSource.Count; i++)
        {
            if (!string.Equals(findKey, _snippetsSource[i].Name)) continue;
            findIdx = i;
            break;
        }

        return findIdx;
    }

    private void _loadData()
    {
        _snippetsSource.Clear();

        List<SnippetModel> snippets;

        var filter = TbFilter.Text.Trim();
        if (string.IsNullOrEmpty(filter))
        {
            snippets = _snippetManage.List();
        }
        else
        {
            if (ComboBoxFilterType.SelectedIndex == 1)
            {
                snippets = _snippetManage.List(value: filter);
            }
            else
            {
                snippets = _snippetManage.List(name: filter);
            }
        }

        foreach (var snippet in snippets)
        {
            _snippetsSource.Add(snippet);
        }
    }

    private void AddContextMenu()
    {
        var deleteItem = new MenuItem
        {
            Header = _publicAPI.GetTranslation("snippets_plugin_delete_snippet")
        };
        deleteItem.Click += (o, args) =>
        {
            var selectedIndex = DataGrid.SelectedIndex;
            if (selectedIndex == -1 || selectedIndex >= _snippetsSource.Count) return;
            var sm = _snippetsSource[selectedIndex];
            _snippetsSource.RemoveAt(selectedIndex);
            _snippetManage.RemoveSnippetById(sm.Id);
            _selectSm = null;
            _renderSelect();
        };

        var editItem = new MenuItem
        {
            Header = _publicAPI.GetTranslation("snippets_plugin_edit_snippet")
        };
        editItem.Click += (o, args) =>
        {
            var selectedIndex = DataGrid.SelectedIndex;
            if (selectedIndex == -1 || selectedIndex >= _snippetsSource.Count) return;
            var sm = _snippetsSource[selectedIndex];
            SnippetDialog.ShowDialog(_publicAPI, _snippetManage, sm, this);
        };

        /*
        var moveUp = new MenuItem
        {
            Header = _publicAPI.GetTranslation("snippets_plugin_move_up")
        };
        moveUp.Click += (o, args) =>
        {
            // < 1 ignore first item
            var selectedIndex = DataGrid.SelectedIndex;
            if (selectedIndex < 1 || selectedIndex >= _snippetsSource.Count) return;

            var prev = _snippetsSource[selectedIndex - 1];
            var current = _snippetsSource[selectedIndex];
            _snippetManage.UpdateByKey(new SnippetModel
            {
                Key = current.Key,
                Score = prev.Score + 1
            });
            _loadData();
        };

        var moveDown = new MenuItem
        {
            Header = _publicAPI.GetTranslation("snippets_plugin_move_down")
        };
        moveDown.Click += (o, args) =>
        {
            // ignore latest item
            var selectedIndex = DataGrid.SelectedIndex;
            if (selectedIndex == -1 || selectedIndex >= _snippetsSource.Count - 1) return;

            var current = _snippetsSource[selectedIndex];
            var next = _snippetsSource[selectedIndex + 1];
            _snippetManage.UpdateByKey(new SnippetModel
            {
                Key = current.Key,
                Score = next.Score - 1
            });
            _loadData();
        };

        var resetScore = new MenuItem
        {
            Header = _publicAPI.GetTranslation("snippets_plugin_reset_score")
        };
        resetScore.Click += (o, args) =>
        {
            var selectedIndex = DataGrid.SelectedIndex;
            if (selectedIndex == -1 || selectedIndex >= _snippetsSource.Count) return;
            var sm = _snippetsSource[selectedIndex];
            // reset order score
            _snippetManage.UpdateByKey(new SnippetModel
            {
                Key = sm.Key,
                Score = 0
            });

            _loadData();
            var findIdx = _findBySelectData(_selectSm?.Key);
            if (findIdx != -1)
            {
                _snippetsSource[findIdx] = sm;
                DataGrid.SelectedIndex = findIdx;
            }
        };
        */

        DataGrid.ContextMenu = new ContextMenu
        {
            Items =
            {
                // moveUp,
                // moveDown,
                deleteItem,
                editItem,
                // resetScore
            }
        };
    }

    private void _renderSelect()
    {
        if (_selectSm != null)
        {
            // update
            BtnSwitch.Content = _publicAPI.GetTranslation("snippets_plugin_edit_item_key");

            TbKey.IsEnabled = false;
            TbKey.Text = _selectSm.Name;
            TbValue.Text = _selectSm.Value;
            TbScore.Text = $"{_selectSm.OrderNum}";
        }
        else
        {
            // add
            BtnSwitch.Content = _publicAPI.GetTranslation("snippets_plugin_add_item_key");
            TbKey.IsEnabled = true;
            TbKey.Text = "";
            TbValue.Text = "";
            TbScore.Text = "0";
        }
    }

    private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnCancelButtonClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ButtonSave_OnClick(object sender, RoutedEventArgs e)
    {
        var key = TbKey.Text.Trim();
        var value = TbValue.Text;
        var scoreResult = int.TryParse(TbScore.Text, out var score);
        if (!scoreResult)
            score = 0;

        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
        {
            return;
        }

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
                return;
            }

            _snippetManage.Add(key, value);

            TbKey.Text = "";
            TbValue.Text = "";
            TbScore.Text = "0";
            _loadData();
        }
        else
        {
            // update
            var sm = new SnippetModel
            {
                Name = _selectSm.Name,
                Value = value,
                OrderNum = score
            };
            _snippetManage.UpdateSnippetById(_selectSm.Id, value: value);
            _loadData();

            var findIdx = _findBySelectData(_selectSm?.Name);
            if (findIdx != -1)
            {
                _snippetsSource[findIdx] = sm;
                DataGrid.SelectedIndex = findIdx;
            }
        }
    }

    private void DataGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        InnerLogger.Logger.Info($"DataGrid_OnSelectionChanged. {sender.GetType()}");
        if (sender is DataGrid dataGrid)
        {
            if (dataGrid.SelectedItem is SnippetModel sm)
            {
                _selectSm = sm;
            }

            _renderSelect();
        }
    }

    private void ButtonSwitch_OnClick(object sender, RoutedEventArgs e)
    {
        _selectSm = null;
        DataGrid.UnselectAll();
        _renderSelect();
    }


    private void ButtonReset_OnClick(object sender, RoutedEventArgs e)
    {
        ComboBoxFilterType.SelectedIndex = 0;
        TbFilter.Text = "";

        _selectSm = null;
        DataGrid.UnselectAll();
        _loadData();
    }

    private void ButtonFilter_OnClick(object sender, RoutedEventArgs e)
    {
        _selectSm = null;
        DataGrid.UnselectAll();
        _loadData();
    }

    private void OnTbFilter_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _selectSm = null;
            DataGrid.UnselectAll();
            _loadData();
        }
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