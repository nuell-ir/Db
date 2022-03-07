using System.Data;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace nuell.Sync
{
    public static partial class Db
    {
        public static string ComplexJson(string query, (string Name, Data.Result ResultType)[] props, params (string name, object value)[] parameters)
            => ComplexJson(query, props, false, Data.SqlParams(parameters));

        public static string ComplexJson(string query, (string Name, Data.Result ResultType)[] props, bool isStoredProc, params (string name, object value)[] parameters)
            => ComplexJson(query, props, isStoredProc, Data.SqlParams(parameters));

        public static string ComplexJson(string query, (string Name, Data.Result ResultType)[] props, bool isStoredProc = false)
            => ComplexJson(query, props, isStoredProc, Data.NoParams);

        public static string ComplexJson(string query, (string Name, Data.Result ResultType)[] props, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            cnnct.Open();
            using var reader = cmnd.ExecuteReader();
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, Data.JsonWriterOptions);
            writer.WriteStartObject();
            for (int i = 0; i < props.Length; i++){
                ReadResult(i);
                reader.NextResult();
            }
            writer.WriteEndObject();
            writer.Flush();
            return Encoding.UTF8.GetString(stream.ToArray());

            void ReadResult(int i)
            {
                writer.WritePropertyName(props[i].Name);
                if (!reader.HasRows)
                    writer.WriteNullValue();
                else
                    switch (props[i].ResultType)
                    {
                        case Data.Result.Value:
                            reader.Read();
                            writer.WriteDbValue(reader, Type.GetTypeCode(reader.GetFieldType(0)), 0);
                            break;
                        case Data.Result.Array:
                            writer.WriteRawValue(reader.ReadJson(Data.Result.Array));
                            break;
                        case Data.Result.Object:
                            writer.WriteRawValue(reader.ReadJson(Data.Result.Object));
                            break;
                        case Data.Result.Csv:
                            writer.WriteStringValue(reader.ReadCsv());
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
        public static Task<string> ComplexJson(string query, (string Name, Data.Result ResultType)[] props, params (string name, object value)[] parameters)
            => ComplexJson(query, props, false, Data.SqlParams(parameters));

        public static Task<string> ComplexJson(string query, (string Name, Data.Result ResultType)[] props, bool isStoredProc, params (string name, object value)[] parameters)
            => ComplexJson(query, props, isStoredProc, Data.SqlParams(parameters));

        public static Task<string> ComplexJson(string query, (string Name, Data.Result ResultType)[] props, bool isStoredProc = false)
            => ComplexJson(query, props, isStoredProc, Data.NoParams);

        public static async Task<string> ComplexJson(string query, (string Name, Data.Result ResultType)[] props, bool isStoredProc = false, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            await cnnct.OpenAsync();
            using var reader = await cmnd.ExecuteReaderAsync();
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, Data.JsonWriterOptions);
            writer.WriteStartObject();
            for (int i = 0; i < props.Length; i++){
                await ReadResult(i);
                await reader.NextResultAsync();
            }
            writer.WriteEndObject();
            await writer.FlushAsync();
            return Encoding.UTF8.GetString(stream.ToArray());

            async Task ReadResult(int i)
            {
                writer.WritePropertyName(props[i].Name);
                if (!reader.HasRows)
                    writer.WriteNullValue();
                else
                    switch (props[i].ResultType)
                    {
                        case Data.Result.Value:
                            await reader.ReadAsync();
                            writer.WriteDbValue(reader, Type.GetTypeCode(reader.GetFieldType(0)), 0);
                            break;
                        case Data.Result.Array:
                            writer.WriteRawValue(await reader.ReadJson(Data.Result.Array));
                            break;
                        case Data.Result.Object:
                            writer.WriteRawValue(await reader.ReadJson(Data.Result.Object));
                            break;
                        case Data.Result.Csv:
                            writer.WriteStringValue(await reader.ReadCsv());
                            break;
                    }
            }
        }
    }
}