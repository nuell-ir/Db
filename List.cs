using System.Data;
using Microsoft.Data.SqlClient;

namespace nuell.Sync
{
    public static partial class Db
    {
        public static List<T> List<T>(string query, params (string name, object value)[] parameters) where T : struct
            => List<T>(query, false, Data.SqlParams(parameters));

        public static List<T> List<T>(string query, bool isStoredProc, params (string name, object value)[] parameters) where T : struct
            => List<T>(query, isStoredProc, Data.SqlParams(parameters));

        public static List<T> List<T>(string query, bool isStoredProc = false) where T : struct
            => List<T>(query, isStoredProc, Data.NoParams);

        public static List<T> List<T>(string query, bool isStoredProc, params SqlParameter[] parameters) where T : struct
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            cnnct.Open();
            using var reader = cmnd.ExecuteReader();
            if (!reader.HasRows)
                return null;

            var list = new List<T>();
            while (reader.Read())
                list.Add((T)Convert.ChangeType(reader[0], typeof(T)));
            return list;
        }
    }
}

namespace nuell.Async
{
    public static partial class Db
    {
        public static Task<List<T>> List<T>(string query, params (string name, object value)[] parameters) where T : struct
            => List<T>(query, false, Data.SqlParams(parameters));

        public static Task<List<T>> List<T>(string query, bool isStoredProc, params (string name, object value)[] parameters) where T : struct
            => List<T>(query, isStoredProc, Data.SqlParams(parameters));

        public static Task<List<T>> List<T>(string query, bool isStoredProc = false) where T : struct
            => List<T>(query, isStoredProc, Data.NoParams);

        public static async Task<List<T>> List<T>(string query, bool isStoredProc, params SqlParameter[] parameters) where T : struct
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            await cnnct.OpenAsync();
            using var reader = cmnd.ExecuteReader();
            if (!reader.HasRows)
                return null;
            var list = new List<T>();
            while (await reader.ReadAsync())
                list.Add((T)Convert.ChangeType(reader[0], typeof(T)));
            return list;
        }
    }
}