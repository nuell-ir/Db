using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;

namespace nuel;

public static partial class Data
{
	internal static (string Query, SqlParameter[] SqlParams) InsertQuery(JsonElement json, string table)
	{
		var sqlParams = new List<SqlParameter>();
		var str = new StringBuilder(256);
		str.Append("INSERT INTO ").Append(table).Append(" (");
		var vals = new StringBuilder(128);
		bool first = true;

		foreach (var prop in json.EnumerateObject())
		{
			if (!first)
			{
				str.Append(',');
				vals.Append(',');
			}
			first = false;

			str.Append('[').Append(prop.Name).Append(']');
			string paramName = "@" + prop.Name;
			vals.Append(paramName);
			sqlParams.Add(CreateSqlParameter(paramName, prop.Value));
		}

		str.Append(") VALUES (").Append(vals).Append(')');

		return (str.ToString(), [.. sqlParams]);

		static SqlParameter CreateSqlParameter(string paramName, JsonElement value)
		{
			switch (value.ValueKind)
			{
				case JsonValueKind.Number:
					if (value.TryGetInt32(out int iVal))
						return new SqlParameter(paramName, iVal);
					if (value.TryGetInt64(out long lVal))
						return new SqlParameter(paramName, lVal);
					if (value.TryGetDecimal(out decimal dVal))
						return new SqlParameter(paramName, dVal);
					return new SqlParameter(paramName, value.GetDouble());

				case JsonValueKind.True:
					return new SqlParameter(paramName, true);

				case JsonValueKind.False:
					return new SqlParameter(paramName, false);

				case JsonValueKind.Null:
					return new SqlParameter(paramName, DBNull.Value);

				case JsonValueKind.String:
					return new SqlParameter(paramName, (object)value.GetString() ?? DBNull.Value);

				default:
					return new SqlParameter(paramName, value.GetRawText());
			}
		}
	}

	internal static (string Query, SqlParameter[] SqlParams) InsertQuery(JsonObject json, string table)
	{
		var sqlParams = new List<SqlParameter>(json.Count);
		var str = new StringBuilder(256);
		str.Append("INSERT INTO ").Append(table).Append(" (");
		var vals = new StringBuilder(128);
		bool first = true;

		foreach (var prop in json)
		{
			if (!first)
			{
				str.Append(',');
				vals.Append(',');
			}
			first = false;

			str.Append('[').Append(prop.Key).Append(']');
			string paramName = "@" + prop.Key;
			vals.Append(paramName);
			sqlParams.Add(CreateSqlParameter(paramName, prop.Value));
		}

		str.Append(") VALUES (").Append(vals).Append(')');

		return (str.ToString(), [.. sqlParams]);

		static SqlParameter CreateSqlParameter(string paramName, JsonNode node)
		{
			if (node is null)
				return new SqlParameter(paramName, DBNull.Value);

			switch (node.GetValueKind())
			{
				case JsonValueKind.Number:
					if (node is JsonValue jvNum)
					{
						if (jvNum.TryGetValue(out int iVal))
							return new SqlParameter(paramName, iVal);
						if (jvNum.TryGetValue(out long lVal))
							return new SqlParameter(paramName, lVal);
						if (jvNum.TryGetValue(out decimal dVal))
							return new SqlParameter(paramName, dVal);
						if (jvNum.TryGetValue(out double dblVal))
							return new SqlParameter(paramName, dblVal);
					}
					return new SqlParameter(paramName, node.AsValue().GetValue<object>());

				case JsonValueKind.True:
					return new SqlParameter(paramName, true);

				case JsonValueKind.False:
					return new SqlParameter(paramName, false);

				case JsonValueKind.Null:
					return new SqlParameter(paramName, DBNull.Value);

				case JsonValueKind.String:
					if (node is JsonValue jvStr && jvStr.TryGetValue(out string sVal))
						return new SqlParameter(paramName, (object)sVal ?? DBNull.Value);
					return new SqlParameter(paramName, (object)node.GetValue<string>() ?? DBNull.Value);

				default:
					return new SqlParameter(paramName, node.ToJsonString());
			}
		}
	}
}

public static partial class Db
{
	/// <summary>Asynchronously inserts a record represented by a <see cref="JsonNode"/> into the specified database table.</summary>
	/// <param name="json">The JSON node containing the column names and values to insert.</param>
	/// <param name="table">The name of the database table.</param>
	/// <returns>A task representing the asynchronous operation, returning the number of rows affected.</returns>
	public static Task<int> Insert(JsonNode json, string table)
		=> Insert(Data.InsertQuery(json.AsObject(), table));

	/// <summary>Asynchronously inserts a record represented by a <see cref="System.Text.Json.Nodes.JsonObject"/> into the specified database table.</summary>
	/// <param name="json">The JSON object containing the column names and values to insert.</param>
	/// <param name="table">The name of the database table.</param>
	/// <returns>A task representing the asynchronous operation, returning the number of rows affected.</returns>
	public static Task<int> Insert(JsonObject json, string table)
		=> Insert(Data.InsertQuery(json, table));

	/// <summary>Asynchronously inserts a record represented by a <see cref="JsonElement"/> into the specified database table.</summary>
	/// <param name="json">The JSON element containing the column names and values to insert.</param>
	/// <param name="table">The name of the database table.</param>
	/// <returns>A task representing the asynchronous operation, returning the number of rows affected.</returns>
	public static Task<int> Insert(JsonElement json, string table)
		=> Insert(Data.InsertQuery(json, table));

	private static async Task<int> Insert((string Query, SqlParameter[] SqlParams) param)
	{
		await using var connection = new SqlConnection(Data.ConnectionString);
		using var cmd = new SqlCommand(param.Query, connection);
		if (param.SqlParams.Length > 0)
			cmd.Parameters.AddRange(param.SqlParams);
		await connection.OpenAsync();
		return await cmd.ExecuteNonQueryAsync();
	}
}
