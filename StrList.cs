using System.Data;
using Microsoft.Data.SqlClient;

namespace nuel;

public static partial class Db
{
        /// <summary>Asynchronously executes the query and returns the values of the first column as a <see cref="System.Collections.Generic.List{String}"/>.</summary>
        /// <param name="query">The SQL query or stored procedure name to execute.</param>
        /// <param name="parameters">The parameters for the SQL query.</param>
        /// <returns>A task representing the asynchronous operation, returning a <see cref="System.Collections.Generic.List{String}"/> containing the values of the first column, or null if no rows were returned.</returns>
        public static Task<List<string>> StrList(string query, params (string name, object value)[] parameters)
            => StrList(query, false, Data.SqlParams(parameters));

        /// <summary>Asynchronously executes the query and returns the values of the first column as a <see cref="System.Collections.Generic.List{String}"/>.</summary>
        /// <param name="query">The SQL query or stored procedure name to execute.</param>
        /// <param name="isStoredProc">Whether the query is a stored procedure.</param>
        /// <param name="parameters">The parameters for the SQL query.</param>
        /// <returns>A task representing the asynchronous operation, returning a <see cref="System.Collections.Generic.List{String}"/> containing the values of the first column, or null if no rows were returned.</returns>
        public static Task<List<string>> StrList(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => StrList(query, isStoredProc, Data.SqlParams(parameters));

        /// <summary>Asynchronously executes the query and returns the values of the first column as a <see cref="System.Collections.Generic.List{String}"/>.</summary>
        /// <param name="query">The SQL query or stored procedure name to execute.</param>
        /// <param name="isStoredProc">Whether the query is a stored procedure.</param>
        /// <returns>A task representing the asynchronous operation, returning a <see cref="System.Collections.Generic.List{String}"/> containing the values of the first column, or null if no rows were returned.</returns>
        public static Task<List<string>> StrList(string query, bool isStoredProc = false)
            => StrList(query, isStoredProc, Data.NoParams);

        /// <summary>Asynchronously executes the query and returns the values of the first column as a <see cref="System.Collections.Generic.List{String}"/>.</summary>
        /// <param name="query">The SQL query or stored procedure name to execute.</param>
        /// <param name="isStoredProc">Whether the query is a stored procedure.</param>
        /// <param name="parameters">The SQL parameters to apply to the command.</param>
        /// <returns>A task representing the asynchronous operation, returning a <see cref="System.Collections.Generic.List{String}"/> containing the values of the first column, or null if no rows were returned.</returns>
        public static async Task<List<string>> StrList(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var connection = new SqlConnection(Data.ConnectionString);
            using var cmd = new SqlCommand(query, connection);
            if (isStoredProc)
                cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddRange(parameters);
            await connection.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
                return null;
            var list = new List<string>();
            while (await reader.ReadAsync())
                list.Add(reader.IsDBNull(0) ? null : reader.GetString(0));
            return list;
        }
    }