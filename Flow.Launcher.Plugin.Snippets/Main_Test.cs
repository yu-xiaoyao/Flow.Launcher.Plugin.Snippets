using System;
using System.Collections.ObjectModel;
using Flow.Launcher.Plugin.Snippets.Json;
using Flow.Launcher.Plugin.Snippets.Sqlite;
using Flow.Launcher.Plugin.Snippets.Util;

namespace Flow.Launcher.Plugin.Snippets;

public class Main_Test
{
    //TEST 
    private static string dbPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\\snippets.db";

    public static void Main()
    {
        // test_json_settings();
        // test_sqlite_add();
        // test_sqlite_query();
        test_variable_expander();
    }

    private static void test_sqlite_query()
    {
        var sm = new SqliteSnippetManage(dbPath);

        var snippetModels = sm.List(key: "key1");
        foreach (var snippetModel in snippetModels)
        {
            Console.WriteLine(snippetModel + " - " + snippetModel.UpdateTime);
        }
    }

    private static void test_sqlite_add()
    {
        var sm = new SqliteSnippetManage(dbPath);

        sm.Add(new SnippetModel
        {
            Key = "key1",
            Value = "value1"
        });
        sm.Add(new SnippetModel
        {
            Key = "key2",
            Value = "value2"
        });
    }

    private static void test_json_settings()
    {
        var snippets = new ObservableCollection<SnippetModel>();

        snippets.Add(new SnippetModel
        {
            Key = "key1",
            Value = "value1"
        });

        snippets.Add(new SnippetModel
        {
            Key = "key2",
            Value = "value2"
        });

        snippets.Add(new SnippetModel
        {
            Key = "key3",
            Value = "value3"
        });

        var sm = new JsonSettingSnippetManage(null);
        var v1 = sm.GetByKey("key1");
        Console.WriteLine(v1 == null);
        Console.WriteLine(v1);
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