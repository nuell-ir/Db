using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace nuell.Sync
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

        public static DataTable Table(string query, params (string name, object value)[] parameters)
            => Table(query, false, Data.SqlParams(parameters));

        public static DataTable Table(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => Table(query, isStoredProc, Data.SqlParams(parameters));

        public static DataTable Table(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
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

        public static JObject JObject(string query, params (string name, object value)[] parameters)
            => JObject(query, false, Data.SqlParams(parameters));

        public static JObject JObject(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => JObject(query, isStoredProc, Data.SqlParams(parameters));

        public static JObject JObject(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            cnnct.Open();
            using var reader = cmnd.ExecuteReader();
            if (reader.HasRows)
            {
                reader.Read();
                return JObject(reader);
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

        public static string Json(string query, params (string name, object value)[] parameters)
            => Json(query, false, Data.SqlParams(parameters));

        public static string Json(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => Json(query, isStoredProc, Data.SqlParams(parameters));

        public static string Json(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            cnnct.Open();
            using var reader = cmnd.ExecuteReader();
            if (reader.HasRows)
                return Json(reader);
            else
                return null;
        }

        private static string Json(SqlDataReader reader)
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

        public static string Csv(object[] items)
        {
            if (items == null)
                return null;
            var str = new StringBuilder();
            var props = new List<PropertyInfo>(items[0].GetType().GetProperties());
            foreach (var prop in props)
            {
                if (prop.PropertyType == typeof(string)
                    || prop.PropertyType == typeof(TimeSpan)
                    || prop.PropertyType == typeof(byte[]))
                    str.Append(stringField);
                else if (prop.PropertyType == typeof(DateTime))
                    str.Append(dateField);
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

        public static List<T> List<T>(string query, params (string name, object value)[] parameters)
            => List<T>(query, false, Data.SqlParams(parameters));

        public static List<T> List<T>(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => List<T>(query, isStoredProc, Data.SqlParams(parameters));

        public static List<T> List<T>(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
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

        public static Dictionary<K, V> Dictionary<K, V>(string query, params (string name, object value)[] parameters)
            => Dictionary<K, V>(query, false, Data.SqlParams(parameters));

        public static Dictionary<K, V> Dictionary<K, V>(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => Dictionary<K, V>(query, isStoredProc, Data.SqlParams(parameters));

        public static Dictionary<K, V> Dictionary<K, V>(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            cnnct.Open();
            using var reader = cmnd.ExecuteReader();
            if (!reader.HasRows)
                return null;
            var dictionary = new Dictionary<K, V>();
            while (reader.Read())
                dictionary.Add((K)Convert.ChangeType(reader[0], typeof(K)), (V)Convert.ChangeType(reader[1], typeof(V)));
            return dictionary;
        }

        public static int Execute(string query, params (string name, object value)[] parameters)
            => Execute(query, false, Data.SqlParams(parameters));

        public static int Execute(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => Execute(query, isStoredProc, Data.SqlParams(parameters));

        public static int Execute(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
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
            using (var cnnct = new SqlConnection(Data.ConnectionString))
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

        public static T GetVal<T>(string query, params (string name, object value)[] parameters) where T : struct
            => GetVal<T>(query, false, Data.SqlParams(parameters));

        public static T GetVal<T>(string query, bool isStoredProc, params (string name, object value)[] parameters) where T : struct
            => GetVal<T>(query, isStoredProc, Data.SqlParams(parameters));

        public static T GetVal<T>(string query, bool isStoredProc, params SqlParameter[] parameters) where T : struct
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            cnnct.Open();
            var val = cmnd.ExecuteScalar();
            return val is null ? default : (T)Convert.ChangeType(val, typeof(T));
        }

        public static string GetStr(string query, params (string name, object value)[] parameters)
            => GetStr(query, false, Data.SqlParams(parameters));

        public static string GetStr(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => GetStr(query, isStoredProc, Data.SqlParams(parameters));

        public static string GetStr(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            cnnct.Open();
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            var val = cmnd.ExecuteScalar();
            return val is DBNull ? null : val?.ToString();
        }

        public static object[] GetValues(string query, params (string name, object value)[] parameters)
            => GetValues(query, false, Data.SqlParams(parameters));

        public static object[] GetValues(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => GetValues(query, isStoredProc, Data.SqlParams(parameters));

        public static object[] GetValues(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            cnnct.Open();
            using var reader = cmnd.ExecuteReader();
            var results = new List<object>();
            AddValues();
            while (reader.NextResult())
                AddValues();
            return results.ToArray();

            void AddValues()
            {
                var values = new object[reader.FieldCount];
                if (reader.Read())
                    reader.GetValues(values);
                results.AddRange(values);
            }
        }

        public static JObject Retrieve(string query, (string Name, Results ResultType)[] props, params (string name, object value)[] parameters)
            => Retrieve(query, props, false, Data.SqlParams(parameters));

        public static JObject Retrieve(string query, (string Name, Results ResultType)[] props, bool isStoredProc, params (string name, object value)[] parameters)
            => Retrieve(query, props, isStoredProc, Data.SqlParams(parameters));

        public static JObject Retrieve(string query, (string Name, Results ResultType)[] props, bool isStoredProc, params SqlParameter[] parameters)
        {
            var result = new JObject();
            int index = 0;
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            cnnct.Open();
            using var reader = cmnd.ExecuteReader();
            Read();
            while (reader.NextResult())
                Read();
            return result;

            void Read()
            {
                if (!reader.HasRows)
                    result[props[index++].Name] = null;
                else
                    switch (props[index].ResultType)
                    {
                        case Results.Object:
                            reader.Read();
                            result.Add(props[index++].Name, JToken.FromObject(reader[0]));
                            break;
                        case Results.JObject:
                            reader.Read();
                            result.Add(props[index++].Name, JObject(reader));
                            break;
                        case Results.Json:
                            result.Add(props[index++].Name, Json(reader));
                            break;
                        case Results.Csv:
                            result.Add(props[index++].Name, ReadCsvResult(reader));
                            break;
                    }
            }
        }

        public static string Csv(string query, params (string name, object value)[] parameters)
            => Csv(query, false, Data.SqlParams(parameters));

        public static string Csv(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => Csv(query, isStoredProc, Data.SqlParams(parameters));

        public static string Csv(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            cnnct.Open();
            using var reader = cmnd.ExecuteReader();
            return ReadCsvResult(reader);
        }
        
        public static string[] MultiCsv(string query, params (string name, object value)[] parameters)
            => MultiCsv(query, false, Data.SqlParams(parameters));

        public static string[] MultiCsv(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => MultiCsv(query, isStoredProc, Data.SqlParams(parameters));

        public static string[] MultiCsv(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
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
            var str = ReadCsvHeader(reader);
            while (reader.Read())
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

        public static int Save(JObject jobj, string table)
            => Save(jobj.Properties().Select(p => (p.Name, Data.JPropValue(p))), table);

        public static int Save(object obj, string table)
            => Save(obj.GetType().GetProperties().Select(p => (p.Name, p.GetValue(obj))), table);

        private static int Save(IEnumerable<(string Name, object Value)> props, string table)
        {
            var idProp = props.Where(prop => string.Compare(prop.Name, "Id", true) == 0);
            int id = (int)idProp.First().Value;
            props = props.Except(idProp);
            var sqlParams = props.Select(prop => new SqlParameter("@" + prop.Name, prop.Value)).ToArray();
            if (id == 0)
                using (var cnnct = new SqlConnection(Data.ConnectionString))
                {
                    using var cmnd = cnnct.CreateCommand();
                    cmnd.CommandText = string.Format("insert into [{0}] ({1}) values ({2})",
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
                    string.Format("update [{0}] set {1} where Id=" + id,
                        table,
                        string.Join(',', props.Select(prop => $"[{prop.Name}]=@{prop.Name}"))),
                    false,
                    sqlParams);

            return id;
        }

        public static string NewItem(string table)
            => GetStr(nuell.Data.NewItem, false, new SqlParameter("@table", table));
    }
}

