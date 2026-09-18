using Flow.Launcher.Plugin.Snippets;

namespace TestCodeProject
{
    /// <summary>
    /// Search Folder Mode 场景测试（改造后）。
    ///
    /// 判定分两类：
    ///   [SPEC] Readme.md 里明确写了的用法 —— 必须完全符合。
    ///   [GAP ] Readme 没写、但实际会走到的输入 —— 记录当前行为。
    ///          改造后原来的 9 条 GAP 有 7 条变成了合理行为（列文件夹 / 明确提示），
    ///          GAP 下降说明可用性变好；剩下的见备注。
    /// </summary>
    public static class FolderSearchModeTests
    {
        private class Case
        {
            public string Kind;          // SPEC / GAP
            public SearchFolderMode Mode;
            public string Input;
            public string[] Expected;
            public string Note;
        }

        private static readonly List<Case> Cases = new();

        private static void Spec(SearchFolderMode mode, string input, string[] expected, string note = "")
            => Cases.Add(new Case { Kind = "SPEC", Mode = mode, Input = input, Expected = expected, Note = note });

        private static void Gap(SearchFolderMode mode, string input, string[] actual, string note)
            => Cases.Add(new Case { Kind = "GAP ", Mode = mode, Input = input, Expected = actual, Note = note });

        // ---- 期望值简写 ----
        private const string Mismatch = "snippets_plugin_folder_param_mismatch";
        private const string Add = "snippets_plugin_add";
        private const string DevJava = "dev-pass | Java";
        private const string DevPython = "dev-pass | Python";
        private const string DevMyFolder = "dev-pass | My Folder";
        private const string DevNoFolder = "dev-pass";
        private const string JdkJava = "jdk-config | Java";

        private static readonly string[] AllDevPass = { DevNoFolder, DevMyFolder, DevPython, DevJava };
        private static readonly string[] AllFolders = { "Java", "Python", "My Folder" };
        private static readonly string[] JavaSnippets = { "Java", JdkJava, DevJava };

        /// <summary>列文件夹 + 按 Snippet 关键字搜的结果</summary>
        private static readonly string[] AllFoldersThenDev =
            { "Java", "Python", "My Folder", DevNoFolder, DevMyFolder, DevPython, DevJava };

        public static bool Run()
        {
            BuildCases();

            var harness = new SnippetsHarness(SnippetsHarness.BuildSampleData());

            Console.WriteLine();
            Console.WriteLine("================ Search Folder Mode 场景测试 ================");
            Console.WriteLine("测试数据：");
            Console.WriteLine("  文件夹: Java / Python / My Folder");
            Console.WriteLine("  Java      -> dev-pass, jdk-config");
            Console.WriteLine("  Python    -> dev-pass, py-env");
            Console.WriteLine("  My Folder -> dev-pass");
            Console.WriteLine("  (无文件夹) -> dev-pass, uat-pass");
            Console.WriteLine();

            var failed = 0;
            var gaps = 0;
            SearchFolderMode? currentMode = null;

            foreach (var c in Cases)
            {
                if (currentMode != c.Mode)
                {
                    currentMode = c.Mode;
                    Console.WriteLine($"--- {DescribeMode(c.Mode)} ---");
                }

                harness.Settings.SearchFolderMode = c.Mode;
                harness.Fake.ClearCallLog();

                string[] actual;
                string error = null;
                try
                {
                    actual = harness.Query(c.Input).ToArray();
                }
                catch (Exception ex)
                {
                    actual = Array.Empty<string>();
                    error = $"{ex.GetType().Name}: {ex.Message}";
                    failed++;
                }

                var ok = error == null && actual.SequenceEqual(c.Expected);
                if (!ok && error == null && c.Kind == "SPEC") failed++;
                if (c.Kind == "GAP ") gaps++;

                var mark = error != null ? "!!" : ok ? "OK" : "XX";
                Console.WriteLine($"  [{mark}] [{c.Kind}] {c.Input,-26} -> [{string.Join(", ", actual)}]");

                if (error != null)
                {
                    Console.WriteLine($"        异常: {error}");
                }
                else if (!ok)
                {
                    Console.WriteLine($"        期望: [{string.Join(", ", c.Expected)}]");
                }

                if (!string.IsNullOrEmpty(c.Note))
                    Console.WriteLine($"        备注: {c.Note}");
            }

            Console.WriteLine();
            Console.WriteLine($"SPEC 失败: {failed}");
            Console.WriteLine($"GAP  记录: {gaps}");

            AutoCompleteCheck();

            return failed == 0;
        }

        private static string DescribeMode(SearchFolderMode mode) => mode switch
        {
            SearchFolderMode.Disable => "SearchFolderMode.Disable (0) —— sp <SnippetName>",
            SearchFolderMode.AutoFolder => "SearchFolderMode.AutoFolder (1) —— sp :f <Folder> <Snippet> / sp <Snippet> :f <Folder>",
            SearchFolderMode.FolderInFirst => "SearchFolderMode.FolderInFirst (2) —— 只支持 sp :f <Folder> <Snippet>",
            SearchFolderMode.FolderInLast => "SearchFolderMode.FolderInLast (3) —— 只支持 sp <Snippet> :f <Folder>",
            _ => mode.ToString()
        };

        private static void BuildCases()
        {
            Cases.Clear();

            // ============================================================
            // 0. Disable —— 完全不认识 :f，就是普通的名字搜索
            // ============================================================
            Spec(SearchFolderMode.Disable, "sp dev-pass", AllDevPass);
            // :f 在 Disable 下不生效，被当成普通关键字。:f 本身是合法的 Snippet 名，
            // 所以搜不到时会给「新建」候选（v3.0.1 的兜底），不再是空列表。
            Spec(SearchFolderMode.Disable, "sp :f Java", new[] { Add },
                ":f 不生效，整串 ':f Java' 搜不到 -> 给出新建候选 name=\":f\" / value=\"Java\"");
            Spec(SearchFolderMode.Disable, "sp dev-pass :f Java", new[] { Add, Add },
                "同上，3 个 token -> 2 个候选");

            // ============================================================
            // 1. AutoFolder
            // ============================================================
            // Readme: `sp :f FodlerName SnippetName`
            Spec(SearchFolderMode.AutoFolder, "sp :f Java dev", new[] { DevJava });
            Spec(SearchFolderMode.AutoFolder, "sp :f Java dev-pass", new[] { DevJava });
            // Readme: `sp SnippetNmae :f FodlerName`
            Spec(SearchFolderMode.AutoFolder, "sp dev :f Java", new[] { DevJava });
            Spec(SearchFolderMode.AutoFolder, "sp dev-pass :f Java", new[] { DevJava });
            // 不带 :f 的普通搜索
            Spec(SearchFolderMode.AutoFolder, "sp dev-pass", AllDevPass);
            // 文件夹名带空格（folder-last 形式可以）
            Spec(SearchFolderMode.AutoFolder, "sp dev :f My Folder", new[] { DevMyFolder });
            // 只打了 : 或 :f -> 列候选文件夹
            Spec(SearchFolderMode.AutoFolder, "sp :f", AllFolders);
            Spec(SearchFolderMode.AutoFolder, "sp :", AllFolders,
                "改造点: 以前只给一个 'sp :f ' 提示项，现在直接把文件夹列出来");
            // :f + 文件夹名 -> 候选文件夹 + 该文件夹下的 Snippet
            Spec(SearchFolderMode.AutoFolder, "sp :f J", JavaSnippets,
                "改造点: 以前只回显文件夹 'Java'，现在同时列出 Java 下的 Snippet");
            Spec(SearchFolderMode.AutoFolder, "sp :f Java", JavaSnippets,
                "改造点: 以前只回显文件夹 'Java'（还白跑一次 List(name: \":f Java\")）");
            // Snippet 关键字 + 尾部 : / :f -> 列文件夹 + 按关键字搜
            Spec(SearchFolderMode.AutoFolder, "sp dev :", AllFoldersThenDev,
                "改造点: 以前整个列表为空");
            Spec(SearchFolderMode.AutoFolder, "sp dev :f", AllFoldersThenDev,
                "改造点: 以前只给一个 'sp dev :f ' 提示项。提示项本身是空操作（改成同一个查询）");

            // 仍然存在的限制
            Gap(SearchFolderMode.AutoFolder, "sp :f My Folder dev", Array.Empty<string>(),
                "folder-first 形式按定义 folderKey 只取 terms[1]，带空格的文件夹名匹配不到");

            // ============================================================
            // 2. FolderInFirst
            // ============================================================
            Spec(SearchFolderMode.FolderInFirst, "sp :f Java dev", new[] { DevJava });
            Spec(SearchFolderMode.FolderInFirst, "sp :f", AllFolders);
            Spec(SearchFolderMode.FolderInFirst, "sp :", AllFolders);
            Spec(SearchFolderMode.FolderInFirst, "sp :f Java", JavaSnippets);
            Spec(SearchFolderMode.FolderInFirst, "sp dev-pass", AllDevPass);
            Spec(SearchFolderMode.FolderInFirst, "sp dev :f", AllFoldersThenDev);
            Spec(SearchFolderMode.FolderInFirst, "sp dev :", AllFoldersThenDev);

            // 不支持的位置 -> 明确提示，而不是静默返回空
            Spec(SearchFolderMode.FolderInFirst, "sp dev :f Java", new[] { Mismatch },
                "改造点: 以前静默返回空列表，现在给出「当前模式不支持该写法」的提示项");

            // ============================================================
            // 3. FolderInLast
            // ============================================================
            Spec(SearchFolderMode.FolderInLast, "sp dev :f Java", new[] { DevJava });
            Spec(SearchFolderMode.FolderInLast, "sp dev-pass :f My Folder", new[] { DevMyFolder });
            Spec(SearchFolderMode.FolderInLast, "sp dev :f", AllFoldersThenDev);
            Spec(SearchFolderMode.FolderInLast, "sp dev-pass", AllDevPass);
            Spec(SearchFolderMode.FolderInLast, "sp :f", AllFolders,
                "改造点: 以前 ':f' 被当成 snippet 名字搜，返回空且无法发现文件夹名");
            Spec(SearchFolderMode.FolderInLast, "sp :", AllFolders,
                "改造点: 同上，以前返回空列表");
            Spec(SearchFolderMode.FolderInLast, "sp :f Java dev", new[] { Mismatch },
                "改造点: 以前静默返回空列表，现在提示当前模式应为 sp <Snippet> :f <Folder>");
        }

        /// <summary>
        /// 检查文件夹过滤生效时，结果的 AutoCompleteText 是否保留了 :f 上下文。
        /// </summary>
        private static void AutoCompleteCheck()
        {
            Console.WriteLine();
            Console.WriteLine("--- AutoCompleteText 检查（在文件夹过滤下选中结果后按 Tab 会补全成什么）---");

            var harness = new SnippetsHarness(SnippetsHarness.BuildSampleData());

            foreach (var (mode, input) in new[]
                     {
                         (SearchFolderMode.AutoFolder, "sp :f Java dev"),
                         (SearchFolderMode.AutoFolder, "sp dev :f Java"),
                         (SearchFolderMode.AutoFolder, "sp :f J"),
                         (SearchFolderMode.FolderInLast, "sp dev-pass :f My Folder"),
                     })
            {
                harness.Settings.SearchFolderMode = mode;
                foreach (var r in harness.QueryFull(input))
                {
                    Console.WriteLine($"  {input,-26} -> Title=\"{r.Title}\"  AutoCompleteText=\"{r.AutoCompleteText}\"");
                }
            }
        }
    }
}
