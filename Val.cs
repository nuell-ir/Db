using System.Data;
using Microsoft.Data.SqlClient;

namespace nuell.Sync
{
    public static partial class Db
    {
        public static T Val<T>(string query, params (string name, object value)[] parameters) where T : struct
            => Val<T>(query, false, Data.SqlParams(parameters));

        public static T Val<T>(string query, bool isStoredProc, params (string name, object value)[] parameters) where T : struct
            => Val<T>(query, isStoredProc, Data.SqlParams(parameters));

        public static T Val<T>(string query, bool isStoredProc = false) where T : struct
            => Val<T>(query, isStoredProc, Data.NoParams);

        public static T Val<T>(string query, bool isStoredProc, params SqlParameter[] parameters) where T : struct
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            cnnct.Open();
            var val = cmnd.ExecuteScalar();
            return val is null || val is DBNull ? default : (T)Convert.ChangeType(val, typeof(T));
        }
    }
}

namespace nuell.Async
{
    public static partial class Db
    {
        public static Task<T> Val<T>(string query, params (string name, object value)[] parameters) where T : struct
            => Val<T>(query, false, Data.SqlParams(parameters));

        public static Task<T> Val<T>(string query, bool isStoredProc, params (string name, object value)[] parameters) where T : struct
            => Val<T>(query, isStoredProc, Data.SqlParams(parameters));

        public static Task<T> Val<T>(string query, bool isStoredProc = false) where T : struct
            => Val<T>(query, isStoredProc, Data.NoParams);

        public async static Task<T> Val<T>(string query, bool isStoredProc, params SqlParameter[] parameters) where T : struct
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            await cnnct.OpenAsync();
            var val = await cmnd.ExecuteScalarAsync();
            return val is null || val is DBNull ? default : (T)Convert.ChangeType(val, typeof(T));
        }
    }
}