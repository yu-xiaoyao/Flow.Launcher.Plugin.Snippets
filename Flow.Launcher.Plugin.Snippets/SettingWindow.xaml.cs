using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Flow.Launcher.Plugin.Snippets.Model;
using JetBrains.Annotations;

namespace Flow.Launcher.Plugin.Snippets;

public partial class SettingWindow : Window
{
    public double WindowMinWidth { get; set; } = 1200;
    public double WindowMinHeight { get; set; } = 640;

    private readonly PluginInitContext _context;
    private readonly SnippetManage _snippetManage;

    private List<FolderModel> _folders;

    public SettingWindow(PluginInitContext context, SnippetManage snippetManage)
    {
        _context = context;
        _snippetManage = snippetManage;
        InitializeComponent();
        // ComboBoxFilterType.SelectedIndex = 0;
        _folders = _getFolders();
    }

    #region 数据加载

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