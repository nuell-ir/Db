using Microsoft.Data.SqlClient;

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

        public static SqlParameter NS(string name, string value)
            => new SqlParameter(name, string.IsNullOrWhiteSpace(value) ? null : value.Trim());

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

