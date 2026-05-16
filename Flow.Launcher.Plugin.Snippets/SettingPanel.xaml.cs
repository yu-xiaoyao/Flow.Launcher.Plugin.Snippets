using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        ComboBoxStorageMode.SelectedIndex = _settings.StorageType == StorageType.Sqlite ? 1 : 0;
        CheckBoxAutoPaste.IsChecked = _settings.AutoPasteEnabled;
        CheckBoxDynamicVariables.IsChecked = _settings.DynamicVariables;

        _initCopyView();

        // CbAutoPasteMethod.Items
    }

    private void _initCopyView()
    {
        // ClipboardUtils.CopyMethod
        ComboBoxCopyMethod.Items.Add("0. Flow Launcher API");
        ComboBoxCopyMethod.Items.Add("1. Dotnet API");
        ComboBoxCopyMethod.Items.Add("2. Win32 Native API");
        ComboBoxCopyMethod.SelectedIndex = _settings.CopyMethod >= 3 ? 0 : _settings.CopyMethod;

        // Auto Paste
        AutoPasteConfigPanel.IsEnabled = _settings.AutoPasteEnabled;
        TbAutoPasteDelayMs.Text = $"{_settings.PasteDelayMs}";

        CbAutoPasteMethod.Items.Add("0. Auto Paste Method. (Default)");
        CbAutoPasteMethod.Items.Add("1. Auto Paste Method. (Hide Flow And Usage Native Send Ctrl+V)");
        CbAutoPasteMethod.Items.Add("2. Auto Paste Method. (Hide Flow And Usage Simple Send Ctrl+V)");
        CbAutoPasteMethod.Items.Add("3. Auto Paste Method. (Enhanced to 0(Default))");
        CbAutoPasteMethod.SelectedIndex = _settings.AutoPasteMethod;
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
                _snippetManage.Add(sm);
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
        var mode = ComboBoxStorageMode.SelectedIndex;
        var storageType = mode == 0 ? StorageType.JsonSetting : StorageType.Sqlite;

        if (storageType != _settings.StorageType)
        {
            _settings.StorageType = storageType;
            _publicApi.SavePluginSettings();
            _publicApi.RestartApp();
        }
    }

    private void CheckBoxAutoPaste_Checked(object sender, RoutedEventArgs e)
    {
        _settings.AutoPasteEnabled = true;
        AutoPasteConfigPanel.IsEnabled = true;
        _publicApi.SavePluginSettings();
    }

    private void CheckBoxAutoPaste_Unchecked(object sender, RoutedEventArgs e)
    {
        _settings.AutoPasteEnabled = false;
        AutoPasteConfigPanel.IsEnabled = false;
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

    private void ButtonSaveSettings_OnClick(object sender, RoutedEventArgs e)
    {
        var delayMs = TbAutoPasteDelayMs.Text.Trim();
        if (int.TryParse(delayMs, out var result))
        {
            _settings.PasteDelayMs = result;
            _publicApi.SavePluginSettings();
        }
    }


    private void AutoPasteDelayMsTextBox(object sender, TextCompositionEventArgs e)
    {
        var text = e.Text;
        if (string.IsNullOrEmpty(text))
        {
            e.Handled = false;
        }
        else
        {
            var regex = NumberRegex();
            e.Handled = regex.IsMatch(text);
        }
    }

    private void ComboBoxCopyMethod_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var index = ComboBoxCopyMethod.SelectedIndex;
        if (Enum.IsDefined(typeof(ClipboardUtils.CopyMethod), index))
        {
            _settings.CopyMethod = index;
            _publicApi.SavePluginSettings();
        }
    }

    private void CbAutoPasteMethod_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var idx = CbAutoPasteMethod.SelectedIndex;
        if (idx >= 4) return;
        _settings.AutoPasteMethod = idx;
        _publicApi.SavePluginSettings();
    }

    [GeneratedRegex("[^0-9]+")]
    private static partial Regex NumberRegex();
}