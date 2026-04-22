using System;
using JetBrains.Annotations;

namespace Flow.Launcher.Plugin.Snippets;

public class SnippetModel
{
    public string Key { get; set; }
    public string Value { get; set; }
    public int Score { get; set; } = 0;
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }

    public long? FolderId { get; set; }


    /// <summary>
    /// Table Not Exists Field
    /// </summary>
    [CanBeNull]
    public string FolderName { get; set; }

    public override string ToString()
    {
        return $"Key: {Key}, Value: {Value}, Score: {Score}";
    }
}