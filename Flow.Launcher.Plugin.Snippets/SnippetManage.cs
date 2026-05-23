using System.Collections.Generic;
using Flow.Launcher.Plugin.Snippets.Model;
using Flow.Launcher.Plugin.Snippets.Sqlite;
using JetBrains.Annotations;

namespace Flow.Launcher.Plugin.Snippets;

public interface SnippetManage
{
    #region Config

    void SetKeywordMatchMode(KeywordMatchMode keywordMatchMode);

    #endregion

    [CanBeNull]
    SnippetModel GetByKey(string key);

    [CanBeNull]
    SnippetModel GetSnippetById(long id);

    List<SnippetModel> List([CanBeNull] string name = null, [CanBeNull] string value = null, bool? favorites = null,
        long? folderId = null, [CanBeNull] string folderName = null, int limit = -1);

    List<SnippetModel> ListRecent([CanBeNull] string name = null, [CanBeNull] string value = null,
        [CanBeNull] string folderName = null, int limit = 20);

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

    bool UpdateSnippetAlwaysById(long id, string name, string value,
        long orderNum,
        [CanBeNull] string syntax = null,
        long? folderId = null,
        bool? favorites = null);

    void Clear();

    void ResetAllScore();

    #region Folder Operations

    [CanBeNull]
    FolderModel GetFolder(string name);

    bool AddFolder(string name, long? orderNum = null);

    bool RemoveFolder(string name);

    bool RemoveFolderById(long folderId);

    bool UpdateFolderById(long id, [CanBeNull] string newName = null, long? orderNum = null);

    List<FolderModel> ListFolders([CanBeNull] string name = null);

    [CanBeNull]
    long? GetFolderUpOrderNum(long id, long orderNum);

    [CanBeNull]
    long? GetFolderDownOrderNum(long id, long orderNum);

    [CanBeNull]
    long? GetFolderMinOrderNum(long id);

    [CanBeNull]
    long? GetFolderMaxOrderNum(long id);

    void CleanFolders();

    #endregion

    #region Snippet Order Operations

    [CanBeNull]
    long? GetSnippetUpOrderNum(long id, long orderNum);

    [CanBeNull]
    long? GetSnippetDownOrderNum(long id, long orderNum);

    [CanBeNull]
    long? GetSnippetMinOrderNum(long id);

    [CanBeNull]
    long? GetSnippetMaxOrderNum(long id);

    #endregion

    void Close()
    {
    }
}