using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;

namespace nuell
{
    public static partial class Data
    {
        internal static (string Query, SqlParameter[] SqlParams) UpdateQuery(JsonElement json, string table, string primaryKey)
        {
            JsonProperty primaryKeyProp = new();
            var sqlParams = new List<SqlParameter>();
            var str = new StringBuilder();

            str.Append("UPDATE ");
            str.Append(table);
            str.Append(" SET ");
            foreach (var p in json.EnumerateObject())
                if (p.Name == primaryKey)
                    primaryKeyProp = p;
                else
                {
                    str.Append('[');
                    str.Append(p.Name);
                    str.Append("]=");
                    AppendValue(p);
                    str.Append(',');
                }
            str.Remove(str.Length - 1, 1);
            str.Append(" WHERE [");
            str.Append(primaryKey);
            str.Append("]=");
            if (primaryKeyProp.Value.ValueKind != JsonValueKind.Undefined && primaryKeyProp.Name == primaryKey)
                AppendValue(primaryKeyProp);
            else
                throw new ArgumentException($"{primaryKey} property was not provided");

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

        internal static (string Query, SqlParameter[] SqlParams) UpdateQuery(JsonObject json, string table, string primaryKey)
        {
            KeyValuePair<string, JsonNode?> primaryKeyProp = new();
            var sqlParams = new List<SqlParameter>();
            var str = new StringBuilder();

            str.Append("UPDATE ");
            str.Append(table);
            str.Append(" SET ");
            foreach (var p in json)
                if (p.Key == primaryKey)
                    primaryKeyProp = p;
                else
                {
                    str.Append('[');
                    str.Append(p.Key);
                    str.Append("]=");
                    AppendValue(p);
                    str.Append(',');
                }
            str.Remove(str.Length - 1, 1);
            str.Append(" WHERE [");
            str.Append(primaryKey);
            str.Append("]=");
            if (primaryKeyProp.Key == primaryKey)
                AppendValue(primaryKeyProp);
            else
                throw new ArgumentException($"{primaryKey} property was not provided");

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
        public static int Update(JsonNode json, string table, string primaryKey)
            => Update(Data.UpdateQuery(json.AsObject(), table, primaryKey));

        public static int Update(JsonObject json, string table, string primaryKey)
            => Update(Data.UpdateQuery(json, table, primaryKey));

        public static int Update(JsonElement json, string table, string primaryKey)
            => Update(Data.UpdateQuery(json, table, primaryKey));

        private static int Update((string Query, SqlParameter[] SqlParams) param)
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
        public static Task<int> Update(JsonNode json, string table, string primaryKey)
            => Update(Data.UpdateQuery(json.AsObject(), table, primaryKey));

        public static Task<int> Update(JsonObject json, string table, string primaryKey)
            => Update(Data.UpdateQuery(json, table, primaryKey));

        public static Task<int> Update(JsonElement json, string table, string primaryKey)
            => Update(Data.UpdateQuery(json, table, primaryKey));

        private static async Task<int> Update((string Query, SqlParameter[] SqlParams) param)
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