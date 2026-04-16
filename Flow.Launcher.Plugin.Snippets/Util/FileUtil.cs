using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Flow.Launcher.Plugin.Snippets.Util;

public class FileUtil
{
    public static void WriteSnippets(string file, List<SnippetModel> sms)
    {
        var json = JsonSerializer.Serialize(sms);
        File.WriteAllText(file, json);
    }

    public static List<SnippetModel> ReadSnippets(string file)
    {
        var json = File.ReadAllText(file);
        return JsonSerializer.Deserialize<List<SnippetModel>>(json);
    }
}