using System.Reflection;
using Flow.Launcher.Plugin;

namespace TestCodeProject
{
    /// <summary>
    /// 复刻 Flow Launcher 的 Query 构造过程，用来在测试里手工造出与真实环境一致的 Query 对象。
    ///
    /// 复刻来源（Flow Launcher v2.1.2，见 .FlowLauncherSource）：
    ///   - Flow.Launcher/ViewModel/MainViewModel.cs:445
    ///         var query = QueryBuilder.Build(QueryText, QueryText.Trim(), PluginManager.GetNonGlobalPlugins());
    ///   - Flow.Launcher.Core/Plugin/QueryBuilder.cs:26-60
    ///
    /// 关键结论（决定了 Main.cs 里 :f 参数解析是否正确）：
    ///   1. SearchTerms = trimmedQuery 按空格切分 + RemoveEmptyEntries 后 **去掉第 0 个（ActionKeyword）**，因此不含空项。
    ///   2. Search = trimmedQuery 中 ActionKeyword 之后的子串，只做 TrimStart；而 trimmedQuery 本身已经是 QueryText.Trim()。
    ///      => Search 既没有前导空格也没有尾随空格。
    ///   3. 只有一个 token 时（例如 `sp :f`），Search 被强制置为 string.Empty —— 但 SearchTerms 仍然是 [":f"]。
    ///
    /// Query 的属性在插件编译期引用的 4.5.0 和运行期的 5.3.0 之间不完全一致，
    /// 所以这里统一用反射写字段，避免和某个具体 API 版本绑死。
    /// </summary>
    public static class QueryFactory
    {
        public const string TermSeparator = " ";

        private static readonly Type QueryType = typeof(Query);

        /// <summary>构造 Query。<paramref name="rawInput"/> 相当于用户在搜索框里敲的内容（含 action keyword）。</summary>
        public static Query Build(string rawInput, string actionKeyword = "sp")
        {
            var trimmedQuery = (rawInput ?? string.Empty).Trim();   // MainViewModel: QueryText.Trim()

            if (string.IsNullOrEmpty(trimmedQuery))
                return Make("", trimmedQuery, Array.Empty<string>(), "", trimmedQuery);

            var terms = trimmedQuery.Split(TermSeparator, StringSplitOptions.RemoveEmptyEntries);
            if (terms.Length == 0)
                return null;

            string search;
            string[] searchTerms;

            if (terms[0] == actionKeyword)   // QueryBuilder: nonGlobalPlugins 命中
            {
                search = terms.Length > 1 ? trimmedQuery[(actionKeyword.Length + 1)..].TrimStart() : string.Empty;
                searchTerms = terms[1..];
            }
            else
            {
                actionKeyword = string.Empty;
                search = trimmedQuery.TrimStart();
                searchTerms = terms;
            }

            return Make(search, trimmedQuery, searchTerms, actionKeyword, trimmedQuery);
        }

        private static Query Make(string search, string trimmed, string[] searchTerms, string actionKeyword,
            string original)
        {
            var q = (Query)Activator.CreateInstance(QueryType, nonPublic: true);
            SetProperty(q, "Search", search);
            SetProperty(q, "TrimmedQuery", trimmed);      // 4.5.0 里叫 RawQuery
            SetProperty(q, "RawQuery", trimmed);
            SetProperty(q, "OriginalQuery", original);
            SetProperty(q, "SearchTerms", searchTerms);
            SetProperty(q, "ActionKeyword", actionKeyword);
            return q;
        }

        private static void SetProperty(object target, string propertyName, object value)
        {
            var field = QueryType.GetField($"<{propertyName}>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(target, value);
                return;
            }

            var prop = QueryType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (prop != null)
            {
                var setter = prop.GetSetMethod(true);
                if (setter != null)
                {
                    setter.Invoke(target, new[] { value });
                    return;
                }
            }
            // 该版本没有这个属性，忽略即可（测试只依赖 Search / SearchTerms / ActionKeyword）
        }

        /// <summary>打印一次 Query 的实际 API 形状，方便确认测试环境和运行期一致。</summary>
        public static void DumpApi()
        {
            Console.WriteLine("--- Flow.Launcher.Plugin.Query API ---");
            Console.WriteLine($"  assembly : {QueryType.Assembly.Location}");
            foreach (var p in QueryType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var backing = QueryType.GetField($"<{p.Name}>k__BackingField",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Console.WriteLine($"  {p.PropertyType.Name,-12} {p.Name,-16} backingField={backing != null}");
            }
        }
    }
}
