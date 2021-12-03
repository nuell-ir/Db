using Microsoft.Data.SqlClient;

namespace nuell
{
    public static partial class Data
    {
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
    }
}

namespace nuell.Sync
{
    public static partial class Db
    {
        public static string NewItem(string table)
            => Str(nuell.Data.NewItem, false, new SqlParameter("@table", table));
    }
}

namespace nuell.Async
{
    public static partial class Db
    {
        public static Task<string> NewItem(string table)
            => Str(nuell.Data.NewItem, false, new SqlParameter("@table", table));
    }
}