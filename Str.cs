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
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            cnnct.Open();
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            var val = cmnd.ExecuteScalar();
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
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            await cnnct.OpenAsync();
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            var val = await cmnd.ExecuteScalarAsync();
            return val is DBNull ? null : val?.ToString();
        }
    }
}