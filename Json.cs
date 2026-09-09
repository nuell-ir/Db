using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace nuell
{
	public static partial class Data
	{
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

		internal static void WriteDbValue(this Utf8JsonWriter writer, DbDataReader reader, Type type, int columnIndex)
		{
			if (reader.IsDBNull(columnIndex))
			{
				writer.WriteNullValue();
				return;
			}

			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Int32:
					writer.WriteNumberValue(reader.GetInt32(columnIndex));
					return;
				case TypeCode.Int16:
					writer.WriteNumberValue(reader.GetInt16(columnIndex));
					return;
				case TypeCode.Byte:
					writer.WriteNumberValue(reader.GetByte(columnIndex));
					return;
				case TypeCode.Int64:
					writer.WriteNumberValue(reader.GetInt64(columnIndex));
					return;
				case TypeCode.Single:
					writer.WriteNumberValue(reader.GetFloat(columnIndex));
					return;
				case TypeCode.Double:
					writer.WriteNumberValue(reader.GetDouble(columnIndex));
					return;
				case TypeCode.Decimal:
					writer.WriteNumberValue(reader.GetDecimal(columnIndex));
					return;
				case TypeCode.DateTime:
					writer.WriteStringValue(reader.GetDateTime(columnIndex));
					return;
				case TypeCode.Boolean:
					writer.WriteBooleanValue(reader.GetBoolean(columnIndex));
					return;
				case TypeCode.Char:
				case TypeCode.String:
					writer.WriteStringValue(reader.GetString(columnIndex));
					return;
			}

			if (type == typeof(Guid))
			{
				writer.WriteStringValue(reader.GetGuid(columnIndex));
				return;
			}
			if (type == typeof(DateTimeOffset))
			{
				writer.WriteStringValue(reader is SqlDataReader sdr ? sdr.GetDateTimeOffset(columnIndex) : reader.GetFieldValue<DateTimeOffset>(columnIndex));
				return;
			}
			if (type == typeof(TimeSpan))
			{
				writer.WriteStringValue((reader is SqlDataReader sdr ? sdr.GetTimeSpan(columnIndex) : reader.GetFieldValue<TimeSpan>(columnIndex)).ToString());
				return;
			}
			if (type == typeof(byte[]))
			{
				writer.WriteBase64StringValue((byte[])reader.GetValue(columnIndex));
				return;
			}

			throw new NotSupportedException($"Type '{type.FullName}' is not supported.");
		}
	}
}

namespace nuell.Sync
{
	public static partial class Db
	{
		/// <summary>Converts the first row of the query result to a JSON object.</summary>
		public static string Json(string query, params (string name, object value)[] parameters)
		=> Json(query, JsonValueType.Object, false, Data.SqlParams(parameters));

		/// <summary>Converts the query results to JSON.</summary>
		/// <param name="result">returned result type as JSON object (the first row) or array (all the rows))</param>        
		public static string Json(string query, JsonValueType result, params (string name, object value)[] parameters)
		=> Json(query, result, false, Data.SqlParams(parameters));

		/// <summary>Converts the query results to JSON.</summary>
		/// <param name="isStoredProc">is the query a stored procedure</param>        
		public static string Json(string query, bool isStoredProc, params (string name, object value)[] parameters)
			 => Json(query, JsonValueType.Object, isStoredProc, Data.SqlParams(parameters));

		/// <summary>Converts the query results to JSON.</summary>
		/// <param name="result">returned result type as JSON object (the first row) or array (all the rows))</param>        
		/// <param name="isStoredProc">is the query a stored procedure</param>        
		public static string Json(string query, JsonValueType result, bool isStoredProc, params (string name, object value)[] parameters)
			 => Json(query, result, isStoredProc, Data.SqlParams(parameters));

		/// <summary>Converts the query results to JSON.</summary>
		/// <param name="result">returned result type as JSON object (the first row) or array (all the rows))</param>        
		/// <param name="isStoredProc">is the query a stored procedure</param>    
		public static string Json(string query, JsonValueType result = JsonValueType.Object, bool isStoredProc = false)
			 => Json(query, result, isStoredProc, Data.NoParams);

		/// <summary>Converts the query results to JSON.</summary>
		/// <param name="result">returned result type as JSON object (the first row) or array (all the rows))</param>        
		/// <param name="isStoredProc">is the query a stored procedure</param>        
		public static string Json(string query, JsonValueType result, bool isStoredProc, params SqlParameter[] parameters)
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
			reader.ReadJson(result, stream, writer);
			writer.Flush();
			return Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
		}

		internal static void ReadJson(this SqlDataReader reader, JsonValueType result, MemoryStream stream, Utf8JsonWriter writer)
		{
			string[] fieldNames;
			Type[] fieldTypes;
			int count;

			switch (result)
			{
				case JsonValueType.Object:
					if (reader.Read())
					{
						(count, fieldNames, fieldTypes) = reader.GetSchema();
						WriteObject();
					}
					else
						writer.WriteNullValue();
					break;
				case JsonValueType.Array:
					writer.WriteStartArray();
					if (reader.Read())
					{
						(count, fieldNames, fieldTypes) = reader.GetSchema();
						WriteObject();
						while (reader.Read())
							WriteObject();
					}
					writer.WriteEndArray();
					break;
				default:
					throw new ArgumentException("The only valid JSON results are array and object.");
			}

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
		/// <summary>Converts the first row of the query result to a JSON object.</summary>
		public static Task<string> Json(string query, params (string name, object value)[] parameters)
			 => Json(query, JsonValueType.Object, false, Data.SqlParams(parameters));

		/// <summary>Converts the query results to JSON.</summary>
		/// <param name="result">returned result type as JSON object (the first row) or array (all the rows))</param>        
		public static Task<string> Json(string query, JsonValueType result, params (string name, object value)[] parameters)
		=> Json(query, result, false, Data.SqlParams(parameters));

		/// <summary>Converts the query results to JSON.</summary>
		/// <param name="isStoredProc">is the query a stored procedure</param>   
		public static Task<string> Json(string query, bool isStoredProc, params (string name, object value)[] parameters)
		=> Json(query, JsonValueType.Object, isStoredProc, Data.SqlParams(parameters));

		/// <summary>Converts the query results to JSON.</summary>
		/// <param name="result">returned result type as JSON object (the first row) or array (all the rows))</param>        
		/// <param name="isStoredProc">is the query a stored procedure</param>   
		public static Task<string> Json(string query, JsonValueType result, bool isStoredProc, params (string name, object value)[] parameters)
		=> Json(query, result, isStoredProc, Data.SqlParams(parameters));

		/// <summary>Converts the query results to JSON.</summary>
		/// <param name="result">returned result type as JSON object (the first row) or array (all the rows))</param>        
		/// <param name="isStoredProc">is the query a stored procedure</param>   
		public static Task<string> Json(string query, JsonValueType result = JsonValueType.Object, bool isStoredProc = false)
			 => Json(query, result, isStoredProc, Data.NoParams);

		/// <summary>Converts the query results to JSON.</summary>
		/// <param name="result">returned result type as JSON object (the first row) or array (all the rows))</param>        
		/// <param name="isStoredProc">is the query a stored procedure</param>   
		public async static Task<string> Json(string query, JsonValueType result, bool isStoredProc, params SqlParameter[] parameters)
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
			await reader.ReadJson(result, stream, writer);
			writer.Flush();
			return Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
		}

		internal async static Task ReadJson(this SqlDataReader reader, JsonValueType result, MemoryStream stream, Utf8JsonWriter writer)
		{
			string[] fieldNames;
			Type[] fieldTypes;
			int count;

			switch (result)
			{
				case JsonValueType.Object:
					if (await reader.ReadAsync())
					{
						(count, fieldNames, fieldTypes) = reader.GetSchema();
						WriteObject();
					}
					else
						writer.WriteNullValue();
					break;
				case JsonValueType.Array:
					writer.WriteStartArray();
					if (await reader.ReadAsync())
					{
						(count, fieldNames, fieldTypes) = reader.GetSchema();
						WriteObject();
						while (await reader.ReadAsync())
							WriteObject();
					}
					writer.WriteEndArray();
					break;
				default:
					throw new ArgumentException("The only valid JSON results are array and object.");
			}
			await writer.FlushAsync();

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