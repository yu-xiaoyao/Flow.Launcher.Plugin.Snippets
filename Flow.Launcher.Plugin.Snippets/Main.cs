using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Flow.Launcher.Plugin.Snippets.Sqlite;
using Flow.Launcher.Plugin.Snippets.Update;
using Flow.Launcher.Plugin.Snippets.Util;
using JetBrains.Annotations;
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
            var manage = new MsSqliteSnippetManage(pluginSettingPath, _flowLauncherFuzzySearch, true);
            manage.SetKeywordMatchMode(_settings.KeywordMatchMode);
            manage.Init(needUpdateDb);
            return manage;
        }

        public List<Result> Query(Query query)
        {
            // all data
            if (string.IsNullOrEmpty(query.Search))
            {
                // return _snippetManage.List().Select(sm => _modelToResult(query, sm)).ToList();
                var allSnips = _snippetManage.ListRecent();
                if (allSnips.Count == 0)
                {
                    return new List<Result>
                    {
                        _buildManageResult(query)
                    };
                }

                return allSnips.Select(sm => _modelToResult(query, sm)).ToList();
            }

            var mode = _settings.SearchFolderMode;

            // 禁用文件夹过滤时 :f 不生效，整串当作 Snippet 名字搜索
            if (mode == SearchFolderMode.Disable)
            {
                return _snippetManage.List(name: query.Search)
                    .Select(sm => _modelToResult(query, sm))
                    .ToList();
            }

            var results = new List<Result>();
            var folderQuery = FolderQuery.Parse(query.SearchTerms);

            // 打了 : / :f 但还没输入 Folder 名 -> 列出候选文件夹，Tab 可直接补全
            if (folderQuery.Kind == FolderParamKind.Incomplete)
            {
                _appendFolderResults(query, results, folderQuery);
                if (folderQuery.HasSnippetKey)
                {
                    results.AddRange(_snippetManage.List(name: folderQuery.SnippetKey)
                        .Select(sm => _modelToResult(query, sm, folderQuery)));
                }

                return results;
            }

            // 当前模式不支持用户写的 :f 位置 -> 明确提示，不要像以前那样静默返回空列表
            if (!_acceptsFolderParam(folderQuery.Kind, mode))
            {
                results.Add(_buildFolderParamMismatchResult(folderQuery.Kind, mode));
                return results;
            }

            // 只给了 Folder 没给 Snippet 关键字时，顺便列出候选文件夹（Tab 可补全）
            if (folderQuery.HasFolderKey && !folderQuery.HasSnippetKey)
            {
                _appendFolderResults(query, results, folderQuery);
            }

            results.AddRange(_snippetManage.List(name: folderQuery.SnippetKey, folderName: folderQuery.FolderKey)
                .Select(sm => _modelToResult(query, sm, folderQuery)));

            return results;
        }

        private Result _modelToResult(Query query, SnippetModel sm, [CanBeNull] FolderQuery folderQuery = null)
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

            // 带上 :f <Folder> 上下文，Tab 补全后不会丢掉文件夹过滤
            var autoCompleteText = folderQuery == null || folderQuery.Kind == FolderParamKind.None
                ? $"{query.ActionKeyword} {name} "
                : $"{query.ActionKeyword} {folderQuery.Render(name, sm.FolderName)}";

            return new Result
            {
                Title = title,
                SubTitle = value.Replace("\r\n", "  ").Replace("\n", "  "),
                IcoPath = IconPath,
                AutoCompleteText = autoCompleteText,
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

        private Result _buildManageResult([CanBeNull] Query query = null)
        {
            return new Result
            {
                Title = _context.API.GetTranslation("snippets_plugin_manage_snippets"),
                IcoPath = IconPath,
                AutoCompleteText = query == null ? "" : $"{query.ActionKeyword} {query.Search}",
                Action = _ =>
                {
                    SettingWindow.Show(_context, _snippetManage);
                    return true;
                }
            };
        }


        private void _add(string name, string value)
        {
            _snippetManage.Add(name, value);
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

                // open manage panel
                menus.Add(_buildManageResult());

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

        /// <summary>
        /// upgrade v2 -> v3
        /// </summary>
        /// <param name="pluginSettingPath"></param>
        private void _upgradeJsonToSqlite(string pluginSettingPath)
        {
            // merge to Sqlite
            UpgradeHelper.UpgradeJsonToSqlite(_context, _snippetManage, pluginSettingPath);
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

            // 参数化时 LikePattern 会把用户输入里的 % _ \ 转义，这里还原成原始关键字。
            // 不能用 Trim('%')：像 "100\%" 这种转义序列的结尾 % 会被一起吃掉。
            var keyword = LikePattern.ParseContains(pattern);

            // 如果用户输入的是空关键字（比如 LIKE '%%'），直接返回 true
            if (string.IsNullOrEmpty(keyword)) return true;

            var match = _context.API.FuzzySearch(keyword, value);
            return match.Success;
        }

        /// <summary>
        /// 当前 <see cref="SearchFolderMode"/> 是否接受这种 <c>:f</c> 位置。
        /// 被拒绝的位置不返回空列表，而是给出提示，避免用户以为「搜不到」。
        /// </summary>
        private static bool _acceptsFolderParam(FolderParamKind kind, SearchFolderMode mode)
        {
            return mode switch
            {
                SearchFolderMode.FolderInFirst => kind is FolderParamKind.None or FolderParamKind.First,
                SearchFolderMode.FolderInLast => kind is FolderParamKind.None or FolderParamKind.Last,
                SearchFolderMode.AutoFolder =>
                    kind is FolderParamKind.None or FolderParamKind.First or FolderParamKind.Last,
                _ => kind == FolderParamKind.None,
            };
        }

        /// <summary>
        /// 追加候选文件夹。选中后会把 Folder 名按当前 <c>:f</c> 位置补进查询。
        /// </summary>
        private void _appendFolderResults(Query query, List<Result> results, FolderQuery folderQuery)
        {
            var folders = _snippetManage.ListFolders(folderQuery.HasFolderKey ? folderQuery.FolderKey : null);
            foreach (var folderModel in folders)
            {
                var name = folderModel.Name;
                var completeText = $"{query.ActionKeyword} {folderQuery.RenderWithFolder(name)}";

                results.Add(new Result
                {
                    Title = name,
                    AutoCompleteText = completeText,
                    IcoPath = FolderIconPath,
                    Action = _ =>
                    {
                        _context.API.ChangeQuery(completeText, true);
                        return false;
                    }
                });
            }
        }

        /// <summary>
        /// 用户写的 <c>:f</c> 位置和当前模式不匹配时的提示项。
        /// </summary>
        private Result _buildFolderParamMismatchResult(FolderParamKind kind, SearchFolderMode mode)
        {
            var current = _context.API.GetTranslation($"snippets_plugin_search_folder_mode_{(int)mode}");
            var expected = _context.API.GetTranslation(kind == FolderParamKind.First
                ? "snippets_plugin_folder_param_form_last"
                : "snippets_plugin_folder_param_form_first");

            return new Result
            {
                Title = _context.API.GetTranslation("snippets_plugin_folder_param_mismatch"),
                SubTitle = string.Format(
                    _context.API.GetTranslation("snippets_plugin_folder_param_mismatch_info"), current, expected),
                IcoPath = FolderIconPath,
                Action = _ => false,
            };
        }
    }
}
