using System.Data;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace nuel.Sync
{
	public static partial class Db
	{
		public static string ComplexJson(string query, (string Name, JsonValueType ResultType)[] props, params (string name, object value)[] parameters)
			 => ComplexJson(query, props, false, Data.SqlParams(parameters));

		public static string ComplexJson(string query, (string Name, JsonValueType ResultType)[] props, bool isStoredProc, params (string name, object value)[] parameters)
			 => ComplexJson(query, props, isStoredProc, Data.SqlParams(parameters));

		public static string ComplexJson(string query, (string Name, JsonValueType ResultType)[] props, bool isStoredProc = false)
			 => ComplexJson(query, props, isStoredProc, Data.NoParams);

		public static string ComplexJson(string query, (string Name, JsonValueType ResultType)[] props, bool isStoredProc, params SqlParameter[] parameters)
		{
			using var connection = new SqlConnection(Data.ConnectionString);
			using var cmd = new SqlCommand(query, connection);
			if (isStoredProc)
				cmd.CommandType = CommandType.StoredProcedure;
			cmd.Parameters.AddRange(parameters);
			connection.Open();
			using var reader = cmd.ExecuteReader();
			using var stream = new MemoryStream();
			using var writer = new Utf8JsonWriter(stream, Data.JsonWriterOptions);
			writer.WriteStartObject();
			for (int i = 0; i < props.Length; i++)
			{
				ReadResult(i);
				reader.NextResult();
			}
			writer.WriteEndObject();
			writer.Flush();
			return Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);

			void ReadResult(int i)
			{
				writer.WritePropertyName(props[i].Name);
				if (!reader.HasRows)
				{
					writer.WriteNullValue();
					return;
				}
				switch (props[i].ResultType)
				{
					case JsonValueType.Value:
						if (reader.Read())
						{
							if (reader.IsDBNull(0))
								writer.WriteNullValue();
							else
								writer.WriteDbValue(reader, reader.GetFieldType(0), 0);
						}
						else
							writer.WriteNullValue();
						break;
					case JsonValueType.Array:
					case JsonValueType.Object:
						reader.ReadJson(props[i].ResultType, stream, writer);
						break;
					case JsonValueType.Csv:
						writer.WriteStringValue(reader.ReadCsv());
						break;
				}
			}
		}
	}
}

namespace nuel.Async
{
	public static partial class Db
	{
		public static Task<string> ComplexJson(string query, (string Name, JsonValueType ResultType)[] props, params (string name, object value)[] parameters)
			 => ComplexJson(query, props, false, Data.SqlParams(parameters));

		public static Task<string> ComplexJson(string query, (string Name, JsonValueType ResultType)[] props, bool isStoredProc, params (string name, object value)[] parameters)
			 => ComplexJson(query, props, isStoredProc, Data.SqlParams(parameters));

		public static Task<string> ComplexJson(string query, (string Name, JsonValueType ResultType)[] props, bool isStoredProc = false)
			 => ComplexJson(query, props, isStoredProc, Data.NoParams);

		public static async Task<string> ComplexJson(string query, (string Name, JsonValueType ResultType)[] props, bool isStoredProc = false, params SqlParameter[] parameters)
		{
			using var connection = new SqlConnection(Data.ConnectionString);
			using var cmd = new SqlCommand(query, connection);
			if (isStoredProc)
				cmd.CommandType = CommandType.StoredProcedure;
			cmd.Parameters.AddRange(parameters);
			await connection.OpenAsync();
			using var reader = await cmd.ExecuteReaderAsync();
			using var stream = new MemoryStream();
			using var writer = new Utf8JsonWriter(stream, Data.JsonWriterOptions);
			writer.WriteStartObject();
			for (int i = 0; i < props.Length; i++)
			{
				await ReadResult(i);
				await reader.NextResultAsync();
			}
			writer.WriteEndObject();
			await writer.FlushAsync();
			return Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);

			async Task ReadResult(int i)
			{
				writer.WritePropertyName(props[i].Name);
				if (!reader.HasRows)
				{
					writer.WriteNullValue();
					return;
				}
				switch (props[i].ResultType)
				{
					case JsonValueType.Value:
						if (await reader.ReadAsync())
						{
							if (reader.IsDBNull(0))
								writer.WriteNullValue();
							else
								writer.WriteDbValue(reader, reader.GetFieldType(0), 0);
						}
						else
							writer.WriteNullValue();
						break;
					case JsonValueType.Array:
					case JsonValueType.Object:
						await reader.ReadJson(props[i].ResultType, stream, writer);
						break;
					case JsonValueType.Csv:
						writer.WriteStringValue(await reader.ReadCsv());
						break;
				}
			}
		}
	}
}