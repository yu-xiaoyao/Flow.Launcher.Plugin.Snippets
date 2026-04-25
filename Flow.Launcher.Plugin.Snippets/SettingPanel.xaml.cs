using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Flow.Launcher.Plugin.Snippets.Util;
using Microsoft.Win32;

namespace Flow.Launcher.Plugin.Snippets;

public partial class SettingPanel : UserControl
{
    private IPublicAPI _publicApi;
    private Settings _settings;
    private SnippetManage _snippetManage;

    public SettingPanel(IPublicAPI contextApi, Settings settings, SnippetManage snippetManage)
    {
        _publicApi = contextApi;
        _settings = settings;
        _snippetManage = snippetManage;
        InitializeComponent();

        // ComboBoxStorageMode.SelectedIndex = _settings.StorageType == StorageType.Sqlite ? 1 : 0;
        CheckBoxAutoPaste.IsChecked = _settings.AutoPasteEnabled;
        CheckBoxDynamicVariables.IsChecked = _settings.DynamicVariables;
    }

    private void ButtonOpenManage_OnClick(object sender, RoutedEventArgs e)
    {
        FormWindows.ShowWindows(_publicApi, _snippetManage);
        /*var fw = new FormWindows(_publicApi, _snippetManage)
        {
            // Title = _publicApi.GetTranslation("snippets_plugin_manage_snippets"),
            // WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Topmost = true,
            // WindowState = WindowState.Normal,
            // ResizeMode = ResizeMode.NoResize,
            // ShowInTaskbar = false
        };
        fw.ShowDialog();*/
    }

    private void ButtonResetScore_OnClick(object sender, RoutedEventArgs e)
    {
        _snippetManage.ResetAllScore();
    }

    private void ButtonClear_OnClick(object sender, RoutedEventArgs e)
    {
        _snippetManage.Clear();
    }

    private void ButtonImport_OnClick(object sender, RoutedEventArgs e)
    {
        // select .json file
        var dialog = new OpenFileDialog
        {
            Filter = "Json file (*.json)|*.json",
            Title = _publicApi.GetTranslation("snippets_plugin_select_json_file"),
            Multiselect = false
        };

        if (dialog.ShowDialog() != true) return;

        var file = dialog.FileName;

        if (!File.Exists(file))
        {
            _publicApi.ShowMsgError(_publicApi.GetTranslation("snippets_plugin_error"),
                _publicApi.GetTranslation("snippets_plugin_file_not_found"));
            return;
        }

        Task.Run(() =>
        {
            var sms = FileUtil.ReadSnippets(file);
            foreach (var sm in sms)
            {
                _snippetManage.Add(sm.Name, sm.Value, folderId: sm.FolderId);
            }
        });
    }

    private void ButtonExport_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Json file (*.json)|*.json",
            Title = _publicApi.GetTranslation("snippets_plugin_save_json_file"),
            FileName = "snippets.json"
        };
        if (dialog.ShowDialog() != true) return;
        var file = dialog.FileName;
        var list = _snippetManage.List();
        Task.Run(() => FileUtil.WriteSnippets(file, list));
    }

    private void ButtonChangeAndRestart_OnClick(object sender, RoutedEventArgs e)
    {
        // var mode = ComboBoxStorageMode.SelectedIndex;
        // var storageType = mode == 0 ? StorageType.JsonSetting : StorageType.Sqlite;
        //
        // if (storageType != _settings.StorageType)
        // {
        //     _settings.StorageType = storageType;
        //     _publicApi.SavePluginSettings();
        //     _publicApi.RestartApp();
        // }
    }

    private void CheckBoxAutoPaste_Checked(object sender, RoutedEventArgs e)
    {
        _settings.AutoPasteEnabled = true;
        _publicApi.SavePluginSettings();
    }

    private void CheckBoxAutoPaste_Unchecked(object sender, RoutedEventArgs e)
    {
        _settings.AutoPasteEnabled = false;
        _publicApi.SavePluginSettings();
    }


    private void CheckBoxDynamicVariables_Checked(object sender, RoutedEventArgs e)
    {
        _settings.DynamicVariables = true;
        _publicApi.SavePluginSettings();
    }

    private void CheckBoxDynamicVariables_Unchecked(object sender, RoutedEventArgs e)
    {
        _settings.DynamicVariables = false;
        _publicApi.SavePluginSettings();
    }
}