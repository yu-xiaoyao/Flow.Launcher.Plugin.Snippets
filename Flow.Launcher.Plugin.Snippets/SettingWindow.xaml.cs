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
    public double WindowMinWidth { get; set; } = 1200;
    public double WindowMinHeight { get; set; } = 640;

    private readonly PluginInitContext _context;
    private readonly SnippetManage _snippetManage;

    private long _selectFolderId = -1L;

    public FolderModel FolderAllSnippets { get; set; } = new FolderModel()
    {
        Name = "All Snippets",
        IsSelected = true, // default select
    };

    public FolderModel FolderFavorites { get; set; } = new FolderModel()
    {
        Name = "Favorites",
    };

    public FolderModel FolderRecent { get; set; } = new FolderModel()
    {
        Name = "Recent",
    };

    public FolderModel FolderNo { get; set; } = new FolderModel()
    {
        Name = "No Folder",
    };


    public ObservableCollection<FolderModel> Folders { get; set; } = new();


    public SettingWindow(PluginInitContext context, SnippetManage snippetManage)
    {
        _context = context;
        _snippetManage = snippetManage;

        DataContext = this;

        InitializeComponent();
        // ComboBoxFilterType.SelectedIndex = 0;
        // _renderItemSelectStyle(false);

        foreach (var folderModel in _getFolders())
            Folders.Add(folderModel);
        _loadAllSnippets();
    }


    #region View Event

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
        _loadAllSnippets();
    }

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
        _loadFavorites();
    }


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
        _loadRecent();
    }

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
        _loadNoFolder();
    }

    private void ClearFolderListSelected()
    {
        _selectFolderId = -1L;
        foreach (var f in Folders)
            f.IsSelected = false;
    }

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
            _reloadSnippets(clicked.Id);
            ClearInnerSelected();
        }
    }

    private void ClearInnerSelected()
    {
        FolderAllSnippets.IsSelected = false;
        FolderFavorites.IsSelected = false;
        FolderRecent.IsSelected = false;
        FolderNo.IsSelected = false;
    }

    #endregion


    #region Data Load

    private void _loadAllSnippets()
    {
        var snippets = _snippetManage.List();
    }

    private void _loadFavorites()
    {
        var snippets = _snippetManage.List();
    }

    private void _loadRecent()
    {
        var snippets = _snippetManage.List();
    }

    private void _loadNoFolder()
    {
        var snippets = _snippetManage.List();
    }

    private void _reloadSnippets(long folderId)
    {
        _selectFolderId = folderId;
        var snippets = _snippetManage.List(folderId: folderId);
    }

    private List<FolderModel> _getFolders([CanBeNull] string name = null)
    {
        return _snippetManage.ListFolders(name);
    }

    #endregion

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
}