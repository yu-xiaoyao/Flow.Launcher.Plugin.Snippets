using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Flow.Launcher.Plugin.Snippets.Sqlite;
using Flow.Launcher.Plugin.Snippets.Util;

namespace Flow.Launcher.Plugin.Snippets;

public class Main_Test
{
    //TEST 
    private static string dbPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\\snippets.db";


    /// <summary>
    /// Run Main Method in Rider.
    /// New Rider Version config:
    /// Edit *.csproj File
    /// Add The Content in First PropertyGroup, after TargetFramework tag
    /// <OutputType>exe</OutputType>
    /// </summary>
    public static void Main()
    {
        InnerLogger.SetAsConsoleLogger(LoggerLevel.DEBUG);

        // test_json_settings();
        // test_sqlite_add();
        // test_sqlite_query();
        // test_variable_expander();

        test_auto_add_flow_data();
    }

    /// <summary>
    /// Generate Test Data
    /// </summary>
    private static void test_auto_add_flow_data()
    {
        const string pluginDir = "FlowLauncher\\Settings\\Plugins\\Flow.Launcher.Plugin.Snippets\\";
        var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var sm = new MsSqliteSnippetManage($"{appDataDir}\\{pluginDir}");

        // add folder 
        var folderNames = new List<string>()
        {
            "Java", "Python", "JavaScript", "DevOps", "Jvm", "Work", "Password", "CodeSnippet", "IDEA", "Windows",
            "Linux", "Mac", "Google", "OpenAi", "Unix", "github", "GoLang", "Rust", "Prod", "Docs"
        };
        var random = new Random();

        foreach (var folderName in folderNames)
        {
            var gfm = sm.GetFolder(folderName);
            if (gfm == null)
            {
                var fm = sm.CreateFolderModel(folderName);
                sm.AddFolder(fm);
                var count = random.Next(10);
                for (var i = 0; i < count; i++)
                {
                    var name = $"{folderName} Name{i}";
                    var value = $"{folderName} Value{i}";
                    sm.Add(name: name, value: value, favorites: random.Next() % 2 == 0, folderId: fm.Id);
                }
            }
        }

        // no folder Snippets
        var noFolderCount = random.Next(2, 20);
        for (int i = 0; i < noFolderCount; i++)
        {
            var name = $"no folder Name{i}";
            var value = $"no folder Value{i}";
            sm.Add(name: name, value: value, favorites: random.Next() % 2 == 0);
        }
    }

    private static void test_sqlite_query()
    {
        var sm = new MsSqliteSnippetManage(dbPath);

        var snippetModels = sm.List(name: "key1");
        foreach (var snippetModel in snippetModels)
        {
            Console.WriteLine(snippetModel + " - " + snippetModel.UpdateTime);
        }
    }

    private static void test_sqlite_add()
    {
        var sm = new MsSqliteSnippetManage(dbPath);

        sm.Add("key1", "value1");
        sm.Add("key2", "value2");
    }

    private static void test_json_settings()
    {
        var snippets = new ObservableCollection<SnippetModel>();

        snippets.Add(new SnippetModel
        {
            Name = "key1",
            Value = "value1"
        });

        snippets.Add(new SnippetModel
        {
            Name = "key2",
            Value = "value2"
        });

        snippets.Add(new SnippetModel
        {
            Name = "key3",
            Value = "value3"
        });
    }

    private static void test_variable_expander()
    {
        Console.WriteLine("Testing VariableExpander...\n");

        // Test basic variables
        Console.WriteLine("1. Basic date variable:");
        var result1 = VariableExpander.Expand("Today is {{date}}");
        Console.WriteLine($"   Input: 'Today is {{{{date}}}}'");
        Console.WriteLine($"   Output: '{result1}'\n");

        Console.WriteLine("2. Basic time variable:");
        var result2 = VariableExpander.Expand("Current time: {{time}}");
        Console.WriteLine($"   Input: 'Current time: {{{{time}}}}'");
        Console.WriteLine($"   Output: '{result2}'\n");

        Console.WriteLine("3. Date and time together:");
        var result3 = VariableExpander.Expand("{{datetime}} - Timestamp: {{timestamp}}");
        Console.WriteLine($"   Input: '{{{{datetime}}}} - Timestamp: {{{{timestamp}}}}'");
        Console.WriteLine($"   Output: '{result3}'\n");

        Console.WriteLine("4. Date components:");
        var result4 = VariableExpander.Expand("Year: {{year}}, Month: {{month}}, Day: {{day}}");
        Console.WriteLine($"   Input: 'Year: {{{{year}}}}, Month: {{{{month}}}}, Day: {{{{day}}}}'");
        Console.WriteLine($"   Output: '{result4}'\n");

        Console.WriteLine("5. Time components:");
        var result5 = VariableExpander.Expand("{{hour}}:{{minute}}:{{second}}");
        Console.WriteLine($"   Input: '{{{{hour}}}}:{{{{minute}}}}:{{{{second}}}}'");
        Console.WriteLine($"   Output: '{result5}'\n");

        Console.WriteLine("6. Custom date format:");
        var result6 = VariableExpander.Expand("{{date:MM/dd/yyyy}}");
        Console.WriteLine($"   Input: '{{{{date:MM/dd/yyyy}}}}'");
        Console.WriteLine($"   Output: '{result6}'\n");

        Console.WriteLine("7. Custom time format:");
        var result7 = VariableExpander.Expand("{{time:hh:mm tt}}");
        Console.WriteLine($"   Input: '{{{{time:hh:mm tt}}}}'");
        Console.WriteLine($"   Output: '{result7}'\n");

        Console.WriteLine("8. Multiple variables in text:");
        var result8 = VariableExpander.Expand("Meeting on {{date}} at {{time}}. File: meeting_{{timestamp}}.txt");
        Console.WriteLine($"   Input: 'Meeting on {{{{date}}}} at {{{{time}}}}. File: meeting_{{{{timestamp}}}}.txt'");
        Console.WriteLine($"   Output: '{result8}'\n");

        Console.WriteLine("9. Text without variables (should remain unchanged):");
        var result9 = VariableExpander.Expand("This is plain text without any variables");
        Console.WriteLine($"   Input: 'This is plain text without any variables'");
        Console.WriteLine($"   Output: '{result9}'\n");

        Console.WriteLine("10. Unknown variable (should remain unchanged):");
        var result10 = VariableExpander.Expand("This has an {{unknown}} variable");
        Console.WriteLine($"   Input: 'This has an {{{{unknown}}}} variable'");
        Console.WriteLine($"   Output: '{result10}'\n");

        Console.WriteLine("All tests completed!");
    }
}