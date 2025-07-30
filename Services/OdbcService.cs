using System.Collections.Generic;
using System.Data.Odbc;

namespace GeminiProvideXReportGenerator.Services
{
    public class OdbcService
    {
        private readonly string _connectionString;

        public OdbcService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<string> GetTableNames()
        {
            var tableNames = new List<string>();
            using (var connection = new OdbcConnection(_connectionString))
            {
                connection.Open();
                var schema = connection.GetSchema("Tables");
                foreach (System.Data.DataRow row in schema.Rows)
                {
                    tableNames.Add(row["TABLE_NAME"].ToString() ?? string.Empty);
                }
            }
            return tableNames;
        }

        public System.Data.DataTable GetTop1000FromProvideX(string tableName)
        {
            using (var connection = new OdbcConnection(_connectionString))
            {
                connection.Open();
                var command = new OdbcCommand($"SELECT TOP 1000 * FROM {tableName}", connection);
                using (var reader = command.ExecuteReader())
                {
                    var dataTable = new System.Data.DataTable();
                    dataTable.Load(reader);
                    return dataTable;
                }
            }
        }

        public long GetTableRowCount(string tableName)
        {
            using (var connection = new OdbcConnection(_connectionString))
            {
                connection.Open();
                var command = new OdbcCommand($"SELECT COUNT(*) FROM {tableName}", connection);
                return Convert.ToInt64(command.ExecuteScalar());
            }
        }
    }
}
