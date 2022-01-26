using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace nuell
{
    public static partial class Data
    {
        internal static (List<string>, List<TypeCode>) GetSchema(ReadOnlyCollection<DbColumn> columns)
        {
            var fieldTypes = new List<TypeCode>(columns.Count);
            var fieldNames = new List<string>(columns.Count);
            foreach (var col in columns)
            {
                fieldTypes.Add(Type.GetTypeCode(col.DataType));
                fieldNames.Add(col.ColumnName);
            }
            return (fieldNames, fieldTypes);
        }

        internal static void WriteDbValue(this Utf8JsonWriter writer, SqlDataReader reader, TypeCode typeCode, int columnIndex)
        {
            switch (typeCode)
            {
                case TypeCode.Int32:
                    writer.WriteNumberValue(reader.GetInt32(columnIndex));
                    break;
                case TypeCode.Int64:
                    writer.WriteNumberValue(reader.GetInt64(columnIndex));
                    break;
                case TypeCode.Int16:
                    writer.WriteNumberValue(reader.GetInt16(columnIndex));
                    break;
                case TypeCode.Byte:
                    writer.WriteNumberValue(reader.GetByte(columnIndex));
                    break;
                case TypeCode.Single:
                    writer.WriteNumberValue(reader.GetFloat(columnIndex));
                    break;
                case TypeCode.Double:
                    writer.WriteNumberValue(reader.GetDouble(columnIndex));
                    break;
                case TypeCode.Decimal:
                    writer.WriteNumberValue(reader.GetDecimal(columnIndex));
                    break;
                case TypeCode.DateTime:
                    writer.WriteStringValue(reader.GetDateTime(columnIndex));
                    break;
                case TypeCode.Boolean:
                    writer.WriteBooleanValue(reader.GetBoolean(columnIndex));
                    break;
                case TypeCode.Char:
                case TypeCode.String:
                    writer.WriteStringValue(reader.GetString(columnIndex));
                    break;
            }
        }
    }
}

namespace nuell.Sync
{
    public static partial class Db
    {
        public static string Json(string query, params (string name, object value)[] parameters)
        => Json(query, Data.Result.Array, false, Data.SqlParams(parameters));
        public static string Json(string query, Data.Result result, params (string name, object value)[] parameters)
        => Json(query, result, false, Data.SqlParams(parameters));

        public static string Json(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => Json(query, Data.Result.Array, isStoredProc, Data.SqlParams(parameters));

        public static string Json(string query, Data.Result result = Data.Result.Array, bool isStoredProc = false)
            => Json(query, result, isStoredProc, Data.NoParams);

        public static string Json(string query, Data.Result result, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            cnnct.Open();
            using var reader = cmnd.ExecuteReader();
            return reader.ReadJson(result);
        }

        internal static string ReadJson(this SqlDataReader reader, Data.Result result)
        {
            var (fieldNames, fieldTypes) = Data.GetSchema(reader.GetColumnSchema());
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, Data.JsonWriterOptions);
            int count = fieldNames.Count;
            switch (result)
            {
                case Data.Result.Object:
                    if (reader.Read())
                        WriteObject();
                    else
                        writer.WriteNullValue();
                    break;
                case Data.Result.Array:
                    writer.WriteStartArray();
                    while (reader.Read())
                        WriteObject();
                    writer.WriteEndArray();
                    break;
                default:
                    throw new ArgumentException("The only valid JSON results are array and object.");
            }
            writer.Flush();
            return Encoding.UTF8.GetString(stream.ToArray());

            void WriteObject()
            {
                writer.WriteStartObject();
                for (int i = 0; i < count; i++)
                {
                    writer.WritePropertyName(fieldNames[i]);
                    if (reader.IsDBNull(i))
                        writer.WriteNullValue();
                    else
                        writer.WriteDbValue(reader, fieldTypes[i], i);
                }
                writer.WriteEndObject();
            }
        }
    }
}

namespace nuell.Async
{
    public static partial class Db
    {
        public static Task<string> Json(string query, params (string name, object value)[] parameters)
            => Json(query, Data.Result.Array, false, Data.SqlParams(parameters));

        public static Task<string> Json(string query, Data.Result result, params (string name, object value)[] parameters)
        => Json(query, result, false, Data.SqlParams(parameters));

        public static Task<string> Json(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => Json(query, Data.Result.Array, isStoredProc, Data.SqlParams(parameters));

        public static Task<string> Json(string query, Data.Result result = Data.Result.Array, bool isStoredProc = false)
            => Json(query, result, isStoredProc, Data.NoParams);

        public async static Task<string> Json(string query, Data.Result result, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            await cnnct.OpenAsync();
            using var reader = await cmnd.ExecuteReaderAsync();
            return await reader.ReadJson(result);
        }

        internal async static Task<string> ReadJson(this SqlDataReader reader, Data.Result result)
        {
            var (fieldNames, fieldTypes) = Data.GetSchema(await reader.GetColumnSchemaAsync());
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, Data.JsonWriterOptions);
            int count = fieldNames.Count;
            switch (result)
            {
                case Data.Result.Object:
                    if (await reader.ReadAsync())
                        WriteObject();
                    else
                        writer.WriteNullValue();
                    break;
                case Data.Result.Array:
                    writer.WriteStartArray();
                    while (await reader.ReadAsync())
                        WriteObject();
                    writer.WriteEndArray();
                    break;
                default:
                    throw new ArgumentException("The only valid JSON results are array and object.");
            }
            await writer.FlushAsync();
            return Encoding.UTF8.GetString(stream.ToArray());

            void WriteObject()
            {
                writer.WriteStartObject();
                for (int i = 0; i < count; i++)
                {
                    writer.WritePropertyName(fieldNames[i]);
                    if (reader.IsDBNull(i))
                        writer.WriteNullValue();
                    else
                        writer.WriteDbValue(reader, fieldTypes[i], i);
                }
                writer.WriteEndObject();
            }
        }
    }
}