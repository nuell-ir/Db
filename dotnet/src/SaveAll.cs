using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace nuel;

internal static partial class SaveAllQuery
{
	internal static string Create(JsonElement json, string deleteIds, string table, string idProp)
	{
		if (json.ValueKind != JsonValueKind.Array)
			throw new ArgumentException("The json parameter must be a JSON array.", nameof(json));

		var str = new StringBuilder("SET XACT_ABORT ON; BEGIN TRY BEGIN TRAN;");

		if (!string.IsNullOrWhiteSpace(deleteIds))
		{
			if (!CommaSeparatedIntegers().IsMatch(deleteIds))
				throw new InvalidDataException("Wrong format of delete IDs");

			str.Append("DELETE FROM ");
			str.Append(table);
			str.Append(" WHERE [");
			str.Append(idProp);
			str.Append("] in (");
			str.Append(deleteIds);
			str.Append(");");
		}

		var props = new List<string>();
		var insertItems = new List<JsonElement>();

		int id;
		bool first = true;
		foreach (var itm in json.EnumerateArray())
		{
			if (first)
			{
				foreach (var p in itm.EnumerateObject())
					if (p.Name != idProp)
						props.Add(p.Name);
				first = false;
			}
			id = itm.GetProperty(idProp).GetInt32();
			if (id == 0)
			{
				insertItems.Add(itm);
				continue;
			}
			str.Append("UPDATE ");
			str.Append(table);
			str.Append(" SET ");
			foreach (string prop in props)
			{
				str.Append('[');
				str.Append(prop);
				str.Append("]=");
				AppendValue(itm.GetProperty(prop));
				str.Append(',');
			}
			str.Remove(str.Length - 1, 1);
			str.Append(" WHERE [");
			str.Append(idProp);
			str.Append("]=");
			str.Append(id);
			str.Append(';');
		}

		if (insertItems.Count > 0)
		{
			var insertHeader = new StringBuilder("INSERT INTO ")
				.Append(table)
				.Append('(');
			foreach (var prop in props)
			{
				insertHeader.Append('[').Append(prop).Append("],");
			}
			insertHeader.Remove(insertHeader.Length - 1, 1);
			insertHeader.Append(") VALUES ");
			string insertHeaderSql = insertHeader.ToString();

			foreach (var chunk in insertItems.Chunk(1000))
			{
				str.Append(insertHeaderSql);
				foreach (var itm in chunk)
				{
					str.Append('(');
					foreach (string prop in props)
					{
						AppendValue(itm.GetProperty(prop));
						str.Append(',');
					}
					str.Remove(str.Length - 1, 1);
					str.Append("),");
				}
				str.Remove(str.Length - 1, 1);
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
					str.Append(e);
					break;
				case JsonValueKind.String:
					str.Append($"N'{e.GetString().Replace("'", "''")}'");
					break;
				case JsonValueKind.True:
					str.Append(1);
					break;
				case JsonValueKind.False:
					str.Append(0);
					break;
				case JsonValueKind.Null:
					str.Append("NULL");
					break;
			}
		}
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
	public static async Task<int> SaveAll(JsonElement json, string deleteIds, string table, string idProp = "Id")
	{
		using var connection = new SqlConnection(Data.ConnectionString);
		using var cmd = new SqlCommand(SaveAllQuery.Create(json, deleteIds, table, idProp), connection);
		await connection.OpenAsync();
		return await cmd.ExecuteNonQueryAsync();
	}
}
