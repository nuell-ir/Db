using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;

namespace nuel
{
	public static partial class Data
	{
		internal static JsonNode GetJsonNode(this DbDataReader reader, Type dataType, int i)
		{
			if (reader.IsDBNull(i))
				return null;

			var typeCode = Type.GetTypeCode(dataType);
			switch (typeCode)
			{
				case TypeCode.Int32: return reader.GetInt32(i);
				case TypeCode.Int64: return reader.GetInt64(i);
				case TypeCode.Int16: return reader.GetInt16(i);
				case TypeCode.Byte: return reader.GetByte(i);
				case TypeCode.Single: return reader.GetFloat(i);
				case TypeCode.Double: return reader.GetDouble(i);
				case TypeCode.Decimal: return reader.GetDecimal(i);
				case TypeCode.DateTime: return reader.GetDateTime(i);
				case TypeCode.Boolean: return reader.GetBoolean(i);
				case TypeCode.Char: return reader.GetChar(i);
				case TypeCode.String: return reader.GetString(i);
			}

			if (dataType == typeof(Guid))
				return JsonValue.Create(reader.GetGuid(i).ToString());
			if (dataType == typeof(DateTimeOffset))
				return JsonValue.Create((reader is SqlDataReader sdr ? sdr.GetDateTimeOffset(i) : reader.GetFieldValue<DateTimeOffset>(i)).ToString("o"));
			if (dataType == typeof(TimeSpan))
				return JsonValue.Create((reader is SqlDataReader sdr ? sdr.GetTimeSpan(i) : reader.GetFieldValue<TimeSpan>(i)).ToString());
			if (dataType == typeof(byte[]))
				return JsonValue.Create(Convert.ToBase64String((byte[])reader.GetValue(i)));

			throw new NotSupportedException($"Type '{dataType.FullName}' is not supported.");
		}

		internal static JsonObject GetJsonObject(this DbDataReader reader, ReadOnlyCollection<DbColumn> columns)
		{
			var obj = new System.Text.Json.Nodes.JsonObject();
			for (int i = 0; i < columns.Count; i++)
			{
				var dataType = columns[i].DataType ?? reader.GetFieldType(i);
				obj[reader.GetName(i)] = reader.GetJsonNode(dataType, i);
			}
			return obj;
		}
	}
}

namespace nuel.Sync
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
			using var connection = new SqlConnection(Data.ConnectionString);
			using var cmd = new SqlCommand(query, connection);
			if (isStoredProc)
				cmd.CommandType = CommandType.StoredProcedure;
			cmd.Parameters.AddRange(parameters);
			connection.Open();
			using var reader = cmd.ExecuteReader();
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

namespace nuel.Async
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
			using var connection = new SqlConnection(Data.ConnectionString);
			using var cmd = new SqlCommand(query, connection);
			if (isStoredProc)
				cmd.CommandType = CommandType.StoredProcedure;
			cmd.Parameters.AddRange(parameters);
			await connection.OpenAsync();
			using var reader = await cmd.ExecuteReaderAsync();
			if (reader.HasRows)
			{
				await reader.ReadAsync();
				return reader.GetJsonObject(await reader.GetColumnSchemaAsync());
			}
			else
				return default;
		}
	}
}