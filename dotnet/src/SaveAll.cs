using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace nuel;

internal static partial class SaveAllQuery
{
	internal static string Create(JsonElement json, string deleteIds, string table, string idProp = "Id")
	{
		if (json.ValueKind != JsonValueKind.Array)
			throw new ArgumentException("The json parameter must be a JSON array.", nameof(json));

		int count = json.GetArrayLength();
		int estimatedCapacity = Math.Max(256, count * 96);
		if (!string.IsNullOrWhiteSpace(deleteIds))
			estimatedCapacity += deleteIds.Length + 64;

		var str = new StringBuilder(estimatedCapacity);
		str.Append("SET XACT_ABORT ON; BEGIN TRY BEGIN TRAN;");

		if (!string.IsNullOrWhiteSpace(deleteIds))
		{
			if (!CommaSeparatedIntegers().IsMatch(deleteIds))
				throw new InvalidDataException("Wrong format of delete IDs");

			str.Append("DELETE FROM ").Append(table).Append(" WHERE [").Append(idProp).Append("] in (").Append(deleteIds).Append(");");
		}

		List<string> props = null;
		var insertItems = new List<JsonElement>(count);

		foreach (var itm in json.EnumerateArray())
		{
			int id = 0;
			if (itm.TryGetProperty(idProp, out var idElement))
			{
				if (idElement.ValueKind == JsonValueKind.Number && idElement.TryGetInt32(out int parsedInt))
					id = parsedInt;
				else if (idElement.ValueKind == JsonValueKind.String && int.TryParse(idElement.GetString(), out int parsedStrId))
					id = parsedStrId;
			}

			if (id == 0)
			{
				if (props is null)
				{
					props = new List<string>();
					foreach (var p in itm.EnumerateObject())
						if (!p.NameEquals(idProp))
							props.Add(p.Name);
				}
				insertItems.Add(itm);
				continue;
			}

			str.Append("UPDATE ").Append(table).Append(" SET ");
			bool firstCol = true;
			foreach (var p in itm.EnumerateObject())
			{
				if (p.NameEquals(idProp))
					continue;

				if (!firstCol)
					str.Append(',');
				firstCol = false;

				str.Append('[').Append(p.Name).Append("]=");
				AppendValue(p.Value);
			}
			str.Append(" WHERE [").Append(idProp).Append("]=").Append(id).Append(';');
		}

		if (insertItems.Count > 0)
		{
			var insertHeader = new StringBuilder(64 + props.Count * 16);
			insertHeader.Append("INSERT INTO ").Append(table).Append(" (");
			bool firstCol = true;
			foreach (var prop in props)
			{
				if (!firstCol)
					insertHeader.Append(',');
				firstCol = false;
				insertHeader.Append('[').Append(prop).Append(']');
			}
			insertHeader.Append(") VALUES ");
			string insertHeaderSql = insertHeader.ToString();

			foreach (var chunk in insertItems.Chunk(1000))
			{
				str.Append(insertHeaderSql);
				bool firstRow = true;
				foreach (var itm in chunk)
				{
					if (!firstRow)
						str.Append(',');
					firstRow = false;

					str.Append('(');
					bool firstVal = true;
					foreach (string prop in props)
					{
						if (!firstVal)
							str.Append(',');
						firstVal = false;

						if (itm.TryGetProperty(prop, out var val))
							AppendValue(val);
						else
							str.Append("NULL");
					}
					str.Append(')');
				}
				str.Append(';');
			}
		}

		str.Append("COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT > 0 ROLLBACK; THROW; END CATCH;");

		return str.ToString();

		void AppendValue(JsonElement e)
		{
			switch (e.ValueKind)
			{
				case JsonValueKind.Number:
					if (e.TryGetInt32(out int iVal))
						str.Append(iVal);
					else if (e.TryGetInt64(out long lVal))
						str.Append(lVal);
					else
						str.Append(e.GetRawText());
					break;

				case JsonValueKind.String:
					string s = e.GetString();
					if (s is null)
					{
						str.Append("NULL");
					}
					else
					{
						str.Append("N'");
						if (s.IndexOf('\'') >= 0)
							str.Append(s.Replace("'", "''"));
						else
							str.Append(s);
						str.Append('\'');
					}
					break;

				case JsonValueKind.True:
					str.Append('1');
					break;

				case JsonValueKind.False:
					str.Append('0');
					break;

				case JsonValueKind.Null:
					str.Append("NULL");
					break;

				default:
					string raw = e.GetRawText();
					str.Append("N'").Append(raw.Replace("'", "''")).Append('\'');
					break;
			}
		}
	}

	internal static string Create(JsonArray json, string deleteIds, string table, string idProp = "Id")
	{
		ArgumentNullException.ThrowIfNull(json);

		int count = json.Count;
		int estimatedCapacity = Math.Max(256, count * 96);
		if (!string.IsNullOrWhiteSpace(deleteIds))
			estimatedCapacity += deleteIds.Length + 64;

		var str = new StringBuilder(estimatedCapacity);
		str.Append("SET XACT_ABORT ON; BEGIN TRY BEGIN TRAN;");

		if (!string.IsNullOrWhiteSpace(deleteIds))
		{
			if (!CommaSeparatedIntegers().IsMatch(deleteIds))
				throw new InvalidDataException("Wrong format of delete IDs");

			str.Append("DELETE FROM ").Append(table).Append(" WHERE [").Append(idProp).Append("] in (").Append(deleteIds).Append(");");
		}

		List<string> props = null;
		var insertItems = new List<JsonObject>(count);

		foreach (var node in json)
		{
			if (node is not JsonObject itm)
				continue;

			int id = 0;
			if (itm.TryGetPropertyValue(idProp, out var idNode) && idNode != null)
			{
				if (idNode.GetValueKind() == JsonValueKind.Number && idNode is JsonValue jvNum && jvNum.TryGetValue(out int numVal))
					id = numVal;
				else if (int.TryParse(idNode.ToString(), out int parsedId))
					id = parsedId;
			}

			if (id == 0)
			{
				if (props is null)
				{
					props = new List<string>(itm.Count);
					foreach (var prop in itm)
						if (prop.Key != idProp)
							props.Add(prop.Key);
				}
				insertItems.Add(itm);
				continue;
			}

			str.Append("UPDATE ").Append(table).Append(" SET ");
			bool firstCol = true;
			foreach (var prop in itm)
			{
				if (prop.Key == idProp)
					continue;

				if (!firstCol)
					str.Append(',');
				firstCol = false;

				str.Append('[').Append(prop.Key).Append("]=");
				AppendValue(prop.Value);
			}
			str.Append(" WHERE [").Append(idProp).Append("]=").Append(id).Append(';');
		}

		if (insertItems.Count > 0)
		{
			var insertHeader = new StringBuilder(64 + props.Count * 16);
			insertHeader.Append("INSERT INTO ").Append(table).Append(" (");
			bool firstCol = true;
			foreach (var prop in props)
			{
				if (!firstCol)
					insertHeader.Append(',');
				firstCol = false;
				insertHeader.Append('[').Append(prop).Append(']');
			}
			insertHeader.Append(") VALUES ");
			string insertHeaderSql = insertHeader.ToString();

			foreach (var chunk in insertItems.Chunk(1000))
			{
				str.Append(insertHeaderSql);
				bool firstRow = true;
				foreach (var itm in chunk)
				{
					if (!firstRow)
						str.Append(',');
					firstRow = false;

					str.Append('(');
					bool firstVal = true;
					foreach (string prop in props)
					{
						if (!firstVal)
							str.Append(',');
						firstVal = false;

						if (itm.TryGetPropertyValue(prop, out var val))
							AppendValue(val);
						else
							str.Append("NULL");
					}
					str.Append(')');
				}
				str.Append(';');
			}
		}

		str.Append("COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT > 0 ROLLBACK; THROW; END CATCH;");

		return str.ToString();

		void AppendValue(JsonNode node)
		{
			if (node is null)
			{
				str.Append("NULL");
				return;
			}

			switch (node.GetValueKind())
			{
				case JsonValueKind.Number:
					if (node is JsonValue jvNum)
					{
						if (jvNum.TryGetValue(out int iVal))
						{
							str.Append(iVal);
							return;
						}
						if (jvNum.TryGetValue(out long lVal))
						{
							str.Append(lVal);
							return;
						}
						if (jvNum.TryGetValue(out decimal dVal))
						{
							str.Append(dVal.ToString(CultureInfo.InvariantCulture));
							return;
						}
						if (jvNum.TryGetValue(out double dblVal))
						{
							str.Append(dblVal.ToString(CultureInfo.InvariantCulture));
							return;
						}
					}
					str.Append(node.ToString());
					break;

				case JsonValueKind.String:
					string s = node is JsonValue jvStr && jvStr.TryGetValue(out string sVal) ? sVal : node.GetValue<string>();
					if (s is null)
					{
						str.Append("NULL");
					}
					else
					{
						str.Append("N'");
						if (s.IndexOf('\'') >= 0)
							str.Append(s.Replace("'", "''"));
						else
							str.Append(s);
						str.Append('\'');
					}
					break;

				case JsonValueKind.True:
					str.Append('1');
					break;

				case JsonValueKind.False:
					str.Append('0');
					break;

				case JsonValueKind.Null:
					str.Append("NULL");
					break;

				default:
					string raw = node.ToJsonString();
					str.Append("N'").Append(raw.Replace("'", "''")).Append('\'');
					break;
			}
		}
	}

	internal static string Create(JsonNode json, string deleteIds, string table, string idProp = "Id")
	{
		ArgumentNullException.ThrowIfNull(json);
		if (json is not JsonArray array)
			throw new ArgumentException("The json parameter must be a JSON array.", nameof(json));
		return Create(array, deleteIds, table, idProp);
	}

	[GeneratedRegex("^(\\d+,)*\\d+$")]
	private static partial Regex CommaSeparatedIntegers();
}

public static partial class Db
{
	/// <summary>Asynchronously executes a batch save operation that deletes records with matching IDs and inserts or updates records from a JSON array in a transaction.</summary>
	/// <param name="json">The JSON array containing records to insert (if ID is 0) or update (if ID > 0).</param>
	/// <param name="deleteIds">A comma-separated string of record IDs to delete, or null/empty if none to delete.</param>
	/// <param name="table">The name of the database table.</param>
	/// <param name="idProp">The name of the identity/primary key property. Defaults to "Id".</param>
	/// <returns>A task representing the asynchronous operation, returning the total number of rows affected by the delete, update, and insert operations.</returns>
	public static Task<int> SaveAll(JsonNode json, string deleteIds, string table, string idProp = "Id")
	{
		ArgumentNullException.ThrowIfNull(json);
		if (json is not JsonArray array)
			throw new ArgumentException("The json parameter must be a JSON array.", nameof(json));
		return SaveAll(array, deleteIds, table, idProp);
	}

	/// <summary>Asynchronously executes a batch save operation that deletes records with matching IDs and inserts or updates records from a JSON array in a transaction.</summary>
	/// <param name="json">The JSON array containing records to insert (if ID is 0) or update (if ID > 0).</param>
	/// <param name="deleteIds">A comma-separated string of record IDs to delete, or null/empty if none to delete.</param>
	/// <param name="table">The name of the database table.</param>
	/// <param name="idProp">The name of the identity/primary key property. Defaults to "Id".</param>
	/// <returns>A task representing the asynchronous operation, returning the total number of rows affected by the delete, update, and insert operations.</returns>
	public static Task<int> SaveAll(JsonArray json, string deleteIds, string table, string idProp = "Id")
		 => SaveAllInternal(SaveAllQuery.Create(json, deleteIds, table, idProp));

	/// <summary>Asynchronously executes a batch save operation that deletes records with matching IDs and inserts or updates records from a JSON array in a transaction.</summary>
	/// <param name="json">The JSON array containing records to insert (if ID is 0) or update (if ID > 0).</param>
	/// <param name="deleteIds">A comma-separated string of record IDs to delete, or null/empty if none to delete.</param>
	/// <param name="table">The name of the database table.</param>
	/// <param name="idProp">The name of the identity/primary key property. Defaults to "Id".</param>
	/// <returns>A task representing the asynchronous operation, returning the total number of rows affected by the delete, update, and insert operations.</returns>
	public static Task<int> SaveAll(JsonElement json, string deleteIds, string table, string idProp = "Id")
		 => SaveAllInternal(SaveAllQuery.Create(json, deleteIds, table, idProp));

	private static async Task<int> SaveAllInternal(string query)
	{
		await using var connection = new SqlConnection(Data.ConnectionString);
		using var cmd = new SqlCommand(query, connection);
		await connection.OpenAsync();
		return await cmd.ExecuteNonQueryAsync();
	}
}
