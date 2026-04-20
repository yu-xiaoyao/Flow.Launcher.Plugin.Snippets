using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Flow.Launcher.Plugin.Snippets.Model;
using JetBrains.Annotations;

namespace Flow.Launcher.Plugin.Snippets;

public partial class SettingWindow : Window
{
    public double WindowMinWidth { get; set; } = 1200;
    public double WindowMinHeight { get; set; } = 640;

    public const int AllSnippets = -1;
    public const int Favorites = -2;
    public const int Recent = -3;
    public const int No = -4;

    private readonly PluginInitContext _context;
    private readonly SnippetManage _snippetManage;


    private int _selectFolder = AllSnippets;

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
                case AllSnippets:
                    AllSnippetsFolder.Background = b;
                    break;
                case Favorites:
                    FavoritesFolder.Background = b;
                    break;
                case Recent:
                    RecentFolder.Background = b;
                    break;
                case No:
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
        _renderItemSelectStyle(true);
        _selectFolder = AllSnippets;
        AllSnippetsFolder.Background = Brushes.LightBlue;
    }

    private void FavoritesFolder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _renderItemSelectStyle(true);
        _selectFolder = Favorites;
        FavoritesFolder.Background = Brushes.LightBlue;
    }

    private void RecentFolder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _renderItemSelectStyle(true);
        _selectFolder = Recent;
        RecentFolder.Background = Brushes.LightBlue;
    }

    private void NoFolder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _renderItemSelectStyle(true);
        _selectFolder = No;
        NoFolder.Background = Brushes.LightBlue;
    }


    private void AllSnippetsFolder_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        _renderItemSelectStyle(true);
        _selectFolder = AllSnippets;
        AllSnippetsFolder.Background = Brushes.LightBlue;
    }

    private void FavoritesFolder_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        _renderItemSelectStyle(true);
        _selectFolder = Favorites;
        FavoritesFolder.Background = Brushes.LightBlue;
    }

    private void RecentFolder_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        _renderItemSelectStyle(true);
        _selectFolder = Recent;
        RecentFolder.Background = Brushes.LightBlue;
    }

    private void NoFolder_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        _renderItemSelectStyle(true);
        _selectFolder = No;
        NoFolder.Background = Brushes.LightBlue;
    }


    private void FolderList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var border = sender as Border;
        if (border?.DataContext is FolderModel folder)
        {
        }
    }

    private void FolderList_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        var border = sender as Border;
        if (border?.DataContext is FolderModel folder)
        {
        }
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