using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace nuell.Sync
{
    public static class Db
    {
        public static DataTable Table(string query, params SqlParameter[] parameters)
            => Table(query, false, parameters);

        public static DataTable Table(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnStr);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            var dt = new DataTable();
            cnnct.Open();
            var read = cmnd.ExecuteReader();
            if (read.HasRows)
                dt.Load(read);
            return dt;
        }

        public static JObject JObject(string query, params SqlParameter[] parameters)
            => JObject(query, isStoredProc: false, parameters);

        public static JObject JObject(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnStr);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            cnnct.Open();
            using var reader = cmnd.ExecuteReader();
            if (reader.HasRows)
            {
                reader.Read();
                var json = new JObject();
                for (int i = 0; i < reader.VisibleFieldCount; i++)
                    json.Add(reader.GetName(i), JToken.FromObject(reader.GetValue(i)));
                return json;
            }
            else
                return null;
        }

        public static string Json(string query, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnStr);
            using var cmnd = new SqlCommand(query, cnnct);
            cmnd.Parameters.AddRange(parameters);
            cnnct.Open();
            using var reader = cmnd.ExecuteReader();
            if (reader.HasRows)
            {
                reader.Read();
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

        public static string Csv(string query, params SqlParameter[] parameters)
            => Csv(query, false, parameters);

        public static string Csv(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnStr);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            cnnct.Open();
            using var reader = cmnd.ExecuteReader();
            return ReadCsvResult(reader);
        }

        public static string[] MultiCsv(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnStr);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            cnnct.Open();
            using var reader = cmnd.ExecuteReader();
            var results = new List<string>
            {
                ReadCsvResult(reader)
            };
            while (reader.NextResult())
                results.Add(ReadCsvResult(reader));
            return results.ToArray();
        }

        private static string ReadCsvResult(SqlDataReader reader)
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
            while (reader.Read())
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
            str.Remove(str.Length - 1, 1);
            return str.ToString();
        }

        public static string Csv(object[] items)
        {
            if (items == null)
                return null;
            const char sep = '~';
            const char line = '|';
            var str = new StringBuilder();
            var props = new List<PropertyInfo>(items[0].GetType().GetProperties());
            foreach (var prop in props)
            {
                if (prop.PropertyType == typeof(string)
                    || prop.PropertyType == typeof(TimeSpan)
                    || prop.PropertyType == typeof(byte[]))
                    str.Append('$');
                else if (prop.PropertyType == typeof(DateTime))
                    str.Append('#');
                str.Append(prop.Name);
                str.Append(sep);
            }
            str.Remove(str.Length - 1, 1);
            str.Append(line);
            foreach (var item in items)
            {
                foreach (var prop in props.Select(p => p.GetValue(item)))
                {
                    if (prop is bool)
                        str.Append((bool)prop ? "true" : "false"); //to prevent boolean capitalisation ('True' is not true in javascript)
                    else if (prop is DateTime d)
                    {
                        if (d.Ticks == 0)
                            str.Append(d.ToString("yyyy-MM-dd"));
                        else
                            str.Append(d.ToString("yyyy-MM-ddTHH:mm:ss"));
                    }
                    else if (prop is TimeSpan)
                        str.Append(((TimeSpan)prop).ToString(@"hh\:mm\:ss"));
                    else if (prop is byte[])
                        str.Append(Convert.ToBase64String((byte[])prop));
                    else
                        str.Append(prop);
                    str.Append(sep);
                }
                str.Remove(str.Length - 1, 1);
                str.Append(line);
            }
            str.Remove(str.Length - 1, 1);
            return str.ToString();
        }

        public static List<T> List<T>(string query, params SqlParameter[] parameters)
            => List<T>(query, isStoredProc: false, parameters);

        public static List<T> List<T>(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnStr);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            cnnct.Open();
            using var reader = cmnd.ExecuteReader();
            if (!reader.HasRows)
                return null;
            var list = new List<T>();
            while (reader.Read())
                list.Add((T)Convert.ChangeType(reader[0], typeof(T)));
            return list;
        }

        public static int Execute(string query, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnStr);
            using var cmnd = new SqlCommand(query, cnnct);
            cmnd.Parameters.AddRange(parameters);
            cnnct.Open();
            return cmnd.ExecuteNonQuery();
        }

        public static int[] Transaction(string queries)
            => Transaction(queries.Split(new string[] { "GO", ";" }, StringSplitOptions.RemoveEmptyEntries));

        public static int[] Transaction(IEnumerable<string> queries)
        {
            if (queries == null)
                return null;
            var result = new int[queries.Count()];
            using (var cnnct = new SqlConnection(Data.ConnStr))
            {
                using var cmnd = cnnct.CreateCommand();
                cnnct.Open();
                using var transaction = cnnct.BeginTransaction();
                cmnd.Transaction = transaction;
                int i = 0;
                foreach (string query in queries)
                {
                    cmnd.CommandText = query;
                    result[i++] = cmnd.ExecuteNonQuery();
                }
                transaction.Commit();
            }
            return result;
        }

        public static JObject Get(int id, string table)
            => JObject($"select top 1 * from {table} where Id={id}");

        public static T GetVal<T>(string query, params SqlParameter[] parameters) where T : struct
            => GetVal<T>(query, false, parameters);

        public static T GetVal<T>(string query, bool isStoredProc, params SqlParameter[] parameters) where T : struct
        {
            using var cnnct = new SqlConnection(Data.ConnStr);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            cnnct.Open();
            var val = cmnd.ExecuteScalar();
            return val is null ? default : (T)Convert.ChangeType(val, typeof(T));
        }

        public static string GetStr(string query)
        {
            using var cnnct = new SqlConnection(Data.ConnStr);
            using var cmnd = new SqlCommand(query, cnnct);
            cnnct.Open();
            var val = cmnd.ExecuteScalar();
            return val is DBNull ? null : val.ToString();
        }

        public static bool Delete(int id, string table)
        {
            try
            {
                return Execute($"delete from [{table}] where Id={id}") == 1;
            }
            catch
            {
                return false;
            }
        }

        public static int Save(JObject jsonEntity, string table)
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

                    cnnct.Open();
                    cmnd.ExecuteNonQuery();
                    cmnd.CommandText = "select @@identity";
                    id = Convert.ToInt32(cmnd.ExecuteScalar());
                }
            else
                Execute(
                    string.Format("update [{0}] set {1} where Id=" + id,
                        table,
                        string.Join(',', props.Select(prop => prop.Name + '=' + prop.ParamName))),
                    props.Select(prop => prop.Value).ToArray());

            return id;
        }

        public static JObject NewItem(string table)
        {
            var regDefault = new Regex(@"\((?:(?:N?'([^']+)')|(?:\(([^()]+)\)))\)", RegexOptions.Compiled);
            // ((some number)) or (N'some text') or ('some text')
            var item = new JObject();
            using var cnnct = new SqlConnection(Data.ConnStr);
            using var cmnd = new SqlCommand($@"select COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT
                    from INFORMATION_SCHEMA.COLUMNS where TABLE_NAME='{table}' order by ORDINAL_POSITION", cnnct);
            cnnct.Open();
            using var reader = cmnd.ExecuteReader();
            while (reader.Read())
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

        // public static T Get<T>(int id, string table) where T : new()
        // {
        //    var entity = new T();
        //    var props = new List<PropertyInfo>(entity.GetType().GetProperties());

        //    using (var cnnct = new SqlConnection(Data.ConnStr))
        //    {
        //        using (var cmnd = new SqlCommand($"select top 1 * from {table} where Id={id}", cnnct))
        //        {
        //            cnnct.Open();
        //            using (var reader = cmnd.ExecuteReader())
        //            {
        //                reader.Read();
        //                foreach (var prop in props)
        //                    prop.SetValue(entity, reader[prop.Name] == DBNull.Value ? null : reader[prop.Name]);
        //            }
        //        }
        //    }
        //    return entity;
        // }

        // public static T Get<T>(string query) where T : new()
        // {
        //    var entity = new T();
        //    var props = new List<PropertyInfo>(entity.GetType().GetProperties());

        //    using (var cnnct = new SqlConnection(Data.ConnStr))
        //    {
        //        using (var cmnd = new SqlCommand(query, cnnct))
        //        {
        //            cnnct.Open();
        //            using (var reader = cmnd.ExecuteReader())
        //            {
        //                reader.Read();
        //                for (int i = 0; i < reader.VisibleFieldCount; i++)
        //                {
        //                    var prop = props.Find(p => p.Name == reader.GetName(i));
        //                    prop.SetValue(entity, Convert.ChangeType(reader.GetValue(i), prop.PropertyType));
        //                }
        //            }
        //        }
        //    }
        //    return entity;
        // }

        //public static Dictionary<string, object> Get(string query)
        //{
        //    var result = new Dictionary<string, object>();
        //    using (var cnnct = new SqlConnection(Data.ConnStr))
        //    {
        //        using (var cmnd = new SqlCommand(query, cnnct))
        //        {
        //            cnnct.Open();
        //            using (var reader = cmnd.ExecuteReader())
        //                if (reader.Read())
        //                    for (int i = 0; i < reader.VisibleFieldCount; i++)
        //                        result[reader.GetName(i)] = reader.GetValue(i);
        //        }
        //    }
        //    return result;
        //}

        //public static int Save(object entity, string table)
        //{
        //    var props = new List<PropertyInfo>(entity.GetType().GetProperties());
        //    var propId = props.Where(p => p.Name == "Id");
        //    int id = Convert.ToInt32(propId.First().GetValue(entity));
        //    string query = "begin transaction;";

        //    if (id == 0)
        //    {
        //        var fields = new List<string>();
        //        var values = new List<string>();

        //        foreach (var prop in props.Except(propId))
        //        {
        //            fields.Add($"[{prop.Name}]");
        //            if (prop.GetValue(entity) == null)
        //                values.Add("null");
        //            else if (prop.PropertyType == typeof(string))
        //                values.Add($"N'{prop.GetValue(entity)}'");
        //            else if (prop.PropertyType == typeof(bool))
        //                values.Add(Convert.ToBoolean(prop.GetValue(entity)) ? "1" : "0");
        //            else if (prop.PropertyType == typeof(DateTime))
        //                values.Add($"'{Convert.ToDateTime(prop.GetValue(entity)).ToString("d MMM yyyy H:mm")}'");
        //            else
        //                values.Add(prop.GetValue(entity).ToString());
        //        }
        //        query += $"insert into [{table}] ({string.Join(",", fields.ToArray())}) values ({string.Join(",", values.ToArray())});";
        //    }
        //    else
        //    {
        //        var fields = new List<string>();
        //        foreach (var prop in props.Except(propId))
        //        {
        //            string value;
        //            if (prop.GetValue(entity) == null)
        //                value = "null";
        //            else if (prop.PropertyType == typeof(string))
        //                value = $"N'{prop.GetValue(entity)}'";
        //            else if (prop.PropertyType == typeof(bool))
        //                value = Convert.ToBoolean(prop.GetValue(entity)) ? "1" : "0";
        //            else if (prop.PropertyType == typeof(DateTime))
        //                value = $"'{Convert.ToDateTime(prop.GetValue(entity)).ToString("d MMM yyyy H:mm")}'";
        //            else
        //                value = prop.GetValue(entity).ToString();
        //            fields.Add($"[{prop.Name}]={value}");
        //        }
        //        query += $"update [{table}] set {string.Join(",", fields.ToArray())} where Id={id};";
        //    }
        //    query += "select @@identity; commit;";

        //    using (var cnnct = new SqlConnection(Data.ConnStr))
        //    {
        //        using (var cmnd = new SqlCommand(query, cnnct))
        //        {
        //            cnnct.Open();
        //            var result = cmnd.ExecuteScalar();
        //            if (!(result is DBNull))
        //                id = Convert.ToInt32(result);
        //        }
        //    }
        //    return id;
        //}
    }
}

