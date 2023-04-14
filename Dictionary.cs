using System.Data;
using Microsoft.Data.SqlClient;

namespace nuell.Sync
{
    public static partial class Db
    {
        public static Dictionary<K, V> Dictionary<K, V>(string query, params (string name, object value)[] parameters)
            => Dictionary<K, V>(query, false, Data.SqlParams(parameters));

        public static Dictionary<K, V> Dictionary<K, V>(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => Dictionary<K, V>(query, isStoredProc, Data.SqlParams(parameters));

        public static Dictionary<K, V> Dictionary<K, V>(string query, bool isStoredProc = false)
            => Dictionary<K, V>(query, isStoredProc, Data.NoParams);

        public static Dictionary<K, V> Dictionary<K, V>(string query, bool isStoredProc, params SqlParameter[] parameters)
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
            var dictionary = new Dictionary<K, V>();
            while (reader.Read())
                dictionary.Add(reader.GetFieldValue<K>(0), reader.GetFieldValue<V>(1));
            return dictionary;
        }
    }
}

namespace nuell.Async
{
    public static partial class Db
    {
        public static Task<Dictionary<K, V>> Dictionary<K, V>(string query, params (string name, object value)[] parameters)
            => Dictionary<K, V>(query, false, Data.SqlParams(parameters));

        public static Task<Dictionary<K, V>> Dictionary<K, V>(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => Dictionary<K, V>(query, isStoredProc, Data.SqlParams(parameters));

        public static Task<Dictionary<K, V>> Dictionary<K, V>(string query, bool isStoredProc = false)
            => Dictionary<K, V>(query, isStoredProc, Data.NoParams);

        public static async Task<Dictionary<K, V>> Dictionary<K, V>(string query, bool isStoredProc, params SqlParameter[] parameters)
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
            var dictionary = new Dictionary<K, V>();
            while (await reader.ReadAsync())
                dictionary.Add(reader.GetFieldValue<K>(0), reader.GetFieldValue<V>(1));
            return dictionary;
        }
    }
}