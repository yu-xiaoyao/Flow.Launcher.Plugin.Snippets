using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Flow.Launcher.Plugin.Snippets.Model;
using Flow.Launcher.Plugin.Snippets.Sqlite;
using Flow.Launcher.Plugin.Snippets.Update;
using Flow.Launcher.Plugin.Snippets.Util;
using Control = System.Windows.Controls.Control;

namespace Flow.Launcher.Plugin.Snippets
{
    public class Snippets : IPlugin, IPluginI18n, IContextMenu, ISettingProvider, IDisposable
    {
        public const string PluginPngIconPath = "Images\\Snippets.png";
        public const string PluginIcoPath = "Images\\Snippets.ico";

        /// <summary>
        /// Result List Icon
        /// </summary>
        public static readonly string IconPath = "Images\\Snippet.png";

        public static readonly string FolderIconPath = "Images\\Folder.png";

        private PluginInitContext _context;
        private Settings _settings;
        private SnippetManage _snippetManage;

        public void Init(PluginInitContext context)
        {
            _context = context;
            _settings = _context.API.LoadSettingJsonStorage<Settings>();

            // add VisibilityChanged Event
            _context.API.VisibilityChanged += VisibilityChangedEventHandler;

            InnerLogger.SetAsFlowLauncherLogger(_context, LoggerLevel.DEBUG);

            var needUpdateDb = _settings.StorageType == StorageType.Sqlite;

            var pluginSettingPath = _context.CurrentPluginMetadata.PluginSettingsDirectoryPath;
            _snippetManage = CreateSnippetManage(pluginSettingPath, needUpdateDb);

            // upgrade
            if (!needUpdateDb)
            {
                _upgradeJsonToSqlite(pluginSettingPath);
            }
        }

        private SnippetManage CreateSnippetManage(string pluginSettingPath, bool needUpdateDb)
        {
            // _snippetManage = new SqliteSnippetManage(pluginSettingPath, needUpdateDb);

            if (_settings.SearchMode == SearchMode.Flow_FuzzySearch)
            {
                return new MsSqliteSnippetManage(pluginSettingPath, _flowLauncherFuzzySearch, true, needUpdateDb);
            }

            return new MsSqliteSnippetManage(pluginSettingPath, needUpdate: needUpdateDb);
        }

        public List<Result> Query(Query query)
        {
            // all data
            if (string.IsNullOrEmpty(query.Search))
            {
                return _snippetManage.List().Select(sm => _modelToResult(query, sm)).ToList();
            }

            var results = new List<Result>();

            var querySearchTerms = query.SearchTerms;

            var snippetKey = query.Search;

            var snippets = new List<Result>();
            if (_settings.FirstKeyPrimaryFolder)
            {
                var folders = _snippetManage.ListFolders(query.FirstSearch);
                if (folders.Count > 0)
                {
                    results.AddRange(folders.Select(fm => _folderToResult(query, fm)));
                    snippetKey = query.SecondToEndSearch;
                    if (!string.IsNullOrEmpty(snippetKey))
                    {
                        snippets = _snippetManage.List(name: snippetKey).Select(sm => _modelToResult(query, sm))
                            .ToList();
                    }
                }
                else
                {
                    // fuzzy search
                    snippets = _snippetManage.List(name: snippetKey).Select(sm => _modelToResult(query, sm)).ToList();
                }
            }
            else
            {
                // fuzzy search
                snippets = _snippetManage.List(name: snippetKey).Select(sm => _modelToResult(query, sm)).ToList();
            }

            if (!snippets.Any() && querySearchTerms.Length >= 2)
            {
                _appendSnippets(query, snippets);
            }

            results.AddRange(snippets);
            return results;
        }

        private Result _modelToResult(Query query, SnippetModel sm)
        {
            var name = sm.Name;
            var value = sm.Value;

            var title = name;
            if (_settings.DisplayFolder && !string.IsNullOrEmpty(sm.FolderName))
            {
                var folder = "";
                if (_settings.DisplayFolderIcon)
                {
                    folder = "📁";
                }

                title = $"{title} | {folder}{sm.FolderName}";
            }

            return new Result
            {
                Title = title,
                SubTitle = value.Replace("\r\n", "  ").Replace("\n", "  "),
                IcoPath = IconPath,
                AutoCompleteText = $"{query.ActionKeyword} {name} ",
                CopyText = value,
                ContextData = sm,
                Preview = new Result.PreviewInfo
                {
                    Description = value,
                    PreviewImagePath = IconPath
                },
                Action = _ =>
                {
                    try
                    {
                        var expandedValue = value;
                        if (_settings.DynamicVariables)
                        {
                            // Expand variables before copying to clipboard
                            expandedValue = VariableExpander.Expand(value);
                        }

                        _copyToClipboard(expandedValue);
                    }
                    catch (Exception ex)
                    {
                        InnerLogger.Logger.Error("Snippets Action", ex);
                    }

                    return true;
                }
            };
        }

        private Result _folderToResult(Query query, FolderModel fm)
        {
            return new Result()
            {
                Title = fm.Name,
                AutoCompleteText = $"{query.ActionKeyword} {fm.Name} ",
                IcoPath = FolderIconPath,
                Action = _ =>
                {
                    _context.API.ChangeQuery($"{query.ActionKeyword} {fm.Name} ", true);
                    return false;
                }
            };
        }

        private void _copyToClipboard(string text)
        {
            switch (_settings.CopyMethod)
            {
                case (int)ClipboardUtils.CopyMethod.Flow:
                    ClipboardUtils.CopyToClipboard(text, new ClipboardUtils.FlowCopyMethod(_context));
                    break;
                case (int)ClipboardUtils.CopyMethod.Win32:
                    ClipboardUtils.CopyToClipboard(text, new ClipboardUtils.Win32CopyMethod());
                    break;
                default:
                    ClipboardUtils.CopyToClipboard(text, new ClipboardUtils.DotnetCopyMethod());
                    break;
            }

            if (_settings.AutoPasteEnabled)
            {
                AutoPasteHelper.AutoPasteAsync(_context, _settings.AutoPasteMethod, _settings.PasteDelayMs, text,
                    _settings.SendCtrlVMethod);
            }
        }

        private void VisibilityChangedEventHandler(object sender, VisibilityChangedEventArgs args)
        {
            if (!args.IsVisible)
            {
                AutoPasteHelper.OnFlowHiddenSendCtrlV(_settings.PasteDelayMs);
            }
        }

        private void _appendSnippets(Query query, List<Result> results)
        {
            var terms = query.SearchTerms;
            var length = terms.Length;

            for (var i = 1; i < length; i++)
            {
                var name = string.Join(" ", terms, 0, i);
                var value = string.Join(" ", terms, i, length - i);

                results.Add(new Result
                {
                    Title = _context.API.GetTranslation("snippets_plugin_add"),
                    SubTitle = string.Format(_context.API.GetTranslation("snippets_plugin_add_info"), name, value),
                    IcoPath = IconPath,
                    Action = c =>
                    {
                        _add(name, value);
                        _context.API.ChangeQuery($"{query.ActionKeyword} {name}", true);
                        return false;
                    }
                });
            }
        }


        private void _add(string name, string value)
        {
            _snippetManage.Add(name, value);
        }

        /*
        private Result _updateSnippets(Query query, string name, string value)
        {
            return new Result
            {
                Title = _context.API.GetTranslation("snippets_plugin_update"),
                SubTitle = string.Format(_context.API.GetTranslation("snippets_plugin_update_info"), name, value),
                IcoPath = IconPath,
                Action = c =>
                {
                    _update(name, value);
                    _context.API.ChangeQuery($"{query.ActionKeyword} {name}", true);
                    return false;
                }
            };
        }

        private void _update(string key, string value)
        {
            // _snippetManage.UpdateSnippetById(key, value: value);
        }
        */

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            var menus = new List<Result>();
            var contextData = selectedResult.ContextData;
            if (contextData is SnippetModel sm)
            {
                menus.Add(new Result
                {
                    Title = _context.API.GetTranslation("snippets_plugin_edit_snippet"),
                    SubTitle = string.Format(_context.API.GetTranslation("snippets_plugin_edit_snippet_info"),
                        sm.Name, sm.Value.Replace("\r\n", "  ").Replace("\n", "  ")),
                    IcoPath = IconPath,
                    Action = _ =>
                    {
                        var w = new SnippetEditWindows(_context, _snippetManage, sm)
                        {
                            WindowStartupLocation = WindowStartupLocation.CenterScreen
                        };
                        w.ShowDialog();

                        return true;
                    }
                });
                menus.Add(new Result
                {
                    Title = _context.API.GetTranslation("snippets_plugin_delete_snippet"),
                    SubTitle = string.Format(_context.API.GetTranslation("snippets_plugin_delete_snippet_info"),
                        sm.Name, sm.Value.Replace("\r\n", "  ").Replace("\n", "  ")),
                    IcoPath = IconPath,
                    Action = _ =>
                    {
                        _snippetManage.RemoveSnippetById(sm.Id);
                        return true;
                    },
                });

                menus.Add(new Result
                {
                    Title = _context.API.GetTranslation("snippets_plugin_manage_snippets"),
                    IcoPath = IconPath,
                    Action = _ =>
                    {
                        SettingWindow.Show(_context, _snippetManage);
                        return true;
                    },
                });

                // open Add Snippet Dialog
                menus.Add(new Result
                {
                    Title = _context.API.GetTranslation("snippets_plugin_add"),
                    IcoPath = IconPath,
                    Action = _ =>
                    {
                        var w = new SnippetEditWindows(_context, _snippetManage)
                        {
                            WindowStartupLocation = WindowStartupLocation.CenterScreen
                        };
                        w.ShowDialog();
                        return true;
                    },
                });
            }

            return menus;
        }


        public string GetTranslatedPluginTitle()
        {
            return _context.API.GetTranslation("snippets_plugin_title");
        }

        public string GetTranslatedPluginDescription()
        {
            return _context.API.GetTranslation("snippets_plugin_description");
        }

        public Control CreateSettingPanel()
        {
            return new SettingPanel(_context, _settings, _snippetManage);
        }

        public void Dispose()
        {
            _snippetManage.Close();
        }

        private List<Result> _buildEmpty(Query query)
        {
            return new List<Result>
            {
                new()
                {
                    Title = _context.API.GetTranslation("snippets_plugin_snippets_empty"),
                    SubTitle = _context.API.GetTranslation("snippets_plugin_snippets_empty_add"),
                    IcoPath = IconPath,
                    AutoCompleteText = $"{query.ActionKeyword} "
                }
            };
        }

        /// <summary>
        /// upgrade v2 -> v3
        /// </summary>
        /// <param name="pluginSettingPath"></param>
        private void _upgradeJsonToSqlite(string pluginSettingPath)
        {
            // merge to Sqlite
            UpgradeHelper.UpgradeJsonToSqlite(_snippetManage, pluginSettingPath);
            _settings.StorageType = StorageType.Sqlite;
            _context.API.SavePluginSettings();
        }

        /// <summary>
        /// 自定义 Sqlite Like 函数
        /// </summary>
        /// <param name="pattern">like 的值</param>
        /// <param name="value">数据库原始值</param>
        /// <returns></returns>
        private bool _flowLauncherFuzzySearch(string pattern, string value)
        {
            // 处理 NULL 值情况
            if (pattern == null || value == null) return false;

            // 2. 剥离 SQL 的 LIKE 通配符，提取纯关键字
            // 当 SQL 执行 LIKE '%abc%' 时，传入的 pattern 实际是字符串 "%abc%"
            // 我们通过 Trim 把首尾的 '%' 去掉，得到 "abc"
            var keyword = pattern.Trim('%').Trim('_');

            // 如果用户输入了全通配符（比如 LIKE '%'），直接返回 true
            if (string.IsNullOrEmpty(keyword)) return true;

            // InnerLogger.Logger.Debug($"MyLike: pattern: {pattern}. value: {value}.");

            var match = _context.API.FuzzySearch(keyword, value);
            return match.Success;
        }

        /// <summary>
        /// v1 version is only save in Plugin Settings
        /// data type: Dictionary <![CDATA[<]]>string, string <![CDATA[>]]> Snippets { get; set; }
        /// 1.x.x version snippets merge to 2.x.x version
        /// </summary>
        // [Obsolete]
        // private void _mergeOldSnippet()
        // {
        //     // Data Type: Dictionary<string, string> Snippets { get; set; }
        //     // old version snippets in Settings.json
        //     var snippets = _settings.Snippets;
        //     if (snippets == null || !snippets.Any()) return;
        //
        //     foreach (var snippet in snippets)
        //     {
        //         _snippetManage.Add(snippet.Key, snippet.Value);
        //     }
        //
        //     // clear old snippets after merge
        //     _settings.Snippets = null;
        //     _context.API.SavePluginSettings();
        // }
    }
}