using System.Data;
using Microsoft.Data.SqlClient;

namespace nuel;

public static partial class Db
{
        /// <summary>Asynchronously executes the query and returns the first column of the first row as a <see cref="string"/>.</summary>
        /// <param name="query">The SQL query or stored procedure name to execute.</param>
        /// <param name="parameters">The parameters for the SQL query.</param>
        /// <returns>A task representing the asynchronous operation, returning the string representation of the first column of the first row, or null if the value is null or DBNull.</returns>
        public static Task<string> Str(string query, params (string name, object value)[] parameters)
            => Str(query, false, Data.SqlParams(parameters));

        /// <summary>Asynchronously executes the query and returns the first column of the first row as a <see cref="string"/>.</summary>
        /// <param name="query">The SQL query or stored procedure name to execute.</param>
        /// <param name="isStoredProc">Whether the query is a stored procedure.</param>
        /// <param name="parameters">The parameters for the SQL query.</param>
        /// <returns>A task representing the asynchronous operation, returning the string representation of the first column of the first row, or null if the value is null or DBNull.</returns>
        public static Task<string> Str(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => Str(query, isStoredProc, Data.SqlParams(parameters));

        /// <summary>Asynchronously executes the query and returns the first column of the first row as a <see cref="string"/>.</summary>
        /// <param name="query">The SQL query or stored procedure name to execute.</param>
        /// <param name="isStoredProc">Whether the query is a stored procedure.</param>
        /// <returns>A task representing the asynchronous operation, returning the string representation of the first column of the first row, or null if the value is null or DBNull.</returns>
        public static Task<string> Str(string query, bool isStoredProc = false)
            => Str(query, isStoredProc, Data.NoParams);

        /// <summary>Asynchronously executes the query and returns the first column of the first row as a <see cref="string"/>.</summary>
        /// <param name="query">The SQL query or stored procedure name to execute.</param>
        /// <param name="isStoredProc">Whether the query is a stored procedure.</param>
        /// <param name="parameters">The SQL parameters to apply to the command.</param>
        /// <returns>A task representing the asynchronous operation, returning the string representation of the first column of the first row, or null if the value is null or DBNull.</returns>
        public static async Task<string> Str(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            using var connection = new SqlConnection(Data.ConnectionString);
            using var cmd = new SqlCommand(query, connection);
            await connection.OpenAsync();
            if (isStoredProc)
                cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddRange(parameters);
            var val = await cmd.ExecuteScalarAsync();
            return val is DBNull ? null : val?.ToString();
        }
    }