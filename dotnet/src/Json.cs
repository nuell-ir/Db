using System.Buffers;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace nuel;

public static partial class Data
{
	internal enum JsonColType : byte
	{
		Int32,
		Int16,
		Byte,
		Int64,
		Single,
		Double,
		Decimal,
		DateTime,
		Boolean,
		String,
		Guid,
		DateTimeOffset,
		TimeSpan,
		ByteArray
	}

	internal static JsonColType GetJsonColType(Type type)
	{
		type = Nullable.GetUnderlyingType(type) ?? type;
		return Type.GetTypeCode(type) switch
		{
			TypeCode.Int32 => JsonColType.Int32,
			TypeCode.Int16 => JsonColType.Int16,
			TypeCode.Byte => JsonColType.Byte,
			TypeCode.Int64 => JsonColType.Int64,
			TypeCode.Single => JsonColType.Single,
			TypeCode.Double => JsonColType.Double,
			TypeCode.Decimal => JsonColType.Decimal,
			TypeCode.DateTime => JsonColType.DateTime,
			TypeCode.Boolean => JsonColType.Boolean,
			TypeCode.Char or TypeCode.String => JsonColType.String,
			_ when type == typeof(Guid) => JsonColType.Guid,
			_ when type == typeof(DateTimeOffset) => JsonColType.DateTimeOffset,
			_ when type == typeof(TimeSpan) => JsonColType.TimeSpan,
			_ when type == typeof(byte[]) => JsonColType.ByteArray,
			_ => throw new NotSupportedException($"Type '{type.FullName}' is not supported.")
		};
	}

	internal static (int count, JsonEncodedText[] encodedNames, JsonColType[] colTypes) GetJsonSchema(this DbDataReader reader)
	{
		int count = reader.FieldCount;
		var encodedNames = new JsonEncodedText[count];
		var colTypes = new JsonColType[count];
		for (int i = 0; i < count; i++)
		{
			encodedNames[i] = JsonEncodedText.Encode(reader.GetName(i));
			colTypes[i] = GetJsonColType(reader.GetFieldType(i));
		}
		return (count, encodedNames, colTypes);
	}

	internal static (int, string[], Type[]) GetSchema(this DbDataReader reader)
	{
		int count = reader.FieldCount;
		var fieldTypes = new Type[count];
		var fieldNames = new string[count];
		for (int i = 0; i < count; i++)
		{
			fieldTypes[i] = reader.GetFieldType(i);
			fieldNames[i] = reader.GetName(i);
		}
		return (count, fieldNames, fieldTypes);
	}

	internal static void WriteDbValue(this Utf8JsonWriter writer, DbDataReader reader, JsonColType colType, int columnIndex)
	{
		if (reader.IsDBNull(columnIndex))
		{
			writer.WriteNullValue();
			return;
		}

		switch (colType)
		{
			case JsonColType.Int32:
				writer.WriteNumberValue(reader.GetInt32(columnIndex));
				return;
			case JsonColType.Int16:
				writer.WriteNumberValue(reader.GetInt16(columnIndex));
				return;
			case JsonColType.Byte:
				writer.WriteNumberValue(reader.GetByte(columnIndex));
				return;
			case JsonColType.Int64:
				writer.WriteNumberValue(reader.GetInt64(columnIndex));
				return;
			case JsonColType.Single:
				writer.WriteNumberValue(reader.GetFloat(columnIndex));
				return;
			case JsonColType.Double:
				writer.WriteNumberValue(reader.GetDouble(columnIndex));
				return;
			case JsonColType.Decimal:
				writer.WriteNumberValue(reader.GetDecimal(columnIndex));
				return;
			case JsonColType.DateTime:
				writer.WriteStringValue(reader.GetDateTime(columnIndex));
				return;
			case JsonColType.Boolean:
				writer.WriteBooleanValue(reader.GetBoolean(columnIndex));
				return;
			case JsonColType.String:
				writer.WriteStringValue(reader.GetString(columnIndex));
				return;
			case JsonColType.Guid:
				writer.WriteStringValue(reader.GetGuid(columnIndex));
				return;
			case JsonColType.DateTimeOffset:
				writer.WriteStringValue(reader is SqlDataReader sdr ? sdr.GetDateTimeOffset(columnIndex) : reader.GetFieldValue<DateTimeOffset>(columnIndex));
				return;
			case JsonColType.TimeSpan:
				TimeSpan ts = reader is SqlDataReader tsSdr ? tsSdr.GetTimeSpan(columnIndex) : reader.GetFieldValue<TimeSpan>(columnIndex);
				Span<char> span = stackalloc char[32];
				ts.TryFormat(span, out int written);
				writer.WriteStringValue(span[..written]);
				return;
			case JsonColType.ByteArray:
				writer.WriteBase64StringValue((byte[])reader.GetValue(columnIndex));
				return;
			default:
				throw new NotSupportedException($"Column type '{colType}' is not supported.");
		}
	}

	internal static void WriteDbValue(this Utf8JsonWriter writer, DbDataReader reader, Type type, int columnIndex)
		=> WriteDbValue(writer, reader, GetJsonColType(type), columnIndex);
}

public static partial class Db
{
	/// <summary>Asynchronously converts the first row of the query result to a JSON object string.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning a JSON string representing the first row.</returns>
	public static Task<string> Json(string query, params (string name, object value)[] parameters)
		 => Json(query, JsonValueType.Object, false, Data.SqlParams(parameters));

	/// <summary>Asynchronously converts the query results to a JSON string.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="result">The JSON structure to return (<see cref="JsonValueType.Object"/> for the first row, or <see cref="JsonValueType.Array"/> for all rows).</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning a JSON string representing the query results.</returns>
	public static Task<string> Json(string query, JsonValueType result, params (string name, object value)[] parameters)
	=> Json(query, result, false, Data.SqlParams(parameters));

	/// <summary>Asynchronously converts the first row of the query result to a JSON object string.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning a JSON string representing the first row.</returns>
	public static Task<string> Json(string query, bool isStoredProc, params (string name, object value)[] parameters)
	=> Json(query, JsonValueType.Object, isStoredProc, Data.SqlParams(parameters));

	/// <summary>Asynchronously converts the query results to a JSON string.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="result">The JSON structure to return (<see cref="JsonValueType.Object"/> for the first row, or <see cref="JsonValueType.Array"/> for all rows).</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning a JSON string representing the query results.</returns>
	public static Task<string> Json(string query, JsonValueType result, bool isStoredProc, params (string name, object value)[] parameters)
	=> Json(query, result, isStoredProc, Data.SqlParams(parameters));

	/// <summary>Asynchronously converts the query results to a JSON string or writes UTF-8 JSON directly to a stream.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="result">The JSON structure to return (<see cref="JsonValueType.Object"/> for the first row, or <see cref="JsonValueType.Array"/> for all rows).</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="stream">The optional stream to write UTF-8 JSON directly to. If null, a JSON string is returned.</param>
	/// <returns>A task representing the asynchronous operation, returning a JSON string representing the query results, or null if a stream is provided.</returns>
	public static Task<string> Json(string query, JsonValueType result = JsonValueType.Object, bool isStoredProc = false, Stream stream = null)
		 => Json(query, stream, result, isStoredProc, Data.NoParams);

	/// <summary>Asynchronously converts the query results to UTF-8 JSON directly written to the specified stream.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="stream">The stream to write UTF-8 JSON directly to.</param>
	/// <param name="result">The JSON structure to return (<see cref="JsonValueType.Object"/> for the first row, or <see cref="JsonValueType.Array"/> for all rows).</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <returns>A task representing the asynchronous operation, returning null after writing UTF-8 JSON directly to the stream.</returns>
	public static Task<string> Json(string query, Stream stream, JsonValueType result = JsonValueType.Object, bool isStoredProc = false)
		 => Json(query, stream, result, isStoredProc, Data.NoParams);

	/// <summary>Asynchronously converts the first row of the query result to UTF-8 JSON directly written to the specified stream.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="stream">The stream to write UTF-8 JSON directly to.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning null after writing UTF-8 JSON directly to the stream.</returns>
	public static Task<string> Json(string query, Stream stream, params (string name, object value)[] parameters)
		=> Json(query, stream, JsonValueType.Object, false, Data.SqlParams(parameters));

	/// <summary>Asynchronously converts the query results to UTF-8 JSON directly written to the specified stream.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="stream">The stream to write UTF-8 JSON directly to.</param>
	/// <param name="result">The JSON structure to return (<see cref="JsonValueType.Object"/> for the first row, or <see cref="JsonValueType.Array"/> for all rows).</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning null after writing UTF-8 JSON directly to the stream.</returns>
	public static Task<string> Json(string query, Stream stream, JsonValueType result, params (string name, object value)[] parameters)
		=> Json(query, stream, result, false, Data.SqlParams(parameters));

	/// <summary>Asynchronously converts the first row of the query result to UTF-8 JSON directly written to the specified stream.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="stream">The stream to write UTF-8 JSON directly to.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning null after writing UTF-8 JSON directly to the stream.</returns>
	public static Task<string> Json(string query, Stream stream, bool isStoredProc, params (string name, object value)[] parameters)
		=> Json(query, stream, JsonValueType.Object, isStoredProc, Data.SqlParams(parameters));

	/// <summary>Asynchronously converts the query results to UTF-8 JSON directly written to the specified stream.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="stream">The stream to write UTF-8 JSON directly to.</param>
	/// <param name="result">The JSON structure to return (<see cref="JsonValueType.Object"/> for the first row, or <see cref="JsonValueType.Array"/> for all rows).</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning null after writing UTF-8 JSON directly to the stream.</returns>
	public static Task<string> Json(string query, Stream stream, JsonValueType result, bool isStoredProc, params (string name, object value)[] parameters)
		=> Json(query, stream, result, isStoredProc, Data.SqlParams(parameters));

	/// <summary>Asynchronously converts the query results to a JSON string.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="result">The JSON structure to return (<see cref="JsonValueType.Object"/> for the first row, or <see cref="JsonValueType.Array"/> for all rows).</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The SQL parameters to apply to the command.</param>
	/// <returns>A task representing the asynchronous operation, returning a JSON string representing the query results.</returns>
	public static Task<string> Json(string query, JsonValueType result, bool isStoredProc, params SqlParameter[] parameters)
		=> Json(query, null, result, isStoredProc, parameters);

	/// <summary>Asynchronously converts the query results to a JSON string or writes UTF-8 JSON directly to a stream.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="stream">The stream to write UTF-8 JSON directly to, or null to return a JSON string.</param>
	/// <param name="result">The JSON structure to return (<see cref="JsonValueType.Object"/> for the first row, or <see cref="JsonValueType.Array"/> for all rows).</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The SQL parameters to apply to the command.</param>
	/// <returns>A task representing the asynchronous operation, returning a JSON string representing the query results, or null if a stream is provided.</returns>
	public async static Task<string> Json(string query, Stream stream, JsonValueType result, bool isStoredProc, params SqlParameter[] parameters)
	{
		using var connection = new SqlConnection(Data.ConnectionString);
		using var cmd = new SqlCommand(query, connection);
		if (isStoredProc)
			cmd.CommandType = CommandType.StoredProcedure;
		cmd.Parameters.AddRange(parameters);
		await connection.OpenAsync();
		using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult);
		if (stream != null)
		{
			await using var writer = new Utf8JsonWriter(stream, Data.JsonWriterOptions);
			await reader.ReadJson(result, writer);
			await writer.FlushAsync();
			return null;
		}
		else
		{
			var bufferWriter = new ArrayBufferWriter<byte>(1024);
			using var writer = new Utf8JsonWriter(bufferWriter, Data.JsonWriterOptions);
			await reader.ReadJson(result, writer);
			writer.Flush();
			return Encoding.UTF8.GetString(bufferWriter.WrittenSpan);
		}
	}

	internal async static Task ReadJson(this DbDataReader reader, JsonValueType result, Utf8JsonWriter writer)
	{
		switch (result)
		{
			case JsonValueType.Object:
				if (await reader.ReadAsync())
				{
					var (count, encodedNames, colTypes) = reader.GetJsonSchema();
					WriteObject(count, encodedNames, colTypes);
				}
				else
					writer.WriteNullValue();
				break;
			case JsonValueType.Array:
				writer.WriteStartArray();
				if (await reader.ReadAsync())
				{
					var (count, encodedNames, colTypes) = reader.GetJsonSchema();
					WriteObject(count, encodedNames, colTypes);
					while (await reader.ReadAsync())
						WriteObject(count, encodedNames, colTypes);
				}
				writer.WriteEndArray();
				break;
			default:
				throw new ArgumentException("The only valid JSON results are array and object.");
		}
		await writer.FlushAsync();

		void WriteObject(int count, JsonEncodedText[] encodedNames, Data.JsonColType[] colTypes)
		{
			writer.WriteStartObject();
			for (int i = 0; i < count; i++)
			{
				writer.WritePropertyName(encodedNames[i]);
				writer.WriteDbValue(reader, colTypes[i], i);
			}
			writer.WriteEndObject();
		}
	}
}
