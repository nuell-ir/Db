using System.Data;
using Microsoft.Data.SqlClient;

namespace nuell.Sync
{
    public static partial class Db
    {
        public static object[] Values(string query, params (string name, object value)[] parameters)
            => Values(query, false, Data.SqlParams(parameters));

        public static object[] Values(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => Values(query, isStoredProc, Data.SqlParams(parameters));

        public static object[] Values(string query, bool isStoredProc = false)
            => Values(query, isStoredProc, Data.NoParams);

        public static object[] Values(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var connection = new SqlConnection(Data.ConnectionString);
            using var cmd = new SqlCommand(query, connection);
            if (isStoredProc)
                cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddRange(parameters);
            connection.Open();
            using var reader = cmd.ExecuteReader();
            var results = new List<object>();
            AddValues();
            while (reader.NextResult())
                AddValues();
            return [.. results];

            void AddValues()
            {
                var values = new object[reader.FieldCount];
                if (reader.Read())
                    reader.GetValues(values);
                results.AddRange(values);
            }
        }
    }
}

namespace nuell.Async
{
    public static partial class Db
    {
        public static Task<object[]> Values(string query, params (string name, object value)[] parameters)
            => Values(query, false, Data.SqlParams(parameters));

        public static Task<object[]> Values(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => Values(query, isStoredProc, Data.SqlParams(parameters));

        public static Task<object[]> Values(string query, bool isStoredProc = false)
            => Values(query, isStoredProc, Data.NoParams);

        public static async Task<object[]> Values(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var connection = new SqlConnection(Data.ConnectionString);
            using var cmd = new SqlCommand(query, connection);
            if (isStoredProc)
                cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddRange(parameters);
            await connection.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            var results = new List<object>();
            await AddValues();
            while (await reader.NextResultAsync())
                await AddValues();
            return [.. results];

            async Task AddValues()
            {
                var values = new object[reader.FieldCount];
                if (await reader.ReadAsync())
                    reader.GetValues(values);
                results.AddRange(values);
            }
        }
    }
}