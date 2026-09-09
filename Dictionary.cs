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
            using var connection = new SqlConnection(Data.ConnectionString);
            using var cmd = new SqlCommand(query, connection);
            if (isStoredProc)
                cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddRange(parameters);
            connection.Open();
            using var reader = cmd.ExecuteReader();
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
            using var connection = new SqlConnection(Data.ConnectionString);
            using var cmd = new SqlCommand(query, connection);
            if (isStoredProc)
                cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddRange(parameters);
            await connection.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
                return null;
            var dictionary = new Dictionary<K, V>();
            while (await reader.ReadAsync())
                dictionary.Add(reader.GetFieldValue<K>(0), reader.GetFieldValue<V>(1));
            return dictionary;
        }
    }
}