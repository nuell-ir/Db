using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;

namespace nuell
{
    public static partial class Data
    {
        internal static JsonObject GetJsonObject(this SqlDataReader reader, ReadOnlyCollection<DbColumn> columns)
        {
            var obj = new System.Text.Json.Nodes.JsonObject();
            for (int i = 0; i < columns.Count; i++)
            {
                if (reader.IsDBNull(i))
                    obj[reader.GetName(i)] = null;
                else switch (Type.GetTypeCode(columns[i].DataType))
                    {
                        case TypeCode.Int32:
                            obj[reader.GetName(i)] = reader.GetInt32(i);
                            break;
                        case TypeCode.Int64:
                            obj[reader.GetName(i)] = reader.GetInt64(i);
                            break;
                        case TypeCode.Int16:
                            obj[reader.GetName(i)] = reader.GetInt16(i);
                            break;
                        case TypeCode.Byte:
                            obj[reader.GetName(i)] = reader.GetByte(i);
                            break;
                        case TypeCode.Single:
                            obj[reader.GetName(i)] = reader.GetFloat(i);
                            break;
                        case TypeCode.Double:
                            obj[reader.GetName(i)] = reader.GetDouble(i);
                            break;
                        case TypeCode.Decimal:
                            obj[reader.GetName(i)] = reader.GetDecimal(i);
                            break;
                        case TypeCode.DateTime:
                            obj[reader.GetName(i)] = reader.GetDateTime(i);
                            break;
                        case TypeCode.Boolean:
                            obj[reader.GetName(i)] = reader.GetBoolean(i);
                            break;
                        case TypeCode.Char:
                            obj[reader.GetName(i)] = reader.GetChar(i);
                            break;
                        case TypeCode.String:
                            obj[reader.GetName(i)] = reader.GetString(i);
                            Console.WriteLine(obj[reader.GetName(i)]);
                            break;
                    }
            }
            return obj;
        }
    }
}

namespace nuell.Sync
{
    public static partial class Db
    {
        public static JsonObject JsonObject(string query, params (string name, object value)[] parameters)
            => JsonObject(query, false, Data.SqlParams(parameters));

        public static JsonObject JsonObject(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => JsonObject(query, isStoredProc, Data.SqlParams(parameters));

        public static JsonObject JsonObject(string query, bool isStoredProc = false)
            => JsonObject(query, isStoredProc, Data.NoParams);

        public static JsonObject JsonObject(string query, bool isStoredProc, params SqlParameter[] parameters)
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
                return reader.GetJsonObject(reader.GetColumnSchema());
            }
            else
                return default(JsonObject);
        }
    }
}

namespace nuell.Async
{
    public static partial class Db
    {
        public static Task<JsonObject> JsonObject(string query, params (string name, object value)[] parameters)
            => JsonObject(query, false, Data.SqlParams(parameters));

        public static Task<JsonObject> JsonObject(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => JsonObject(query, isStoredProc, Data.SqlParams(parameters));

        public static Task<JsonObject> JsonObject(string query, bool isStoredProc = false)
            => JsonObject(query, isStoredProc, Data.NoParams);

        public static async Task<JsonObject> JsonObject(string query, bool isStoredProc, params SqlParameter[] parameters)
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
                return reader.GetJsonObject(await reader.GetColumnSchemaAsync());
            }
            else
                return default(JsonObject);
        }
    }
}