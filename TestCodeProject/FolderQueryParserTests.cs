using Flow.Launcher.Plugin.Snippets;
using Flow.Launcher.Plugin.Snippets.Sqlite;

namespace TestCodeProject
{
    /// <summary>
    /// FolderQuery.Parse 是纯函数，直接断言结构体，不用起插件实例。
    /// 期望值 = 改造前 Main.cs 在「行为正确」的那些输入上的实际解析结果。
    /// </summary>
    public static class FolderQueryParserTests
    {
        private static int _failed;

        public static bool Run()
        {
            _failed = 0;

            Console.WriteLine();
            Console.WriteLine("================ FolderQuery.Parse 单测 ================");
            Console.WriteLine($"  {"SearchTerms",-34} {"期望",-46} 实际");

            Parse("dev", FolderParamKind.None, "dev", "");
            Parse("dev pass", FolderParamKind.None, "dev pass", "");

            // : / :f 单独出现 -> 还没填 Folder 名
            Parse(":", FolderParamKind.Incomplete, "", "");
            Parse(":f", FolderParamKind.Incomplete, "", "");

            // FolderInFirst: 第 2 个参数是 Folder 名，其余全部是 Snippet 过滤名
            Parse(":f Java", FolderParamKind.First, "", "Java");
            Parse(":f Java dev", FolderParamKind.First, "dev", "Java");
            Parse(":f Java dev pass", FolderParamKind.First, "dev pass", "Java");
            Parse(":f Java dev pass more", FolderParamKind.First, "dev pass more", "Java");

            // FolderInLast: 最后一个参数是 Folder 名，其前全部是 Snippet 过滤名
            Parse("dev :f", FolderParamKind.Incomplete, "dev", "");
            Parse("dev :", FolderParamKind.Incomplete, "dev", "");
            Parse("dev pass :f", FolderParamKind.Incomplete, "dev pass", "");
            Parse("dev :f Java", FolderParamKind.Last, "dev", "Java");
            Parse("dev pass :f Java", FolderParamKind.Last, "dev pass", "Java");
            Parse("dev pass more :f Java", FolderParamKind.Last, "dev pass more", "Java");

            // :f 之后的多个 token 拼成 Folder 名（Folder 名允许空格）
            Parse("dev :f My Folder", FolderParamKind.Last, "dev", "My Folder");

            // 大小写不敏感
            Parse(":F Java dev", FolderParamKind.First, "dev", "Java");
            Parse("dev :F Java", FolderParamKind.Last, "dev", "Java");

            // 空输入
            Parse("", FolderParamKind.None, "", "");

            // ---------------- Render ----------------
            Console.WriteLine();
            Console.WriteLine("--- Render / RenderWithFolder ---");
            Render(":f", "Java", ":f Java ");
            Render("dev :f", "Java", "dev :f Java ");
            Render(":f J", "Java", ":f Java ");
            Render("dev :f J", "Java", "dev :f Java ");
            Render(":f", "", ":f ");

            // ---------------- LikePattern ----------------
            Console.WriteLine();
            Console.WriteLine("--- LikePattern 转义 ---");
            Like("a_b", "%a\\_b%");
            Like("100%", "%100\\%%");
            Like("a\\b", "%a\\\\b%");
            Like("normal", "%normal%");
            RoundTrip("a_b");
            RoundTrip("100%");
            RoundTrip("a\\b");
            RoundTrip("%_%");
            RoundTrip("__");
            RoundTrip("user_name_2024");

            Console.WriteLine();
            Console.WriteLine($"失败: {_failed}");
            return _failed == 0;
        }

        private static void Parse(string termsText, FolderParamKind kind, string snippetKey, string folderKey)
        {
            var terms = termsText.Length == 0
                ? Array.Empty<string>()
                : termsText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var q = FolderQuery.Parse(terms);
            var actual = $"Kind={q.Kind}, SnippetKey=\"{q.SnippetKey}\", FolderKey=\"{q.FolderKey}\"";
            var expected = $"Kind={kind}, SnippetKey=\"{snippetKey}\", FolderKey=\"{folderKey}\"";
            Check(actual == expected, $"[{termsText}]", expected, actual);
        }

        private static void Render(string termsText, string folderName, string expected)
        {
            var q = FolderQuery.Parse(termsText.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            var actual = q.RenderWithFolder(folderName);
            Check(actual == expected, $"Render([{termsText}], \"{folderName}\")", $"\"{expected}\"", $"\"{actual}\"");
        }

        private static void Like(string input, string expected)
        {
            var actual = LikePattern.Contains(input);
            Check(actual == expected, $"Contains(\"{input}\")", $"\"{expected}\"", $"\"{actual}\"");
        }

        private static void RoundTrip(string input)
        {
            var actual = LikePattern.ParseContains(LikePattern.Contains(input));
            Check(actual == input, $"RoundTrip(\"{input}\")", $"\"{input}\"", $"\"{actual}\"");
        }

        private static void Check(bool ok, string label, string expected, string actual)
        {
            if (ok)
            {
                Console.WriteLine($"  [OK] {label,-34} {expected}");
                return;
            }

            _failed++;
            Console.WriteLine($"  [XX] {label,-34} 期望 {expected}, 实际 {actual}");
        }
    }
}
