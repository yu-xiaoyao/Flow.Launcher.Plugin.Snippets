using System;

namespace Flow.Launcher.Plugin.Snippets;

public class Settings : BaseModel
{
    /// <summary>
    /// remove in next version
    /// </summary>
    [Obsolete]
    public StorageType StorageType { get; set; } = StorageType.Sqlite;

    /// <summary>
    /// Enable Folder Mode
    /// </summary>
    public bool EnableFolder { get; set; } = false;

    /// <summary>
    /// copy method
    /// </summary>
    public int CopyMethod { get; set; }

    /// <summary>
    /// Enable Auto Paste Feature
    /// </summary>
    public bool AutoPasteEnabled { get; set; } = true;

    /// <summary>
    /// Delay in milliseconds before pasting
    /// </summary>
    public int PasteDelayMs { get; set; } = 50;

    /// <summary>
    /// Auto Paste Method
    /// </summary>
    public int AutoPasteMethod { get; set; } = 3;

    /// <summary>
    /// Send Ctrl + V Method
    /// </summary>
    public int SendCtrlVMethod { get; set; }

    /// <summary>
    /// Enable Dynamic Variables when value is {{VAR}}
    /// </summary>
    public bool DynamicVariables { get; set; }


    public bool DisplayFolder { get; set; } = true;
    public bool DisplayFolderIcon { get; set; } = true;

    public bool FirstKeyPrimaryFolder { get; set; } = true;

    /// <summary>
    /// 1.x.x version snippets
    /// </summary>
    // [Obsolete]
    // public Dictionary<string, string> Snippets { get; set; }
}

public enum StorageType
{
    [Obsolete] JsonSetting,
    Sqlite
}