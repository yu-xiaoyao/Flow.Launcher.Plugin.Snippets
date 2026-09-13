using Flow.Launcher.Plugin.Snippets.Sqlite;

namespace TestCodeProject
{
    /// <summary>
    /// 用真实 SQLite 验证 LIKE 通配符转义，两种 KeywordMatchMode 都要覆盖。
    ///
    /// 为什么必须两种都测：SQLite 在 SQL 带 <c>ESCAPE</c> 子句时会去调**三参数**版本 like(x,y,z)，
    /// 而插件只注册了两参数的自定义 like —— 这时 SQLite 会退回内置实现，自定义模糊搜索被静默绕过。
    /// 所以 MsSqliteSnippetManage 按模式走了两条不同路径：
    ///   Sql_Like          -> 内置 LIKE + ESCAPE 子句
    ///   Flow_FuzzySearch  -> 不加 ESCAPE，自定义 like 自己反转义
    /// 这里把两条路径都钉住，避免以后有人「顺手」把 ESCAPE 加回去。
    /// </summary>
    public static class SqliteLikeTests
    {
        private static int _failed;

        public static bool Run()
        {
            _failed = 0;

            Console.WriteLine();
            Console.WriteLine("================ SQLite LIKE 转义（真实库） ================");

            RunCase(KeywordMatchMode.Sql_Like);
            RunCase(KeywordMatchMode.Flow_FuzzySearch);

            Console.WriteLine();
            Console.WriteLine($"失败: {_failed}");
            return _failed == 0;
        }

        private static void RunCase(KeywordMatchMode mode)
        {
            Console.WriteLine($"--- {mode} ---");

            var dir = Path.Combine(Path.GetTempPath(), "snippets-like-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);

            try
            {
                var likeCalls = 0;

                // 统计自定义 like 被调用次数，语义用「反转义后做包含匹配」
                var manage = new MsSqliteSnippetManage(dir, (pattern, value) =>
                {
                    likeCalls++;
                    return value.Contains(LikePattern.ParseContains(pattern), StringComparison.OrdinalIgnoreCase);
                }, true);

                manage.SetKeywordMatchMode(mode);
                manage.Init(false);

                manage.Add("a_b", "underscore");
                manage.Add("axb", "no underscore");
                manage.Add("100%", "percent");
                manage.Add("100x", "no percent");

                manage.AddFolder("My_Folder");
                manage.AddFolder("My.Folder");
                var folder = manage.GetFolder("My_Folder");
                manage.Add("in folder", "value", folderId: folder.Id);

                // `_` 如果没被转义，会当成「任意单字符」而命中 axb
                Check(mode, "List(name: \"a_b\")", manage.List(name: "a_b").Select(s => s.Name), new[] { "a_b" });

                // `%` 如果没被转义，会当成「任意串」而命中 100x
                Check(mode, "List(name: \"100%\")", manage.List(name: "100%").Select(s => s.Name), new[] { "100%" });

                // 文件夹名同样走 LIKE，`_` 不能匹配到 My.Folder
                Check(mode, "ListFolders(\"My_Folder\")", manage.ListFolders("My_Folder").Select(f => f.Name),
                    new[] { "My_Folder" });

                // folderName 过滤 + 转义
                Check(mode, "List(name: \"in\", folderName: \"My_Folder\")",
                    manage.List(name: "in", folderName: "My_Folder").Select(s => s.Name), new[] { "in folder" });

                // 普通关键字的正常路径不能被转义改坏
                Check(mode, "List(name: \"axb\")", manage.List(name: "axb").Select(s => s.Name), new[] { "axb" });
                Check(mode, "List(name: \"100x\")", manage.List(name: "100x").Select(s => s.Name), new[] { "100x" });

                if (mode == KeywordMatchMode.Flow_FuzzySearch)
                {
                    Check(mode, "自定义 like 被调用", likeCalls > 0 ? "yes" : "no", "yes");
                }
                else
                {
                    Check(mode, "自定义 like 未被调用", likeCalls == 0 ? "no" : "yes", "no");
                }
            }
            finally
            {
                try
                {
                    Directory.Delete(dir, true);
                }
                catch (IOException)
                {
                    // 临时目录清理失败不影响测试结论
                }
            }
        }

        private static void Check<T>(KeywordMatchMode mode, string label, IEnumerable<T> actual, IEnumerable<T> expected)
            => Check(mode, label, string.Join(", ", actual), string.Join(", ", expected));

        private static void Check(KeywordMatchMode mode, string label, string actual, string expected)
        {
            if (actual == expected)
            {
                Console.WriteLine($"  [OK] {label,-46} [{actual}]");
                return;
            }

            _failed++;
            Console.WriteLine($"  [XX] {label,-46} 期望 [{expected}], 实际 [{actual}]");
        }
    }
}
