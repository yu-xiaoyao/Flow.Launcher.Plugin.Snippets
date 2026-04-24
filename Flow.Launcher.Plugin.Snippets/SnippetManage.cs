using System.Collections.Generic;
using Flow.Launcher.Plugin.Snippets.Model;
using JetBrains.Annotations;

namespace Flow.Launcher.Plugin.Snippets;

public interface SnippetManage
{
    [CanBeNull]
    SnippetModel GetByKey(string key);

    [CanBeNull]
    SnippetModel GetSnippetById(long id);

    List<SnippetModel> List([CanBeNull] string name = null, [CanBeNull] string value = null, bool? favorites = null,
        long? folderId = null);

    List<SnippetModel> ListRecent([CanBeNull] string name = null, [CanBeNull] string value = null, int limit = 20);

    List<SnippetModel> ListNoFolder([CanBeNull] string name = null, [CanBeNull] string value = null);

    bool Add(string name, string value, [CanBeNull] string syntax = null, bool? favorites = null,
        long? folderId = null);

    bool Add(SnippetModel sm);

    bool RemoveSnippetById(long id);

    // bool RemoveByKey(string key);

    // bool UpdateByKey(string key, [CanBeNull] string value = null, long? folderId = null, int? score = null);
    bool UpdateSnippetById(long id, [CanBeNull] string name = null, [CanBeNull] string value = null,
        [CanBeNull] string syntax = null,
        long? orderNum = null,
        long? folderId = null,
        bool? favorites = null);

    void Clear();

    void ResetAllScore();

    #region Folder Operations

    [CanBeNull]
    FolderModel GetFolder(string name);

    bool AddFolder(string name);

    bool RemoveFolder(string name);

    bool RemoveFolderById(long folderId);

    bool UpdateFolderById(long id, string newName);

    List<FolderModel> ListFolders([CanBeNull] string name = null);

    void CleanFolders();

    #endregion

    void Close()
    {
    }
}