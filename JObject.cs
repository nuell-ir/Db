using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;

namespace nuell.Sync
{
    public static partial class Db
    {
        public static JObject JObject(string query, params (string name, object value)[] parameters)
            => JObject(query, false, Data.SqlParams(parameters));

        public static JObject JObject(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => JObject(query, isStoredProc, Data.SqlParams(parameters));

        public static JObject JObject(string query, bool isStoredProc = false)
            => JObject(query, isStoredProc, Data.NoParams);

        public static JObject JObject(string query, bool isStoredProc, params SqlParameter[] parameters)
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
                return JObject(reader);
            }
            else
                return null;
        }

        private static JObject JObject(SqlDataReader reader)
        {
            var json = new JObject();
            for (int i = 0; i < reader.FieldCount; i++)
                json.Add(reader.GetName(i), JToken.FromObject(reader.GetValue(i)));
            return json;
        }
    }
}

namespace nuell.Async
{
    public static partial class Db
    {
        public static Task<JObject> JObject(string query, params (string name, object value)[] parameters)
            => JObject(query, false, Data.SqlParams(parameters));

        public static Task<JObject> JObject(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => JObject(query, isStoredProc, Data.SqlParams(parameters));

        public static Task<JObject> JObject(string query, bool isStoredProc = false)
            => JObject(query, isStoredProc, Data.NoParams);

        public async static Task<JObject> JObject(string query, bool isStoredProc, params SqlParameter[] parameters)
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
                var json = new JObject();
                for (int i = 0; i < reader.VisibleFieldCount; i++)
                    json.Add(reader.GetName(i), JToken.FromObject(reader.GetValue(i)));

                return json;
            }
            else
                return null;
        }

        private static JObject JObject(SqlDataReader reader)
        {
            var json = new JObject();
            for (int i = 0; i < reader.FieldCount; i++)
                json.Add(reader.GetName(i), JToken.FromObject(reader.GetValue(i)));
            return json;
        }
    }
}