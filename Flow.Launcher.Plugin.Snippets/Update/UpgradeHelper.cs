using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Flow.Launcher.Plugin.Snippets.Util;

namespace Flow.Launcher.Plugin.Snippets.Update;

public class UpgradeHelper
{
    private class JsonSnippetModel
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public int? Score { get; set; }
        public DateTime? UpdateTime { get; set; }
    }

    private class SettingV2
    {
        public List<JsonSnippetModel> SnippetList { get; set; }
    }

    public static void UpgradeV2ToV3(SnippetManage sm, string settingPath)
    {
        // v2 -> v3
        var v2JsonPath = Path.Combine(settingPath, "JsonSetting.json");
        if (File.Exists(v2JsonPath))
        {
            try
            {
                var json = File.ReadAllText(v2JsonPath);
                var v2 = JsonSerializer.Deserialize<SettingV2>(json);
                if (v2 is { SnippetList.Count: > 0 })
                {
                    var list = v2.SnippetList;
                    foreach (var jsm in list)
                    {
                        var dateTime = DateTimeUtil.TrimMilliseconds(jsm.UpdateTime ?? DateTime.Now);
                        var score = jsm.Score ?? 0;
                        var model = new SnippetModel
                        {
                            Key = jsm.Key,
                            Value = jsm.Value,
                            Score = score,
                            CreateTime = dateTime,
                            UpdateTime = dateTime
                        };
                        sm.Add(model);
                    }
                }
            }
            catch (JsonException)
            {
            }

            // Rename 
            File.Move(v2JsonPath, v2JsonPath + ".v2");
        }
    }
}