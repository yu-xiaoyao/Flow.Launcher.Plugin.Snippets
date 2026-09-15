using Flow.Launcher.Plugin.Snippets;

namespace TestCodeProject
{
    /// <summary>
    /// 穷举各种 token 组合，跑遍 4 个 SearchFolderMode：
    ///   1) 保证不抛异常（越界 / NRE 之类）；
    ///   2) 输出各模式解析出的 (snippetKey, folderKey)，用来肉眼比对语义是否合理。
    /// </summary>
    public static class FuzzTests
    {
        private static readonly string[] Tokens = { ":", ":f", "dev", "Java", "My", "Folder", "Name0" };

        public static bool Run()
        {
            Console.WriteLine();
            Console.WriteLine("================ 穷举测试（查越界 / 异常） ================");

            var modes = (SearchFolderMode[])Enum.GetValues(typeof(SearchFolderMode));
            var crash = new List<string>();
            var total = 0;

            foreach (var input in GenerateInputs(maxTokens: 4))
            {
                foreach (var mode in modes)
                {
                    var harness = new SnippetsHarness(SnippetsHarness.BuildSampleData());
                    harness.Settings.SearchFolderMode = mode;
                    total++;
                    try
                    {
                        harness.Query(input);
                    }
                    catch (Exception ex)
                    {
                        crash.Add($"mode={mode} input=\"{input}\" -> {ex.GetType().Name}: {ex.Message}");
                    }
                }
            }

            Console.WriteLine($"  组合数: {total}");
            if (crash.Count == 0)
            {
                Console.WriteLine("  没有异常。");
            }
            else
            {
                foreach (var c in crash.Distinct())
                    Console.WriteLine("  !! " + c);
            }

            DumpResolvedKeys();
            return crash.Count == 0;
        }

        private static IEnumerable<string> GenerateInputs(int maxTokens)
        {
            var results = new List<string>();

            void Recurse(string prefix, int depth)
            {
                if (depth > 0)
                    results.Add(prefix);
                if (depth == maxTokens) return;

                foreach (var t in Tokens)
                    Recurse(prefix.Length == 0 ? t : $"{prefix} {t}", depth + 1);
            }

            Recurse("", 0);
            return results;
        }

        /// <summary>
        /// 打印每种输入下，Main.cs 最终传给 List() 的 (snippetKey, folderKey)。
        /// 这是排查 :f 解析问题最直观的一张表。
        /// </summary>
        private static void DumpResolvedKeys()
        {
            Console.WriteLine();
            Console.WriteLine("--- 各模式下 (:f <Folder> <Snippet>) 解析结果 ---");
            Console.WriteLine($"  {"输入",-26} {"Disable",-24} {"Auto",-24} {"First",-24} {"Last",-24}");

            var inputs = new[]
            {
                "sp dev",
                "sp :",
                "sp :f",
                "sp :f Java",
                "sp :f Java dev",
                "sp :f My Folder dev",
                "sp dev :",
                "sp dev :f",
                "sp dev :f Java",
                "sp dev :f My Folder",
                "sp dev pass :f Java",
            };

            foreach (var input in inputs)
            {
                var cells = new List<string>();
                foreach (var mode in (SearchFolderMode[])Enum.GetValues(typeof(SearchFolderMode)))
                {
                    var harness = new SnippetsHarness(SnippetsHarness.BuildSampleData());
                    harness.Settings.SearchFolderMode = mode;
                    harness.Fake.ClearCallLog();
                    harness.Query(input);

                    var call = harness.Fake.LastListCall;
                    cells.Add(call == null
                        ? "(无 List 调用)"
                        : $"n=\"{call.Value.name}\",f=\"{call.Value.folderName}\"");
                }

                var display = input.StartsWith("sp ") ? input[3..] : input;
                Console.WriteLine($"  {display,-26} {cells[0],-24} {cells[1],-24} {cells[2],-24} {cells[3],-24}");
            }

            Console.WriteLine("  (n = snippetKey / name 过滤，f = folderKey / folderName 过滤)");
        }
    }
}
