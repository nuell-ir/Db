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

        internal static SaveParams SaveQuery(JsonObject obj, string table)
        {
            string idKey = 
                obj.ContainsKey("Id") ? "Id" : 
                obj.ContainsKey("id") ? "id" :
                obj.ContainsKey("ID") ? "ID" : null;
                
            int id = obj[idKey].GetValue<int>();
            var sqlParams = new List<SqlParameter>();
            var str = new StringBuilder();
            if (id == 0)
            {
                str.Append("INSERT INTO ");
                str.Append(table);
                str.Append('(');
                foreach (var p in obj)
                    if (p.Key != idKey)
                    {
                        str.Append('[');
                        str.Append(p.Key);
                        str.Append("],");
                    }
                str.Remove(str.Length - 1, 1);
                str.Append(") VALUES (");
                foreach (var p in obj)
                    if (p.Key != idKey)
                    {
                        AppendValue(p);
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
                foreach (var p in obj)
                    if (p.Key != idKey)
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
                if (prop.Value is null)
                    str.Append("NULL");
                else switch (prop.Value.GetValue<JsonElement>().ValueKind)
                    {
                        case JsonValueKind.Number:
                            str.Append(prop.Value.ToString());
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
                            sqlParams.Add(new SqlParameter(paramName, prop.Value.GetValue<string>()));
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
        public static int Save(JsonObject json, string table)
            => Save(Data.SaveQuery(json, table));

        public static int Save(JsonElement json, string table)
            => Save(System.Text.Json.Nodes.JsonObject.Create(json), table);

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
        public static Task<int> Save(JsonObject json, string table)
            => Save(Data.SaveQuery(json, table));

        public static Task<int> Save(JsonElement json, string table)
            => Save(System.Text.Json.Nodes.JsonObject.Create(json), table);

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