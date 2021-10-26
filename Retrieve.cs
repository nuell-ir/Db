using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;

namespace nuell
{
    public enum Results
    {
        Object, JObject, Json, Csv
    }
}

namespace nuell.Sync
{
    public static partial class Db
    {
        public static JObject Retrieve(string query, (string Name, Results ResultType)[] props, params (string name, object value)[] parameters)
            => Retrieve(query, props, false, Data.SqlParams(parameters));

        public static JObject Retrieve(string query, (string Name, Results ResultType)[] props, bool isStoredProc, params (string name, object value)[] parameters)
            => Retrieve(query, props, isStoredProc, Data.SqlParams(parameters));

        public static JObject Retrieve(string query, (string Name, Results ResultType)[] props, bool isStoredProc = false)
            => Retrieve(query, props, isStoredProc, Data.NoParams);

        public static JObject Retrieve(string query, (string Name, Results ResultType)[] props, bool isStoredProc, params SqlParameter[] parameters)
        {
            var result = new JObject();
            int index = 0;
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            cnnct.Open();
            using var reader = cmnd.ExecuteReader();
            Read();
            while (reader.NextResult())
                Read();
            return result;

            void Read()
            {
                if (!reader.HasRows)
                    result[props[index++].Name] = null;
                else
                    switch (props[index].ResultType)
                    {
                        case Results.Object:
                            reader.Read();
                            result.Add(props[index++].Name, JToken.FromObject(reader[0]));
                            break;
                        case Results.JObject:
                            reader.Read();
                            result.Add(props[index++].Name, JObject(reader));
                            break;
                        case Results.Json:
                            result.Add(props[index++].Name, Json(reader));
                            break;
                        case Results.Csv:
                            result.Add(props[index++].Name, ReadCsvResult(reader));
                            break;
                    }
            }
        }
    }
}

namespace nuell.Async
{
    public static partial class Db
    {
        public static Task<JObject> Retrieve(string query, (string Name, Results ResultType)[] props, params (string name, object value)[] parameters)
            => Retrieve(query, props, false, Data.SqlParams(parameters));

        public static Task<JObject> Retrieve(string query, (string Name, Results ResultType)[] props, bool isStoredProc, params (string name, object value)[] parameters)
            => Retrieve(query, props, isStoredProc, Data.SqlParams(parameters));

        public static Task<JObject> Retrieve(string query, (string Name, Results ResultType)[] props, bool isStoredProc = false)
            => Retrieve(query, props, isStoredProc, Data.NoParams);

        public static async Task<JObject> Retrieve(string query, (string Name, Results ResultType)[] props, bool isStoredProc = false, params SqlParameter[] parameters)
        {
            var result = new JObject();
            int index = 0;
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            await cnnct.OpenAsync();
            using var reader = cmnd.ExecuteReader();
            await Read();
            while (reader.NextResult())
                await Read();
            return result;

            async Task Read()
            {
                if (!reader.HasRows)
                    result[props[index++].Name] = null;
                else
                    switch (props[index].ResultType)
                    {
                        case Results.Object:
                            await reader.ReadAsync();
                            result.Add(props[index++].Name, JToken.FromObject(reader[0]));
                            break;
                        case Results.JObject:
                            await reader.ReadAsync();
                            result.Add(props[index++].Name, JObject(reader));
                            break;
                        case Results.Json:
                            result.Add(props[index++].Name, await Json(reader));
                            break;
                        case Results.Csv:
                            result.Add(props[index++].Name, await ReadCsvResult(reader));
                            break;
                    }
            }
        }
    }
}