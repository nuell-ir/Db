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
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            cnnct.Open();
            using var reader = cmnd.ExecuteReader();
            var results = new List<object>();
            AddValues();
            while (reader.NextResult())
                AddValues();
            return results.ToArray();

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
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            await cnnct.OpenAsync();
            using var reader = await cmnd.ExecuteReaderAsync();
            var results = new List<object>();
            await AddValues();
            while (await reader.NextResultAsync())
                await AddValues();
            return results.ToArray();

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