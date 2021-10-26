using System.Data;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

namespace nuell.Sync
{
    public static partial class Db
    {
        public static string Json(string query, params (string name, object value)[] parameters)
            => Json(query, false, Data.SqlParams(parameters));

        public static string Json(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => Json(query, isStoredProc, Data.SqlParams(parameters));

        public static string Json(string query, bool isStoredProc = false)
            => Json(query, isStoredProc, Data.NoParams);

        public static string Json(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            cnnct.Open();
            using var reader = cmnd.ExecuteReader();
            if (reader.HasRows)
                return Json(reader);
            else
                return null;
        }

        private static string Json(SqlDataReader reader)
        {
            reader.Read();
            var str = new StringBuilder();
            var sw = new StringWriter(str);
            using var writer = new JsonTextWriter(sw);
            writer.WriteStartObject();
            int count = reader.VisibleFieldCount;
            for (int i = 0; i < count; i++)
            {
                writer.WritePropertyName(reader.GetName(i));
                writer.WriteValue(reader.GetValue(i));
            }
            writer.WriteEndObject();
            return str.ToString();
        }
    }
}

namespace nuell.Async
{
    public static partial class Db
    {
        public static Task<string> Json(string query, params (string name, object value)[] parameters)
            => Json(query, false, Data.SqlParams(parameters));

        public static Task<string> Json(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => Json(query, isStoredProc, Data.SqlParams(parameters));

        public static Task<string> Json(string query, bool isStoredProc = false)
            => Json(query, isStoredProc, Data.NoParams);

        public async static Task<string> Json(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            await cnnct.OpenAsync();
            using var reader = await cmnd.ExecuteReaderAsync();
            if (reader.HasRows)
                return await Json(reader);
            else
                return null;
        }

        private static async Task<string> Json(SqlDataReader reader)
        {
            await reader.ReadAsync();
            var str = new StringBuilder();
            var sw = new StringWriter(str);
            using var writer = new JsonTextWriter(sw);
            writer.WriteStartObject();
            int count = reader.VisibleFieldCount;
            for (int i = 0; i < count; i++)
            {
                writer.WritePropertyName(reader.GetName(i));
                writer.WriteValue(reader.GetValue(i));
            }
            writer.WriteEndObject();
            return str.ToString();
        }
    }
}