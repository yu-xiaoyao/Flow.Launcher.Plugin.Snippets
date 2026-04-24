using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Flow.Launcher.Plugin.Snippets.Model;
using Flow.Launcher.Plugin.Snippets.Util;
using JetBrains.Annotations;

namespace Flow.Launcher.Plugin.Snippets;

public partial class SettingWindow : Window, INotifyPropertyChanged
{
    public const int IndexAllSnippets = -1;
    public const int IndexFavorites = -2;
    public const int IndexRecent = -3;
    public const int IndexNoFolder = -4;

    public double WindowMinWidth { get; set; } = 1200;
    public double WindowMinHeight { get; set; } = 640;

    private readonly PluginInitContext _context;
    private readonly SnippetManage _snippetManage;


    public FolderModel FolderAllSnippets { get; set; } = new();

    public FolderModel FolderFavorites { get; set; } = new();

    public FolderModel FolderRecent { get; set; } = new();

    public FolderModel FolderNo { get; set; } = new();


    public ObservableCollection<FolderModel> Folders { get; set; } = new();

    public ObservableCollection<SnippetModel> Snippets { get; set; } = new();

    private long _selectFolderId = IndexAllSnippets;


    #region UI Fields

    private bool _isEditing;

    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            _isEditing = value;
            OnPropertyChanged();
        }
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion


    public SettingWindow(PluginInitContext context, SnippetManage snippetManage)
    {
        _context = context;
        _snippetManage = snippetManage;

        DataContext = this;

        InitializeComponent();
        // ComboBoxFilterType.SelectedIndex = 0;
        // _renderItemSelectStyle(false);
        _reloadFolders();
        _loadSnippets();
        // _loadAllSnippets();
    }

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
        IsEditing = false;

        Snippets.Clear();

        List<SnippetModel> queryList;
        switch (_selectFolderId)
        {
            case IndexAllSnippets:
                FolderAllSnippets.IsSelected = true;
                queryList = _snippetManage.List();
                break;
            case IndexFavorites:
                FolderFavorites.IsSelected = true;
                queryList = _snippetManage.List(favorites: true);
                break;
            case IndexRecent:
                FolderRecent.IsSelected = true;
                queryList = _snippetManage.ListRecent();
                break;
            case IndexNoFolder:
                FolderNo.IsSelected = true;
                queryList = _snippetManage.ListNoFolder();
                break;
            default:
                queryList = _selectFolderId > 0
                    ? _snippetManage.List(folderId: _selectFolderId)
                    : new List<SnippetModel>();
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
        InnerLogger.Logger.Info($"FolderList_MouseLeftButtonUp. {sender}");
        FolderListClick(sender as Border);
    }

    private void FolderList_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        InnerLogger.Logger.Info("FolderList_MouseRightButtonUp");
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
            //TODO
        }
    }

    private void FolderList_MoveDownOnClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem &&
            menuItem.Parent is ContextMenu menu &&
            menu.PlacementTarget is Border border &&
            border.DataContext is FolderModel folder)
        {
            //TODO
        }
    }

    private void FolderList_RenameFolderOnClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem &&
            menuItem.Parent is ContextMenu menu &&
            menu.PlacementTarget is Border border &&
            border.DataContext is FolderModel folder)
        {
            var fed = new FolderEditDialog(_context, _snippetManage, folder);
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
            _snippetManage.RemoveFolderById(folder.Id);
            _reloadFolders();
        }
    }

    #endregion


    #region Left Events

    private void BtnAddFolder_OnClick(object sender, RoutedEventArgs e)
    {
        _snippetManage.AddFolder($"Folder - {new Random().Next()}");
        var fed = new FolderEditDialog(_context, _snippetManage);
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
        var ew = new SnippetEditWindows(_snippetManage);
        ew.ShowDialog();
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


    private void BtnReset_OnClick(object sender, RoutedEventArgs e)
    {
        TbFilterKey.Text = "";
        TbFilterValue.Text = "";
        TbFilterFolder.Text = "";
    }

    private void BtnFilter_OnClick(object sender, RoutedEventArgs e)
    {
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


    private void DataGridSnippets_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // 确保点击的是行，而不是空白处
        var row = ItemsControl.ContainerFromElement(
            (DataGrid)sender,
            e.OriginalSource as DependencyObject) as DataGridRow;

        if (row?.Item is SnippetModel item)
        {
            IsEditing = true;
            // 调用 ViewModel 命令，传入选中项
            // (DataContext as SnippetModel)?.BeginEditCommand.Execute(item);
        }
    }
}