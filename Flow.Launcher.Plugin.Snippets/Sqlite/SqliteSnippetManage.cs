using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using Flow.Launcher.Plugin.Snippets.Model;
using Flow.Launcher.Plugin.Snippets.Util;

namespace Flow.Launcher.Plugin.Snippets.Sqlite;

public class SqliteSnippetManage : SnippetManage
{
    /**
    * version match plugin version
    */
    private const string TABLE_NAME_V2 = "snippets";

    private const string TABLE_NAME_V3 = "snippets_3";

    private const string TABLE_NAME_FOLDER_V3 = "folder_3";

    /// <summary>
    /// current table name
    /// </summary>
    private const string TABLE_NAME = TABLE_NAME_V3;

    private const string TABLE_NAME_FOLDER = TABLE_NAME_FOLDER_V3;

    // v2 Table DDL
    [Obsolete] private const string TableDDL2 =
        $"create table {TABLE_NAME_V2} (key varchar(200) not null primary key, value text not null, score int not null default 0, update_time datetime not null DEFAULT CURRENT_TIMESTAMP, create_time datetime not null DEFAULT CURRENT_TIMESTAMP)";

    private const string QueryAllSql2 = $"SELECT key, value, score, update_time from {TABLE_NAME_V2}";


    //language=SQL
    private const string TableDDL3 = $"""
                                      create table {TABLE_NAME}
                                      (
                                          id          BIGINT       NOT NULL PRIMARY KEY,
                                          name        varchar(200) NOT NULL,
                                          value       text         NOT NULL,
                                          order_num   BIGINT       NOT NULL,
                                          create_time datetime     NOT NULL,
                                          update_time datetime     NOT NULL,
                                          favorites   INTEGER      NOT NULL default 0,
                                          syntax      varchar(100),
                                          folder_id   BIGINT
                                      )
                                      """;

    private const string QueryAllSql3 =
        $"SELECT s.id, s.name, s.value, s.order_num, s.create_time, s.update_time, s.favorites, s.syntax, f.id as folder_id, f.name as folder_name FROM {TABLE_NAME} s left join {TABLE_NAME_FOLDER} f on s.folder_id = f.id";

    private const string QueryAllSql = QueryAllSql3;

    //language=SQL
    private const string FolderTableDDL3 = $"""
                                            create table {TABLE_NAME_FOLDER}
                                            (
                                                id          BIGINT             NOT NULL PRIMARY KEY,
                                                name        varchar(200)       NOT NULL UNIQUE,
                                                order_num   BIGINT             NOT NULL,
                                                create_time DATETIME           NOT NULL,
                                                update_time DATETIME           NOT NULL
                                            )
                                            """;

    private const string FolderQueryAllSql3 =
        $"select id, name, order_num, create_time, update_time from {TABLE_NAME_FOLDER}";

    private const string FolderQueryAllSql = FolderQueryAllSql3;

    private readonly string _connectionString;

    public SqliteSnippetManage(string dbDir, string pluginVersion)
    {
        var dbPath = Path.Combine(dbDir, "snippets.db");
        _connectionString = $"Data Source={dbPath};Version=3;";
        _initCheckTable(pluginVersion);
    }

    private void _initCheckTable(string pluginVersion)
    {
        InnerLogger.Logger.Debug($"_connectionString: {_connectionString}. pluginVersion: {pluginVersion}");
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        // Folder 
        const string queryFolder = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{TABLE_NAME_FOLDER}';";
        using var folderCommand = new SQLiteCommand(queryFolder, connection);
        var folderResult = folderCommand.ExecuteScalar();
        if (folderResult == null)
        {
            // create Folder table
            using var createTableCommand = new SQLiteCommand(FolderTableDDL3, connection);
            createTableCommand.ExecuteNonQuery();
        }

        const string query = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{TABLE_NAME}';";
        using var command = new SQLiteCommand(query, connection);
        var result = command.ExecuteScalar();
        if (result == null)
        {
            // create table
            using var createTableCommand = new SQLiteCommand(TableDDL3, connection);
            createTableCommand.ExecuteNonQuery();

            // merge v2 data to current, after Table is Created
            _mergeV2DataListToV3(connection);
        }
    }

    #region 版本更新-表数据迁移

    private void _mergeV2DataListToV3(SQLiteConnection connection)
    {
        const string query = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{TABLE_NAME_V2}';";
        using var command = new SQLiteCommand(query, connection);
        var result = command.ExecuteScalar();

        var list = new List<SnippetModel>();

        if (result != null)
        {
            // read all data
            using var queryCommand = new SQLiteCommand(QueryAllSql2, connection);
            using var reader = queryCommand.ExecuteReader();
            while (reader.Read())
            {
                var key = reader.GetString(0);
                var value = reader.GetString(1);
                var score = reader.GetInt32(2);
                var updateTime = reader.IsDBNull(3) ? DateTime.Now : reader.GetDateTime(3);
                list.Add(new SnippetModel
                {
                    Id = IdHelper.NewId(),
                    Name = key,
                    Value = value,
                    OrderNum = score,
                    CreateTime = updateTime,
                    UpdateTime = updateTime
                });
            }
        }

        if (list.Count <= 0) return;

        InnerLogger.Logger.Info($"Upgrade v2 data to v3. merge.size = {list.Count}");

        var transaction = connection.BeginTransaction();
        foreach (var sm in list)
            _addSnippet(connection, sm);
        transaction.Commit();
    }

    private void _updateSnippetsTableByVersion(SQLiteConnection connection)
    {
        // version 3.x.x support folder

        var existFavorites = _checkTableColumnExists(connection, TABLE_NAME, "favorites");
        InnerLogger.Logger.Debug($"UpdateTableQuery: {TABLE_NAME}. favorites: {existFavorites}");
        if (!existFavorites)
        {
            //language=SQL
            const string alterSql = $"alter table {TABLE_NAME} add column favorites INTEGER not null default 0";
            _alertTable(connection, alterSql);
        }

        var existSyntax = _checkTableColumnExists(connection, TABLE_NAME, "syntax");
        InnerLogger.Logger.Debug($"UpdateTableQuery: {TABLE_NAME}. syntax: {existSyntax}");
        if (!existSyntax)
        {
            //language=SQL
            const string alterSql = $"alter table {TABLE_NAME} add column syntax varchar(100)";
            _alertTable(connection, alterSql);
        }

        var existFolder = _checkTableColumnExists(connection, TABLE_NAME, "folder_id");
        InnerLogger.Logger.Debug($"UpdateTableQuery: {TABLE_NAME}. folder_id: {existFolder}");
        if (!existFolder)
        {
            //language=SQL
            const string alterSql = $"alter table {TABLE_NAME} add column folder_id BIGINT";
            _alertTable(connection, alterSql);
        }
    }


    private bool _checkTableColumnExists(SQLiteConnection connection, string tableName, string columnName)
    {
        using var cmd = new SQLiteCommand($"PRAGMA table_info({tableName})", connection);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            // "name" ignoreCase
            if (reader["name"].ToString().Equals(columnName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private void _alertTable(SQLiteConnection connection, string sql)
    {
        using var cmd = new SQLiteCommand(sql, connection);
        cmd.ExecuteNonQuery();
    }

    #endregion

    private SnippetModel _readSnippetModel(SQLiteDataReader reader)
    {
        return new SnippetModel
        {
            Id = reader.GetInt64(0),
            Name = reader.GetString(1),
            Value = reader.GetString(2),
            OrderNum = reader.GetInt64(3),
            CreateTime = reader.GetDateTime(4),
            UpdateTime = reader.GetDateTime(5),
            Faviorites = reader.GetInt32(6),
            Syntax = reader.IsDBNull(7) ? null : reader.GetString(7),
            FolderId = reader.IsDBNull(8) ? null : reader.GetInt64(8),
            FolderName = reader.IsDBNull(9) ? null : reader.GetString(9),
        };
    }


    public SnippetModel GetByKey(string key)
    {
        const string sql = $"{QueryAllSql} where s.key = @key";
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var command = new SQLiteCommand(sql, connection);
        command.Parameters.AddWithValue("@key", key);
        using var reader = command.ExecuteReader();
        return reader.Read() ? _readSnippetModel(reader) : null;
    }

    public SnippetModel GetSnippetById(long id)
    {
        const string sql = $"{QueryAllSql} where s.id = @id";
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var command = new SQLiteCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? _readSnippetModel(reader) : null;
    }

    public List<SnippetModel> List(string name = null, string value = null, bool? favorites = null,
        long? folderId = null)
    {
        var sql = $"{QueryAllSql} where 1=1";

        if (!string.IsNullOrEmpty(name))
            sql += " and s.name like @name";

        if (!string.IsNullOrEmpty(value))
            sql += " and s.value like @value";

        if (favorites != null)
            sql += " and s.favorites = @favorites";

        if (folderId != null)
            sql += " and f.id = @folder_id";

        sql += " order by s.order_num desc";

        InnerLogger.Logger.Info($"List: {sql}");

        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var command = new SQLiteCommand(sql, connection);

        if (!string.IsNullOrEmpty(name))
            command.Parameters.AddWithValue("@name", $"%{name}%");

        if (!string.IsNullOrEmpty(value))
            command.Parameters.AddWithValue("@value", $"%{value}%");

        if (favorites != null)
            command.Parameters.AddWithValue("@favorites", Utils.BoolToInt(favorites));

        if (folderId != null)
            command.Parameters.AddWithValue("@folder_id", folderId);

        using var reader = command.ExecuteReader();
        var result = new List<SnippetModel>();
        while (reader.Read())
        {
            result.Add(_readSnippetModel(reader));
        }

        return result;
    }

    public List<SnippetModel> ListRecent(string name = null, string value = null, int limit = 20)
    {
        var sql = $"{QueryAllSql} where 1=1";

        if (!string.IsNullOrEmpty(name))
            sql += " and s.name like @name";

        if (!string.IsNullOrEmpty(value))
            sql += " and s.value like @value";

        sql += $" order by s.update_time desc limit {limit}";

        InnerLogger.Logger.Info($"List: {sql}");

        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var command = new SQLiteCommand(sql, connection);

        if (!string.IsNullOrEmpty(name))
            command.Parameters.AddWithValue("@name", $"%{name}%");

        if (!string.IsNullOrEmpty(value))
            command.Parameters.AddWithValue("@value", $"%{value}%");

        using var reader = command.ExecuteReader();
        var result = new List<SnippetModel>();
        while (reader.Read())
        {
            result.Add(_readSnippetModel(reader));
        }

        return result;
    }

    public List<SnippetModel> ListNoFolder(string name = null, string value = null)
    {
        var sql = $"{QueryAllSql} where 1=1";

        if (!string.IsNullOrEmpty(name))
            sql += " and s.name like @name";

        if (!string.IsNullOrEmpty(value))
            sql += " and s.value like @value";

        sql += " and f.id is null order by s.order_num desc";

        InnerLogger.Logger.Debug($"ListNoFolder: {sql}");

        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var command = new SQLiteCommand(sql, connection);

        if (!string.IsNullOrEmpty(name))
            command.Parameters.AddWithValue("@name", $"%{name}%");

        if (!string.IsNullOrEmpty(value))
            command.Parameters.AddWithValue("@value", $"%{value}%");

        using var reader = command.ExecuteReader();
        var result = new List<SnippetModel>();
        while (reader.Read())
            result.Add(_readSnippetModel(reader));
        return result;
    }

    public bool Add(string name, string value, string syntax = null, bool? favorites = null,
        long? folderId = null)
    {
        var now = DateTimeUtil.TrimMilliseconds(DateTime.Now);
        var id = IdHelper.NewId();
        var sm = new SnippetModel
        {
            Id = id,
            Name = name,
            Value = value,
            OrderNum = id,
            CreateTime = now,
            UpdateTime = now,
            Faviorites = Utils.BoolToInt(favorites),
            Syntax = syntax,
            FolderId = folderId
        };
        return Add(sm);
    }

    public bool Add(SnippetModel sm)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        return _addSnippet(connection, sm) > 0;
    }

    private int _addSnippet(SQLiteConnection connection, SnippetModel sm)
    {
        const string sql =
            $"insert into {TABLE_NAME} (id, name, value, order_num, create_time, update_time, favorites, syntax, folder_id) values (@id, @name, @value, @order_num, @create_time, @update_time, @favorites, @syntax, @folder_id)";

        using var command = new SQLiteCommand(sql, connection);
        command.Parameters.AddWithValue("@id", sm.Id);
        command.Parameters.AddWithValue("@name", sm.Name);
        command.Parameters.AddWithValue("@value", sm.Value);
        command.Parameters.AddWithValue("@order_num", sm.OrderNum);
        command.Parameters.AddWithValue("@create_time", sm.CreateTime);
        command.Parameters.AddWithValue("@update_time", sm.UpdateTime);
        command.Parameters.AddWithValue("@favorites", sm.Faviorites);
        command.Parameters.AddWithValue("@syntax", sm.Syntax);
        command.Parameters.AddWithValue("@folder_id", sm.FolderId);
        return command.ExecuteNonQuery();
    }

    public bool RemoveSnippetById(long id)
    {
        const string sql = $"delete from {TABLE_NAME} where id = @id";
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var command = new SQLiteCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        return command.ExecuteNonQuery() > 0;
    }

    // public bool RemoveByKey(string key)
    // {
    //     const string sql = $"delete from {TABLE_NAME} where key = @key";
    //     using var connection = new SQLiteConnection(_connectionString);
    //     connection.Open();
    //     using var command = new SQLiteCommand(sql, connection);
    //     command.Parameters.AddWithValue("@key", key);
    //     return command.ExecuteNonQuery() > 0;
    // }

    public bool UpdateSnippetById(long id, string name = null, string value = null, string syntax = null,
        long? orderNum = null,
        long? folderId = null,
        bool? favorites = null)
    {
        if (string.IsNullOrEmpty(name) &&
            string.IsNullOrEmpty(value) &&
            string.IsNullOrEmpty(syntax) &&
            orderNum == null &&
            folderId == null &&
            favorites == null)
        {
            return false;
        }

        var updateSqls = new List<string>();
        if (!string.IsNullOrEmpty(name))
            updateSqls.Add("name = @name");
        if (!string.IsNullOrEmpty(value))
            updateSqls.Add("value = @value");
        if (!string.IsNullOrEmpty(syntax))
            updateSqls.Add("syntax = @syntax");
        if (orderNum != null)
            updateSqls.Add("order_num = @order_num");
        if (folderId != null)
            updateSqls.Add("folder_id = @folder_id");
        if (favorites != null)
            updateSqls.Add("favorites = @favorites");
        updateSqls.Add("update_time = @update_time");

        var updateSql = string.Join(", ", updateSqls);
        var sql = $"update {TABLE_NAME} set {updateSql} where id = @id";

        InnerLogger.Logger.Info($"UpdateByKey: {sql}");

        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var command = new SQLiteCommand(sql, connection);

        if (!string.IsNullOrEmpty(name))
            command.Parameters.AddWithValue("@name", name);
        if (!string.IsNullOrEmpty(value))
            command.Parameters.AddWithValue("@value", value);
        if (!string.IsNullOrEmpty(syntax))
            command.Parameters.AddWithValue("@syntax", syntax);
        if (orderNum != null)
            command.Parameters.AddWithValue("@order_num", orderNum);
        if (folderId != null)
            command.Parameters.AddWithValue("@folder_id", folderId);
        if (favorites != null)
            command.Parameters.AddWithValue("@favorites", Utils.BoolToInt(favorites));

        command.Parameters.AddWithValue("@update_time", DateTimeUtil.TrimMilliseconds(DateTime.Now));
        command.Parameters.AddWithValue("@id", id);

        return command.ExecuteNonQuery() > 0;
    }

    public bool UpdateByKey(string key, string value = null, long? folderId = null, int? score = null)
    {
        if (value == null && folderId == null && score == null) return false;

        var updateSqls = new List<string>();
        if (value != null)
            updateSqls.Add("value = @value");
        if (folderId != null)
            updateSqls.Add("folder_id = @folderId");
        if (score != null)
            updateSqls.Add("score = @score");

        updateSqls.Add("update_time = @update_time");

        var updateSql = string.Join(", ", updateSqls);
        var sql = $"update {TABLE_NAME} set {updateSql} where key = @key";

        InnerLogger.Logger.Info($"UpdateByKey: {sql}");

        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var command = new SQLiteCommand(sql, connection);

        if (value != null)
            command.Parameters.AddWithValue("@value", value);
        if (folderId != null)
            command.Parameters.AddWithValue("@folder_id", folderId);
        if (score != null)
            command.Parameters.AddWithValue("@score", score);

        command.Parameters.AddWithValue("@update_time", DateTimeUtil.TrimMilliseconds(DateTime.Now));

        command.Parameters.AddWithValue("@key", key);

        return command.ExecuteNonQuery() > 0;
    }

    public void Clear()
    {
        const string sql = $"delete from {TABLE_NAME}";
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var command = new SQLiteCommand(sql, connection);
        command.ExecuteNonQuery();
    }

    public void ResetAllScore()
    {
        const string sql = $"update {TABLE_NAME} set score = 0";
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var command = new SQLiteCommand(sql, connection);
        command.ExecuteNonQuery();
    }

    public FolderModel GetFolder(string name)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        return _getFolder(connection, name);
    }

    private FolderModel _getFolder(SQLiteConnection connection, string name)
    {
        const string sql = $"{FolderQueryAllSql} where name = @name";
        using var command = new SQLiteCommand(sql, connection);
        command.Parameters.AddWithValue("@name", name);
        using var reader = command.ExecuteReader();

        return reader.Read() ? _readFolderModel(reader) : null;
    }

    private FolderModel _createFolderModel(string name)
    {
        var id = IdHelper.NewId();
        var now = DateTimeUtil.TrimMilliseconds(DateTime.Now);
        return new FolderModel
        {
            Id = id,
            Name = name,
            OrderNum = id,
            CreateTime = now,
            UpdateTime = now,
        };
    }

    public bool AddFolder(string name)
    {
        var fm = _createFolderModel(name);
        return _addFolder(fm);
    }

    private bool _addFolder(FolderModel fm)
    {
        const string sql =
            $"replace into {TABLE_NAME_FOLDER} (id, name, order_num, create_time, update_time) values (@id, @name, @order_num, @create_time, @update_time)";
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        var dbFm = _getFolder(connection, fm.Name);
        if (dbFm != null)
        {
            InnerLogger.Logger.Warn($"Create folder failed. cause folder name is exists : {dbFm.Name}");
            return false;
        }

        using var command = new SQLiteCommand(sql, connection);
        command.Parameters.AddWithValue("@id", fm.Id);
        command.Parameters.AddWithValue("@name", fm.Name);
        command.Parameters.AddWithValue("@order_num", fm.OrderNum);
        command.Parameters.AddWithValue("@create_time", fm.CreateTime);
        command.Parameters.AddWithValue("@update_time", fm.UpdateTime);
        return command.ExecuteNonQuery() > 0;
    }

    public bool RemoveFolder(string name)
    {
        const string sql = $"delete from {TABLE_NAME_FOLDER} where name = @name";
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var command = new SQLiteCommand(sql, connection);
        command.Parameters.AddWithValue("@name", name);
        return command.ExecuteNonQuery() > 0;
    }

    public bool RemoveFolderById(long folderId)
    {
        const string sql = $"delete from {TABLE_NAME_FOLDER} where id = @id";
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var command = new SQLiteCommand(sql, connection);
        command.Parameters.AddWithValue("@id", folderId);
        return command.ExecuteNonQuery() > 0;
    }

    public bool UpdateFolderById(long id, string newName)
    {
        const string sql =
            $"update {TABLE_NAME_FOLDER} set name = @new_name, update_time = @update_time where id = @id";
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var command = new SQLiteCommand(sql, connection);
        command.Parameters.AddWithValue("@new_name", newName);
        command.Parameters.AddWithValue("@update_time", DateTimeUtil.TrimMilliseconds(DateTime.Now));
        command.Parameters.AddWithValue("@id", id);
        return command.ExecuteNonQuery() > 0;
    }

    public List<FolderModel> ListFolders(string name = null)
    {
        var isEmptyQuery = string.IsNullOrEmpty(name);
        var sql = isEmptyQuery ? $"{FolderQueryAllSql}" : $"{FolderQueryAllSql} where name like @name";

        sql = $"{sql} order by order_num";

        InnerLogger.Logger.Debug($"ListFolders. sql = {sql}, name = {name}");

        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        using var command = new SQLiteCommand(sql, connection);
        if (!isEmptyQuery)
            command.Parameters.AddWithValue("@name", $"%{name}%");

        var folders = new List<FolderModel>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
            folders.Add(_readFolderModel(reader));
        return folders;
    }

    public void CleanFolders()
    {
        const string sql = $"delete from {TABLE_NAME_FOLDER}";
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var command = new SQLiteCommand(sql, connection);
        command.ExecuteNonQuery();
    }


    private FolderModel _readFolderModel(SQLiteDataReader reader)
    {
        return new FolderModel
        {
            Id = reader.GetInt64(0),
            Name = reader.GetString(1),
            OrderNum = reader.GetInt64(2),
            CreateTime = reader.GetDateTime(3),
            UpdateTime = reader.GetDateTime(4)
        };
    }
}