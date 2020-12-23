using System;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;

namespace nuell
{
    public static class Data
    {
        public static string ConnStr { get; set; }

        public static string Obj()
            => Util.Compress(
                @"function obj(C) {
                            let R = C.split('|'), L = R.length, A = [], h = R[0].split('~'), l = h.length;
                            for (let i = 1; i < L; i++) {
                                let o = {}, c = R[i].split('~');
                                for (let j = 0; j < l; j++)
                                    h[j][0] == '$' ? o[h[j].slice(1)] = c[j] : 
                                    h[j][0] == '#' ? o[h[j].slice(1)] = new Date(c[j]) 
                                        : o[h[j]] = eval(c[j]);
                                A.push(o);
                            }
                            return A;
                        }");

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

