using Flow.Launcher.Plugin.Snippets;
using Flow.Launcher.Plugin.Snippets.Model;
using Flow.Launcher.Plugin.Snippets.Sqlite;

namespace TestCodeProject
{
    /// <summary>
    /// 用**真实数据库的副本**跑完整链路：
    ///   Snippets.Query -> FolderQuery.Parse -> MsSqliteSnippetManage -> SQLite
    ///
    /// 只读副本，不动用户原始数据。找不到库就跳过。
    /// </summary>
    public static class RealDbIntegrationTests
    {
        private static int _failed;

        public static bool Run()
        {
            _failed = 0;

            var settingsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FlowLauncher", "Settings", "Plugins", "Flow.Launcher.Plugin.Snippets");

            Console.WriteLine();
            Console.WriteLine("================ 真实数据库集成测试 ================");

            // 目录里可能同时存在多个 snippets*.db（历史上换过文件名），挑数据最多的那个，
            // 保证这个测试不是空跑。
            var source = PickDatabaseWithMostData(settingsDir);
            if (source == null)
            {
                Console.WriteLine($"  [跳过] {settingsDir} 下没有可用的 snippets*.db");
                return true;
            }

            var dir = Path.Combine(Path.GetTempPath(), "snippets-realdb-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);

            try
            {
                File.Copy(source, Path.Combine(dir, MsSqliteSnippetManage.DbFileName), true);

                var manage = new MsSqliteSnippetManage(dir, null, true);
                manage.SetKeywordMatchMode(KeywordMatchMode.Sql_Like);
                manage.Init(false);

                var harness = new SnippetsHarness(manage);

                var folders = manage.ListFolders();
                Console.WriteLine($"  数据来源: {source}");
                Console.WriteLine($"  文件夹 {folders.Count} 个: [{string.Join(", ", folders.Take(10).Select(f => f.Name))}]");
                Console.WriteLine($"  Snippet 共 {manage.List().Count} 条");

                // 每个模式下，拿真实文件夹名 / 前缀当关键字跑一遍
                var keywords = BuildKeywords(manage, folders);
                Console.WriteLine($"  测试关键字: [{string.Join(", ", keywords)}]");

                foreach (var mode in (SearchFolderMode[])Enum.GetValues(typeof(SearchFolderMode)))
                {
                    harness.Settings.SearchFolderMode = mode;
                    Console.WriteLine($"--- {mode} ---");

                    foreach (var kw in keywords)
                    {
                        foreach (var input in new[]
                                 {
                                     $"sp {kw}", $"sp :f {kw}", $"sp {kw} :", $"sp {kw} :f", "sp :", "sp :f"
                                 })
                        {
                            try
                            {
                                var results = harness.Query(input);
                                if (input.Contains(":f"))
                                {
                                    Console.WriteLine(
                                        $"  {input,-30} -> {results.Count} 条  [{string.Join(", ", results.Take(3).Select(Short))}]");
                                }
                            }
                            catch (Exception ex)
                            {
                                _failed++;
                                Console.WriteLine($"  [XX] {input,-30} 异常 {ex.GetType().Name}: {ex.Message}");
                            }
                        }
                    }
                }

                // 不变量：folderName 过滤的结果必须是 name 过滤结果的子集
                harness.Settings.SearchFolderMode = SearchFolderMode.AutoFolder;
                foreach (var kw in keywords.Take(5))
                {
                    foreach (var folder in folders.Take(5))
                    {
                        var byName = manage.List(name: kw).Select(s => s.Id).ToHashSet();
                        var byBoth = manage.List(name: kw, folderName: folder.Name).Select(s => s.Id).ToHashSet();
                        Check(byBoth.IsSubsetOf(byName),
                            $"List(name:\"{kw}\", folderName:\"{folder.Name}\") ⊆ List(name:\"{kw}\")");
                    }
                }

                // 不变量：`sp :f` 列出的文件夹数 == ListFolders() 的数量
                foreach (var mode in new[]
                         {
                             SearchFolderMode.AutoFolder, SearchFolderMode.FolderInFirst,
                             SearchFolderMode.FolderInLast
                         })
                {
                    harness.Settings.SearchFolderMode = mode;
                    Check(harness.Query("sp :f").Count == folders.Count,
                        $"{mode}: `sp :f` 列出全部 {folders.Count} 个文件夹");
                }

                Check(harness.Query("sp 不存在的关键字zzz").Count == 0, "搜不到的关键字返回空");
            }
            finally
            {
                try
                {
                    Directory.Delete(dir, true);
                }
                catch (IOException)
                {
                }
            }

            Console.WriteLine();
            Console.WriteLine($"失败: {_failed}");
            return _failed == 0;
        }

        /// <summary>
        /// 挑一个 snippets*.db。历史上这个文件名从 snippets.db 改成了 snippets3.db 且没有迁移，
        /// 目录里可能两个都在，所以按 snippets_3 表行数挑最多的那个。
        /// </summary>
        private static string PickDatabaseWithMostData(string settingsDir)
        {
            if (!Directory.Exists(settingsDir)) return null;

            string best = null;
            var bestCount = -1;

            foreach (var file in Directory.GetFiles(settingsDir, "snippets*.db"))
            {
                var count = CountV3Snippets(file);
                Console.WriteLine($"  发现 {Path.GetFileName(file)}: {count} 条 snippet");
                if (count <= bestCount) continue;
                bestCount = count;
                best = file;
            }

            return best;
        }

        private static int CountV3Snippets(string dbFile)
        {
            try
            {
                using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbFile}");
                connection.Open();
                using var command = new Microsoft.Data.Sqlite.SqliteCommand("select count(*) from snippets_3", connection);
                return Convert.ToInt32(command.ExecuteScalar());
            }
            catch (Exception)
            {
                return -1;
            }
        }

        private static IEnumerable<string> BuildKeywords(MsSqliteSnippetManage manage,
            List<FolderModel> folders)
        {
            var keywords = new List<string>();

            // 文件夹名（取前 3 个）
            keywords.AddRange(folders.Take(3).Select(f => f.Name));

            // 真实 Snippet 名字里前 3 条的片段，方便验证 name 过滤
            keywords.AddRange(manage.List().Take(3).Select(s => s.Name.Split(' ')[0]));

            return keywords.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct().ToArray();
        }

        private static string Short(string title) => title.Length > 24 ? title[..24] + "…" : title;

        private static void Check(bool ok, string label)
        {
            if (ok)
            {
                Console.WriteLine($"  [OK] {label}");
                return;
            }

            _failed++;
            Console.WriteLine($"  [XX] {label}");
        }
    }
}
