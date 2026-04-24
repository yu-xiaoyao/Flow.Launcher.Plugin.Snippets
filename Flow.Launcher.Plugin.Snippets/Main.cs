using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using Flow.Launcher.Plugin.Snippets.Sqlite;
using Flow.Launcher.Plugin.Snippets.Update;
using Flow.Launcher.Plugin.Snippets.Util;

namespace Flow.Launcher.Plugin.Snippets
{
    public class Snippets : IPlugin, IPluginI18n, IContextMenu, ISettingProvider, IDisposable
    {
        public static readonly string IconPath = "Images\\Snippet.png";

        private PluginInitContext _context;
        private Settings _settings;
        private SnippetManage _snippetManage;

        public void Init(PluginInitContext context)
        {
            _context = context;
            _settings = _context.API.LoadSettingJsonStorage<Settings>();

            InnerLogger.SetAsFlowLauncherLogger(_context, LoggerLevel.DEBUG);

            var pluginSettingPath = context.CurrentPluginMetadata.PluginSettingsDirectoryPath;

            var needUpdateDb = _settings.StorageType == StorageType.Sqlite;

            _snippetManage = new SqliteSnippetManage(pluginSettingPath, needUpdateDb);

            // upgrade
            if (!needUpdateDb)
            {
                _upgradeJsonToSqlite(pluginSettingPath);
            }
        }

        public List<Result> Query(Query query)
        {
            var search = query.Search;

            // all data
            if (string.IsNullOrEmpty(search))
            {
                return _snippetManage.List().Select(sm => _modelToResult(query, sm)).ToList();
            }

            // fuzzy search
            var results = _snippetManage.List(name: search).Select(sm => _modelToResult(query, sm)).ToList();

            if (!results.Any() && query.SearchTerms.Length >= 2)
            {
                _appendSnippets(query, results);
            }

            return results;
        }

        private Result _modelToResult(Query query, SnippetModel sm)
        {
            var key = sm.Name ?? string.Empty;
            var value = sm.Value ?? string.Empty;
            return new Result
            {
                Title = key,
                SubTitle = value.Replace("\r\n", "  ").Replace("\n", "  "),
                IcoPath = IconPath,
                AutoCompleteText = $"{query.ActionKeyword} {key}",
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

                        // copy to clipboard first
                        _context.API.CopyToClipboard(expandedValue, showDefaultNotification: false);

                        // after Flow Launcher hides, wait until Flow Launcher no longer has focus and paste into previous active window
                        if (_settings.AutoPasteEnabled)
                        {
                            Task.Run(() =>
                                AutoPasteHelper.PasteWhenFocusRestoredAsyncNew(_context, _settings.PasteDelayMs));
                        }
                    }
                    catch (Exception ex)
                    {
                        InnerLogger.Logger.Error("Snippets Action", ex);
                    }

                    return true;
                }
            };
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

        private void _add(string key, string value)
        {
            _snippetManage.Add(key, value);
        }

        private void _update(string key, string value)
        {
            // _snippetManage.UpdateSnippetById(key, value: value);
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            var menus = new List<Result>();
            var contextData = selectedResult.ContextData;
            if (contextData is SnippetModel sm)
            {
                //TODO TEST
                menus.Add(new Result
                {
                    Title = "New SettingWindow",
                    IcoPath = IconPath,
                    Action = _ =>
                    {
                        var sw = new SettingWindow(_context, _snippetManage);
                        sw.Show();

                        // var ew = new SnippetEditWindows(_snippetManage);
                        // ew.ShowDialog();

                        return true;
                    }
                });

                menus.Add(new Result
                {
                    Title = _context.API.GetTranslation("snippets_plugin_edit_snippet"),
                    SubTitle = string.Format(_context.API.GetTranslation("snippets_plugin_edit_snippet_info"),
                        sm.Name, sm.Value.Replace("\r\n", "  ").Replace("\n", "  ")),
                    IcoPath = IconPath,
                    Action = _ =>
                    {
                        FormWindows.ShowWindows(_context.API, _snippetManage, sm);
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

                // new edit
                menus.Add(new Result
                {
                    Title = _context.API.GetTranslation("snippets_plugin_edit_snippet"),
                    SubTitle = string.Format(_context.API.GetTranslation("snippets_plugin_edit_snippet_info"),
                        sm.Name, sm.Value.Replace("\r\n", "  ").Replace("\n", "  ")),
                    IcoPath = IconPath,
                    Action = _ =>
                    {
                        SnippetDialog.ShowDialog(_context.API, _snippetManage, sm);
                        return true;
                    }
                });

                menus.Add(new Result
                {
                    Title = _context.API.GetTranslation("snippets_plugin_add"),
                    IcoPath = IconPath,
                    Action = _ =>
                    {
                        SnippetDialog.ShowDialog(_context.API, _snippetManage);
                        return true;
                    },
                });
                menus.Add(new Result
                {
                    Title = _context.API.GetTranslation("snippets_plugin_manage_snippets"),
                    IcoPath = IconPath,
                    Action = _ =>
                    {
                        FormWindows.ShowWindows(_context.API, _snippetManage);
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
            return new SettingPanel(_context.API, _settings, _snippetManage);
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

        private void _mergeJsonToSqlite(string pluginSettingPath)
        {
            // v1
            var v1JsonPath = Path.Combine(pluginSettingPath, "Settings.json");
            if (File.Exists(v1JsonPath))
            {
                var settings = _context.API.LoadSettingJsonStorage<Settings>();

                // File.Delete(v1JsonPath);
            }

            // v2
            var v2JsonPath = Path.Combine(pluginSettingPath, "JsonSetting.json");
            if (File.Exists(v2JsonPath))
            {
                // File.Delete(v2JsonPath);
            }
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