using System.Data;
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

        internal static SaveParams SaveQuery((string Name, JsonElement Value)[] props, string table)
        {
            int idIndex = Array.FindIndex<(string Name, JsonElement Value)>(props, prop => string.Compare(prop.Name, "Id", true) == 0);
            int id = props[idIndex].Value.GetInt32();
            var sqlParams = new List<SqlParameter>();
            SqlParameter param;
            var str = new StringBuilder();
            if (id == 0)
            {
                str.Append("INSERT INTO ");
                str.Append(table);
                str.Append('(');
                for (int i = 0; i < props.Length; i++)
                    if (i != idIndex)
                    {
                        str.Append('[');
                        str.Append(props[i].Name);
                        str.Append("],");
                    }
                str.Remove(str.Length - 1, 1);
                str.Append(") VALUES (");
                for (int i = 0; i < props.Length; i++)
                    if (i != idIndex)
                    {
                        AppendValue(props[i]);
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
                for (int i = 0; i < props.Length; i++)
                    if (i != idIndex)
                    {
                        str.Append('[');
                        str.Append(props[i].Name);
                        str.Append("]=");
                        AppendValue(props[i]);
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

            void AppendValue((string Name, JsonElement Value) prop)
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
                        param = new SqlParameter("@" + prop.Name, prop.Value.GetString());
                        str.Append(param.ParameterName);
                        sqlParams.Add(param);
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
            => Save(Data.SaveQuery(json.Select(p => (p.Key, p.Value.GetValue<JsonElement>())).ToArray(), table));

        public static int Save(JsonElement json, string table)
            => Save(Data.SaveQuery(json.EnumerateObject().Select(p => (p.Name, p.Value)).ToArray(), table));

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
            => Save(Data.SaveQuery(json.Select(p => (p.Key, p.Value.GetValue<JsonElement>())).ToArray(), table));

        public static Task<int> Save(JsonElement json, string table)
            => Save(Data.SaveQuery(json.EnumerateObject().Select(p => (p.Name, p.Value)).ToArray(), table));

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