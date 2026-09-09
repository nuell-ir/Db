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
				obj[reader.GetName(i)] = reader.IsDBNull(i) ? null :
				Type.GetTypeCode(columns[i].DataType) switch
				{
					TypeCode.Int32 => reader.GetInt32(i),
					TypeCode.Int64 => reader.GetInt64(i),
					TypeCode.Int16 => reader.GetInt16(i),
					TypeCode.Byte => reader.GetByte(i),
					TypeCode.Single => reader.GetFloat(i),
					TypeCode.Double => reader.GetDouble(i),
					TypeCode.Decimal => reader.GetDecimal(i),
					TypeCode.DateTime => reader.GetDateTime(i),
					TypeCode.Boolean => reader.GetBoolean(i),
					TypeCode.Char => reader.GetChar(i),
					TypeCode.String => reader.GetString(i),
				};
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