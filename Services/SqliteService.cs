using Microsoft.Data.Sqlite;
using System.Data;
using System.Data.Odbc;

namespace GeminiProvideXReportGenerator.Services
{
    public class SqliteService
    {
        private readonly SqliteConnection _connection;

        public SqliteService()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
        }

        public void MirrorTable(OdbcConnection odbcConnection, string tableName)
        {
            // Drop table if it exists to allow re-mirroring
            var dropCommand = new SqliteCommand($"DROP TABLE IF EXISTS [{tableName}]", _connection);
            dropCommand.ExecuteNonQuery();

            DataTable? schemaTable = null;
            
            // Get table schema from ProvideX
            using (var schemaCommand = new OdbcCommand($"SELECT * FROM {tableName}", odbcConnection))
            using (var schemaReader = schemaCommand.ExecuteReader(CommandBehavior.SchemaOnly))
            {
                schemaTable = schemaReader.GetSchemaTable();
            }

            if (schemaTable != null)
            {
                // Debug: Check what columns are available in the schema table
                System.Diagnostics.Debug.WriteLine("Available schema columns:");
                foreach (DataColumn col in schemaTable.Columns)
                {
                    System.Diagnostics.Debug.WriteLine($"  {col.ColumnName}");
                }

                // Create table in SQLite
                var createTableSql = $"CREATE TABLE [{tableName}] (";
                foreach (DataRow row in schemaTable.Rows)
                {
                    var columnName = row["ColumnName"].ToString() ?? string.Empty;
                    var dataType = GetDataType(row);
                    createTableSql += $"[{columnName}] {GetSqliteType(dataType)}, ";
                }
                createTableSql = createTableSql.TrimEnd(',', ' ') + ")";
                
                var createTableCommand = new SqliteCommand(createTableSql, _connection);
                createTableCommand.ExecuteNonQuery();
            }

            // Copy data from ProvideX to SQLite with a fresh command
            using (var dataCommand = new OdbcCommand($"SELECT * FROM {tableName}", odbcConnection))
            using (var dataReader = dataCommand.ExecuteReader())
            using (var transaction = _connection.BeginTransaction())
            {
                var insertCommand = new SqliteCommand();
                insertCommand.Connection = _connection;
                insertCommand.Transaction = transaction;
                
                while (dataReader.Read())
                {
                    var insertSql = $"INSERT INTO [{tableName}] VALUES (";
                    for (int i = 0; i < dataReader.FieldCount; i++)
                    {
                        insertSql += "@p" + i + ", ";
                        insertCommand.Parameters.AddWithValue("@p" + i, dataReader.GetValue(i));
                    }
                    insertSql = insertSql.TrimEnd(',', ' ') + ")";
                    insertCommand.CommandText = insertSql;
                    insertCommand.ExecuteNonQuery();
                    insertCommand.Parameters.Clear();
                }
                transaction.Commit();
            }
        }

        public DataTable ExecuteQuery(string query)
        {
            var command = new SqliteCommand(query, _connection);
            using (var reader = command.ExecuteReader())
            {
                var dataTable = new DataTable();
                dataTable.Load(reader);
                return dataTable;
            }
        }

        public bool TableExists(string tableName)
        {
            var command = new SqliteCommand($"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}'", _connection);
            return command.ExecuteScalar() != null;
        }

        public long GetTableRowCount(string tableName)
        {
            if (!TableExists(tableName)) return 0;
            
            var command = new SqliteCommand($"SELECT COUNT(*) FROM [{tableName}]", _connection);
            return Convert.ToInt64(command.ExecuteScalar());
        }

        public DataTable GetTop1000FromSqlite(string tableName)
        {
            if (!TableExists(tableName)) return new DataTable();
            
            return ExecuteQuery($"SELECT * FROM [{tableName}] LIMIT 1000");
        }

        public string GetTableSchema(string tableName)
        {
            if (!TableExists(tableName)) return $"Table '{tableName}' does not exist in SQLite.";
            
            try
            {
                var schemaQuery = $"PRAGMA table_info([{tableName}])";
                var schemaResult = ExecuteQuery(schemaQuery);
                
                var schema = $"Schema for table '{tableName}':\n";
                foreach (System.Data.DataRow row in schemaResult.Rows)
                {
                    var columnName = row["name"].ToString();
                    var dataType = row["type"].ToString();
                    var notNull = Convert.ToBoolean(row["notnull"]) ? "NOT NULL" : "NULL";
                    var pk = Convert.ToBoolean(row["pk"]) ? " (PRIMARY KEY)" : "";
                    
                    schema += $"  {columnName}: {dataType} {notNull}{pk}\n";
                }
                
                return schema;
            }
            catch (Exception ex)
            {
                return $"Error getting schema for '{tableName}': {ex.Message}";
            }
        }

        public string GetSampleData(string tableName, int maxRows = 5)
        {
            if (!TableExists(tableName)) return $"Table '{tableName}' does not exist in SQLite.";
            
            try
            {
                var sampleResult = ExecuteQuery($"SELECT * FROM [{tableName}] LIMIT {maxRows}");
                
                if (sampleResult.Rows.Count == 0)
                {
                    return $"Table '{tableName}' exists but contains no data.";
                }
                
                var sample = $"Sample data from '{tableName}' ({sampleResult.Rows.Count} rows shown):\n";
                
                // Add column headers
                var columnNames = sampleResult.Columns.Cast<System.Data.DataColumn>().Select(c => c.ColumnName).ToArray();
                sample += string.Join(" | ", columnNames) + "\n";
                sample += new string('-', columnNames.Sum(c => c.Length) + (columnNames.Length - 1) * 3) + "\n";
                
                // Add data rows
                foreach (System.Data.DataRow row in sampleResult.Rows)
                {
                    var values = row.ItemArray.Select(v => v?.ToString()?.Trim() ?? "NULL").ToArray();
                    sample += string.Join(" | ", values) + "\n";
                }
                
                return sample;
            }
            catch (Exception ex)
            {
                return $"Error getting sample data for '{tableName}': {ex.Message}";
            }
        }

        public string ExploreTable(string tableName)
        {
            if (!TableExists(tableName)) return $"Table '{tableName}' does not exist in SQLite.";
            
            var exploration = $"=== TABLE EXPLORATION: {tableName} ===\n\n";
            exploration += GetTableSchema(tableName) + "\n";
            exploration += GetSampleData(tableName, 10) + "\n";
            exploration += $"Total rows: {GetTableRowCount(tableName)}\n";
            
            return exploration;
        }

        private string GetDataType(DataRow schemaRow)
        {
            // Try different possible column names for data type
            if (schemaRow.Table.Columns.Contains("DataTypeName"))
                return schemaRow["DataTypeName"].ToString() ?? "TEXT";
            
            if (schemaRow.Table.Columns.Contains("DataType"))
            {
                var dataType = schemaRow["DataType"];
                if (dataType is Type type)
                    return type.Name;
                return dataType.ToString() ?? "TEXT";
            }
            
            if (schemaRow.Table.Columns.Contains("ProviderType"))
                return schemaRow["ProviderType"].ToString() ?? "TEXT";
            
            // Fallback - use .NET type from the column if available
            if (schemaRow.Table.Columns.Contains("DataType"))
            {
                var type = schemaRow["DataType"] as Type;
                if (type != null)
                    return type.Name;
            }
            
            return "TEXT"; // Default fallback
        }

        private string GetSqliteType(string odbcType)
        {
            switch (odbcType.ToUpper())
            {
                case "VARCHAR":
                case "CHAR":
                case "TEXT":
                case "STRING":
                    return "TEXT";
                case "INT":
                case "INTEGER":
                case "INT32":
                case "INT64":
                    return "INTEGER";
                case "DECIMAL":
                case "NUMERIC":
                case "DOUBLE":
                case "SINGLE":
                case "FLOAT":
                    return "REAL";
                case "DATETIME":
                case "DATE":
                    return "TEXT";
                default:
                    return "TEXT";
            }
        }
    }
}
