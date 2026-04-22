using System.Collections.Generic;
using Flow.Launcher.Plugin.Snippets.Model;
using JetBrains.Annotations;

namespace Flow.Launcher.Plugin.Snippets;

public interface SnippetManage
{
    [CanBeNull]
    SnippetModel GetByKey(string key);

    List<SnippetModel> List([CanBeNull] string key = null, [CanBeNull] string value = null, long? folderId = null);

    List<SnippetModel> ListRecent([CanBeNull] string key = null, [CanBeNull] string value = null, int limit = 20);

    List<SnippetModel> ListNoFolder([CanBeNull] string key = null, [CanBeNull] string value = null);

    bool Add(string key, string value, long? folderId = null, int score = 0);

    bool RemoveByKey(string key);

    bool UpdateByKey(string key, [CanBeNull] string value = null, long? folderId = null, int? score = null);

    void Clear();

    void ResetAllScore();

    #region Folder Operations

    [CanBeNull]
    FolderModel GetFolder(string name);

    bool AddFolder(string name);

    bool RemoveFolder(string name);

    bool UpdateFolderById(long id, string newName);

    List<FolderModel> ListFolders([CanBeNull] string name = null);

    void CleanFolders();

    #endregion

    void Close()
    {
    }
}