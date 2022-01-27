using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;

namespace nuell
{
    public static partial class Data
    {
        internal class SaveParams
        {
            public int Id;
            public string Query;
            public SqlParameter[] SqlParams;
        }

        internal static SaveParams SaveQuery(JsonElement json, string table, string idProp)
        {
            int id = json.GetProperty(idProp).GetInt32();
            var sqlParams = new List<SqlParameter>();
            var str = new StringBuilder();
            if (id == 0)
            {
                str.Append("INSERT INTO ");
                str.Append(table);
                str.Append('(');
                foreach (var prop in json.EnumerateObject())
                    if (prop.Name != idProp)
                    {
                        str.Append('[');
                        str.Append(prop.Name);
                        str.Append("],");
                    }
                str.Remove(str.Length - 1, 1);
                str.Append(") VALUES (");
                foreach (var prop in json.EnumerateObject())
                    if (prop.Name != idProp)
                    {
                        AppendValue(prop);
                        str.Append(',');
                    }
                str.Remove(str.Length - 1, 1);
                str.Append(')');
            }
            else
            {
                str.Append("UPDATE ");
                str.Append(table);
                str.Append(" SET ");
                foreach (var p in json.EnumerateObject())
                    if (p.Name != idProp)
                    {
                        str.Append('[');
                        str.Append(p.Name);
                        str.Append("]=");
                        AppendValue(p);
                        str.Append(',');
                    }
                str.Remove(str.Length - 1, 1);
                str.Append(" WHERE Id=");
                str.Append(id);
            }
            return new SaveParams
            {
                Id = id,
                Query = str.ToString(),
                SqlParams = sqlParams.ToArray()
            };

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

        internal static SaveParams SaveQuery(JsonObject json, string table, string idProp)
        {
            int id = json[idProp].GetValue<int>();
            var sqlParams = new List<SqlParameter>();
            var str = new StringBuilder();
            if (id == 0)
            {
                str.Append("INSERT INTO ");
                str.Append(table);
                str.Append('(');
                foreach (var prop in json)
                    if (prop.Key != idProp)
                    {
                        str.Append('[');
                        str.Append(prop.Key);
                        str.Append("],");
                    }
                str.Remove(str.Length - 1, 1);
                str.Append(") VALUES (");
                foreach (var prop in json)
                    if (prop.Key != idProp)
                    {
                        AppendValue(prop);
                        str.Append(',');
                    }
                str.Remove(str.Length - 1, 1);
                str.Append(')');
            }
            else
            {
                str.Append("UPDATE ");
                str.Append(table);
                str.Append(" SET ");
                foreach (var p in json)
                    if (p.Key != idProp)
                    {
                        str.Append('[');
                        str.Append(p.Key);
                        str.Append("]=");
                        AppendValue(p);
                        str.Append(',');
                    }
                str.Remove(str.Length - 1, 1);
                str.Append(" WHERE Id=");
                str.Append(id);
            }
            return new SaveParams
            {
                Id = id,
                Query = str.ToString(),
                SqlParams = sqlParams.ToArray()
            };

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
        public static int Save(JsonNode json, string table, string idProp = "Id")
            => Save(Data.SaveQuery(json.AsObject(), table, idProp));

        public static int Save(JsonObject json, string table, string idProp = "Id")
            => Save(Data.SaveQuery(json, table, idProp));

        public static int Save(JsonElement json, string table, string idProp = "Id")
            => Save(Data.SaveQuery(json, table, idProp));

        private static int Save(Data.SaveParams param)
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(param.Query, cnnct);
            if (param.SqlParams.Length > 0)
                cmnd.Parameters.AddRange(param.SqlParams);
            cnnct.Open();
            cmnd.ExecuteNonQuery();
            if (param.Id == 0)
            {
                cmnd.CommandText = "select @@identity";
                param.Id = Convert.ToInt32(cmnd.ExecuteScalar());
            }
            return param.Id;
        }
    }
}

namespace nuell.Async
{
    public static partial class Db
    {
        public static Task<int> Save(JsonNode json, string table, string idProp = "Id")
            => Save(Data.SaveQuery(json.AsObject(), table, idProp));
            
        public static Task<int> Save(JsonObject json, string table, string idProp = "Id")
            => Save(Data.SaveQuery(json, table, idProp));

        public static Task<int> Save(JsonElement json, string table, string idProp = "Id")
            => Save(Data.SaveQuery(json, table, idProp));

        private static async Task<int> Save(Data.SaveParams param)
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(param.Query, cnnct);
            if (param.SqlParams.Length > 0)
                cmnd.Parameters.AddRange(param.SqlParams);
            await cnnct.OpenAsync();
            await cmnd.ExecuteNonQueryAsync();
            if (param.Id == 0)
            {
                cmnd.CommandText = "select @@identity";
                param.Id = Convert.ToInt32(await cmnd.ExecuteScalarAsync());
            }
            return param.Id;
        }
    }
}