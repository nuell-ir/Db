using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;

namespace nuell
{
    public static class Data
    {
        const char sep = '~';
        const char line = '|';

        public static string ConnectionString { get; set; }

        public static readonly SqlParameter[] NoParams = new SqlParameter[] { };

        public static SqlParameter[] SqlParams((string name, object value)[] parameters)
            => parameters.Select(p => new SqlParameter(p.name, p.value)).ToArray();

        private static string Compress(string code)
        {
            string s2 = Regex.Replace(code, @"([;{})>+-,:])\s*\n\s*", "$1", RegexOptions.Multiline);
            s2 = Regex.Replace(s2, @"\s*\n\s*", " ", RegexOptions.Multiline);
            return Regex.Replace(s2, @"\s*([(){},;:=<>/*+\-?&|'])\s*", "$1", RegexOptions.Multiline);
        }

        public static readonly string NewItem =
            @"select '{' + (
                select '""' + COLUMN_NAME + '"":' + 
                    case 
                        when COLUMN_DEFAULT is null then
                            case
                                when IS_NULLABLE = 'YES' then 'null'
                                else iif(DATA_TYPE in ('int', 'smallint', 'tinyint', 'bigint', 'float', 'real', 'bit'), '0', '""""')
                            end
                        else
                            case 
                                when DATA_TYPE in ('int', 'smallint', 'tinyint', 'bigint', 'real', 'decimal', 'float') 
                                    then substring(COLUMN_DEFAULT, 3, len(COLUMN_DEFAULT) - 4)
                                when DATA_TYPE in ('bit')
                                    then iif(upper(substring(COLUMN_DEFAULT, 3, len(COLUMN_DEFAULT) - 4)) in ('1', 'TRUE'), 'true', 'false')
                                else '""' + iif(upper(left(COLUMN_DEFAULT, 3)) = '(N''', 
                                    substring(COLUMN_DEFAULT, 4, len(COLUMN_DEFAULT) - 5), 
                                    substring(COLUMN_DEFAULT, 3, len(COLUMN_DEFAULT) - 4)) + '""'
                            end
                    end + ','
                from INFORMATION_SCHEMA.COLUMNS 
                where TABLE_NAME = @table 
                order by ORDINAL_POSITION
                for xml path ('')
            ) + '}'";

        public static SqlParameter NS(string name, string value)
            => new SqlParameter(name, string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim());

        public static object JPropValue(JProperty prop)
            => prop.Value.Type switch
            {
                JTokenType.Null => DBNull.Value,
                JTokenType.Integer => (long)prop.Value,
                JTokenType.Float => (float)prop.Value,
                JTokenType.Boolean => (bool)prop.Value,
                JTokenType.Date => (DateTime)prop.Value,
                JTokenType.TimeSpan => (TimeSpan)prop.Value,
                JTokenType.Bytes => (byte[])prop.Value,
                _ => (string)prop.Value,
            };

        internal static List<TypeCode> GetCsvHeader(ReadOnlyCollection<DbColumn> columns, StringBuilder str)
        {
            var fieldTypes = new List<TypeCode>(columns.Count);
            TypeCode type;
            foreach (var col in columns)
            {
                type = Type.GetTypeCode(col.DataType);
                fieldTypes.Add(type);
                str.Append(GetCsvTypeMarker(type));
                str.Append(col.ColumnName);
                str.Append(sep);
            }
            str.Remove(str.Length - 1, 1);
            str.Append(line);
            return fieldTypes;
        }

        private static char GetCsvTypeMarker(TypeCode colType)
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

