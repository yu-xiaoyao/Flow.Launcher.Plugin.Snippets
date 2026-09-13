using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Flow.Launcher.Plugin.Snippets;

/// <summary>
/// <c>:f</c>（Folder 过滤参数）在查询里的位置。
/// </summary>
public enum FolderParamKind
{
    /// <summary>
    /// 查询里没有 <c>:</c> / <c>:f</c>，按普通 Snippet 名字搜索。
    /// </summary>
    None,

    /// <summary>
    /// <c>:f</c> 在第 1 位：<c>sp :f Folder Snippet1 Snippet2</c>。
    /// 第 2 个参数是 Folder 名，其后**全部**参数拼成 Snippet 过滤名。
    /// </summary>
    First,

    /// <summary>
    /// <c>:f</c> 在倒数第 2 位：<c>sp Snippet1 Snippet2 :f Folder</c>。
    /// 最后 1 个参数是 Folder 名，其前**全部**参数拼成 Snippet 过滤名。
    /// </summary>
    Last,

    /// <summary>
    /// 打了 <c>:</c> / <c>:f</c>，但还没输入 Folder 名（<c>sp :</c> / <c>sp :f</c> / <c>sp dev :f</c>）。
    /// </summary>
    Incomplete,
}

/// <summary>
/// 把 <see cref="Query.SearchTerms"/> 解析成 <c>(SnippetKey, FolderKey)</c>。
///
/// Flow Launcher 在构造 <see cref="Query"/> 时已经做完了两件事，所以这里只需要关心 <c>:</c> / <c>:f</c> 的位置：
/// 按空格切分并去掉空项（<c>StringSplitOptions.RemoveEmptyEntries</c>），以及去掉开头的 ActionKeyword。
///
/// 约定：
/// <list type="bullet">
/// <item><description><see cref="SnippetKey"/> 为空串表示不按名字过滤；</description></item>
/// <item><description><see cref="FolderKey"/> 为空串表示不按文件夹过滤；</description></item>
/// <item><description>Folder 名允许带空格 —— <c>:f</c> 之后的全部 token 会拼成 Folder 名。</description></item>
/// </list>
/// </summary>
public sealed class FolderQuery
{
    private const string Separator = " ";

    /// <summary>
    /// Folder 参数的标志。单独的 <c>:</c> 是还没敲完的 <c>:f</c>。
    /// </summary>
    public const string FolderSign = ":f";

    /// <summary>
    /// 传给 <c>SnippetManage.List(name:)</c> 的 Snippet 过滤名，空串表示不过滤。
    /// </summary>
    [NotNull]
    public string SnippetKey { get; }

    /// <summary>
    /// 传给 <c>SnippetManage.List(folderName:)</c> 的 Folder 过滤名，空串表示不过滤。
    /// </summary>
    [NotNull]
    public string FolderKey { get; }

    /// <summary>
    /// <c>:</c> / <c>:f</c> 出现的位置。
    /// </summary>
    public FolderParamKind Kind { get; }

    /// <summary>
    /// <c>:</c> / <c>:f</c> 在 SearchTerms 里的下标，没有时为 <c>-1</c>。
    /// </summary>
    public int SignIndex { get; }

    /// <summary>
    /// 是否指定了 Snippet 过滤名。
    /// </summary>
    public bool HasSnippetKey => !string.IsNullOrEmpty(SnippetKey);

    /// <summary>
    /// 是否指定了 Folder 过滤名。
    /// </summary>
    public bool HasFolderKey => !string.IsNullOrEmpty(FolderKey);

    private FolderQuery(string snippetKey, string folderKey, FolderParamKind kind, int signIndex)
    {
        SnippetKey = snippetKey ?? "";
        FolderKey = folderKey ?? "";
        Kind = kind;
        SignIndex = signIndex;
    }

    /// <summary>
    /// 解析 SearchTerms。
    /// </summary>
    /// <param name="searchTerms">
    /// 已去掉 ActionKeyword、已去掉空项的查询分词。传 <c>null</c> 或空集合会得到 <see cref="FolderParamKind.None"/> 的空查询。
    /// </param>
    public static FolderQuery Parse([CanBeNull] IEnumerable<string> searchTerms)
    {
        var terms = searchTerms?.Where(t => !string.IsNullOrEmpty(t)).ToArray() ?? Array.Empty<string>();

        var index = -1;
        for (var i = 0; i < terms.Length; i++)
        {
            if (!IsFolderSign(terms[i])) continue;
            index = i;
            break;
        }

        if (index == -1)
            return new FolderQuery(string.Join(Separator, terms), "", FolderParamKind.None, -1);

        if (index == 0)
        {
            // `:f Folder Snippet...` —— 第 2 个参数是 Folder 名，剩下全部是 Snippet 过滤名
            return terms.Length < 2
                ? new FolderQuery("", "", FolderParamKind.Incomplete, index)
                : new FolderQuery(string.Join(Separator, terms[2..]), terms[1], FolderParamKind.First, index);
        }

        var snippetKey = string.Join(Separator, terms[..index]);

        // `Snippet... :f` —— 打了 :f 但还没填 Folder 名
        return terms.Length - index == 1
            ? new FolderQuery(snippetKey, "", FolderParamKind.Incomplete, index)
            : new FolderQuery(snippetKey, string.Join(Separator, terms[(index + 1)..]), FolderParamKind.Last, index);
    }

    /// <summary>
    /// 判断一个 token 是不是 Folder 参数标志。<c>:</c> 视为还没敲完的 <c>:f</c>。
    /// </summary>
    public static bool IsFolderSign([CanBeNull] string term) =>
        term == ":" || string.Equals(FolderSign, term, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 按当前 <c>:f</c> 位置，拼出不含 ActionKeyword 的查询文本（结尾带一个空格方便继续输入）。
    /// </summary>
    /// <param name="snippetKey">Snippet 过滤名。</param>
    /// <param name="folderName">Folder 名。</param>
    public string Render([CanBeNull] string snippetKey, [CanBeNull] string folderName)
    {
        var parts = SignIndex <= 0
            ? new[] { FolderSign, folderName, snippetKey }   // :f Folder Snippet...
            : new[] { snippetKey, FolderSign, folderName };  // Snippet... :f Folder

        // 去掉空片段，避免 "Snippet :f " 这种尾部空格被拼成双空格
        return string.Join(Separator, parts.Where(p => !string.IsNullOrWhiteSpace(p))) + Separator;
    }

    /// <summary>
    /// 把 Folder 名填进当前查询，生成文件夹候选的补全文本。
    /// </summary>
    public string RenderWithFolder([CanBeNull] string folderName) => Render(SnippetKey, folderName);

    /// <inheritdoc />
    public override string ToString() =>
        $"Kind={Kind}, SignIndex={SignIndex}, SnippetKey=\"{SnippetKey}\", FolderKey=\"{FolderKey}\"";
}
