using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace nuell
{
    public static partial class Data
    {        
        internal const char sep = '~';
        internal const char line = '|';

        internal static List<TypeCode> GetCsvHeader(ReadOnlyCollection<DbColumn> columns, StringBuilder str)
        {
            var fieldTypes = new List<TypeCode>(columns.Count);
            TypeCode type;
            foreach (var col in columns)
            {
                type = Type.GetTypeCode(col.DataType);
                fieldTypes.Add(type);
                str.Append(GetCsvTypeFlag(type));
                str.Append(col.ColumnName);
                str.Append(sep);
            }
            str.Remove(str.Length - 1, 1);
            str.Append(line);
            return fieldTypes;
        }

        private static char GetCsvTypeFlag(TypeCode colType)
        {
            switch (colType)
            {
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return '!';

                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return '%';

                case TypeCode.DateTime:
                    return '#';

                case TypeCode.Boolean:
                    return '^';

                default:
                    return '$';
            }
        }

        internal static void ReadCsvRow(this SqlDataReader reader, StringBuilder str, List<TypeCode> fieldTypes)
        {
            for (int i = 0; i < fieldTypes.Count; i++)
            {
                if (reader.IsDBNull(i))
                {
                    str.Append('Ø');
                    str.Append(sep);
                    continue;
                }

                switch (fieldTypes[i])
                {
                    case TypeCode.Int32:
                        str.Append(reader.GetInt32(i));
                        break;
                    case TypeCode.Int64:
                        str.Append(reader.GetInt64(i));
                        break;
                    case TypeCode.Int16:
                        str.Append(reader.GetInt16(i));
                        break;
                    case TypeCode.Byte:
                        str.Append(reader.GetByte(i));
                        break;
                    case TypeCode.Single:
                        str.Append(reader.GetFloat(i));
                        break;
                    case TypeCode.Double:
                        str.Append(reader.GetDouble(i));
                        break;
                    case TypeCode.Decimal:
                        str.Append(reader.GetDecimal(i));
                        break;
                    case TypeCode.DateTime:
                        str.Append(new DateTimeOffset(reader.GetDateTime(i)).ToUnixTimeMilliseconds());
                        break;
                    case TypeCode.Boolean:
                        str.Append(reader.GetBoolean(i) ? 1 : 0);
                        break;
                    case TypeCode.Char:
                    case TypeCode.String:
                        str.Append(reader.GetString(i));
                        break;
                }
                str.Append(sep);
            }
            str.Remove(str.Length - 1, 1);
            str.Append(line);
        }
    }
}

namespace nuell.Sync
{
    public static partial class Db
    {        
        public static string Csv(string query, params (string name, object value)[] parameters)
            => Csv(query, false, Data.SqlParams(parameters));

        public static string Csv(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => Csv(query, isStoredProc, Data.SqlParams(parameters));

        public static string Csv(string query, bool isStoredProc = false)
            => Csv(query, isStoredProc, Data.NoParams);

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

        public static string[] MultiCsv(string query, bool isStoredProc = false)
            => MultiCsv(query, isStoredProc, Data.NoParams);

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
                reader.ReadCsvResult()
            };
            while (reader.NextResult())
                results.Add(reader.ReadCsvResult());
            return results.ToArray();
        }

        private static string ReadCsvResult(this SqlDataReader reader)
        {
            if (!reader.HasRows)
                return null;
            var str = new StringBuilder();
            var fieldTypes = Data.GetCsvHeader(reader.GetColumnSchema(), str);
            while (reader.Read())
                reader.ReadCsvRow(str, fieldTypes);
            str.Remove(str.Length - 1, 1);
            return str.ToString();
        }
    }
}

namespace nuell.Async
{
    public static partial class Db
    {
        public static Task<string> Csv(string query, params (string name, object value)[] parameters)
            => Csv(query, false, Data.SqlParams(parameters));

        public static Task<string> Csv(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => Csv(query, isStoredProc, Data.SqlParams(parameters));

        public static Task<string> Csv(string query, bool isStoredProc = false)
            => Csv(query, isStoredProc, Data.NoParams);

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

        public static Task<string[]> MultiCsv(string query, bool isStoredProc = false)
            => MultiCsv(query, isStoredProc, Data.NoParams);

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
                await reader.ReadCsvResult()
            };
            while (await reader.NextResultAsync())
                results.Add(await reader.ReadCsvResult());
            return results.ToArray();
        }

        private static async Task<string> ReadCsvResult(this SqlDataReader reader)
        {
            if (!reader.HasRows)
                return null;
            int fieldCount = reader.FieldCount;
            var str = new StringBuilder();
            var fieldTypes = Data.GetCsvHeader(await reader.GetColumnSchemaAsync(), str);
            while (await reader.ReadAsync())
                reader.ReadCsvRow(str, fieldTypes);
            str.Remove(str.Length - 1, 1);
            return str.ToString();
        }
    }
}