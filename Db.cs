using System;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;

namespace nuell
{
    public static class Data
    {
        public static string ConnectionString { get; set; }

        public static string ParseCsv()
            => Compress(
                @"function parseCsv(csv) {
                    let output = [];
                    if (!csv)
                        return output;
                    const rows = csv.split('|'),
                          rowCount = rows.length,
                          headers = rows[0].split('~'),
                          headerCount = headers.length,
                          parse = (header, val) => {
                              switch (header[0]) {
                                  case '$':
                                    return { [header.slice(1)]: val };
                                  case '#':
                                    return { [header.slice(1)]: new Date(val) };
                                  default:
                                    return { [header]: eval(val) };
                              }
                          };
                    for (let i = 1; i < rowCount; i++) {
                        let obj = {},
                        values = rows[i].split('~');
                        for (let j = 0; j < headerCount; j++)
                            obj = Object.assign(obj, parse(headers[j], values[j]));
                        output.push(obj);
                    }
                    return output;
                }");

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
            => new SqlParameter(name, string.IsNullOrWhiteSpace(value) ? null : value.Trim());

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

        //public static T RowToEntity<T>(DataRow row) where T : new()
        //{
        //    var entity = new T();
        //    var props = new List<PropertyInfo>(entity.GetType().GetProperties());
        //    foreach (var prop in props)
        //        prop.SetValue(entity, row[prop.Name] == DBNull.Value ? null : row[prop.Name]);
        //    return entity;
        //}
    }
}

