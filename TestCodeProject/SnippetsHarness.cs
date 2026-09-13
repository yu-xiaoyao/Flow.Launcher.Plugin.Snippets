using System.Reflection;
using Flow.Launcher.Plugin;
using Flow.Launcher.Plugin.Snippets;
using Flow.Launcher.Plugin.Snippets.Util;

namespace TestCodeProject
{
    /// <summary>
    /// 把 Main.cs 里的 Snippets 插件实例起来（不开 Flow Launcher），
    /// 通过反射注入 Settings / SnippetManage，然后直接调 Query()。
    /// </summary>
    public class SnippetsHarness
    {
        private static readonly FieldInfo SettingsField =
            typeof(Snippets).GetField("_settings", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo ManageField =
            typeof(Snippets).GetField("_snippetManage", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo ContextField =
            typeof(Snippets).GetField("_context", BindingFlags.Instance | BindingFlags.NonPublic);

        public Snippets Plugin { get; }
        public Settings Settings { get; }

        /// <summary>真实的 SnippetManage（可以是 FakeSnippetManage，也可以是 Sqlite 实现）</summary>
        public SnippetManage Manage { get; }

        /// <summary><see cref="Manage"/> 是 FakeSnippetManage 时，用来读 List() 调用记录</summary>
        public FakeSnippetManage Fake => (FakeSnippetManage)Manage;

        public PluginInitContext Context { get; }

        public SnippetsHarness(FakeSnippetManage manage = null, Settings settings = null)
            : this((SnippetManage)manage, settings)
        {
        }

        public SnippetsHarness(SnippetManage manage, Settings settings = null)
        {
            Manage = manage ?? new FakeSnippetManage();
            Settings = settings ?? new Settings
            {
                DisplayFolder = true,
                DisplayFolderIcon = false,
                DynamicVariables = false,
            };

            // PluginInitContext 有无参构造 + 公开的 API setter
            Context = new PluginInitContext { API = FakeApiProxy.Create() };

            Plugin = new Snippets();
            SettingsField.SetValue(Plugin, Settings);
            ManageField.SetValue(Plugin, Manage);
            ContextField.SetValue(Plugin, Context);
        }

        /// <summary>跑一次查询，返回结果标题列表。</summary>
        public List<string> Query(string rawInput, string actionKeyword = "sp")
        {
            var query = QueryFactory.Build(rawInput, actionKeyword);
            if (query == null) return new List<string>();

            var results = Plugin.Query(query);
            return results.Select(r => r.Title).ToList();
        }

        /// <summary>跑一次查询，返回完整 Result（要检查 AutoCompleteText 之类时用）。</summary>
        public List<Result> QueryFull(string rawInput, string actionKeyword = "sp")
        {
            var query = QueryFactory.Build(rawInput, actionKeyword);
            return query == null ? new List<Result>() : Plugin.Query(query);
        }

        /// <summary>构造测试数据。返回 (标题, 值)。</summary>
        public static FakeSnippetManage BuildSampleData()
        {
            var manage = new FakeSnippetManage();

            manage.AddFolder("Java");
            manage.AddFolder("Python");
            manage.AddFolder("My Folder");

            manage.AddSnippet("dev-pass", "Java");
            manage.AddSnippet("jdk-config", "Java");
            manage.AddSnippet("dev-pass", "Python");
            manage.AddSnippet("py-env", "Python");
            manage.AddSnippet("dev-pass", "My Folder");
            manage.AddSnippet("dev-pass");
            manage.AddSnippet("uat-pass");

            return manage;
        }

        public static void SetLogger()
        {
            InnerLogger.SetAsConsoleLogger(LoggerLevel.OFF);
        }
    }
}
