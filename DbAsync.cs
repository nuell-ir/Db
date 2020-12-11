using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace nuell.Async
{
    public static class Db
    {
        public static Task<DataTable> Table(string query, params SqlParameter[] parameters)
            => Table(query, false, parameters);

        public static async Task<DataTable> Table(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnStr);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            var dt = new DataTable();
            await cnnct.OpenAsync();
            using var reader = await cmnd.ExecuteReaderAsync();
            if (reader.HasRows)
                dt.Load(reader);
            return dt;
        }

        public static Task<JObject> JObject(string query, params SqlParameter[] parameters)
            => JObject(query, isStoredProc: false, parameters);

        public async static Task<JObject> JObject(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnStr);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            await cnnct.OpenAsync();
            using var reader = await cmnd.ExecuteReaderAsync();
            if (reader.HasRows)
            {
                await reader.ReadAsync();
                var json = new JObject();
                for (int i = 0; i < reader.VisibleFieldCount; i++)
                    json.Add(reader.GetName(i), JToken.FromObject(reader.GetValue(i)));

                return json;
            }
            else
                return null;
        }

        public async static Task<string> Json(string query, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnStr);
            using var cmnd = new SqlCommand(query, cnnct);
            cmnd.Parameters.AddRange(parameters);
            await cnnct.OpenAsync();
            using var reader = await cmnd.ExecuteReaderAsync();
            if (reader.HasRows)
            {
                await reader.ReadAsync();
                var str = new StringBuilder();
                var sw = new StringWriter(str);
                using var writer = new JsonTextWriter(sw);
                writer.WriteStartObject();
                int count = reader.VisibleFieldCount;
                for (int i = 0; i < count; i++)
                {
                    writer.WritePropertyName(reader.GetName(i));
                    writer.WriteValue(reader.GetValue(i));
                }
                writer.WriteEndObject();
                return str.ToString();
            }
            else
                return null;
        }

        public static Task<string> Csv(string query, params SqlParameter[] parameters)
            => Csv(query, false, parameters);

        public static async Task<string> Csv(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnStr);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            await cnnct.OpenAsync();
            using var reader = await cmnd.ExecuteReaderAsync();
            return await ReadCsvResult(reader);
        }
        public static async Task<string[]> MultiCsv(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnStr);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            await cnnct.OpenAsync();
            using var reader = await cmnd.ExecuteReaderAsync();
            var results = new List<string>
            {
                (await ReadCsvResult(reader)).ToString()
            };
            while (await reader.NextResultAsync())
                results.Add(await ReadCsvResult(reader));
            return results.ToArray();
        }

        private static async Task<string> ReadCsvResult(SqlDataReader reader)
        {
            if (!reader.HasRows)
                return null;
            const char sep = '~';
            const char line = '|';
            var str = new StringBuilder();
            int count = reader.VisibleFieldCount;
            for (int i = 0; i < count; i++)
            {
                var fieldType = reader.GetFieldType(i);
                if (fieldType == typeof(string) || fieldType == typeof(TimeSpan) || fieldType == typeof(byte[]))
                    str.Append('$');
                else if (fieldType == typeof(DateTime))
                    str.Append('#');
                str.Append(reader.GetName(i));
                str.Append(sep);
            }
            str.Remove(str.Length - 1, 1);
            str.Append(line);
            while (await reader.ReadAsync())
            {
                for (int i = 0; i < count; i++)
                {
                    var val = reader.GetValue(i);
                    if (val is bool b)
                        str.Append(b ? "true" : "false"); //to prevent boolean capitalisation ('True' is not true in javascript)
                    else if (val is DateTime d)
                    {
                        if (d.Ticks == 0)
                            str.Append(d.ToString("yyyy-MM-dd"));
                        else
                            str.Append(d.ToString("yyyy-MM-ddTHH:mm:ss"));
                    }
                    else if (val is TimeSpan t)
                        str.Append(t.ToString(@"hh\:mm\:ss"));
                    else if (val is byte[] bytes)
                        str.Append(Convert.ToBase64String(bytes));
                    else
                        str.Append(val);
                    str.Append(sep);
                }
                str.Remove(str.Length - 1, 1);
                str.Append(line);
            }
            str.Remove(str.Length - 1, 1);
            return str.ToString();
        }

        public static Task<int> Execute(string query, params SqlParameter[] parameters)
            => Execute(query, false, parameters);

        public async static Task<int> Execute(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnStr);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            await cnnct.OpenAsync();
            return await cmnd.ExecuteNonQueryAsync();
        }

        public static Task<int[]> Transaction(string queries)
            => Transaction(queries.Split(new string[] { "GO", ";" }, StringSplitOptions.RemoveEmptyEntries));

        public static async Task<int[]> Transaction(IEnumerable<string> queries)
        {
            if (queries == null)
                return null;
            var result = new int[queries.Count()];
            using (var cnnct = new SqlConnection(Data.ConnStr))
            {
                using var cmnd = cnnct.CreateCommand();
                await cnnct.OpenAsync();
                using var transaction = cnnct.BeginTransaction();
                cmnd.Transaction = transaction;
                int i = 0;
                foreach (string query in queries)
                {
                    cmnd.CommandText = query;
                    result[i++] = await cmnd.ExecuteNonQueryAsync();
                }
                await transaction.CommitAsync();
            }
            return result;
        }

        public static Task<JObject> Get(int id, string table)
            => JObject($"select top 1 * from {table} where Id={id}");

        public static Task<T> GetVal<T>(string query, params SqlParameter[] parameters) where T : struct
            => GetVal<T>(query, false, parameters);

        public async static Task<T> GetVal<T>(string query, bool isStoredProc, params SqlParameter[] parameters) where T : struct
        {
            using var cnnct = new SqlConnection(Data.ConnStr);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            await cnnct.OpenAsync();
            var val = await cmnd.ExecuteScalarAsync();
            return val is null ? default : (T)Convert.ChangeType(val, typeof(T));
        }

        public static Task<object[]> GetValues(string query, params SqlParameter[] parameters)
            => GetValues(query, false, parameters);
        
        public async static Task<object[]> GetValues(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnStr);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            await cnnct.OpenAsync();
            using var reader = await cmnd.ExecuteReaderAsync();
            var results = new List<object>();
            await AddValues();
            while (await reader.NextResultAsync())
                await AddValues();
            return results.ToArray();

            async Task AddValues()
            {
                if (await reader.ReadAsync())
                {
                    var values = new object[reader.FieldCount];
                    reader.GetValues(values);
                    results.AddRange(values);
                }
            }
        }

        public async static Task<string> GetStr(string query)
        {
            using var cnnct = new SqlConnection(Data.ConnStr);
            using var cmnd = new SqlCommand(query, cnnct);
            await cnnct.OpenAsync();
            var val = await cmnd.ExecuteScalarAsync();
            return val is DBNull ? null : val.ToString();
        }

        public async static Task<bool> Delete(int id, string table)
        {
            try
            {
                return await Execute($"delete from [{table}] where Id={id}") == 1;
            }
            catch
            {
                return false;
            }
        }

        public async static Task<int> Save(JObject jsonEntity, string table)
        {
            var props =
                from prop in jsonEntity.Properties()
                where prop.Name != "Id"
                select new
                {
                    Name = $"[{prop.Name}]",
                    ParamName = "@" + prop.Name,
                    Value = new SqlParameter
                    {
                        ParameterName = "@" + prop.Name,
                        Value = prop.Value.Type switch
                        {
                            JTokenType.Null => DBNull.Value,
                            JTokenType.String => (string)prop.Value,
                            JTokenType.Integer => (long)prop.Value,
                            JTokenType.Float => (float)prop.Value,
                            JTokenType.Boolean => (bool)prop.Value,
                            JTokenType.Date => (DateTime)prop.Value,
                            JTokenType.TimeSpan => (TimeSpan)prop.Value,
                            JTokenType.Bytes => (byte[])prop.Value
                        }
                    }
                };
            int id = (int)jsonEntity.Properties().First(p => p.Name == "Id").Value;
            if (id == 0)
                using (var cnnct = new SqlConnection(Data.ConnStr))
                {
                    using var cmnd = cnnct.CreateCommand();
                    cmnd.CommandText = string.Format("insert into [{0}] ({1}) values ({2})",
                        table,
                        string.Join(',', props.Select(prop => prop.Name)),
                        string.Join(',', props.Select(prop => prop.ParamName)));
                    cmnd.Parameters.AddRange(props.Select(prop => prop.Value).ToArray());

                    await cnnct.OpenAsync();
                    await cmnd.ExecuteNonQueryAsync();
                    cmnd.CommandText = "select @@identity";
                    id = Convert.ToInt32(await cmnd.ExecuteScalarAsync());
                }
            else
                await Execute(
                    string.Format("update [{0}] set {1} where Id=" + id,
                        table,
                        string.Join(',', props.Select(prop => prop.Name + '=' + prop.ParamName))),
                    props.Select(prop => prop.Value).ToArray());

            return id;
        }

        public async static Task<JObject> NewItem(string table)
        {
            var regDefault = new Regex(@"\((?:(?:N?'([^']+)')|(?:\(([^()]+)\)))\)", RegexOptions.Compiled);
            // ((some number)) or (N'some text') or ('some text')
            var item = new JObject();
            using var cnnct = new SqlConnection(Data.ConnStr);
            using var cmnd = new SqlCommand($@"select COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT
                    from INFORMATION_SCHEMA.COLUMNS where TABLE_NAME='{table}' order by ORDINAL_POSITION", cnnct);
            await cnnct.OpenAsync();
            using var reader = await cmnd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string col = reader.GetString("COLUMN_NAME");
                var colDefault = reader.GetValue("COLUMN_DEFAULT");
                if (!Convert.IsDBNull(colDefault))
                {
                    var match = regDefault.Match(colDefault.ToString());
                    string defaultVal = match.Groups[match.Groups[1].Success ? 1 : 2].Value;
                    switch (reader.GetString("DATA_TYPE"))
                    {
                        case "int":
                        case "smallint":
                        case "tinyint":
                        case "bigint":
                            item[col] = long.Parse(defaultVal);
                            break;
                        case "real":
                        case "float":
                            item[col] = float.Parse(defaultVal);
                            break;
                        case "bit":
                            item[col] = float.TryParse(defaultVal, out float bitDefault)
                                ? bitDefault != 0
                                : string.Equals(defaultVal, "true", StringComparison.InvariantCultureIgnoreCase);
                            break;
                        default:
                            item[col] = defaultVal;
                            break;
                    }
                }
                else if (reader.GetString("IS_NULLABLE") == "NO")
                    switch (reader.GetString("DATA_TYPE"))
                    {
                        case "int":
                        case "smallint":
                        case "tinyint":
                        case "bigint":
                        case "real":
                        case "float":
                            item[col] = 0;
                            break;
                        case "bit":
                            item[col] = false;
                            break;
                        default:
                            item[col] = string.Empty;
                            break;
                    }
            }
            return item;
        }
    }
}

