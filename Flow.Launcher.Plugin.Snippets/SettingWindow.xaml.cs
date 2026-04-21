using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Flow.Launcher.Plugin.Snippets.Model;
using Flow.Launcher.Plugin.Snippets.Util;
using JetBrains.Annotations;

namespace Flow.Launcher.Plugin.Snippets;

public partial class SettingWindow : Window
{
    public double WindowMinWidth { get; set; } = 1200;
    public double WindowMinHeight { get; set; } = 640;

    public const int AllSnippetsIndex = -1;
    public const int FavoritesIndex = -2;
    public const int RecentIndex = -3;
    public const int NoIndex = -4;

    private readonly PluginInitContext _context;
    private readonly SnippetManage _snippetManage;


    private int _selectFolder = AllSnippetsIndex;


    public FolderModel AllSnippets { get; set; } = new FolderModel()
    {
        Name = "All Snippets",
        IsSelected = true
    };


    public ObservableCollection<FolderModel> Folders { get; set; } = new();


    public SettingWindow(PluginInitContext context, SnippetManage snippetManage)
    {
        _context = context;
        _snippetManage = snippetManage;

        DataContext = this;

        InitializeComponent();
        // ComboBoxFilterType.SelectedIndex = 0;
        _renderItemSelectStyle(false);

        foreach (var folderModel in _getFolders())
        {
            Folders.Add(folderModel);
        }

        foreach (var folderModel in _getFolders())
        {
            Folders.Add(folderModel);
        }

        foreach (var folderModel in _getFolders())
        {
            Folders.Add(folderModel);
        }

        foreach (var folderModel in _getFolders())
        {
            Folders.Add(folderModel);
        }
    }


    #region View Event

    private void BorderFolder_MouseEnter(object sender, MouseEventArgs e)
    {
        // 鼠标移入时的逻辑
        var bd = sender as Border;
        if (bd != null)
        {
            // bd.Background = Brushes.DarkGray;
            // 你还可以在这里启动动画、记录日志或弹出提示
        }
    }

    private void BorderFolder_MouseLeave(object sender, MouseEventArgs e)
    {
        // 鼠标移出时恢复原状
        var bd = sender as Border;
        if (bd != null)
        {
            // bd.Background = Brushes.White;
        }
    }

    private void _renderItemSelectStyle(bool reset)
    {
        var b = reset ? Brushes.White : Brushes.LightBlue;
        if (_selectFolder < 0)
        {
            switch (_selectFolder)
            {
                case AllSnippetsIndex:
                    AllSnippetsFolder.Background = b;
                    break;
                case FavoritesIndex:
                    FavoritesFolder.Background = b;
                    break;
                case RecentIndex:
                    RecentFolder.Background = b;
                    break;
                case NoIndex:
                    NoFolder.Background = b;
                    break;
            }
        }
        else if (_selectFolder > 0)
        {
        }
    }


    private void AllSnippetsFolder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        ClearFolderListSelected();
        _renderItemSelectStyle(true);
        _selectFolder = AllSnippetsIndex;
        AllSnippetsFolder.Background = Brushes.LightBlue;
    }

    private void FavoritesFolder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        ClearFolderListSelected();
        _renderItemSelectStyle(true);
        _selectFolder = FavoritesIndex;
        FavoritesFolder.Background = Brushes.LightBlue;
    }

    private void RecentFolder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        ClearFolderListSelected();
        _renderItemSelectStyle(true);
        _selectFolder = RecentIndex;
        RecentFolder.Background = Brushes.LightBlue;
    }

    private void NoFolder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        ClearFolderListSelected();
        _renderItemSelectStyle(true);
        _selectFolder = NoIndex;
        NoFolder.Background = Brushes.LightBlue;
    }


    private void AllSnippetsFolder_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        ClearFolderListSelected();
        _renderItemSelectStyle(true);
        _selectFolder = AllSnippetsIndex;
        AllSnippetsFolder.Background = Brushes.LightBlue;
    }

    private void FavoritesFolder_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        ClearFolderListSelected();
        _renderItemSelectStyle(true);
        _selectFolder = FavoritesIndex;
        FavoritesFolder.Background = Brushes.LightBlue;
    }

    private void RecentFolder_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        ClearFolderListSelected();
        _renderItemSelectStyle(true);
        _selectFolder = RecentIndex;
        RecentFolder.Background = Brushes.LightBlue;
    }

    private void NoFolder_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        ClearFolderListSelected();
        _renderItemSelectStyle(true);
        _selectFolder = NoIndex;
        NoFolder.Background = Brushes.LightBlue;
    }


    private void FolderList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        InnerLogger.Logger.Info($"FolderList_MouseLeftButtonUp. {sender}");
        if ((sender as Border)?.DataContext is FolderModel clicked)
        {
            InnerLogger.Logger.Info($"FolderList_MouseLeftButtonUp. {clicked}");
            foreach (var f in Folders)
                f.IsSelected = false;
            clicked.IsSelected = true;
            ClearInnerSelected();
        }
    }

    private void FolderList_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        InnerLogger.Logger.Info("FolderList_MouseRightButtonUp");
        if ((sender as Border)?.DataContext is FolderModel clicked)
        {
            foreach (var f in Folders)
                f.IsSelected = false;
            clicked.IsSelected = true;
            ClearInnerSelected();
        }
    }

    private void ClearFolderListSelected()
    {
        foreach (var f in Folders)
            f.IsSelected = false;
    }

    private void ClearInnerSelected()
    {
        AllSnippetsFolder.Background = Brushes.White;
        FavoritesFolder.Background = Brushes.White;
        RecentFolder.Background = Brushes.White;
        NoFolder.Background = Brushes.White;
    }

    #endregion


    #region Data Load

    private List<FolderModel> _getFolders([CanBeNull] string name = null)
    {
        return new List<FolderModel>()
        {
            new FolderModel()
            {
                Id = 1,
                OrderNum = 1,
                Name = "Java",
                UpdateTime = DateTime.Now,
            },
            new FolderModel()
            {
                Id = 2,
                OrderNum = 2,
                Name = "Rust",
                UpdateTime = DateTime.Now,
            },
            new FolderModel()
            {
                Id = 3,
                OrderNum = 3,
                Name = "GoLang",
                UpdateTime = DateTime.Now,
            },
            new FolderModel()
            {
                Id = 4,
                OrderNum = 4,
                Name = "Python",
                UpdateTime = DateTime.Now,
            }
        };
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