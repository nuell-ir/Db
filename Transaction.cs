using System.Data;
using Microsoft.Data.SqlClient;

namespace nuel.Sync
{
    public static partial class Db
    {
        /// <summary>Executes a SQL query or batch within a database transaction.</summary>
        /// <param name="query">The SQL query or batch to execute.</param>
        /// <returns>An array containing the number of affected rows, or null if the input is null.</returns>
        public static int[] Transaction(string query)
            => Transaction(query, false, Data.NoParams);

        /// <summary>Executes a SQL query or stored procedure within a database transaction.</summary>
        /// <param name="query">The SQL query or stored procedure name to execute.</param>
        /// <param name="isStoredProc">Whether the query is a stored procedure.</param>
        /// <returns>An array containing the number of affected rows, or null if the input is null.</returns>
        public static int[] Transaction(string query, bool isStoredProc)
            => Transaction(query, isStoredProc, Data.NoParams);

        /// <summary>Executes a SQL query within a database transaction with parameters.</summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="parameters">The parameters for the SQL query.</param>
        /// <returns>An array containing the number of affected rows, or null if the input is null.</returns>
        public static int[] Transaction(string query, params (string name, object value)[] parameters)
            => Transaction(query, false, Data.SqlParams(parameters));

        /// <summary>Executes a SQL query or stored procedure within a database transaction with parameters.</summary>
        /// <param name="query">The SQL query or stored procedure name to execute.</param>
        /// <param name="isStoredProc">Whether the query is a stored procedure.</param>
        /// <param name="parameters">The parameters for the SQL query.</param>
        /// <returns>An array containing the number of affected rows, or null if the input is null.</returns>
        public static int[] Transaction(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => Transaction(query, isStoredProc, Data.SqlParams(parameters));

        /// <summary>Executes a SQL query within a database transaction with parameters.</summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="parameters">The SQL parameters to apply to the command.</param>
        /// <returns>An array containing the number of affected rows, or null if the input is null.</returns>
        public static int[] Transaction(string query, params SqlParameter[] parameters)
            => Transaction(query, false, parameters);

        /// <summary>Executes a SQL query or stored procedure within a database transaction with parameters.</summary>
        /// <param name="query">The SQL query or stored procedure name to execute.</param>
        /// <param name="isStoredProc">Whether the query is a stored procedure.</param>
        /// <param name="parameters">The SQL parameters to apply to the command.</param>
        /// <returns>An array containing the number of affected rows, or null if the input is null.</returns>
        public static int[] Transaction(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            if (query == null)
                return null;
            using var connection = new SqlConnection(Data.ConnectionString);
            using var cmd = new SqlCommand(query, connection);
            if (isStoredProc)
                cmd.CommandType = CommandType.StoredProcedure;
            if (parameters?.Length > 0)
                cmd.Parameters.AddRange(parameters);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            cmd.Transaction = transaction;
            int result = cmd.ExecuteNonQuery();
            transaction.Commit();
            return [result];
        }

        /// <summary>Executes a collection of SQL queries within a database transaction.</summary>
        /// <param name="queries">An enumerable collection of SQL query strings to execute.</param>
        /// <returns>An array of integers containing the number of affected rows for each query, or null if the input is null.</returns>
        public static int[] Transaction(IEnumerable<string> queries)
        {
            if (queries == null)
                return null;
            var result = queries.TryGetNonEnumeratedCount(out int count) ? new List<int>(count) : new List<int>();
            using var connection = new SqlConnection(Data.ConnectionString);
            using var cmd = connection.CreateCommand();
            connection.Open();
            using var transaction = connection.BeginTransaction();
            cmd.Transaction = transaction;
            foreach (string query in queries)
            {
                cmd.CommandText = query;
                result.Add(cmd.ExecuteNonQuery());
            }
            transaction.Commit();
            return result.ToArray();
        }

        /// <summary>Executes a collection of SQL commands with parameters within a database transaction.</summary>
        /// <param name="commands">An enumerable collection of SQL queries and their parameters to execute.</param>
        /// <returns>An array of integers containing the number of affected rows for each command, or null if the input is null.</returns>
        public static int[] Transaction(IEnumerable<(string query, SqlParameter[] parameters)> commands)
        {
            if (commands == null)
                return null;
            var result = commands.TryGetNonEnumeratedCount(out int count) ? new List<int>(count) : new List<int>();
            using var connection = new SqlConnection(Data.ConnectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            foreach (var (query, parameters) in commands)
            {
                using var cmd = new SqlCommand(query, connection, transaction);
                if (parameters?.Length > 0)
                    cmd.Parameters.AddRange(parameters);
                result.Add(cmd.ExecuteNonQuery());
            }
            transaction.Commit();
            return result.ToArray();
        }

        /// <summary>Executes multiple SQL commands with parameters within a database transaction.</summary>
        /// <param name="commands">The SQL commands and their parameters to execute.</param>
        /// <returns>An array of integers containing the number of affected rows for each command, or null if the input is null.</returns>
        public static int[] Transaction(params (string query, SqlParameter[] parameters)[] commands)
            => Transaction((IEnumerable<(string query, SqlParameter[] parameters)>)commands);

        /// <summary>Executes a collection of SQL commands with parameters within a database transaction.</summary>
        /// <param name="commands">An enumerable collection of SQL queries and their parameters to execute.</param>
        /// <returns>An array of integers containing the number of affected rows for each command, or null if the input is null.</returns>
        public static int[] Transaction(IEnumerable<(string query, (string name, object value)[] parameters)> commands)
        {
            if (commands == null)
                return null;
            var result = commands.TryGetNonEnumeratedCount(out int count) ? new List<int>(count) : new List<int>();
            using var connection = new SqlConnection(Data.ConnectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            foreach (var (query, parameters) in commands)
            {
                using var cmd = new SqlCommand(query, connection, transaction);
                if (parameters?.Length > 0)
                    cmd.Parameters.AddRange(Data.SqlParams(parameters));
                result.Add(cmd.ExecuteNonQuery());
            }
            transaction.Commit();
            return result.ToArray();
        }

        /// <summary>Executes multiple SQL commands with parameters within a database transaction.</summary>
        /// <param name="commands">The SQL commands and their parameters to execute.</param>
        /// <returns>An array of integers containing the number of affected rows for each command, or null if the input is null.</returns>
        public static int[] Transaction(params (string query, (string name, object value)[] parameters)[] commands)
            => Transaction((IEnumerable<(string query, (string name, object value)[] parameters)>)commands);

        /// <summary>Executes a collection of SQL commands with stored procedure flags and parameters within a database transaction.</summary>
        /// <param name="commands">An enumerable collection of SQL queries, stored procedure flags, and parameters to execute.</param>
        /// <returns>An array of integers containing the number of affected rows for each command, or null if the input is null.</returns>
        public static int[] Transaction(IEnumerable<(string query, bool isStoredProc, SqlParameter[] parameters)> commands)
        {
            if (commands == null)
                return null;
            var result = commands.TryGetNonEnumeratedCount(out int count) ? new List<int>(count) : new List<int>();
            using var connection = new SqlConnection(Data.ConnectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            foreach (var (query, isStoredProc, parameters) in commands)
            {
                using var cmd = new SqlCommand(query, connection, transaction);
                if (isStoredProc)
                    cmd.CommandType = CommandType.StoredProcedure;
                if (parameters?.Length > 0)
                    cmd.Parameters.AddRange(parameters);
                result.Add(cmd.ExecuteNonQuery());
            }
            transaction.Commit();
            return result.ToArray();
        }

        /// <summary>Executes multiple SQL commands with stored procedure flags and parameters within a database transaction.</summary>
        /// <param name="commands">The SQL commands, stored procedure flags, and parameters to execute.</param>
        /// <returns>An array of integers containing the number of affected rows for each command, or null if the input is null.</returns>
        public static int[] Transaction(params (string query, bool isStoredProc, SqlParameter[] parameters)[] commands)
            => Transaction((IEnumerable<(string query, bool isStoredProc, SqlParameter[] parameters)>)commands);

        /// <summary>Executes a collection of SQL commands with stored procedure flags and parameters within a database transaction.</summary>
        /// <param name="commands">An enumerable collection of SQL queries, stored procedure flags, and parameters to execute.</param>
        /// <returns>An array of integers containing the number of affected rows for each command, or null if the input is null.</returns>
        public static int[] Transaction(IEnumerable<(string query, bool isStoredProc, (string name, object value)[] parameters)> commands)
        {
            if (commands == null)
                return null;
            var result = commands.TryGetNonEnumeratedCount(out int count) ? new List<int>(count) : new List<int>();
            using var connection = new SqlConnection(Data.ConnectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            foreach (var (query, isStoredProc, parameters) in commands)
            {
                using var cmd = new SqlCommand(query, connection, transaction);
                if (isStoredProc)
                    cmd.CommandType = CommandType.StoredProcedure;
                if (parameters?.Length > 0)
                    cmd.Parameters.AddRange(Data.SqlParams(parameters));
                result.Add(cmd.ExecuteNonQuery());
            }
            transaction.Commit();
            return result.ToArray();
        }

        /// <summary>Executes multiple SQL commands with stored procedure flags and parameters within a database transaction.</summary>
        /// <param name="commands">The SQL commands, stored procedure flags, and parameters to execute.</param>
        /// <returns>An array of integers containing the number of affected rows for each command, or null if the input is null.</returns>
        public static int[] Transaction(params (string query, bool isStoredProc, (string name, object value)[] parameters)[] commands)
            => Transaction((IEnumerable<(string query, bool isStoredProc, (string name, object value)[] parameters)>)commands);

        /// <summary>Executes a collection of <see cref="SqlCommand"/> instances within a database transaction.</summary>
        /// <param name="commands">An enumerable collection of <see cref="SqlCommand"/> instances to execute.</param>
        /// <returns>An array of integers containing the number of affected rows for each command, or null if the input is null.</returns>
        public static int[] Transaction(IEnumerable<SqlCommand> commands)
        {
            if (commands == null)
                return null;
            var result = commands.TryGetNonEnumeratedCount(out int count) ? new List<int>(count) : new List<int>();
            using var connection = new SqlConnection(Data.ConnectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            foreach (var cmd in commands)
            {
                if (cmd == null)
                    continue;
                cmd.Connection = connection;
                cmd.Transaction = transaction;
                result.Add(cmd.ExecuteNonQuery());
            }
            transaction.Commit();
            return result.ToArray();
        }

        /// <summary>Executes multiple <see cref="SqlCommand"/> instances within a database transaction.</summary>
        /// <param name="commands">The <see cref="SqlCommand"/> instances to execute.</param>
        /// <returns>An array of integers containing the number of affected rows for each command, or null if the input is null.</returns>
        public static int[] Transaction(params SqlCommand[] commands)
            => Transaction((IEnumerable<SqlCommand>)commands);
    }
}

namespace nuel.Async
{
    public static partial class Db
    {
        /// <summary>Asynchronously executes a SQL query or batch within a database transaction.</summary>
        /// <param name="query">The SQL query or batch to execute.</param>
        /// <returns>A task representing the asynchronous operation, returning an array containing the number of affected rows, or null if the input is null.</returns>
        public static Task<int[]> Transaction(string query)
            => Transaction(query, false, Data.NoParams);

        /// <summary>Asynchronously executes a SQL query or stored procedure within a database transaction.</summary>
        /// <param name="query">The SQL query or stored procedure name to execute.</param>
        /// <param name="isStoredProc">Whether the query is a stored procedure.</param>
        /// <returns>A task representing the asynchronous operation, returning an array containing the number of affected rows, or null if the input is null.</returns>
        public static Task<int[]> Transaction(string query, bool isStoredProc)
            => Transaction(query, isStoredProc, Data.NoParams);

        /// <summary>Asynchronously executes a SQL query within a database transaction with parameters.</summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="parameters">The parameters for the SQL query.</param>
        /// <returns>A task representing the asynchronous operation, returning an array containing the number of affected rows, or null if the input is null.</returns>
        public static Task<int[]> Transaction(string query, params (string name, object value)[] parameters)
            => Transaction(query, false, Data.SqlParams(parameters));

        /// <summary>Asynchronously executes a SQL query or stored procedure within a database transaction with parameters.</summary>
        /// <param name="query">The SQL query or stored procedure name to execute.</param>
        /// <param name="isStoredProc">Whether the query is a stored procedure.</param>
        /// <param name="parameters">The parameters for the SQL query.</param>
        /// <returns>A task representing the asynchronous operation, returning an array containing the number of affected rows, or null if the input is null.</returns>
        public static Task<int[]> Transaction(string query, bool isStoredProc, params (string name, object value)[] parameters)
            => Transaction(query, isStoredProc, Data.SqlParams(parameters));

        /// <summary>Asynchronously executes a SQL query within a database transaction with parameters.</summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="parameters">The SQL parameters to apply to the command.</param>
        /// <returns>A task representing the asynchronous operation, returning an array containing the number of affected rows, or null if the input is null.</returns>
        public static Task<int[]> Transaction(string query, params SqlParameter[] parameters)
            => Transaction(query, false, parameters);

        /// <summary>Asynchronously executes a SQL query or stored procedure within a database transaction with parameters.</summary>
        /// <param name="query">The SQL query or stored procedure name to execute.</param>
        /// <param name="isStoredProc">Whether the query is a stored procedure.</param>
        /// <param name="parameters">The SQL parameters to apply to the command.</param>
        /// <returns>A task representing the asynchronous operation, returning an array containing the number of affected rows, or null if the input is null.</returns>
        public static async Task<int[]> Transaction(string query, bool isStoredProc, params SqlParameter[] parameters)
        {
            if (query == null)
                return null;
            using var connection = new SqlConnection(Data.ConnectionString);
            using var cmd = new SqlCommand(query, connection);
            if (isStoredProc)
                cmd.CommandType = CommandType.StoredProcedure;
            if (parameters?.Length > 0)
                cmd.Parameters.AddRange(parameters);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            cmd.Transaction = transaction;
            int result = await cmd.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
            return [result];
        }

        /// <summary>Asynchronously executes a collection of SQL queries within a database transaction.</summary>
        /// <param name="queries">An enumerable collection of SQL query strings to execute.</param>
        /// <returns>A task representing the asynchronous operation, returning an array of integers containing the number of affected rows for each query, or null if the input is null.</returns>
        public static async Task<int[]> Transaction(IEnumerable<string> queries)
        {
            if (queries == null)
                return null;
            var result = queries.TryGetNonEnumeratedCount(out int count) ? new List<int>(count) : new List<int>();
            using var connection = new SqlConnection(Data.ConnectionString);
            using var cmd = connection.CreateCommand();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            cmd.Transaction = transaction;
            foreach (string query in queries)
            {
                cmd.CommandText = query;
                result.Add(await cmd.ExecuteNonQueryAsync());
            }
            await transaction.CommitAsync();
            return result.ToArray();
        }

        /// <summary>Asynchronously executes a collection of SQL commands with parameters within a database transaction.</summary>
        /// <param name="commands">An enumerable collection of SQL queries and their parameters to execute.</param>
        /// <returns>A task representing the asynchronous operation, returning an array of integers containing the number of affected rows for each command, or null if the input is null.</returns>
        public static async Task<int[]> Transaction(IEnumerable<(string query, SqlParameter[] parameters)> commands)
        {
            if (commands == null)
                return null;
            var result = commands.TryGetNonEnumeratedCount(out int count) ? new List<int>(count) : new List<int>();
            using var connection = new SqlConnection(Data.ConnectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            foreach (var (query, parameters) in commands)
            {
                using var cmd = new SqlCommand(query, connection, transaction);
                if (parameters?.Length > 0)
                    cmd.Parameters.AddRange(parameters);
                result.Add(await cmd.ExecuteNonQueryAsync());
            }
            await transaction.CommitAsync();
            return result.ToArray();
        }

        /// <summary>Asynchronously executes multiple SQL commands with parameters within a database transaction.</summary>
        /// <param name="commands">The SQL commands and their parameters to execute.</param>
        /// <returns>A task representing the asynchronous operation, returning an array of integers containing the number of affected rows for each command, or null if the input is null.</returns>
        public static Task<int[]> Transaction(params (string query, SqlParameter[] parameters)[] commands)
            => Transaction((IEnumerable<(string query, SqlParameter[] parameters)>)commands);

        /// <summary>Asynchronously executes a collection of SQL commands with parameters within a database transaction.</summary>
        /// <param name="commands">An enumerable collection of SQL queries and their parameters to execute.</param>
        /// <returns>A task representing the asynchronous operation, returning an array of integers containing the number of affected rows for each command, or null if the input is null.</returns>
        public static async Task<int[]> Transaction(IEnumerable<(string query, (string name, object value)[] parameters)> commands)
        {
            if (commands == null)
                return null;
            var result = commands.TryGetNonEnumeratedCount(out int count) ? new List<int>(count) : new List<int>();
            using var connection = new SqlConnection(Data.ConnectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            foreach (var (query, parameters) in commands)
            {
                using var cmd = new SqlCommand(query, connection, transaction);
                if (parameters?.Length > 0)
                    cmd.Parameters.AddRange(Data.SqlParams(parameters));
                result.Add(await cmd.ExecuteNonQueryAsync());
            }
            await transaction.CommitAsync();
            return result.ToArray();
        }

        /// <summary>Asynchronously executes multiple SQL commands with parameters within a database transaction.</summary>
        /// <param name="commands">The SQL commands and their parameters to execute.</param>
        /// <returns>A task representing the asynchronous operation, returning an array of integers containing the number of affected rows for each command, or null if the input is null.</returns>
        public static Task<int[]> Transaction(params (string query, (string name, object value)[] parameters)[] commands)
            => Transaction((IEnumerable<(string query, (string name, object value)[] parameters)>)commands);

        /// <summary>Asynchronously executes a collection of SQL commands with stored procedure flags and parameters within a database transaction.</summary>
        /// <param name="commands">An enumerable collection of SQL queries, stored procedure flags, and parameters to execute.</param>
        /// <returns>A task representing the asynchronous operation, returning an array of integers containing the number of affected rows for each command, or null if the input is null.</returns>
        public static async Task<int[]> Transaction(IEnumerable<(string query, bool isStoredProc, SqlParameter[] parameters)> commands)
        {
            if (commands == null)
                return null;
            var result = commands.TryGetNonEnumeratedCount(out int count) ? new List<int>(count) : new List<int>();
            using var connection = new SqlConnection(Data.ConnectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            foreach (var (query, isStoredProc, parameters) in commands)
            {
                using var cmd = new SqlCommand(query, connection, transaction);
                if (isStoredProc)
                    cmd.CommandType = CommandType.StoredProcedure;
                if (parameters?.Length > 0)
                    cmd.Parameters.AddRange(parameters);
                result.Add(await cmd.ExecuteNonQueryAsync());
            }
            await transaction.CommitAsync();
            return result.ToArray();
        }

        /// <summary>Asynchronously executes multiple SQL commands with stored procedure flags and parameters within a database transaction.</summary>
        /// <param name="commands">The SQL commands, stored procedure flags, and parameters to execute.</param>
        /// <returns>A task representing the asynchronous operation, returning an array of integers containing the number of affected rows for each command, or null if the input is null.</returns>
        public static Task<int[]> Transaction(params (string query, bool isStoredProc, SqlParameter[] parameters)[] commands)
            => Transaction((IEnumerable<(string query, bool isStoredProc, SqlParameter[] parameters)>)commands);

        /// <summary>Asynchronously executes a collection of SQL commands with stored procedure flags and parameters within a database transaction.</summary>
        /// <param name="commands">An enumerable collection of SQL queries, stored procedure flags, and parameters to execute.</param>
        /// <returns>A task representing the asynchronous operation, returning an array of integers containing the number of affected rows for each command, or null if the input is null.</returns>
        public static async Task<int[]> Transaction(IEnumerable<(string query, bool isStoredProc, (string name, object value)[] parameters)> commands)
        {
            if (commands == null)
                return null;
            var result = commands.TryGetNonEnumeratedCount(out int count) ? new List<int>(count) : new List<int>();
            using var connection = new SqlConnection(Data.ConnectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            foreach (var (query, isStoredProc, parameters) in commands)
            {
                using var cmd = new SqlCommand(query, connection, transaction);
                if (isStoredProc)
                    cmd.CommandType = CommandType.StoredProcedure;
                if (parameters?.Length > 0)
                    cmd.Parameters.AddRange(Data.SqlParams(parameters));
                result.Add(await cmd.ExecuteNonQueryAsync());
            }
            await transaction.CommitAsync();
            return result.ToArray();
        }

        /// <summary>Asynchronously executes multiple SQL commands with stored procedure flags and parameters within a database transaction.</summary>
        /// <param name="commands">The SQL commands, stored procedure flags, and parameters to execute.</param>
        /// <returns>A task representing the asynchronous operation, returning an array of integers containing the number of affected rows for each command, or null if the input is null.</returns>
        public static Task<int[]> Transaction(params (string query, bool isStoredProc, (string name, object value)[] parameters)[] commands)
            => Transaction((IEnumerable<(string query, bool isStoredProc, (string name, object value)[] parameters)>)commands);

        /// <summary>Asynchronously executes a collection of <see cref="SqlCommand"/> instances within a database transaction.</summary>
        /// <param name="commands">An enumerable collection of <see cref="SqlCommand"/> instances to execute.</param>
        /// <returns>A task representing the asynchronous operation, returning an array of integers containing the number of affected rows for each command, or null if the input is null.</returns>
        public static async Task<int[]> Transaction(IEnumerable<SqlCommand> commands)
        {
            if (commands == null)
                return null;
            var result = commands.TryGetNonEnumeratedCount(out int count) ? new List<int>(count) : new List<int>();
            using var connection = new SqlConnection(Data.ConnectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            foreach (var cmd in commands)
            {
                if (cmd == null)
                    continue;
                cmd.Connection = connection;
                cmd.Transaction = transaction;
                result.Add(await cmd.ExecuteNonQueryAsync());
            }
            await transaction.CommitAsync();
            return result.ToArray();
        }

        /// <summary>Asynchronously executes multiple <see cref="SqlCommand"/> instances within a database transaction.</summary>
        /// <param name="commands">The <see cref="SqlCommand"/> instances to execute.</param>
        /// <returns>A task representing the asynchronous operation, returning an array of integers containing the number of affected rows for each command, or null if the input is null.</returns>
        public static Task<int[]> Transaction(params SqlCommand[] commands)
            => Transaction((IEnumerable<SqlCommand>)commands);
    }
}