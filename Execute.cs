using System.Data;
using Microsoft.Data.SqlClient;

namespace nuell.Sync
{
    public static partial class Db
    {
        /// <summary>Executes the query.</summary>
        /// <returns>the number of affected rows</returns>
        public static int Execute(string query, params (string name, object value)[] parameters)
            => Execute(query, false, Data.SqlParams(parameters));

        /// <summary>Executes the query.</summary>
        /// <returns>the number of affected rows</returns>
        /// <param name="isStoredProc">is the query a stored procedure</param>   
        public static int Execute(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => Execute(query, isStoredProc, Data.SqlParams(parameters));

        /// <summary>Executes the query.</summary>
        /// <returns>the number of affected rows</returns>
        /// <param name="isStoredProc">is the query a stored procedure</param>   
        public static int Execute(string query, bool isStoredProc = false)
            => Execute(query, isStoredProc, Data.NoParams);

        /// <summary>Executes the query.</summary>
        /// <returns>the number of affected rows</returns>
        /// <param name="isStoredProc">is the query a stored procedure</param>   
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
    }
}

namespace nuell.Async
{
    public static partial class Db
    {
        /// <summary>Executes the query.</summary>
        /// <returns>the number of affected rows</returns>
        public static Task<int> Execute(string query, params (string name, object value)[] parameters)
            => Execute(query, false, Data.SqlParams(parameters));

        /// <summary>Executes the query.</summary>
        /// <returns>the number of affected rows</returns>
        /// <param name="isStoredProc">is the query a stored procedure</param>   
        public static Task<int> Execute(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => Execute(query, isStoredProc, Data.SqlParams(parameters));

        /// <summary>Executes the query.</summary>
        /// <returns>the number of affected rows</returns>
        /// <param name="isStoredProc">is the query a stored procedure</param>   
        public static Task<int> Execute(string query, bool isStoredProc = false)
            => Execute(query, isStoredProc, Data.NoParams);

        /// <summary>Executes the query.</summary>
        /// <returns>the number of affected rows</returns>
        /// <param name="isStoredProc">is the query a stored procedure</param>   
        public async static Task<int> Execute(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            await cnnct.OpenAsync();
            return await cmnd.ExecuteNonQueryAsync();
        }
    }
}