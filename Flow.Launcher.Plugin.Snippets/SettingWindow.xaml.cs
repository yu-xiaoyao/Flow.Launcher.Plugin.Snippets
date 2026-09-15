using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Flow.Launcher.Plugin.Snippets.Model;
using Flow.Launcher.Plugin.Snippets.Util;
using JetBrains.Annotations;

namespace Flow.Launcher.Plugin.Snippets;

public partial class SettingWindow : Window
{
    private static SettingWindow _instance;

    public static void Show(PluginInitContext context, SnippetManage snippetManage,
        [CanBeNull] SnippetModel selectSm = null)
    {
        if (_instance == null)
        {
            _instance = new SettingWindow(context, snippetManage);
            _instance.Show();
        }
        else
        {
            _instance.Activate();
        }
    }

    public const int IndexAllSnippets = -1;
    public const int IndexFavorites = -2;
    public const int IndexRecent = -3;
    public const int IndexNoFolder = -4;

    private readonly PluginInitContext _context;
    private readonly SnippetManage _snippetManage;


    public FolderModel FolderAllSnippets { get; set; } = new();

    public FolderModel FolderFavorites { get; set; } = new();

    public FolderModel FolderRecent { get; set; } = new();

    public FolderModel FolderNo { get; set; } = new();

    public ObservableCollection<FolderModel> Folders { get; set; } = new();

    public ObservableCollection<SnippetModel> Snippets { get; set; } = new();

    private long _selectFolderId = IndexAllSnippets;

    public SettingWindow(PluginInitContext context, SnippetManage snippetManage)
    {
        _context = context;
        _snippetManage = snippetManage;

        DataContext = this;
        InitializeComponent();

        Activated += (sender, args) => { _loadSnippets(); };
        Closed += (sender, args) => { _instance = null; };

        _loadPluginImage();

        // ComboBoxFilterType.SelectedIndex = 0;
        // _renderItemSelectStyle(false);
        _reloadFolders();
        _loadSnippets();
        // _loadAllSnippets();
    }

    #region Init

    private void _loadPluginImage()
    {
        var ico = Utils.LoadPluginIcon(_context);
        if (ico == null) return;
        Icon = ico;
        IconImage.Source = ico;

        // header logo
        Logo.Source = ico;
    }

    #endregion

    #region Data Load

    private List<FolderModel> _getFolders([CanBeNull] string name = null)
    {
        return _snippetManage.ListFolders(name);
    }

    private void _reloadFolders()
    {
        Folders.Clear();
        foreach (var folderModel in _getFolders())
            Folders.Add(folderModel);
    }

    private void _loadSnippets()
    {
        var name = TbFilterName.Text.Trim();
        var value = TbFilterValue.Text.Trim();
        var folderName = TbFilterFolder.Text.Trim();

        Snippets.Clear();
        List<SnippetModel> queryList;
        switch (_selectFolderId)
        {
            case IndexAllSnippets:
                FolderAllSnippets.IsSelected = true;
                TbFilterFolder.IsEnabled = true;
                queryList = _snippetManage.List(name: name, value: value, folderName: folderName);
                break;
            case IndexFavorites:
                FolderFavorites.IsSelected = true;
                TbFilterFolder.IsEnabled = true;
                queryList = _snippetManage.List(name: name, value: value, favorites: true, folderName: folderName);
                break;
            case IndexRecent:
                FolderRecent.IsSelected = true;
                TbFilterFolder.IsEnabled = true;
                queryList = _snippetManage.ListRecent(name: name, value: value, folderName: folderName);
                break;
            case IndexNoFolder:
                FolderNo.IsSelected = true;
                TbFilterFolder.IsEnabled = false;
                queryList = _snippetManage.ListNoFolder(name, value);
                break;
            default:

                if (_selectFolderId > 0)
                {
                    TbFilterFolder.IsEnabled = false;
                    queryList = _snippetManage.List(name: name, value: value, folderId: _selectFolderId,
                        folderName: folderName);
                }
                else
                {
                    TbFilterFolder.IsEnabled = true;
                    queryList = new List<SnippetModel>();
                }

                break;
        }

        foreach (var sm in queryList)
            Snippets.Add(sm);
    }


    private void _reloadSnippets(long folderId)
    {
        _selectFolderId = folderId;
        Snippets.Clear();
        foreach (var s in _snippetManage.List(folderId: folderId))
            Snippets.Add(s);
    }

    #endregion

    private void ClearFolderListSelected()
    {
        foreach (var f in Folders)
            f.IsSelected = false;
    }

    private void ClearInnerSelected()
    {
        FolderAllSnippets.IsSelected = false;
        FolderFavorites.IsSelected = false;
        FolderRecent.IsSelected = false;
        FolderNo.IsSelected = false;
    }


    #region All Snippets Events

    private void DataGridSnippets_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        var selectedCount = DataGridSnippets.SelectedItems.Count;
        // 场景 A：如果用户点在了空白处，一行都没选，直接拦截事件，不弹菜单
        if (selectedCount == 0)
        {
            e.Handled = true; // 意思是“事件已处理”，系统就不会再弹出菜单了
            return;
        }

        // 场景 B：只选中了 1 行
        if (selectedCount == 1)
        {
            SnippetContextMenuEdit.IsEnabled = true;
            SnippetContextMenuDelete.IsEnabled = true;
            SnippetContextMenuMoveUp.IsEnabled = true;
            SnippetContextMenuMoveDown.IsEnabled = true;
        }
        // 场景 C：选中了多行
        else
        {
            SnippetContextMenuEdit.IsEnabled = false;
            SnippetContextMenuDelete.IsEnabled = true;
            SnippetContextMenuMoveUp.IsEnabled = false;
            SnippetContextMenuMoveDown.IsEnabled = false;
        }
    }

    private void DataGridSnippetsCommandBinding_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = DataGridSnippets.SelectedItems.Count > 0;
        e.Handled = true;
    }

    private void DataGridSnippetsCommandBinding_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        var selectedItems = DataGridSnippets.SelectedItems;
        if (selectedItems.Count > 0)
        {
            foreach (var selectedItem in selectedItems)
            {
                if (selectedItem is SnippetModel snippet)
                {
                    _snippetManage.RemoveSnippetById(snippet.Id);
                }
            }

            _loadSnippets();
        }

        e.Handled = true;
    }

    private void AllSnippetsFolder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        AllSnippetsFolderClick();
    }

    private void AllSnippetsFolder_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        AllSnippetsFolderClick();
    }

    private void AllSnippetsFolderClick()
    {
        FolderAllSnippets.IsSelected = true;
        FolderFavorites.IsSelected = false;
        FolderRecent.IsSelected = false;
        FolderNo.IsSelected = false;
        ClearFolderListSelected();
        _selectFolderId = IndexAllSnippets;
        _loadSnippets();
    }

    #endregion

    #region Favorites Events

    private void FavoritesFolder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        FavoritesFolderClick();
    }

    private void FavoritesFolder_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        FavoritesFolderClick();
    }

    private void FavoritesFolderClick()
    {
        FolderAllSnippets.IsSelected = false;
        FolderFavorites.IsSelected = true;
        FolderRecent.IsSelected = false;
        FolderNo.IsSelected = false;
        ClearFolderListSelected();
        _selectFolderId = IndexFavorites;
        _loadSnippets();
    }

    #endregion

    #region Recent Events

    private void RecentFolder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        RecentFolderClick();
    }

    private void RecentFolder_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        RecentFolderClick();
    }

    private void RecentFolderClick()
    {
        FolderAllSnippets.IsSelected = false;
        FolderFavorites.IsSelected = false;
        FolderRecent.IsSelected = true;
        FolderNo.IsSelected = false;
        ClearFolderListSelected();
        _selectFolderId = IndexRecent;
        _loadSnippets();
    }

    #endregion

    #region No Folder Events

    private void NoFolder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        NoFolderClick();
    }


    private void NoFolder_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        NoFolderClick();
    }

    private void NoFolderClick()
    {
        FolderAllSnippets.IsSelected = false;
        FolderFavorites.IsSelected = false;
        FolderRecent.IsSelected = false;
        FolderNo.IsSelected = true;
        ClearFolderListSelected();
        _selectFolderId = IndexNoFolder;
        _loadSnippets();
    }

    #endregion


    #region Folder List

    private void FolderList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        FolderListClick(sender as Border);
    }

    private void FolderList_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        FolderListClick(sender as Border);
    }

    private void FolderListClick([CanBeNull] Border border)
    {
        if (border?.DataContext is FolderModel clicked)
        {
            foreach (var f in Folders)
                f.IsSelected = false;
            clicked.IsSelected = true;
            ClearInnerSelected();
            _reloadSnippets(clicked.Id);
            _selectFolderId = clicked.Id;
            _loadSnippets();
        }
    }


    private void FolderList_MoveUpOnClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem &&
            menuItem.Parent is ContextMenu menu &&
            menu.PlacementTarget is Border border &&
            border.DataContext is FolderModel folder)
        {
            var newOrderNum = _snippetManage.GetFolderUpOrderNum(folder.Id, folder.OrderNum);
            _updateFolderNewOrderNum(folder, newOrderNum);
        }
    }


    private void FolderList_MoveDownOnClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem &&
            menuItem.Parent is ContextMenu menu &&
            menu.PlacementTarget is Border border &&
            border.DataContext is FolderModel folder)
        {
            var newOrderNum = _snippetManage.GetFolderDownOrderNum(folder.Id, folder.OrderNum);
            _updateFolderNewOrderNum(folder, newOrderNum);
        }
    }

    private void FolderList_MoveTopOnClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem &&
            menuItem.Parent is ContextMenu menu &&
            menu.PlacementTarget is Border border &&
            border.DataContext is FolderModel folder)
        {
            var newOrderNum = _snippetManage.GetFolderMinOrderNum(folder.Id);
            _updateFolderNewOrderNum(folder, newOrderNum);
        }
    }

    private void FolderList_MoveBottomOnClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem &&
            menuItem.Parent is ContextMenu menu &&
            menu.PlacementTarget is Border border &&
            border.DataContext is FolderModel folder)
        {
            var newOrderNum = _snippetManage.GetFolderMaxOrderNum(folder.Id);
            _updateFolderNewOrderNum(folder, newOrderNum);
        }
    }

    private void _updateFolderNewOrderNum(FolderModel folder, long? newOrderNum)
    {
        if (newOrderNum != null)
        {
            var result = _snippetManage.UpdateFolderById(folder.Id, orderNum: newOrderNum);
            if (result)
            {
                _reloadFolders();
            }
        }
    }


    private void FolderList_RenameFolderOnClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem &&
            menuItem.Parent is ContextMenu menu &&
            menu.PlacementTarget is Border border &&
            border.DataContext is FolderModel folder)
        {
            var fed = new FolderEditDialog(_context, _snippetManage, folder)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            var result = fed.ShowDialog();
            if (result == true)
            {
                _reloadFolders();
            }
        }
    }

    private void FolderList_DeleteFolderOnClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem &&
            menuItem.Parent is ContextMenu menu &&
            menu.PlacementTarget is Border border &&
            border.DataContext is FolderModel folder)
        {
            // _snippetManage.RemoveFolderById(folder.Id);
            // _reloadFolders();
            var result = _context.API.ShowMsgBox(
                string.Format(_context.API.GetTranslation("snippets_plugin_confirm_delete"), folder.Name),
                _context.API.GetTranslation("snippets_plugin_confirm_delete"),
                button: MessageBoxButton.YesNo, icon: MessageBoxImage.Asterisk);
            if (result == MessageBoxResult.Yes)
            {
                _snippetManage.RemoveFolderById(folder.Id);
                _reloadFolders();
                Snippets.Clear();
            }
        }
    }

    #endregion


    #region Left Events

    private void BtnAddFolder_OnClick(object sender, RoutedEventArgs e)
    {
        var fed = new FolderEditDialog(_context, _snippetManage)
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var result = fed.ShowDialog();
        if (result == true)
        {
            _reloadFolders();
        }
    }

    #endregion


    private void BtnAddSnippets_Click(object sender, RoutedEventArgs e)
    {
        // IsEditing = true;
        _openEditSnippetDialog();
    }


    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RefreshMaximizeRestoreButton();
    }

    private void OnClosed(object sender, EventArgs e)
    {
    }

    private void OnCloseExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        Close();
    }


    private void Window_StateChanged(object sender, EventArgs e)
    {
        RefreshMaximizeRestoreButton();
    }


    private void OnCancelButtonClick(object sender, RoutedEventArgs e)
    {
        Close();
    }


    private void BtnReset_OnClick(object sender, RoutedEventArgs e)
    {
        TbFilterName.Text = "";
        TbFilterValue.Text = "";
        TbFilterFolder.Text = "";
        _loadSnippets();
    }

    private void BtnFilter_OnClick(object sender, RoutedEventArgs e)
    {
        _loadSnippets();
    }


    private void DataGridSnippets_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // 确保点击的是行，而不是空白处
        var row = ItemsControl.ContainerFromElement(
            (DataGrid)sender,
            e.OriginalSource as DependencyObject) as DataGridRow;

        if (row?.Item is SnippetModel item)
        {
            // IsEditing = true;
            // 调用 ViewModel 命令，传入选中项
            // (DataContext as SnippetModel)?.BeginEditCommand.Execute(item);
            _openEditSnippetDialog(item);
        }
    }

    private void DataGrid_EditSnippetOnClick(object sender, RoutedEventArgs e)
    {
        if (DataGridSnippets.SelectedItem is SnippetModel snippet)
        {
            _openEditSnippetDialog(snippet);
        }
    }

    private void DataGrid_DeleteSnippetOnClick(object sender, RoutedEventArgs e)
    {
        var selectedItems = DataGridSnippets.SelectedItems;
        if (selectedItems.Count <= 0) return;
        foreach (var selectedItem in selectedItems)
        {
            if (selectedItem is SnippetModel snippet)
            {
                _snippetManage.RemoveSnippetById(snippet.Id);
            }
        }

        _loadSnippets();

        // if (DataGridSnippets.SelectedItem is SnippetModel snippet)
        // {
        //     _snippetManage.RemoveSnippetById(snippet.Id);
        //     _loadSnippets();
        // }
    }

    private void DataGrid_MoveUpOnClick(object sender, RoutedEventArgs e)
    {
        if (DataGridSnippets.SelectedItem is SnippetModel snippet)
        {
            if (_selectFolderId > 0 || _selectFolderId == IndexAllSnippets)
            {
                // Snippets are sorted by order_num desc, so "up" in the visual list means larger order_num
                var newOrderNum = _snippetManage.GetSnippetDownOrderNum(snippet.Id, snippet.OrderNum,
                    _selectFolderId > 0 ? _selectFolderId : null);
                _updateSnippetNewOrderNum(snippet, newOrderNum);
            }
        }
    }

    private void DataGrid_MoveDownOnClick(object sender, RoutedEventArgs e)
    {
        if (DataGridSnippets.SelectedItem is SnippetModel snippet)
        {
            if (_selectFolderId > 0 || _selectFolderId == IndexAllSnippets)
            {
                // Snippets are sorted by order_num desc, so "down" in the visual list means smaller order_num
                var newOrderNum = _snippetManage.GetSnippetUpOrderNum(snippet.Id, snippet.OrderNum,
                    _selectFolderId > 0 ? _selectFolderId : null);
                _updateSnippetNewOrderNum(snippet, newOrderNum);
            }
        }
    }

    private void _updateSnippetNewOrderNum(SnippetModel snippet, long? newOrderNum)
    {
        if (newOrderNum != null)
        {
            var result = _snippetManage.UpdateSnippetById(snippet.Id, orderNum: newOrderNum);
            if (result)
            {
                _loadSnippets();
            }
        }
    }

    private void _openEditSnippetDialog([CanBeNull] SnippetModel sm = null)
    {
        SnippetEditWindows ew;
        if (sm == null)
            ew = new SnippetEditWindows(_context, _snippetManage, _selectFolderId > 0 ? _selectFolderId : null);
        else
            ew = new SnippetEditWindows(_context, _snippetManage, sm);
        ew.Owner = this;
        ew.WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var result = ew.ShowDialog();
        if (result == true)
        {
            _loadSnippets();
        }
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