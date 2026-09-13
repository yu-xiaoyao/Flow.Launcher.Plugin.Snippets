using System.Text;
using JetBrains.Annotations;

namespace Flow.Launcher.Plugin.Snippets.Sqlite;

/// <summary>
/// SQLite <c>LIKE</c> 模式串的转义 / 反转义。
///
/// <c>LIKE</c> 里 <c>%</c> 是「任意串」、<c>_</c> 是「任意单字符」。用户输入的这两个字符是普通字符
/// （Snippet 里出现 <c>_</c> 和 <c>%</c> 很常见，比如 <c>user_name</c>、<c>%s</c>、<c>100%</c>），
/// 直接拼进模式串会被当成通配符，导致搜 <c>a_b</c> 命中 <c>axb</c>、搜 <c>100%</c> 命中所有含 <c>100</c> 的。
///
/// 用法：参数化时用 <see cref="Contains"/> 生成模式串，SQL 里配 <c>escape '\'</c>。
/// </summary>
public static class LikePattern
{
    /// <summary>
    /// 转义字符，需要和 SQL 里的 <c>ESCAPE</c> 子句保持一致。
    /// </summary>
    public const char EscapeChar = '\\';

    /// <summary>
    /// SQL 里要拼的 <c>ESCAPE</c> 子句。
    /// </summary>
    public const string EscapeClause = " escape '\\'";

    /// <summary>
    /// 转义模式串里的通配符，使其只按字面量匹配。
    /// </summary>
    [NotNull]
    public static string Escape([CanBeNull] string value)
    {
        if (string.IsNullOrEmpty(value)) return "";

        var builder = new StringBuilder(value.Length + 4);
        foreach (var c in value)
        {
            if (c == EscapeChar || c == '%' || c == '_')
                builder.Append(EscapeChar);
            builder.Append(c);
        }

        return builder.ToString();
    }

    /// <summary>
    /// 把 <see cref="Escape"/> 转义过的模式串还原成原始文本。
    /// </summary>
    [NotNull]
    public static string Unescape([CanBeNull] string value)
    {
        if (string.IsNullOrEmpty(value)) return "";

        var builder = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == EscapeChar && i + 1 < value.Length)
                i++;
            builder.Append(value[i]);
        }

        return builder.ToString();
    }

    /// <summary>
    /// 生成「包含」查询用的模式串，等价于 SQL 的 <c>%value%</c>（但 value 里的通配符已被转义）。
    /// </summary>
    [NotNull]
    public static string Contains([CanBeNull] string value) => $"%{Escape(value)}%";

    /// <summary>
    /// <see cref="Contains"/> 的逆运算：从模式串还原出用户输入的关键字。
    /// </summary>
    [NotNull]
    public static string ParseContains([CanBeNull] string pattern)
    {
        if (string.IsNullOrEmpty(pattern)) return "";

        // 剥掉 Contains() 加上的那一层 %
        if (pattern.Length >= 2 && pattern[0] == '%' && pattern[^1] == '%')
            pattern = pattern[1..^1];

        return Unescape(pattern);
    }
}
