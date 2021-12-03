using System.Data;
using Microsoft.Data.SqlClient;

namespace nuell
{
    public static partial class Data
    {
        internal static T GetObject<T>(this SqlDataReader reader) where T : new()
        {
            int fieldCount = reader.FieldCount;
            var props = typeof(T).GetProperties().ToDictionary(p => p.Name, p => p);
            var obj = new T();
            for (int i = 0; i < fieldCount; i++)
                props[reader.GetName(i)].SetValue(obj, reader.GetValue(i));
            return obj;
        }
    }
}

namespace nuell.Sync
{
    public static partial class Db
    {
        public static T Object<T>(string query, params (string name, object value)[] parameters) where T : new()
            => Object<T>(query, false, Data.SqlParams(parameters));

        public static T Object<T>(string query, bool isStoredProc, params (string name, object value)[] parameters) where T : new()
            => Object<T>(query, isStoredProc, Data.SqlParams(parameters));

        public static T Object<T>(string query, bool isStoredProc = false) where T : new()
            => Object<T>(query, isStoredProc, Data.NoParams);

        public static T Object<T>(string query, bool isStoredProc, params SqlParameter[] parameters) where T : new()
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            cnnct.Open();
            using var reader = cmnd.ExecuteReader();
            if (reader.HasRows)
            {
                reader.Read();
                return reader.GetObject<T>();
            }
            else
                return default(T);
        }
    }
}

namespace nuell.Async
{
    public static partial class Db
    {
        public static Task<T> Object<T>(string query, params (string name, object value)[] parameters) where T : new()
            => Object<T>(query, false, Data.SqlParams(parameters));

        public static Task<T> Object<T>(string query, bool isStoredProc, params (string name, object value)[] parameters) where T : new()
            => Object<T>(query, isStoredProc, Data.SqlParams(parameters));

        public static Task<T> Object<T>(string query, bool isStoredProc = false) where T : new()
            => Object<T>(query, isStoredProc, Data.NoParams);

        public static async Task<T> Object<T>(string query, bool isStoredProc, params SqlParameter[] parameters) where T : new()
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            await cnnct.OpenAsync();
            using var reader = await cmnd.ExecuteReaderAsync();
            if (reader.HasRows)
            {
                await reader.ReadAsync();
                return reader.GetObject<T>();
            }
            else
                return default(T);
        }
    }
}