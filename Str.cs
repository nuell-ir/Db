using System.Data;
using Microsoft.Data.SqlClient;

namespace nuell.Sync
{
    public static partial class Db
    {
        public static string Str(string query, params (string name, object value)[] parameters)
            => Str(query, false, Data.SqlParams(parameters));

        public static string Str(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => Str(query, isStoredProc, Data.SqlParams(parameters));

        public static string Str(string query, bool isStoredProc = false)
            => Str(query, isStoredProc, Data.NoParams);

        public static string Str(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var connection = new SqlConnection(Data.ConnectionString);
            using var cmd = new SqlCommand(query, connection);
            connection.Open();
            if (isStoredProc)
                cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddRange(parameters);
            var val = cmd.ExecuteScalar();
            return val is DBNull ? null : val?.ToString();
        }
    }
}

namespace nuell.Async
{
    public static partial class Db
    {
        public static Task<string> Str(string query, params (string name, object value)[] parameters)
            => Str(query, false, Data.SqlParams(parameters));

        public static Task<string> Str(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => Str(query, isStoredProc, Data.SqlParams(parameters));

        public static Task<string> Str(string query, bool isStoredProc = false)
            => Str(query, isStoredProc, Data.NoParams);

        public static async Task<string> Str(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var connection = new SqlConnection(Data.ConnectionString);
            using var cmd = new SqlCommand(query, connection);
            await connection.OpenAsync();
            if (isStoredProc)
                cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddRange(parameters);
            var val = await cmd.ExecuteScalarAsync();
            return val is DBNull ? null : val?.ToString();
        }
    }
}