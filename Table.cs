using System.Data;
using Microsoft.Data.SqlClient;

namespace nuell.Sync
{
    public static partial class Db
    {
        public static DataTable Table(string query, params (string name, object value)[] parameters)
            => Table(query, false, Data.SqlParams(parameters));

        public static DataTable Table(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => Table(query, isStoredProc, Data.SqlParams(parameters));

        public static DataTable Table(string query, bool isStoredProc = false)
            => Table(query, isStoredProc, Data.NoParams);

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
    }
}

namespace nuell.Async
{
    public static partial class Db
    {
        public static Task<DataTable> Table(string query, params (string name, object value)[] parameters)
            => Table(query, false, Data.SqlParams(parameters));

        public static Task<DataTable> Table(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => Table(query, isStoredProc, Data.SqlParams(parameters));

        public static Task<DataTable> Table(string query, bool isStoredProc = false)
            => Table(query, isStoredProc, Data.NoParams);

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
    }
}