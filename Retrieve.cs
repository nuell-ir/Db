using System.Data;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace nuell
{
    public enum Results
    {
        Object, Json, Csv
    }
}

namespace nuell.Sync
{
    public static partial class Db
    {
        public static string Retrieve(string query, (string Name, Results ResultType)[] props, params (string name, object value)[] parameters)
            => Retrieve(query, props, false, Data.SqlParams(parameters));

        public static string Retrieve(string query, (string Name, Results ResultType)[] props, bool isStoredProc, params (string name, object value)[] parameters)
            => Retrieve(query, props, isStoredProc, Data.SqlParams(parameters));

        public static string Retrieve(string query, (string Name, Results ResultType)[] props, bool isStoredProc = false)
            => Retrieve(query, props, isStoredProc, Data.NoParams);

        public static string Retrieve(string query, (string Name, Results ResultType)[] props, bool isStoredProc, params SqlParameter[] parameters)
        {
            int index = 0;
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
            ReadResult();
            while (reader.NextResult())
                ReadResult();
            writer.WriteEndObject();
            writer.Flush();
            return Encoding.UTF8.GetString(stream.ToArray());

            void ReadResult()
            {
                writer.WritePropertyName(props[index].Name);
                if (!reader.HasRows)
                    writer.WriteNullValue();
                else
                {
                    switch (props[index].ResultType)
                    {
                        case Results.Object:
                            reader.Read();
                            writer.WriteDbValue(reader, Type.GetTypeCode(reader.GetFieldType(0)), 0);
                            break;
                        case Results.Json:
                            writer.WriteRawValue(reader.ReadJson());
                            break;
                        case Results.Csv:
                            writer.WriteStringValue(reader.ReadCsv());
                            break;
                    }
                    index++;
                }
            }
        }
    }
}

namespace nuell.Async
{
    public static partial class Db
    {
        public static Task<string> Retrieve(string query, (string Name, Results ResultType)[] props, params (string name, object value)[] parameters)
            => Retrieve(query, props, false, Data.SqlParams(parameters));

        public static Task<string> Retrieve(string query, (string Name, Results ResultType)[] props, bool isStoredProc, params (string name, object value)[] parameters)
            => Retrieve(query, props, isStoredProc, Data.SqlParams(parameters));

        public static Task<string> Retrieve(string query, (string Name, Results ResultType)[] props, bool isStoredProc = false)
            => Retrieve(query, props, isStoredProc, Data.NoParams);

        public static async Task<string> Retrieve(string query, (string Name, Results ResultType)[] props, bool isStoredProc = false, params SqlParameter[] parameters)
        {
            int index = 0;
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
            await ReadResult();
            while (await reader.NextResultAsync())
                await ReadResult();
            writer.WriteEndObject();
            await writer.FlushAsync();
            return Encoding.UTF8.GetString(stream.ToArray());

            async Task ReadResult()
            {
                writer.WritePropertyName(props[index].Name);
                if (!reader.HasRows)
                    writer.WriteNullValue();
                else
                {
                    switch (props[index].ResultType)
                    {
                        case Results.Object:
                            await reader.ReadAsync();
                            writer.WriteDbValue(reader, Type.GetTypeCode(reader.GetFieldType(0)), 0);
                            break;
                        case Results.Json:
                            writer.WriteRawValue(await reader.ReadJson());
                            break;
                        case Results.Csv:
                            writer.WriteStringValue(await reader.ReadCsv());
                            break;
                    }
                    index++;
                }
            }
        }
    }
}