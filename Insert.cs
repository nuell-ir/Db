using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;

namespace nuell
{
	public static partial class Data
	{
		internal static (string Query, SqlParameter[] SqlParams) InsertQuery(JsonElement json, string table)
		{
			var sqlParams = new List<SqlParameter>();
			var str = new StringBuilder();

			str.Append("INSERT INTO ");
			str.Append(table);
			str.Append('(');
			foreach (var prop in json.EnumerateObject())
			{
				str.Append('[');
				str.Append(prop.Name);
				str.Append("],");
			}
			str.Remove(str.Length - 1, 1);
			str.Append(") VALUES (");
			foreach (var prop in json.EnumerateObject())
			{
				AppendValue(prop);
				str.Append(',');
			}
			str.Remove(str.Length - 1, 1);
			str.Append(')');

			return (str.ToString(), sqlParams.ToArray());

			void AppendValue(JsonProperty prop)
			{
				switch (prop.Value.ValueKind)
				{
					case JsonValueKind.Number:
						str.Append(prop.Value);
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
					case JsonValueKind.String:
						string paramName = $"@{prop.Name}";
						str.Append(paramName);
						sqlParams.Add(new SqlParameter(paramName, prop.Value.GetString()));
						break;
				}
			}
		}

		internal static (string Query, SqlParameter[] SqlParams) InsertQuery(JsonObject json, string table)
		{
			var sqlParams = new List<SqlParameter>();
			var str = new StringBuilder();

			str.Append("INSERT INTO ");
			str.Append(table);
			str.Append('(');
			foreach (var prop in json)
			{
				str.Append('[');
				str.Append(prop.Key);
				str.Append("],");
			}
			str.Remove(str.Length - 1, 1);
			str.Append(") VALUES (");
			foreach (var prop in json)
			{
				AppendValue(prop);
				str.Append(',');
			}
			str.Remove(str.Length - 1, 1);
			str.Append(')');

			return (str.ToString(), sqlParams.ToArray());

			void AppendValue(KeyValuePair<string, JsonNode?> prop)
			{
				JsonNode val = prop.Value;

				if (val is null)
				{
					str.Append("NULL");
					return;
				}

				// Because setting JsonObject.index[] does not automatically convert POCO values to JsonElement,
				// if a value is assigned in the code, it should be manually converted to JsonElement first.
				// But to check whether a value is JsonElement or an assigned PCOO value, 
				// 'is JsonElement' can't be applied to JsonValue, 
				// so this is to check the value type:
				if (!val.AsValue().TryGetValue(out JsonElement _))
					val = JsonNode.Parse(val.ToJsonString());

				switch (val.GetValue<JsonElement>().ValueKind)
				{
					case JsonValueKind.Number:
						str.Append(val);
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
					case JsonValueKind.String:
						string paramName = $"@{prop.Key}";
						str.Append(paramName);
						sqlParams.Add(new SqlParameter(paramName, (string)val));
						break;
				}
			}
		}
	}
}

namespace nuell.Sync
{
	public static partial class Db
	{
		public static int Insert(JsonNode json, string table)
			=> Insert(Data.InsertQuery(json.AsObject(), table));

		public static int Insert(JsonObject json, string table)
			=> Insert(Data.InsertQuery(json, table));

		public static int Insert(JsonElement json, string table)
			=> Insert(Data.InsertQuery(json, table));

		private static int Insert((string Query, SqlParameter[] SqlParams) param)
		{
			using var cnnct = new SqlConnection(Data.ConnectionString);
			using var cmnd = new SqlCommand(param.Query, cnnct);
			if (param.SqlParams.Length > 0)
				cmnd.Parameters.AddRange(param.SqlParams);
			cnnct.Open();
			return cmnd.ExecuteNonQuery();
		}
	}
}

namespace nuell.Async
{
	public static partial class Db
	{
		public static Task<int> Insert(JsonNode json, string table)
			=> Insert(Data.InsertQuery(json.AsObject(), table));

		public static Task<int> Insert(JsonObject json, string table)
			=> Insert(Data.InsertQuery(json, table));

		public static Task<int> Insert(JsonElement json, string table)
			=> Insert(Data.InsertQuery(json, table));

		private static async Task<int> Insert((string Query, SqlParameter[] SqlParams) param)
		{
			using var cnnct = new SqlConnection(Data.ConnectionString);
			using var cmnd = new SqlCommand(param.Query, cnnct);
			if (param.SqlParams.Length > 0)
				cmnd.Parameters.AddRange(param.SqlParams);
			await cnnct.OpenAsync();
			return await cmnd.ExecuteNonQueryAsync();
		}
	}
}