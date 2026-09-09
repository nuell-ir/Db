using System.Data;
using Microsoft.Data.SqlClient;

namespace nuel.Sync
{
    public static partial class Db
    {
        public static List<T> ObjList<T>(string query, params (string name, object value)[] parameters) where T : new()
            => ObjList<T>(query, false, Data.SqlParams(parameters));

        public static List<T> ObjList<T>(string query, bool isStoredProc, params (string name, object value)[] parameters) where T : new()
            => ObjList<T>(query, isStoredProc, Data.SqlParams(parameters));

        public static List<T> ObjList<T>(string query, bool isStoredProc = false) where T : new()
            => ObjList<T>(query, isStoredProc, Data.NoParams);

        public static List<T> ObjList<T>(string query, bool isStoredProc, params SqlParameter[] parameters) where T : new()
        {
            using var connection = new SqlConnection(Data.ConnectionString);
            using var cmd = new SqlCommand(query, connection);
            if (isStoredProc)
                cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddRange(parameters);
            connection.Open();
            using var reader = cmd.ExecuteReader();
            if (!reader.HasRows)
                return null;

            var list = new List<T>();
            var props = typeof(T).GetProperties().ToDictionary(p => p.Name, p => p);
            while (reader.Read())
                list.Add(reader.GetObject<T>(props));
            return list;
        }
    }
}

namespace nuel.Async
{
    public static partial class Db
    {
        public static Task<List<T>> ObjList<T>(string query, params (string name, object value)[] parameters) where T : new()
            => ObjList<T>(query, false, Data.SqlParams(parameters));

        public static Task<List<T>> ObjList<T>(string query, bool isStoredProc, params (string name, object value)[] parameters) where T : new()
            => ObjList<T>(query, isStoredProc, Data.SqlParams(parameters));

        public static Task<List<T>> ObjList<T>(string query, bool isStoredProc = false) where T : new()
            => ObjList<T>(query, isStoredProc, Data.NoParams);

        public static async Task<List<T>> ObjList<T>(string query, bool isStoredProc, params SqlParameter[] parameters) where T : new()
        {
            using var connection = new SqlConnection(Data.ConnectionString);
            using var cmd = new SqlCommand(query, connection);
            if (isStoredProc)
                cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddRange(parameters);
            await connection.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
                return null;

            var list = new List<T>();
            var props = typeof(T).GetProperties().ToDictionary(p => p.Name, p => p);
            while (await reader.ReadAsync())
                list.Add(reader.GetObject<T>(props));
            return list;
        }
    }
}