using System.Data;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace nuell
{
	public static partial class Data
	{
		internal static (int, string[], TypeCode[]) GetSchema(this SqlDataReader reader)
		{
			int count = reader.FieldCount;
			var fieldTypes = new TypeCode[count];
			var fieldNames = new string[count];
			for (int i = 0; i < count; i++)
			{
				fieldTypes[i] = Type.GetTypeCode(reader.GetFieldType(i));
				fieldNames[i] = reader.GetName(i);
			}
			return (count, fieldNames, fieldTypes);
		}

		internal static void WriteDbValue(this Utf8JsonWriter writer, SqlDataReader reader, TypeCode typeCode, int columnIndex)
		{
			switch (typeCode)
			{
				case TypeCode.Int32:
					writer.WriteNumberValue(reader.GetInt32(columnIndex));
					break;
				case TypeCode.Int16:
					writer.WriteNumberValue(reader.GetInt16(columnIndex));
					break;
				case TypeCode.Byte:
					writer.WriteNumberValue(reader.GetByte(columnIndex));
					break;
				case TypeCode.Int64:
					writer.WriteNumberValue(reader.GetInt64(columnIndex));
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
			using var cnnct = new SqlConnection(Data.ConnectionString);
			using var cmnd = new SqlCommand(query, cnnct);
			if (isStoredProc)
				cmnd.CommandType = CommandType.StoredProcedure;
			cmnd.Parameters.AddRange(parameters);
			cnnct.Open();
			using var reader = cmnd.ExecuteReader();
			using var stream = new MemoryStream();
			using var writer = new Utf8JsonWriter(stream, Data.JsonWriterOptions);
			reader.ReadJson(result, stream, writer);
			writer.Flush();
			return Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
		}

		internal static void ReadJson(this SqlDataReader reader, JsonValueType result, MemoryStream stream, Utf8JsonWriter writer)
		{
			string[] fieldNames;
			TypeCode[] fieldTypes;
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
			using var cnnct = new SqlConnection(Data.ConnectionString);
			using var cmnd = new SqlCommand(query, cnnct);
			if (isStoredProc)
				cmnd.CommandType = CommandType.StoredProcedure;
			cmnd.Parameters.AddRange(parameters);
			await cnnct.OpenAsync();
			using var reader = await cmnd.ExecuteReaderAsync();
			using var stream = new MemoryStream();
			using var writer = new Utf8JsonWriter(stream, Data.JsonWriterOptions);
			await reader.ReadJson(result, stream, writer);
			writer.Flush();
			return Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
		}

		internal async static Task ReadJson(this SqlDataReader reader, JsonValueType result, MemoryStream stream, Utf8JsonWriter writer)
		{
			string[] fieldNames;
			TypeCode[] fieldTypes;
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