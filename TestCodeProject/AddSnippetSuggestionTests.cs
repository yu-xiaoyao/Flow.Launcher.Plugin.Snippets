using Flow.Launcher.Plugin;
using Flow.Launcher.Plugin.Snippets;

namespace TestCodeProject
{
    /// <summary>
    /// v3.0.1 引入的「搜不到就提示新建 Snippet」这条链路：
    ///
    ///     Query() -> _wrapperResultList() -> 结果为空 &amp;&amp; SearchTerms.Length &gt;= 2 -> _appendSnippets()
    ///
    /// _appendSnippets 会把 SearchTerms 在 i = 1..n-1 处切成
    ///     name = terms[0..i]，value = terms[i..n]
    /// 于是「sp hello world」搜不到时，会给出一个「新建 name=hello / value=world」的候选。
    ///
    /// _appendSnippets 是按 SearchTerms 原样切分的，不认 <c>:f</c>，所以带 <c>:f</c> 的查询
    /// 要分两种情况看：
    ///   - 非 Disable 模式：<c>:f</c> 是 folder 参数，不能进 name / value（会建出一个叫 ":f" 的 Snippet，
    ///     而且新建时 ChangeQuery("sp :f") 又回到 Incomplete 分支）。
    ///   - Disable 模式：<c>:f</c> 不生效，就是普通关键字，**允许**把它当成 Snippet 名。
    ///
    /// Main.cs 里 _wrapperResultList 有 4 个调用点：
    ///     Main.cs:94   Disable 分支            —— 兜底 ✔（:f 当名字用，符合预期）
    ///     Main.cs:110  Kind == Incomplete      —— 直接 return results，不兜底 ✔
    ///     Main.cs:117  Kind 位置不匹配的提示项   —— 没判断，但 results 非空所以不会触发
    ///     Main.cs:129  正常搜索                —— Kind == None 才兜底 ✔
    ///
    /// 判定分两类（和 FolderSearchModeTests 的 SPEC/GAP 约定一致）：
    ///   [SPEC] 期望的行为 —— 必须通过，失败计入返回值。
    ///   [GAP ] 已知限制 / 边界 —— 记录现状，只打印不计失败。
    ///
    /// 用例会真的点一次候选（调用 Result.Action），从而断言「点了之后到底新建了什么」，
    /// 因为 Title 全是 snippets_plugin_add，光看标题分不出生成出来的 name / value 对不对。
    /// </summary>
    public static class AddSnippetSuggestionTests
    {
        // ---- 期望标题简写 ----
        private const string Add = "snippets_plugin_add";
        private const string Manage = "snippets_plugin_manage_snippets";
        private const string Edit = "snippets_plugin_edit_snippet";
        private const string Delete = "snippets_plugin_delete_snippet";

        // ---- 点击策略 ----
        private const string ClickNone = "none";
        private const string ClickAdd = "add";

        // ---- 数据集 ----
        private const string DataSample = "sample";
        private const string DataEmpty = "empty";
        private const string DataFolder = "folder";
        private const string DataColonName = "colonName";

        private class Case
        {
            public string Kind;              // SPEC / GAP
            public SearchFolderMode Mode;
            public string Data;
            public string Input;
            public string[] Expected;        // 期望的结果标题列表
            public (string name, string value)[] ExpectedCreates;  // 点一次「新建」候选之后应新增的 (name, value)
            public string Click;
            public string Note;
        }

        private static readonly List<Case> Cases = new();

        private static void Spec(SearchFolderMode mode, string data, string input, string[] expected,
            (string, string)[] creates = null, string click = ClickNone, string note = "")
            => Cases.Add(new Case
            {
                Kind = "SPEC", Mode = mode, Data = data, Input = input, Expected = expected,
                ExpectedCreates = creates, Click = click, Note = note
            });

        private static void Gap(SearchFolderMode mode, string data, string input, string[] actual, string note)
            => Cases.Add(new Case
            {
                Kind = "GAP ", Mode = mode, Data = data, Input = input, Expected = actual,
                ExpectedCreates = null, Click = ClickNone, Note = note
            });

        public static bool Run()
        {
            BuildCases();

            Console.WriteLine();
            Console.WriteLine("============ 搜不到 -> 提示新建 Snippet (v3.0.1) ============");
            Console.WriteLine("数据:");
            Console.WriteLine("  sample: Java/Python/My Folder + dev-pass x4 / jdk-config / py-env / uat-pass");
            Console.WriteLine("  empty : 无文件夹、无 Snippet");
            Console.WriteLine("  folder: Java -> [alpha one, alpha one two, beta]   Python -> [alpha one]");
            Console.WriteLine("  colonName: 一个名叫 \":f\" 的 Snippet");
            Console.WriteLine("约定: [SPEC] 期望行为(失败会返回 false) / [GAP ] 已知限制(仅记录)");
            Console.WriteLine();

            var failed = 0;
            var gaps = 0;

            foreach (var c in Cases)
            {
                var harness = CreateHarness(c.Data);
                harness.Settings.SearchFolderMode = c.Mode;
                harness.Fake.ClearCallLog();
                harness.Fake.ClearAddLog();

                List<Result> results;
                string error = null;
                try
                {
                    results = harness.QueryFull(c.Input);
                }
                catch (Exception ex)
                {
                    results = new List<Result>();
                    error = $"{ex.GetType().Name}: {ex.Message}";
                }

                var titles = results.Select(r => r.Title).ToArray();

                // 只点「新建候选」：_appendSnippets 生成的 Result 特征是有 SubTitle；
                // 而 _buildAddDialogResult 生成的（会弹 WPF 窗口）没有 SubTitle，绝不能点。
                string clickError = null;
                if (error == null && c.Click == ClickAdd)
                {
                    foreach (var r in results.Where(r => r.Title == Add && r.SubTitle != null))
                    {
                        try
                        {
                            r.Action(null);   // _appendSnippets 的 lambda 不用 ActionContext
                        }
                        catch (Exception ex)
                        {
                            clickError = $"{ex.GetType().Name}: {ex.Message}";
                            break;
                        }
                    }
                }

                var creates = harness.Fake.AddCalls.ToArray();

                var titlesOk = error == null && titles.SequenceEqual(c.Expected);
                var createsOk = c.ExpectedCreates == null ||
                                (clickError == null && creates.SequenceEqual(c.ExpectedCreates));
                var ok = titlesOk && createsOk;

                var isGap = c.Kind == "GAP ";
                if (isGap)
                {
                    gaps++;
                }
                else if (!ok)
                {
                    failed++;
                }

                var mark = error != null ? "!!" : ok ? "OK" : (isGap ? "--" : "XX");
                var display = c.Input.StartsWith("sp ") ? c.Input[3..] : c.Input;
                Console.WriteLine($"  [{mark}] [{c.Kind}] {c.Mode,-14} {display,-30} -> [{string.Join(", ", titles)}]");

                if (creates.Length > 0)
                    Console.WriteLine($"        点击后新建: {DescribeCreates(creates)}");
                if (c.ExpectedCreates != null && creates.Length > 0 && !createsOk)
                    Console.WriteLine($"        期望新建  : {DescribeCreates(c.ExpectedCreates)}");

                if (error != null)
                    Console.WriteLine($"        异常: {error}");
                if (clickError != null)
                    Console.WriteLine($"        点击异常: {clickError}");
                else if (!ok && titlesOk && !isGap)
                    Console.WriteLine($"        结果不符: 期望 [{string.Join(", ", c.Expected)}]");

                if (!string.IsNullOrEmpty(c.Note))
                    Console.WriteLine($"        备注: {c.Note}");
            }

            if (!ContextMenuCheck()) failed++;

            Console.WriteLine();
            Console.WriteLine($"SPEC 失败: {failed}");
            Console.WriteLine($"GAP  记录: {gaps}");

            return failed == 0;
        }

        private static string DescribeCreates(IEnumerable<(string name, string value)> creates)
            => string.Join("  |  ", creates.Select(c => $"name=[{c.name}] value=[{c.value}]"));

        private static SnippetsHarness CreateHarness(string data) => data switch
        {
            DataEmpty => new SnippetsHarness(new FakeSnippetManage()),
            DataFolder => new SnippetsHarness(BuildFolderData()),
            DataColonName => new SnippetsHarness(BuildColonNameData()),
            _ => new SnippetsHarness(SnippetsHarness.BuildSampleData()),
        };

        /// <summary>多词 Snippet 名专用数据集，和 FolderParamPositionTests 保持一致。</summary>
        private static FakeSnippetManage BuildFolderData()
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

        /// <summary>一个名叫 ":f" 的 Snippet（没有任何文件夹），用来验证「允许 :f 做名字」的后果。</summary>
        private static FakeSnippetManage BuildColonNameData()
        {
            var manage = new FakeSnippetManage();
            manage.AddSnippet(":f", value: "colon snippet");
            return manage;
        }

        private static void BuildCases()
        {
            Cases.Clear();

            // ============================================================
            // 0. 空搜索 -> 有数据列最近，没数据才给「管理 / 新建」两个入口
            // ============================================================
            Spec(SearchFolderMode.AutoFolder, DataEmpty, "sp", new[] { Manage, Add },
                note: "空库: 原来的 _buildManageResult + 新增的 _buildAddDialogResult");

            Spec(SearchFolderMode.AutoFolder, DataSample, "sp",
                new[] { "uat-pass", "dev-pass", "dev-pass | My Folder", "py-env | Python",
                        "dev-pass | Python", "jdk-config | Java", "dev-pass | Java" },
                note: "有数据: 仍然是 ListRecent（不加新建入口）");

            // ============================================================
            // 1. 新功能: 搜不到 + >= 2 个词 -> 给出「新建 Snippet」候选
            // ============================================================
            Spec(SearchFolderMode.AutoFolder, DataSample, "sp hello world", new[] { Add },
                new[] { ("hello", "world") }, ClickAdd,
                "本次改造的主场景: 2 个词切成 name/value");

            Spec(SearchFolderMode.AutoFolder, DataSample, "sp hello world foo", new[] { Add, Add },
                new[] { ("hello", "world foo"), ("hello world", "foo") }, ClickAdd,
                "3 个词 -> 2 个候选，按切分点 i=1..n-1");

            Spec(SearchFolderMode.FolderInFirst, DataSample, "sp hello world", new[] { Add },
                new[] { ("hello", "world") }, ClickAdd,
                "没有 :f 时 4 个模式行为一致");

            Spec(SearchFolderMode.FolderInLast, DataSample, "sp hello world", new[] { Add },
                new[] { ("hello", "world") }, ClickAdd);

            Spec(SearchFolderMode.Disable, DataSample, "sp hello world", new[] { Add },
                new[] { ("hello", "world") }, ClickAdd,
                "Disable 走的是另一个分支，同样生效");

            // 只有 1 个词时不做切分（切不出 value），保持原来的空列表
            Spec(SearchFolderMode.AutoFolder, DataSample, "sp zzz", Array.Empty<string>(),
                note: "守卫条件 SearchTerms.Length >= 2");

            // 搜得到就不加新建候选
            Spec(SearchFolderMode.AutoFolder, DataSample, "sp dev",
                new[] { "dev-pass", "dev-pass | My Folder", "dev-pass | Python", "dev-pass | Java" },
                note: "有结果 -> 不追加");

            // ============================================================
            // 2. 带 :f 的查询不兜底（Main.cs:129 的 Kind == None 判断）
            // ============================================================
            Spec(SearchFolderMode.FolderInFirst, DataFolder, "sp :f Java alpha one two three",
                Array.Empty<string>(),
                note: "Kind=First: 修复前会给出 5 个 name 里带字面量 \":f\" 的候选");

            Spec(SearchFolderMode.AutoFolder, DataSample, "sp :f Java zzz", Array.Empty<string>(),
                note: "Kind=First: 文件夹合法、只是 Snippet 关键字搜不到，也不兜底");

            Spec(SearchFolderMode.FolderInLast, DataSample, "sp dev-pass zzz :f Java", Array.Empty<string>(),
                note: "Kind=Last: 修复前 value 里会带 \":f\"");

            // ============================================================
            // 3. Disable 模式下 :f 不是 folder 参数，就是个普通关键字
            //    —— 允许把它当 Snippet 名，所以这里会兜底出 name=":f"
            // ============================================================
            Spec(SearchFolderMode.Disable, DataSample, "sp :f Java", new[] { Add },
                new[] { (":f", "Java") }, ClickAdd,
                "Disable: :f 不生效 -> 整串当关键字搜不到 -> 新建候选 name=\":f\" / value=\"Java\"");

            Spec(SearchFolderMode.Disable, DataSample, "sp dev-pass :f Java", new[] { Add, Add },
                new[] { ("dev-pass", ":f Java"), ("dev-pass :f", "Java") }, ClickAdd,
                "Disable: 3 个 token -> 2 个候选，切分点不特殊对待 :f");

            // ============================================================
            // 4. Incomplete 分支不兜底（Main.cs:110 直接 return results）
            // ============================================================
            Spec(SearchFolderMode.AutoFolder, DataEmpty, "sp dev :f", Array.Empty<string>(),
                note: "Kind=Incomplete: 打了 :f 但还没填 Folder，不给新建候选");

            // ============================================================
            // 5. 已知限制：名叫 ":f" 的 Snippet 在非 Disable 模式下拿不到
            // ============================================================
            Gap(SearchFolderMode.AutoFolder, DataColonName, "sp :f", Array.Empty<string>(),
                "非 Disable 模式: ':f' 被解析成 folder 参数，名为 \":f\" 的 Snippet 搜不到");

            Gap(SearchFolderMode.Disable, DataColonName, "sp :f", new[] { ":f" },
                "Disable 模式: 此时 ':f' 是普通关键字，能搜到（也就是允许 :f 做名字的直接后果）");
        }

        /// <summary>
        /// 右键菜单里新加的「新建 Snippet」入口。
        /// </summary>
        private static bool ContextMenuCheck()
        {
            Console.WriteLine();
            Console.WriteLine("--- IContextMenu（右键菜单）---");

            var harness = new SnippetsHarness(SnippetsHarness.BuildSampleData());
            harness.Settings.SearchFolderMode = SearchFolderMode.AutoFolder;

            var snippet = harness.QueryFull("sp dev-pass").First(r => r.Title == "dev-pass | Java");
            var menus = harness.Plugin.LoadContextMenus(snippet);

            var titles = menus.Select(m => m.Title).ToArray();
            var expected = new[] { Edit, Delete, Manage, Add };
            var ok = titles.SequenceEqual(expected);

            Console.WriteLine($"  [{(ok ? "OK" : "XX")}] 菜单: [{string.Join(", ", titles)}]");
            if (!ok)
                Console.WriteLine($"        期望: [{string.Join(", ", expected)}]");
            Console.WriteLine($"       新增项 AutoCompleteText = \"{menus[^1].AutoCompleteText}\"（原内联写法是 null，现在统一走 _buildAddDialogResult）");

            return ok;
        }
    }
}
