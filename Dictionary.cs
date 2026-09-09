using System.Data;
using Microsoft.Data.SqlClient;

namespace nuel;

public static partial class Db
{
        /// <summary>Asynchronously executes the query and maps the first two columns of the result set into a <see cref="System.Collections.Generic.Dictionary{K, V}"/>.</summary>
        /// <typeparam name="K">The type of the dictionary keys, read from the first column.</typeparam>
        /// <typeparam name="V">The type of the dictionary values, read from the second column.</typeparam>
        /// <param name="query">The SQL query or stored procedure name to execute.</param>
        /// <param name="parameters">The parameters for the SQL query.</param>
        /// <returns>A task representing the asynchronous operation, returning a dictionary of keys and values, or null if no rows were returned.</returns>
        public static Task<Dictionary<K, V>> Dictionary<K, V>(string query, params (string name, object value)[] parameters)
            => Dictionary<K, V>(query, false, Data.SqlParams(parameters));

        /// <summary>Asynchronously executes the query and maps the first two columns of the result set into a <see cref="System.Collections.Generic.Dictionary{K, V}"/>.</summary>
        /// <typeparam name="K">The type of the dictionary keys, read from the first column.</typeparam>
        /// <typeparam name="V">The type of the dictionary values, read from the second column.</typeparam>
        /// <param name="query">The SQL query or stored procedure name to execute.</param>
        /// <param name="isStoredProc">Whether the query is a stored procedure.</param>
        /// <param name="parameters">The parameters for the SQL query.</param>
        /// <returns>A task representing the asynchronous operation, returning a dictionary of keys and values, or null if no rows were returned.</returns>
        public static Task<Dictionary<K, V>> Dictionary<K, V>(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => Dictionary<K, V>(query, isStoredProc, Data.SqlParams(parameters));

        /// <summary>Asynchronously executes the query and maps the first two columns of the result set into a <see cref="System.Collections.Generic.Dictionary{K, V}"/>.</summary>
        /// <typeparam name="K">The type of the dictionary keys, read from the first column.</typeparam>
        /// <typeparam name="V">The type of the dictionary values, read from the second column.</typeparam>
        /// <param name="query">The SQL query or stored procedure name to execute.</param>
        /// <param name="isStoredProc">Whether the query is a stored procedure.</param>
        /// <returns>A task representing the asynchronous operation, returning a dictionary of keys and values, or null if no rows were returned.</returns>
        public static Task<Dictionary<K, V>> Dictionary<K, V>(string query, bool isStoredProc = false)
            => Dictionary<K, V>(query, isStoredProc, Data.NoParams);

        /// <summary>Asynchronously executes the query and maps the first two columns of the result set into a <see cref="System.Collections.Generic.Dictionary{K, V}"/>.</summary>
        /// <typeparam name="K">The type of the dictionary keys, read from the first column.</typeparam>
        /// <typeparam name="V">The type of the dictionary values, read from the second column.</typeparam>
        /// <param name="query">The SQL query or stored procedure name to execute.</param>
        /// <param name="isStoredProc">Whether the query is a stored procedure.</param>
        /// <param name="parameters">The SQL parameters to apply to the command.</param>
        /// <returns>A task representing the asynchronous operation, returning a dictionary of keys and values, or null if no rows were returned.</returns>
        public static async Task<Dictionary<K, V>> Dictionary<K, V>(string query, bool isStoredProc, params SqlParameter[] parameters)
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
            var dictionary = new Dictionary<K, V>();
            while (await reader.ReadAsync())
                dictionary.Add(reader.GetFieldValue<K>(0), reader.GetFieldValue<V>(1));
            return dictionary;
        }
    }