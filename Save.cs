using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace nuell
{
    public static partial class Data
    {
        internal static object JsonProp(JsonElement prop)
            => prop.ValueKind switch
            {
                JsonValueKind.Null => DBNull.Value,
                JsonValueKind.Number => prop.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => prop.GetString(),
            };
    }
}

namespace nuell.Sync
{
    public static partial class Db
    {
        public static int Save(JsonElement json, string table)
            => Save(json.EnumerateObject().Select(p => (p.Name, Data.JsonProp(p.Value))), table);

        public static int Save(object obj, string table)
            => Save(obj.GetType().GetProperties().Select(p => (p.Name, p.GetValue(obj))), table);

        private static int Save(IEnumerable<(string Name, object Value)> props, string table)
        {
            var idProp = props.Where(prop => string.Compare(prop.Name, "Id", true) == 0);
            int id = Convert.ToInt32(idProp.First().Value);
            props = props.Except(idProp);
            var sqlParams = props.Select(prop => new SqlParameter("@" + prop.Name, prop.Value ?? DBNull.Value)).ToArray();
            if (id == 0)
                using (var cnnct = new SqlConnection(Data.ConnectionString))
                {
                    using var cmnd = cnnct.CreateCommand();
                    cmnd.CommandText = string.Format("insert into {0} ({1}) values ({2})",
                        table,
                        string.Join(',', props.Select(prop => $"[{prop.Name}]")),
                        string.Join(',', props.Select(prop => $"@{prop.Name}")));
                    cmnd.Parameters.AddRange(sqlParams);

                    cnnct.Open();
                    cmnd.ExecuteNonQuery();
                    cmnd.CommandText = "select @@identity";
                    id = Convert.ToInt32(cmnd.ExecuteScalar());
                }
            else
                Execute(
                    string.Format("update {0} set {1} where Id=" + id,
                        table,
                        string.Join(',', props.Select(prop => $"[{prop.Name}]=@{prop.Name}"))),
                    false,
                    sqlParams);

            return id;
        }
    }
}

namespace nuell.Async
{
    public static partial class Db
    {
        public static Task<int> Save(JsonElement json, string table)
            => Save(json.EnumerateObject().Select(p => (p.Name, Data.JsonProp(p.Value))), table);

        public static Task<int> Save(object obj, string table)
            => Save(obj.GetType().GetProperties().Select(p => (p.Name, p.GetValue(obj))), table);

        private async static Task<int> Save(IEnumerable<(string Name, object Value)> props, string table)
        {
            var idProp = props.Where(prop => string.Compare(prop.Name, "Id", true) == 0);
            int id = Convert.ToInt32(idProp.First().Value);
            props = props.Except(idProp);
            var sqlParams = props.Select(prop => new SqlParameter("@" + prop.Name, prop.Value)).ToArray();
            if (id == 0)
                using (var cnnct = new SqlConnection(Data.ConnectionString))
                {
                    using var cmnd = cnnct.CreateCommand();
                    cmnd.CommandText = string.Format("insert into [{0}] ({1}) values ({2})",
                        table,
                        string.Join(',', props.Select(prop => $"[{prop.Name}]")),
                        string.Join(',', props.Select(prop => "@" + prop.Name)));
                    cmnd.Parameters.AddRange(sqlParams);

                    await cnnct.OpenAsync();
                    await cmnd.ExecuteNonQueryAsync();
                    cmnd.CommandText = "select @@identity";
                    id = Convert.ToInt32(await cmnd.ExecuteScalarAsync());
                }
            else
                await Execute(
                    string.Format("update [{0}] set {1} where Id=" + id,
                        table,
                        string.Join(',', props.Select(prop => $"[{prop.Name}]=@{prop.Name}"))),
                    false,
                    sqlParams);

            return id;
        }
    }
}