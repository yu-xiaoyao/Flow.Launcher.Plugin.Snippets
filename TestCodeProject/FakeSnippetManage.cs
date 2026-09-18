using System.Collections.Generic;
using System.Linq;
using Flow.Launcher.Plugin.Snippets;
using Flow.Launcher.Plugin.Snippets.Model;
using Flow.Launcher.Plugin.Snippets.Sqlite;

namespace TestCodeProject
{
    /// <summary>
    /// 内存版 SnippetManage。
    /// 过滤语义刻意和 MsSqliteSnippetManage 保持一致：
    ///   name       -> name LIKE '%name%'       （空/null 表示不过滤）
    ///   folderName -> folder.name LIKE '%folderName%' （空/null 表示不过滤）
    ///   folderId   -> folder.id == folderId
    /// 同时记录每次 List 调用的实参，方便断言 Main.cs 到底把哪个 key 解析成了 folderKey / snippetKey。
    /// </summary>
    public class FakeSnippetManage : SnippetManage
    {
        public readonly List<FolderModel> Folders = new();
        public readonly List<SnippetModel> Snippets = new();

        /// <summary>每次 List() 调用的实参记录</summary>
        public readonly List<(string name, string folderName, long? folderId)> ListCalls = new();

        /// <summary>每次 Add(name, value) 的记录 —— 用来断言「点了新建候选之后到底建了什么」</summary>
        public readonly List<(string name, string value)> AddCalls = new();

        /// <summary>最后一次 List() 调用</summary>
        public (string name, string folderName, long? folderId)? LastListCall =>
            ListCalls.Count == 0 ? null : ListCalls[^1];

        public void ClearCallLog() => ListCalls.Clear();

        public void ClearAddLog() => AddCalls.Clear();

        #region data setup

        public FakeSnippetManage AddFolder(string folderName)
        {
            Folders.Add(new FolderModel { Id = Folders.Count + 1, Name = folderName, OrderNum = Folders.Count + 1 });
            return this;
        }

        public FakeSnippetManage AddSnippet(string name, string folderName = null, string value = null)
        {
            long? folderId = null;
            if (folderName != null)
            {
                var folder = Folders.FirstOrDefault(f => f.Name == folderName)
                             ?? throw new InvalidOperationException($"folder not found: {folderName}");
                folderId = folder.Id;
            }

            Snippets.Add(new SnippetModel
            {
                Id = Snippets.Count + 1,
                Name = name,
                Value = value ?? $"value of {name}",
                FolderId = folderId,
                FolderName = folderName,
            });
            return this;
        }

        #endregion

        #region filtering (mirrors SQL LIKE + order by order_num desc)

        public List<SnippetModel> List(string name = null, string value = null, bool? favorites = null,
            long? folderId = null, string folderName = null, int limit = -1)
        {
            ListCalls.Add((name, folderName, folderId));

            var query = Snippets.AsEnumerable();

            if (!string.IsNullOrEmpty(name))
                query = query.Where(s => s.Name.Contains(name));

            if (!string.IsNullOrEmpty(value))
                query = query.Where(s => s.Value.Contains(value));

            if (folderId != null)
                query = query.Where(s => s.FolderId == folderId);

            if (!string.IsNullOrEmpty(folderName))
                query = query.Where(s => s.FolderName != null && s.FolderName.Contains(folderName));

            var result = query.OrderByDescending(s => s.Id).ToList();
            return limit > 0 ? result.Take(limit).ToList() : result;
        }

        public List<SnippetModel> ListRecent(string name = null, string value = null, string folderName = null,
            int limit = 20)
        {
            return List(name, value, null, null, folderName).Take(limit).ToList();
        }

        public List<SnippetModel> ListNoFolder(string name = null, string value = null)
        {
            ListCalls.Add((name, null, null));

            var query = Snippets.Where(s => s.FolderId == null).AsEnumerable();
            if (!string.IsNullOrEmpty(name))
                query = query.Where(s => s.Name.Contains(name));
            if (!string.IsNullOrEmpty(value))
                query = query.Where(s => s.Value.Contains(value));
            return query.ToList();
        }

        public List<FolderModel> ListFolders(string name = null)
        {
            var query = Folders.AsEnumerable();
            if (!string.IsNullOrEmpty(name))
                query = query.Where(f => f.Name.Contains(name));
            return query.OrderBy(f => f.OrderNum).ToList();
        }

        #endregion

        #region unused members

        public void SetKeywordMatchMode(KeywordMatchMode keywordMatchMode)
        {
        }

        public SnippetModel GetByKey(string key) => Snippets.FirstOrDefault(s => s.Name == key);

        public SnippetModel GetSnippetById(long id) => Snippets.FirstOrDefault(s => s.Id == id);

        public FolderModel GetFolder(string name) => Folders.FirstOrDefault(f => f.Name == name);

        public bool Add(string name, string value, string syntax = null, bool? favorites = null, long? folderId = null)
        {
            AddCalls.Add((name, value));
            return true;
        }

        public bool Add(SnippetModel sm) => throw new NotSupportedException();

        public bool RemoveSnippetById(long id) => throw new NotSupportedException();

        public bool UpdateSnippetById(long id, string name = null, string value = null, string syntax = null,
            long? orderNum = null, long? folderId = null, bool? favorites = null) => throw new NotSupportedException();

        public bool UpdateSnippetAlwaysById(long id, string name, string value, long orderNum, string syntax = null,
            long? folderId = null, bool? favorites = null) => throw new NotSupportedException();

        public void Clear() => throw new NotSupportedException();

        public void ResetAllScore() => throw new NotSupportedException();

        public bool AddFolder(string name, long? orderNum = null) => throw new NotSupportedException();

        public bool RemoveFolder(string name) => throw new NotSupportedException();

        public bool RemoveFolderById(long folderId) => throw new NotSupportedException();

        public bool UpdateFolderById(long id, string newName = null, long? orderNum = null)
            => throw new NotSupportedException();

        public long? GetFolderUpOrderNum(long id, long orderNum) => throw new NotSupportedException();

        public long? GetFolderDownOrderNum(long id, long orderNum) => throw new NotSupportedException();

        public long? GetFolderMinOrderNum(long id) => throw new NotSupportedException();

        public long? GetFolderMaxOrderNum(long id) => throw new NotSupportedException();

        public void CleanFolders() => throw new NotSupportedException();

        public long? GetSnippetUpOrderNum(long id, long orderNum, long? folderId = null)
            => throw new NotSupportedException();

        public long? GetSnippetDownOrderNum(long id, long orderNum, long? folderId = null)
            => throw new NotSupportedException();

        public long? GetSnippetMinOrderNum(long id) => throw new NotSupportedException();

        public long? GetSnippetMaxOrderNum(long id) => throw new NotSupportedException();

        #endregion
    }
}
