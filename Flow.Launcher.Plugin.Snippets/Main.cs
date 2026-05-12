using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Flow.Launcher.Plugin.Snippets.Json;
using Flow.Launcher.Plugin.Snippets.Sqlite;
using Flow.Launcher.Plugin.Snippets.Util;
using Control = System.Windows.Controls.Control;

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

            InnerLogger.SetAsFlowLauncherLogger(_context.API, LoggerLevel.TRACE);

            if (_settings.StorageType == StorageType.Sqlite)
            {
                _snippetManage =
                    new SqliteSnippetManage(context.CurrentPluginMetadata.PluginSettingsDirectoryPath);
            }
            else
            {
                _snippetManage = new JsonSettingSnippetManage(context);
                _mergeOldSnippet(); // merge old snippets
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
            var results = _snippetManage.List(key: search).Select(sm => _modelToResult(query, sm)).ToList();

            if (!results.Any() && query.SearchTerms.Length >= 2)
            {
                _appendSnippets(query, results);
            }

            return results;
        }

        private Result _modelToResult(Query query, SnippetModel sm)
        {
            var key = sm.Key ?? string.Empty;
            var value = sm.Value ?? string.Empty;
            return new Result
            {
                Title = key,
                SubTitle = value.Replace("\r\n", "  ").Replace("\n", "  "),
                IcoPath = IconPath,
                Score = sm.Score,
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

                        _copyToClipboard(expandedValue);

                        // copy to clipboard first
                        // _context.API.CopyToClipboard(expandedValue, showDefaultNotification: false);

                        // ClipboardUtils.CopyToClipboard(expandedValue, new ClipboardUtils.FlowCopyContext(_context));
                        ClipboardUtils.CopyToClipboard(expandedValue, new ClipboardUtils.Win32CopyMethod());
                        // ClipboardUtils.CopyToClipboard(expandedValue, new ClipboardUtils.DotnetCopyContext());

                        // after Flow Launcher hides, wait until Flow Launcher no longer has focus and paste into previous active window
                        if (_settings.AutoPasteEnabled)
                        {
                            Task.Run(() =>
                            {
                                // AutoPasteHelper.PasteWhenFocusRestoredAsyncNew(_context, _settings.PasteDelayMs);

                                Thread.Sleep(100);
                                SendKeys.SendWait("^v");
                            });
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
                AutoPasteHelper.AutoPasteAsync(_context, _settings.AutoPasteMehtod, _settings.PasteDelayMs);
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
            _snippetManage.Add(new SnippetModel
            {
                Key = key,
                Value = value
            });
        }

        private void _update(string key, string value)
        {
            _snippetManage.UpdateByKey(new SnippetModel
            {
                Key = key,
                Value = value
            });
        }

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
                        sm.Key, sm.Value.Replace("\r\n", "  ").Replace("\n", "  ")),
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
                        sm.Key, sm.Value.Replace("\r\n", "  ").Replace("\n", "  ")),
                    IcoPath = IconPath,
                    Action = _ =>
                    {
                        _snippetManage.RemoveByKey(sm.Key);
                        return true;
                    },
                });

                // new edit
                menus.Add(new Result
                {
                    Title = _context.API.GetTranslation("snippets_plugin_edit_snippet"),
                    SubTitle = string.Format(_context.API.GetTranslation("snippets_plugin_edit_snippet_info"),
                        sm.Key, sm.Value.Replace("\r\n", "  ").Replace("\n", "  ")),
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


        /// <summary>
        /// 1.x.x version snippets merge to 2.x.x version
        /// </summary>
        [Obsolete]
        private void _mergeOldSnippet()
        {
            // old version snippets in Settings.json
            var snippets = _settings.Snippets;
            if (snippets == null || !snippets.Any()) return;

            foreach (var snippet in snippets)
            {
                _snippetManage.Add(new SnippetModel
                {
                    Key = snippet.Key,
                    Value = snippet.Value
                });
            }

            // clear old snippets after merge
            _settings.Snippets = null;
            _context.API.SavePluginSettings();
        }
    }
}