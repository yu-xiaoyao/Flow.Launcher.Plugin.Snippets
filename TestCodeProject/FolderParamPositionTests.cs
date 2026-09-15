using Flow.Launcher.Plugin.Snippets;

namespace TestCodeProject
{
    /// <summary>
    /// 专门验证「:f 的位置语义」+「多词 Snippet 过滤名」：
    ///
    ///   FolderInFirst: `:f` 在第 1 位 → 第 2 个参数是 Folder 名，其后**全部**参数拼成 Snippet 过滤名
    ///       sp :f Folder SnippetName1 SnippetName2 SnippetName3
    ///
    ///   FolderInLast : `:f` 在倒数第 2 位 → 最后 1 个参数是 Folder 名，其前**全部**参数拼成 Snippet 过滤名
    ///       sp SnippetName1 SnippetName2 SnippetName3 :f Folder
    ///
    ///   AutoFolder   : 上面两种的并集
    ///
    /// 每条用例同时校验：
    ///   - Main.cs 最终传给 LIST 的 (snippetKey, folderKey)  ← 解析结果
    ///   - 返回的 Result 标题列表                             ← 端到端结果
    /// </summary>
    public static class FolderParamPositionTests
    {
        private class Case
        {
            public SearchFolderMode Mode;
            public string Input;
            public string Name;     // 期望传给 List 的 name
            public string Folder;   // 期望传给 List 的 folderName
            public string[] Titles;
        }

        private static readonly List<Case> Cases = new();

        private static void Add(SearchFolderMode mode, string input, string name, string folder, string[] titles)
            => Cases.Add(new Case { Mode = mode, Input = input, Name = name, Folder = folder, Titles = titles });

        // 期望标题简写
        private const string A1J = "alpha one | Java";
        private const string A12J = "alpha one two | Java";
        private const string BJ = "beta | Java";
        private const string A1P = "alpha one | Python";

        /// <summary>多词 snippet 名专用数据集，避免和 dev-pass 那套数据互相干扰。</summary>
        private static FakeSnippetManage BuildData()
        {
            var manage = new FakeSnippetManage();
            manage.AddFolder("Java");
            manage.AddFolder("Python");
            manage.AddSnippet("alpha one", "Java");
            manage.AddSnippet("alpha one two", "Java");
            manage.AddSnippet("beta", "Java");
            manage.AddSnippet("alpha one", "Python");
            return manage;
        }

        public static bool Run()
        {
            BuildCases();

            Console.WriteLine();
            Console.WriteLine("================ :f 位置语义 + 多词 Snippet 名 ================");
            Console.WriteLine("数据: Java -> [alpha one, alpha one two, beta]   Python -> [alpha one]");
            Console.WriteLine();

            var failed = 0;
            var harness = new SnippetsHarness(BuildData());
            SearchFolderMode? currentMode = null;

            foreach (var c in Cases)
            {
                if (currentMode != c.Mode)
                {
                    currentMode = c.Mode;
                    Console.WriteLine($"--- {c.Mode} ---");
                    Console.WriteLine($"  {"输入",-34} {"解析 snippetKey",-20} {"解析 folderKey",-14} 结果");
                }

                harness.Settings.SearchFolderMode = c.Mode;
                harness.Fake.ClearCallLog();

                string[] titles;
                string error = null;
                try
                {
                    titles = harness.Query(c.Input).ToArray();
                }
                catch (Exception ex)
                {
                    titles = Array.Empty<string>();
                    error = $"{ex.GetType().Name}: {ex.Message}";
                }

                var call = harness.Fake.LastListCall;
                var actualName = call?.name ?? "(无)";
                var actualFolder = call?.folderName ?? "(无)";

                // Folder == null 表示「只看解析出的 snippetKey，folder 不作断言」
                var keysOk = error == null && actualName == c.Name &&
                             (c.Folder == null || actualFolder == c.Folder);
                var titlesOk = error == null && titles.SequenceEqual(c.Titles);
                var ok = keysOk && titlesOk;
                if (!ok) failed++;

                var display = c.Input.StartsWith("sp ") ? c.Input[3..] : c.Input;
                var mark = error != null ? "!!" : ok ? "OK" : "XX";
                Console.WriteLine(
                    $"  [{mark}] {display,-32} {actualName,-20} {actualFolder,-14} [{string.Join(", ", titles)}]");

                if (error != null)
                    Console.WriteLine($"       异常: {error}");
                if (!ok && error == null)
                {
                    if (!keysOk)
                        Console.WriteLine($"       解析不符: 期望 snippetKey=\"{c.Name}\" folderKey=\"{c.Folder}\"");
                    if (!titlesOk)
                        Console.WriteLine($"       结果不符: 期望 [{string.Join(", ", c.Titles)}]");
                }
            }

            Console.WriteLine();
            Console.WriteLine($"用例数: {Cases.Count}, 失败: {failed}");
            return failed == 0;
        }

        private static void BuildCases()
        {
            Cases.Clear();

            // ============================================================
            // FolderInFirst: `:f` 在第 1 位，其后全部是 Snippet 过滤名
            // ============================================================
            Add(SearchFolderMode.FolderInFirst, "sp :f Java alpha one",
                "alpha one", "Java", new[] { A12J, A1J });
            Add(SearchFolderMode.FolderInFirst, "sp :f Java alpha one two",
                "alpha one two", "Java", new[] { A12J });
            Add(SearchFolderMode.FolderInFirst, "sp :f Java alpha one two three",
                "alpha one two three", "Java", Array.Empty<string>());
            Add(SearchFolderMode.FolderInFirst, "sp :f Java beta",
                "beta", "Java", new[] { BJ });
            Add(SearchFolderMode.FolderInFirst, "sp :f Java",
                "", "Java", new[] { "Java", BJ, A12J, A1J });
            Add(SearchFolderMode.FolderInFirst, "sp :f Java alpha",
                "alpha", "Java", new[] { A12J, A1J });

            // ============================================================
            // FolderInLast: `:f` 在倒数第 2 位，最后一个参数是 Folder 名
            // ============================================================
            Add(SearchFolderMode.FolderInLast, "sp alpha one :f Java",
                "alpha one", "Java", new[] { A12J, A1J });
            Add(SearchFolderMode.FolderInLast, "sp alpha one two :f Java",
                "alpha one two", "Java", new[] { A12J });
            Add(SearchFolderMode.FolderInLast, "sp alpha one two three :f Java",
                "alpha one two three", "Java", Array.Empty<string>());
            Add(SearchFolderMode.FolderInLast, "sp beta :f Java",
                "beta", "Java", new[] { BJ });
            Add(SearchFolderMode.FolderInLast, "sp alpha one :f Python",
                "alpha one", "Python", new[] { A1P });
            // `:f` 后面跟多个 token -> 全部当成 Folder 名
            Add(SearchFolderMode.FolderInLast, "sp alpha one :f Java extra",
                "alpha one", "Java extra", Array.Empty<string>());

            // ============================================================
            // AutoFolder: 上面两种形式的并集
            // ============================================================
            Add(SearchFolderMode.AutoFolder, "sp :f Java alpha one",
                "alpha one", "Java", new[] { A12J, A1J });
            Add(SearchFolderMode.AutoFolder, "sp :f Java alpha one two",
                "alpha one two", "Java", new[] { A12J });
            Add(SearchFolderMode.AutoFolder, "sp alpha one :f Java",
                "alpha one", "Java", new[] { A12J, A1J });
            Add(SearchFolderMode.AutoFolder, "sp alpha one two :f Java",
                "alpha one two", "Java", new[] { A12J });
            Add(SearchFolderMode.AutoFolder, "sp alpha one :f Python",
                "alpha one", "Python", new[] { A1P });
            Add(SearchFolderMode.AutoFolder, "sp :f Java",
                "", "Java", new[] { "Java", BJ, A12J, A1J });
            Add(SearchFolderMode.AutoFolder, "sp alpha one :f",
                "alpha one", null, new[] { "Java", "Python", A1P, A12J, A1J });
        }
    }
}
