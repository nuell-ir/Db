using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace nuell.Sync
{
    public static partial class Db
    {
        public static List<string> StrList(string query, params (string name, object value)[] parameters)
            => StrList(query, false, Data.SqlParams(parameters));

        public static List<string> StrList(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => StrList(query, isStoredProc, Data.SqlParams(parameters));

        public static List<string> StrList(string query, bool isStoredProc = false)
            => StrList(query, isStoredProc, Data.NoParams);

        public static List<string> StrList(string query, bool isStoredProc, params SqlParameter[] parameters)
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

            var list = new List<string>();
            while (reader.Read())
                list.Add(reader.GetString(0));
            return list;
        }
    }
}

namespace nuell.Async
{
    public static partial class Db
    {
        public static Task<List<string>> StrList(string query, params (string name, object value)[] parameters)
            => StrList(query, false, Data.SqlParams(parameters));

        public static Task<List<string>> StrList(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => StrList(query, isStoredProc, Data.SqlParams(parameters));

        public static Task<List<string>> StrList(string query, bool isStoredProc = false)
            => StrList(query, isStoredProc, Data.NoParams);

        public static async Task<List<string>> StrList(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var cnnct = new SqlConnection(Data.ConnectionString);
            using var cmnd = new SqlCommand(query, cnnct);
            if (isStoredProc)
                cmnd.CommandType = CommandType.StoredProcedure;
            cmnd.Parameters.AddRange(parameters);
            await cnnct.OpenAsync();
            using var reader = cmnd.ExecuteReader();
            if (!reader.HasRows)
                return null;
            var list = new List<string>();
            while (await reader.ReadAsync())
                list.Add(reader.GetString(0));
            return list;
        }
    }
}