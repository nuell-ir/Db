using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;

namespace nuel;

internal readonly struct SaveParams(int id, string query, SqlParameter[] sqlParams)
{
	public int Id { get; init; } = id;
	public string Query { get; init; } = query;
	public SqlParameter[] SqlParams { get; init; } = sqlParams;
}

internal static partial class SaveQuery
{
	internal static SaveParams Create(JsonElement json, string table, string idProp)
	{
		int id = 0;
		if (json.TryGetProperty(idProp, out var idElement))
		{
			if (idElement.ValueKind == JsonValueKind.Number && idElement.TryGetInt32(out int parsedInt))
				id = parsedInt;
			else if (idElement.ValueKind == JsonValueKind.String && int.TryParse(idElement.GetString(), out int parsedStrId))
				id = parsedStrId;
		}

		var sqlParams = new List<SqlParameter>();
		var str = new StringBuilder(256);
		if (id == 0)
		{
			str.Append("INSERT INTO ").Append(table).Append(" (");
			var vals = new StringBuilder(128);
			bool first = true;

			foreach (var prop in json.EnumerateObject())
			{
				if (prop.NameEquals(idProp))
					continue;

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

			str.Append(") VALUES (").Append(vals).Append("); SELECT CAST(SCOPE_IDENTITY() AS int);");
		}
		else
		{
			str.Append("UPDATE ").Append(table).Append(" SET ");
			bool first = true;

			foreach (var prop in json.EnumerateObject())
			{
				if (prop.NameEquals(idProp))
					continue;

				if (!first)
					str.Append(',');
				first = false;

				string paramName = "@" + prop.Name;
				str.Append('[').Append(prop.Name).Append("]=").Append(paramName);
				sqlParams.Add(CreateSqlParameter(paramName, prop.Value));
			}

			string idParamName = "@" + idProp;
			str.Append(" WHERE [").Append(idProp).Append("]=").Append(idParamName);
			sqlParams.Add(new SqlParameter(idParamName, id));
		}

		return new SaveParams(id, str.ToString(), [.. sqlParams]);

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

	internal static SaveParams Create(JsonObject json, string table, string idProp)
	{
		int id = 0;
		if (json.TryGetPropertyValue(idProp, out var idNode) && idNode != null)
		{
			if (idNode.GetValueKind() == JsonValueKind.Number && idNode is JsonValue jvNum && jvNum.TryGetValue(out int numVal))
				id = numVal;
			else if (int.TryParse(idNode.ToString(), out int parsedId))
				id = parsedId;
		}

		var sqlParams = new List<SqlParameter>(json.Count);
		var str = new StringBuilder(256);
		if (id == 0)
		{
			str.Append("INSERT INTO ").Append(table).Append(" (");
			var vals = new StringBuilder(128);
			bool first = true;

			foreach (var prop in json)
			{
				if (prop.Key == idProp)
					continue;

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

			str.Append(") VALUES (").Append(vals).Append("); SELECT CAST(SCOPE_IDENTITY() AS int);");
		}
		else
		{
			str.Append("UPDATE ").Append(table).Append(" SET ");
			bool first = true;

			foreach (var prop in json)
			{
				if (prop.Key == idProp)
					continue;

				if (!first)
					str.Append(',');
				first = false;

				string paramName = "@" + prop.Key;
				str.Append('[').Append(prop.Key).Append("]=").Append(paramName);
				sqlParams.Add(CreateSqlParameter(paramName, prop.Value));
			}

			string idParamName = "@" + idProp;
			str.Append(" WHERE [").Append(idProp).Append("]=").Append(idParamName);
			sqlParams.Add(new SqlParameter(idParamName, id));
		}

		return new SaveParams(id, str.ToString(), [.. sqlParams]);

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
	/// <summary>Saves the JsonNode via an insert or update operation.</summary>
	/// <remarks>If the value of the identity field is 0, the values will be inserted as a record; otherwise, a record with the specified identity will be updated.</remarks>
	/// <returns>The identity of the inserted/updated record, or 0 if no record was updated.</returns>
	/// <param name="json">The JsonNode to save</param>
	/// <param name="table">Table name</param>   
	/// <param name="idProp">The name of the identity field, the value of which decides whether to insert or update the record</param>   
	public static Task<int> Save(JsonNode json, string table, string idProp = "Id")
		 => Save(SaveQuery.Create(json.AsObject(), table, idProp));

	/// <summary>Saves the JsonObject via an insert or update operation.</summary>
	/// <remarks>If the value of the identity field is 0, the values will be inserted as a record; otherwise, a record with the specified identity will be updated.</remarks>
	/// <returns>The identity of the inserted/updated record, or 0 if no record was updated.</returns>
	/// <param name="json">The JsonObject to save</param>
	/// <param name="table">Table name</param>   
	/// <param name="idProp">The name of the identity field, the value of which decides whether to insert or update the record</param>   
	public static Task<int> Save(JsonObject json, string table, string idProp = "Id")
		 => Save(SaveQuery.Create(json, table, idProp));

	/// <summary>Saves the JsonElement via an insert or update operation.</summary>
	/// <remarks>If the value of the identity field is 0, the values will be inserted as a record; otherwise, a record with the specified identity will be updated.</remarks>
	/// <returns>The identity of the inserted/updated record, or 0 if no record was updated.</returns>
	/// <param name="json">The JsonElement to save</param>
	/// <param name="table">Table name</param>   
	/// <param name="idProp">The name of the identity field, the value of which decides whether to insert or update the record</param>   
	public static Task<int> Save(JsonElement json, string table, string idProp = "Id")
		 => Save(SaveQuery.Create(json, table, idProp));

	private static async Task<int> Save(SaveParams param)
	{
		using var connection = new SqlConnection(Data.ConnectionString);
		using var cmd = new SqlCommand(param.Query, connection);
		if (param.SqlParams.Length > 0)
			cmd.Parameters.AddRange(param.SqlParams);
		await connection.OpenAsync();
		if (param.Id == 0)
			return Convert.ToInt32(await cmd.ExecuteScalarAsync());

		return await cmd.ExecuteNonQueryAsync() > 0 ? param.Id : 0;
	}
}
