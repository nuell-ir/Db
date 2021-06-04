using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace nuell.Async
{
    public enum Results
    {
        Object, JObject, Json, Csv
    }

    public static class Db
    {
        const char sep = '~';
        const char line = '|';
        const char stringField = '$';
        const char dateField = '#';

        public static Task<DataTable> Table(string query, params (string name, object value)[] parameters)
            => Table(query, false, Data.SqlParams(parameters));

        public static Task<DataTable> Table(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => Table(query, isStoredProc, Data.SqlParams(parameters));

        public static async Task<DataTable> Table(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
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

        public static Task<JObject> JObject(string query, params (string name, object value)[] parameters)
            => JObject(query, false, Data.SqlParams(parameters));

        public static Task<JObject> JObject(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => JObject(query, isStoredProc, Data.SqlParams(parameters));

        public async static Task<JObject> JObject(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
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

        private static JObject JObject(SqlDataReader reader)
        {
            var json = new JObject();
            for (int i = 0; i < reader.FieldCount; i++)
                json.Add(reader.GetName(i), JToken.FromObject(reader.GetValue(i)));
            return json;
        }

        public static Task<string> Json(string query, params (string name, object value)[] parameters)
            => Json(query, false, Data.SqlParams(parameters));

        public static Task<string> Json(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => Json(query, isStoredProc, Data.SqlParams(parameters));

        public async static Task<string> Json(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            await cnnct.OpenAsync();
            using var reader = await cmnd.ExecuteReaderAsync();
            if (reader.HasRows)
                return await Json(reader);
            else
                return null;
        }

        private static async Task<string> Json(SqlDataReader reader)
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

        public static Task<List<T>> List<T>(string query, params (string name, object value)[] parameters)
            => List<T>(query, false, Data.SqlParams(parameters));

        public static Task<List<T>> List<T>(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => List<T>(query, isStoredProc, Data.SqlParams(parameters));

        public static async Task<List<T>> List<T>(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            await cnnct.OpenAsync();
            using var reader = cmnd.ExecuteReader();
            if (!reader.HasRows)
                return null;
            var list = new List<T>();
            while (await reader.ReadAsync())
                list.Add((T)Convert.ChangeType(reader[0], typeof(T)));
            return list;
        }

        public static Task<Dictionary<K, V>> Dictionary<K, V>(string query, params (string name, object value)[] parameters)
            => Dictionary<K, V>(query, false, Data.SqlParams(parameters));

        public static Task<Dictionary<K, V>> Dictionary<K, V>(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => Dictionary<K, V>(query, isStoredProc, Data.SqlParams(parameters));

        public static async Task<Dictionary<K, V>> Dictionary<K, V>(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            await cnnct.OpenAsync();
            using var reader = cmnd.ExecuteReader();
            if (!reader.HasRows)
                return null;
            var dictionary = new Dictionary<K, V>();
            while (await reader.ReadAsync())
                dictionary.Add((K)Convert.ChangeType(reader[0], typeof(K)), (V)Convert.ChangeType(reader[1], typeof(V)));
            return dictionary;
        }

        public static Task<int> Execute(string query, params (string name, object value)[] parameters)
            => Execute(query, false, Data.SqlParams(parameters));

        public static Task<int> Execute(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => Execute(query, isStoredProc, Data.SqlParams(parameters));

        public async static Task<int> Execute(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
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
            using (var cnnct = new SqlConnection(Data.ConnectionString))
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

        public static Task<T> GetVal<T>(string query, params (string name, object value)[] parameters) where T : struct
            => GetVal<T>(query, false, Data.SqlParams(parameters));

        public static Task<T> GetVal<T>(string query, bool isStoredProc, params (string name, object value)[] parameters) where T : struct
            => GetVal<T>(query, isStoredProc, Data.SqlParams(parameters));
        
        public async static Task<T> GetVal<T>(string query, bool isStoredProc, params SqlParameter[] parameters) where T : struct
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            await cnnct.OpenAsync();
            var val = await cmnd.ExecuteScalarAsync();
            return val is null ? default : (T)Convert.ChangeType(val, typeof(T));
        }

        public static Task<string> GetStr(string query, params (string name, object value)[] parameters)
            => GetStr(query, false, Data.SqlParams(parameters));

        public static Task<string> GetStr(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => GetStr(query, isStoredProc, Data.SqlParams(parameters));

        public static async Task<string> GetStr(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            await cnnct.OpenAsync();
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            var val = await cmnd.ExecuteScalarAsync();
            return val is DBNull ? null : val?.ToString();
        }

        public static Task<object[]> GetValues(string query, params (string name, object value)[] parameters)
            => GetValues(query, false, Data.SqlParams(parameters));

        public static Task<object[]> GetValues(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => GetValues(query, isStoredProc, Data.SqlParams(parameters));

        public static async Task<object[]> GetValues(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            await cnnct.OpenAsync();
            using var reader = await cmnd.ExecuteReaderAsync();
            var results = new List<object>();
            await AddValues();
            while (reader.NextResult())
                await AddValues();
            return results.ToArray();

            async Task AddValues()
            {
                var values = new object[reader.FieldCount];
                if (await reader.ReadAsync())
                    reader.GetValues(values);
                results.AddRange(values);
            }
        }

        public static Task<JObject> Retrieve(string query, (string Name, Results ResultType)[] props, params (string name, object value)[] parameters)
            => Retrieve(query, props, false, Data.SqlParams(parameters));

        public static Task<JObject> Retrieve(string query, (string Name, Results ResultType)[] props, bool isStoredProc, params (string name, object value)[] parameters)
            => Retrieve(query, props, isStoredProc, Data.SqlParams(parameters));

        public static async Task<JObject> Retrieve(string query, (string Name, Results ResultType)[] props, bool isStoredProc = false, params SqlParameter[] parameters)
        {
            var result = new JObject();
            int index = 0;
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            await cnnct.OpenAsync();
            using var reader = cmnd.ExecuteReader();
            await Read();
            while (reader.NextResult())
                await Read();
            return result;

            async Task Read()
            {
                if (!reader.HasRows)
                    result[props[index++].Name] = null;
                else
                    switch (props[index].ResultType)
                    {
                        case Results.Object:
                            await reader.ReadAsync();
                            result.Add(props[index++].Name, JToken.FromObject(reader[0]));
                            break;
                        case Results.JObject:
                            await reader.ReadAsync();
                            result.Add(props[index++].Name, JObject(reader));
                            break;
                        case Results.Json:
                            result.Add(props[index++].Name, await Json(reader));
                            break;
                        case Results.Csv:
                            result.Add(props[index++].Name, await ReadCsvResult(reader));
                            break;
                    }
            }
        }

        public static Task<string> Csv(string query, params (string name, object value)[] parameters)
            => Csv(query, false, Data.SqlParams(parameters));

        public static Task<string> Csv(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => Csv(query, isStoredProc, Data.SqlParams(parameters));

        public static async Task<string> Csv(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            await cnnct.OpenAsync();
            using var reader = await cmnd.ExecuteReaderAsync();
            return await ReadCsvResult(reader);
        }

        public static Task<string[]> MultiCsv(string query, params (string name, object value)[] parameters)
            => MultiCsv(query, false, Data.SqlParams(parameters));

        public static Task<string[]> MultiCsv(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => MultiCsv(query, isStoredProc, Data.SqlParams(parameters));

        public static async Task<string[]> MultiCsv(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            await cnnct.OpenAsync();
            using var reader = await cmnd.ExecuteReaderAsync();
            var results = new List<string>
            {
                await ReadCsvResult(reader)
            };
            while (await reader.NextResultAsync())
                results.Add(await ReadCsvResult(reader));
            return results.ToArray();
        }

        private static async Task<string> ReadCsvResult(SqlDataReader reader)
        {
            if (!reader.HasRows)
                return null;
            var str = ReadCsvHeader(reader);
            while (await reader.ReadAsync())
                ReadCsvRow(reader, str);
            str.Remove(str.Length - 1, 1);
            return str.ToString();
        }

        private static StringBuilder ReadCsvHeader(SqlDataReader reader)
        {
            var str = new StringBuilder();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var fieldType = reader.GetFieldType(i);
                if (fieldType == typeof(string) || fieldType == typeof(TimeSpan) || fieldType == typeof(byte[]))
                    str.Append(stringField);
                else if (fieldType == typeof(DateTime))
                    str.Append(dateField);
                str.Append(reader.GetName(i));
                str.Append(sep);
            }
            str.Remove(str.Length - 1, 1);
            str.Append(line);
            return str;
        }

        private static void ReadCsvRow(SqlDataReader reader, StringBuilder str)
        {
            for (int i = 0; i < reader.FieldCount; i++)
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
                else if (val is TimeSpan)
                    str.Append(reader.GetTimeSpan(i).ToString(@"hh\:mm\:ss"));
                else if (val is byte[] bytes)
                    str.Append(Convert.ToBase64String(bytes));
                else
                    str.Append(val);
                str.Append(sep);
            }
            str.Remove(str.Length - 1, 1);
            str.Append(line);
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

        public static Task<int> Save(JObject jobj, string table)
            => Save(jobj.Properties().Select(p => (p.Name, Data.JPropValue(p))), table);

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

        public static Task<string> NewItem(string table)
            => GetStr(nuell.Data.NewItem, false, new SqlParameter("@table", table));
    }
}

