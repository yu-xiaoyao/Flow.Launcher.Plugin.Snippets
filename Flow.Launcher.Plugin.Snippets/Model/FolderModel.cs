using System;

namespace Flow.Launcher.Plugin.Snippets.Model;

public class FolderModel
{
    public long Id { get; set; }

    public string Name { get; set; }

    public long OrderNum { get; set; }

    public DateTime CreateTime { get; set; }

    public DateTime UpdateTime { get; set; }
}