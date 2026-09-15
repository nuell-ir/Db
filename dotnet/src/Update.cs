using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;

namespace nuel;

public static class UpdateQuery
{
	internal static (string Query, SqlParameter[] SqlParams) Create(JsonElement json, string table, string primaryKey)
	{
		JsonProperty primaryKeyProp = default;
		bool hasPk = false;
		var sqlParams = new List<SqlParameter>();
		var str = new StringBuilder(256);

		str.Append("UPDATE ").Append(table).Append(" SET ");
		bool first = true;

		foreach (var p in json.EnumerateObject())
		{
			if (p.NameEquals(primaryKey))
			{
				primaryKeyProp = p;
				hasPk = true;
				continue;
			}

			if (!first)
				str.Append(',');
			first = false;

			string paramName = "@" + p.Name;
			str.Append('[').Append(p.Name).Append("]=").Append(paramName);
			sqlParams.Add(CreateSqlParameter(paramName, p.Value));
		}

		if (!hasPk || primaryKeyProp.Value.ValueKind == JsonValueKind.Undefined)
			throw new ArgumentException($"{primaryKey} property was not provided");

		string pkParamName = "@" + primaryKey;
		str.Append(" WHERE [").Append(primaryKey).Append("]=").Append(pkParamName);
		sqlParams.Add(CreateSqlParameter(pkParamName, primaryKeyProp.Value));

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

	internal static (string Query, SqlParameter[] SqlParams) Create(JsonObject json, string table, string primaryKey)
	{
		JsonNode primaryKeyNode = null;
		bool hasPk = false;
		var sqlParams = new List<SqlParameter>(json.Count);
		var str = new StringBuilder(256);

		str.Append("UPDATE ").Append(table).Append(" SET ");
		bool first = true;

		foreach (var p in json)
		{
			if (p.Key == primaryKey)
			{
				primaryKeyNode = p.Value;
				hasPk = true;
				continue;
			}

			if (!first)
				str.Append(',');
			first = false;

			string paramName = "@" + p.Key;
			str.Append('[').Append(p.Key).Append("]=").Append(paramName);
			sqlParams.Add(CreateSqlParameter(paramName, p.Value));
		}

		if (!hasPk)
			throw new ArgumentException($"{primaryKey} property was not provided");

		string pkParamName = "@" + primaryKey;
		str.Append(" WHERE [").Append(primaryKey).Append("]=").Append(pkParamName);
		sqlParams.Add(CreateSqlParameter(pkParamName, primaryKeyNode));

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
	/// <summary>Asynchronously updates a database record using the properties from a <see cref="JsonNode"/> matching the specified primary key.</summary>
	/// <param name="json">The JSON node containing the column names and updated values, including the primary key property.</param>
	/// <param name="table">The name of the database table.</param>
	/// <param name="primaryKey">The name of the primary key column.</param>
	/// <returns>A task representing the asynchronous operation, returning the number of rows affected.</returns>
	public static Task<int> Update(JsonNode json, string table, string primaryKey)
		=> Update(UpdateQuery.Create(json.AsObject(), table, primaryKey));

	/// <summary>Asynchronously updates a database record using the properties from a <see cref="System.Text.Json.Nodes.JsonObject"/> matching the specified primary key.</summary>
	/// <param name="json">The JSON object containing the column names and updated values, including the primary key property.</param>
	/// <param name="table">The name of the database table.</param>
	/// <param name="primaryKey">The name of the primary key column.</param>
	/// <returns>A task representing the asynchronous operation, returning the number of rows affected.</returns>
	public static Task<int> Update(JsonObject json, string table, string primaryKey)
		=> Update(UpdateQuery.Create(json, table, primaryKey));

	/// <summary>Asynchronously updates a database record using the properties from a <see cref="JsonElement"/> matching the specified primary key.</summary>
	/// <param name="json">The JSON element containing the column names and updated values, including the primary key property.</param>
	/// <param name="table">The name of the database table.</param>
	/// <param name="primaryKey">The name of the primary key column.</param>
	/// <returns>A task representing the asynchronous operation, returning the number of rows affected.</returns>
	public static Task<int> Update(JsonElement json, string table, string primaryKey)
		=> Update(UpdateQuery.Create(json, table, primaryKey));

	private static async Task<int> Update((string Query, SqlParameter[] SqlParams) param)
	{
		await using var connection = new SqlConnection(Data.ConnectionString);
		using var cmd = new SqlCommand(param.Query, connection);
		if (param.SqlParams.Length > 0)
			cmd.Parameters.AddRange(param.SqlParams);
		await connection.OpenAsync();
		return await cmd.ExecuteNonQueryAsync();
	}
}
