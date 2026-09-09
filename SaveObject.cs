using System.Text;
using Microsoft.Data.SqlClient;

namespace nuell
{
    internal static partial class SaveQuery
    {
        internal static SaveParams Create<T>(T obj, string table, string idProp)
        {
            var propInfo = obj.GetType().GetProperties();
            var props = new (string Name, object Value)[propInfo.Length];
            for (int i = 0; i < propInfo.Length; i++)
                props[i] = (propInfo[i].Name, propInfo[i].GetValue(obj));
            int idIndex = Array.FindIndex(props, prop => prop.Name == idProp);
            int id = Convert.ToInt32(props[idIndex].Value);
            var sqlParams = new List<SqlParameter>();
            SqlParameter param;
            var str = new StringBuilder();
            if (id == 0)
            {
                str.Append("INSERT INTO ");
                str.Append(table);
                str.Append('(');
                for (int i = 0; i < props.Length; i++)
                    if (i != idIndex)
                    {
                        str.Append('[');
                        str.Append(props[i].Name);
                        str.Append("],");
                    }
                str.Remove(str.Length - 1, 1);
                str.Append(") VALUES (");
                for (int i = 0; i < props.Length; i++)
                    if (i != idIndex)
                    {
                        AppendValue(props[i]);
                        str.Append(',');
                    }
                str.Remove(str.Length - 1, 1);
                str.Append("); SELECT CAST(SCOPE_IDENTITY() AS int);");
            }
            else
            {
                str.Append("UPDATE ");
                str.Append(table);
                str.Append(" SET ");
                for (int i = 0; i < props.Length; i++)
                    if (i != idIndex)
                    {
                        str.Append('[');
                        str.Append(props[i].Name);
                        str.Append("]=");
                        AppendValue(props[i]);
                        str.Append(',');
                    }
                str.Remove(str.Length - 1, 1);
                str.Append(" WHERE ");
                str.Append(idProp);
                str.Append('=');
                str.Append(id);
            }
            return new SaveParams
            {
                Id = id,
                Query = str.ToString(),
                SqlParams = sqlParams.ToArray()
            };

            void AppendValue((string Name, object Value) prop)
            {
                if (prop.Value is null)
                    str.Append("NULL");
                else
                    switch (Type.GetTypeCode(prop.Value.GetType()))
                    {
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                        case TypeCode.Decimal:
                        case TypeCode.Double:
                        case TypeCode.Single:
                            str.Append(prop.Value);
                            break;
                        case TypeCode.Boolean:
                            str.Append((bool)prop.Value ? 1 : 0);
                            break;
                        default:
                            param = new SqlParameter("@" + prop.Name, prop.Value);
                            str.Append(param.ParameterName);
                            sqlParams.Add(param);
                            break;
                    }
            }
        }
    }
}

namespace nuell.Sync
{
    public static partial class Db
    {
        /// <summary>Saves an object of type T via an insert or update operation.</summary>
        /// <remarks>If the value of the identity field is 0, the values will be inserted as a record; otherwise, a record with the specified identity will be updated.</remarks>
        /// <returns>The identity of the inserted/updated record, or 0 if no record was updated.</returns>
        /// <param name="obj">The object to save</param>
        /// <param name="table">Table name</param>   
        /// <param name="idProp">The name of the identity field, the value of which decides whether to insert or update the record</param>
        public static int Save<T>(T obj, string table, string idProp = "Id")
            => Save(SaveQuery.Create(obj, table, idProp));
    }
}

namespace nuell.Async
{
    public static partial class Db
    {
        /// <summary>Saves an object of type T via an insert or update operation.</summary>
        /// <remarks>If the value of the identity field is 0, the values will be inserted as a record; otherwise, a record with the specified identity will be updated.</remarks>
        /// <returns>The identity of the inserted/updated record, or 0 if no record was updated.</returns>
        /// <param name="obj">The object to save</param>
        /// <param name="table">Table name</param>   
        /// <param name="idProp">The name of the identity field, the value of which decides whether to insert or update the record</param>
        public static Task<int> Save<T>(T obj, string table, string idProp = "Id")
            => Save(SaveQuery.Create(obj, table, idProp));
    }
}