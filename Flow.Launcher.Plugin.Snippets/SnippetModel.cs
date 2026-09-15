using System;
using JetBrains.Annotations;

namespace Flow.Launcher.Plugin.Snippets;

public class SnippetModel
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Value { get; set; }
    public long OrderNum { get; set; } = 0;
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }

    public int Faviorites { get; set; }
    [CanBeNull] public string Syntax { get; set; }

    public long? FolderId { get; set; }


    /// <summary>
    /// Table Not Exists Field
    /// </summary>
    [CanBeNull]
    public string FolderName { get; set; }

    public SnippetModel Clone() => new()
    {
        Id = Id,
        Name = Name,
        Value = Value,
        OrderNum = OrderNum,
        Faviorites = Faviorites,
        Syntax = Syntax,
        FolderId = FolderId,
        FolderName = FolderName,
        CreateTime = CreateTime,
        UpdateTime = UpdateTime,
    };


    public override string ToString()
    {
        return $"Name: {Name}, Value: {Value}";
    }
}