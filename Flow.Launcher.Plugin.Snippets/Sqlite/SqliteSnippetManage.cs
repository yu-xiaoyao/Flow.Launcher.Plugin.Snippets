using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using Flow.Launcher.Plugin.Snippets.Model;
using Flow.Launcher.Plugin.Snippets.Util;
using static Flow.Launcher.Plugin.Snippets.Util.DateTimeUtil;

namespace Flow.Launcher.Plugin.Snippets.Sqlite;

public class SqliteSnippetManage : SnippetManage
{
    private const string TABLE_NAME = "snippets";
    private const string TABLE_NAME_FOLDER = "folder";

    //language=SQL
    private const string _tableDDL = $"""
                                      create table {TABLE_NAME}
                                      (
                                          key         varchar(200) not null primary key,
                                          value       text         not null,
                                          score       BIGINT       not null default 0,
                                          update_time datetime     not null DEFAULT CURRENT_TIMESTAMP,
                                          create_time datetime     not null DEFAULT CURRENT_TIMESTAMP,
                                          favorites   INTEGER      not  null default 0,
                                          syntax      varchar(100),
                                          folder_id   BIGINT
                                      )
                                      """;

    // private const string QueryAllSql = $"select key, value, score, create_time, update_time from {TABLE_NAME}";
    // private const string QueryAllSql = $"select key, value, score, create_time, update_time from {TABLE_NAME}";
    private const string QueryAllSql =
        $"SELECT s.key, s.value, s.score, s.create_time, s.update_time, s.favorites, s.syntax, f.id as folder_id, f.name as folder_name FROM {TABLE_NAME} s left join {TABLE_NAME_FOLDER} f on s.folder_id = f.id";

    //language=SQL
    private const string FolderTableDDL = $"""
                                           create table {TABLE_NAME_FOLDER}
                                           (
                                               id          BIGINT             NOT NULL PRIMARY KEY,
                                               name        varchar(200)       NOT NULL UNIQUE,
                                               order_num   BIGINT             NOT NULL,
                                               create_time DATETIME           NOT NULL,
                                               update_time DATETIME           NOT NULL
                                           )
                                           """;

    private const string FolderQueryAllSql =
        $"select id, name, order_num, create_time, update_time from {TABLE_NAME_FOLDER}";

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

        var createSnippetsTable = false;

        const string query = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{TABLE_NAME}';";
        using var command = new SQLiteCommand(query, connection);
        var result = command.ExecuteScalar();
        if (result == null)
        {
            // create table
            using var createTableCommand = new SQLiteCommand(_tableDDL, connection);
            createTableCommand.ExecuteNonQuery();

            createSnippetsTable = true;
        }


        // Folder 
        const string queryFolder = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{TABLE_NAME_FOLDER}';";
        using var folderCommand = new SQLiteCommand(queryFolder, connection);
        var folderResult = folderCommand.ExecuteScalar();
        if (folderResult == null)
        {
            // create Folder table
            using var createTableCommand = new SQLiteCommand(FolderTableDDL, connection);
            createTableCommand.ExecuteNonQuery();

            if (!createSnippetsTable)
            {
                _updateSnippetsTableByVersion(connection);
            }
        }
    }

    #region 版本更新-表修改

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
            Key = reader.GetString(0),
            Value = reader.GetString(1),
            Score = reader.GetInt32(2),
            CreateTime = reader.GetDateTime(3),
            UpdateTime = reader.GetDateTime(4),
            Faviorites = reader.GetInt32(5),
            Syntax = reader.IsDBNull(6) ? null : reader.GetString(6),
            FolderId = reader.IsDBNull(7) ? null : reader.GetInt64(7),
            FolderName = reader.IsDBNull(8) ? null : reader.GetString(8),
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

    public List<SnippetModel> List(string key = null, string value = null, bool? favorites = null,
        long? folderId = null)
    {
        var sql = $"{QueryAllSql} where 1=1";

        if (!string.IsNullOrEmpty(key))
            sql += " and s.key like @key";

        if (!string.IsNullOrEmpty(value))
            sql += " and s.value like @value";

        if (favorites != null)
            sql += " and s.favorites = @favorites";

        if (folderId != null)
            sql += " and f.id = @folder_id";

        sql += " order by s.score desc";

        InnerLogger.Logger.Info($"List: {sql}");

        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var command = new SQLiteCommand(sql, connection);

        if (!string.IsNullOrEmpty(key))
            command.Parameters.AddWithValue("@key", $"%{key}%");

        if (!string.IsNullOrEmpty(value))
            command.Parameters.AddWithValue("@value", $"%{value}%");

        if (favorites != null)
            command.Parameters.AddWithValue("@favorites", boolToInt((bool)favorites));

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

    public List<SnippetModel> ListRecent(string key = null, string value = null, int limit = 20)
    {
        var sql = $"{QueryAllSql} where 1=1";

        if (!string.IsNullOrEmpty(key))
            sql += " and s.key like @key";

        if (!string.IsNullOrEmpty(value))
            sql += " and s.value like @value";

        sql += $" order by s.update_time desc limit {limit}";

        InnerLogger.Logger.Info($"List: {sql}");

        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var command = new SQLiteCommand(sql, connection);

        if (!string.IsNullOrEmpty(key))
            command.Parameters.AddWithValue("@key", $"%{key}%");

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

    public List<SnippetModel> ListNoFolder(string key = null, string value = null)
    {
        var sql = $"{QueryAllSql} where 1=1";

        if (!string.IsNullOrEmpty(key))
            sql += " and s.key like @key";

        if (!string.IsNullOrEmpty(value))
            sql += " and s.value like @value";

        sql += " and f.id is null order by s.score desc";

        InnerLogger.Logger.Debug($"ListNoFolder: {sql}");

        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var command = new SQLiteCommand(sql, connection);

        if (!string.IsNullOrEmpty(key))
            command.Parameters.AddWithValue("@key", $"%{key}%");

        if (!string.IsNullOrEmpty(value))
            command.Parameters.AddWithValue("@value", $"%{value}%");

        using var reader = command.ExecuteReader();
        var result = new List<SnippetModel>();
        while (reader.Read())
            result.Add(_readSnippetModel(reader));
        return result;
    }

    public bool Add(string key, string value, long? folderId = null, int score = 0)
    {
        var now = TrimMilliseconds(DateTime.Now);
        var sm = new SnippetModel
        {
            Key = key,
            Value = value,
            Score = score,
            CreateTime = now,
            UpdateTime = now,
            FolderId = folderId
        };
        return _add(sm);
    }

    public bool Add(SnippetModel sm)
    {
        return _add(sm);
    }


    private bool _add(SnippetModel sm)
    {
        const string sql =
            $"replace into {TABLE_NAME} (key, value, score, update_time, create_time, favorites, syntax, folder_id) values (@key, @value, @score, @update_time, @create_time, @favorites, @syntax, @folder_id)";
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var command = new SQLiteCommand(sql, connection);
        command.Parameters.AddWithValue("@key", sm.Key);
        command.Parameters.AddWithValue("@value", sm.Value);
        command.Parameters.AddWithValue("@score", sm.Score);
        command.Parameters.AddWithValue("@update_time", sm.UpdateTime);
        command.Parameters.AddWithValue("@create_time", sm.CreateTime);
        command.Parameters.AddWithValue("@favorites", sm.Faviorites);
        command.Parameters.AddWithValue("@syntax", sm.Syntax);
        command.Parameters.AddWithValue("@folder_id", sm.FolderId);
        return command.ExecuteNonQuery() > 0;
    }

    public bool RemoveByKey(string key)
    {
        const string sql = $"delete from {TABLE_NAME} where key = @key";
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var command = new SQLiteCommand(sql, connection);
        command.Parameters.AddWithValue("@key", key);
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

        command.Parameters.AddWithValue("@update_time", TrimMilliseconds(DateTime.Now));

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
        var now = TrimMilliseconds(DateTime.Now);
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
        command.Parameters.AddWithValue("@update_time", TrimMilliseconds(DateTime.Now));
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

    private int boolToInt(bool value)
    {
        return value ? 1 : 0;
    }

    private bool intToBool(int value)
    {
        return value != 0;
    }
}