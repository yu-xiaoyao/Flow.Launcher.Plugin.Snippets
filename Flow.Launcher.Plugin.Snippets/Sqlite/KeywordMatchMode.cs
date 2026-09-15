namespace Flow.Launcher.Plugin.Snippets.Sqlite;

public enum KeywordMatchMode
{
    /// <summary>
    /// default: use sqlite like.
    /// if value = snippets
    /// search key: nip / ppe ...
    /// </summary>
    Sql_Like = 0,

    /// <summary>
    /// flow launcher fuzzy search
    /// if value = snippet
    /// search key: sp / spp
    /// </summary>
    Flow_FuzzySearch = 1
}