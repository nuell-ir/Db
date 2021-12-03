using System.Data;
using Microsoft.Data.SqlClient;

namespace nuell.Sync
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
            var props = typeof(T).GetProperties().ToDictionary(p => p.Name, p => p);
            T obj;
            int fieldCount = reader.FieldCount;
            while (reader.Read())
            {
                obj = new T();
                for (int i = 0; i < fieldCount; i++)
                    props[reader.GetName(i)].SetValue(obj, reader.GetValue(i));
                list.Add(obj);
            }
            return list;
        }
    }
}

namespace nuell.Async
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
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            await cnnct.OpenAsync();
            using var reader = await cmnd.ExecuteReaderAsync();
            if (!reader.HasRows)
                return null;

            var list = new List<T>();
            var props = typeof(T).GetProperties().ToDictionary(p => p.Name, p => p);
            T obj;
            int fieldCount = reader.FieldCount;
            while (await reader.ReadAsync())
            {
                obj = new T();
                for (int i = 0; i < fieldCount; i++)
                    props[reader.GetName(i)].SetValue(obj, reader.GetValue(i));
                list.Add(obj);
            }
            return list;
        }
    }
}