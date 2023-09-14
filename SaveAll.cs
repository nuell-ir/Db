using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace nuell
{
	internal static partial class SaveAllQuery
	{
		internal static string Create(JsonElement json, string deleteIds, string table, string idProp)
		{
			var str = new StringBuilder("BEGIN TRAN;");

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
				str.Append("INSERT INTO ");
				str.Append(table);
				str.Append('(');
				foreach (var prop in props)
				{
					str.Append('[');
					str.Append(prop);
					str.Append("],");
				}
				str.Remove(str.Length - 1, 1);
				str.Append(") VALUES ");
				foreach (var itm in insertItems)
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

			str.Append("COMMIT;");

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
}

namespace nuell.Sync
{
	public static partial class Db
	{
		public static int SaveAll(JsonElement json, string deleteIds, string table, string idProp = "Id")
		{
			using var cnnct = new SqlConnection(Data.ConnectionString);
			using var cmnd = new SqlCommand(SaveAllQuery.Create(json, deleteIds, table, idProp), cnnct);
			cnnct.Open();
			return cmnd.ExecuteNonQuery();
		}
	}
}

namespace nuell.Async
{
	public static partial class Db
	{
		public static async Task<int> SaveAll(JsonElement json, string deleteIds, string table, string idProp = "Id")
		{
			using var cnnct = new SqlConnection(Data.ConnectionString);
			using var cmnd = new SqlCommand(SaveAllQuery.Create(json, deleteIds, table, idProp), cnnct);
			await cnnct.OpenAsync();
			return await cmnd.ExecuteNonQueryAsync();
		}
	}
}