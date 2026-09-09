using Microsoft.Data.SqlClient;

namespace nuel.Sync
{
    public static partial class Db
    {
        /// <summary>Executes multiple SQL queries separated by "GO" or ";" within a database transaction.</summary>
        /// <param name="queries">A string containing multiple SQL queries separated by "GO" or ";".</param>
        /// <returns>An array of integers containing the number of affected rows for each query, or null if the input is null.</returns>
        public static int[] Transaction(string queries)
            => Transaction(queries.Split(["GO", ";"], StringSplitOptions.RemoveEmptyEntries));

        /// <summary>Executes a collection of SQL queries within a database transaction.</summary>
        /// <param name="queries">An enumerable collection of SQL query strings to execute.</param>
        /// <returns>An array of integers containing the number of affected rows for each query, or null if the input is null.</returns>
        public static int[] Transaction(IEnumerable<string> queries)
        {
            if (queries == null)
                return null;
            var result = new int[queries.Count()];
            using (var connection = new SqlConnection(Data.ConnectionString))
            {
                using var cmd = connection.CreateCommand();
                connection.Open();
                using var transaction = connection.BeginTransaction();
                cmd.Transaction = transaction;
                int i = 0;
                foreach (string query in queries)
                {
                    cmd.CommandText = query;
                    result[i++] = cmd.ExecuteNonQuery();
                }
                transaction.Commit();
            }
            return result;
        }
    }
}

namespace nuel.Async
{
    public static partial class Db
    {
        /// <summary>Asynchronously executes multiple SQL queries separated by "GO" or ";" within a database transaction.</summary>
        /// <param name="queries">A string containing multiple SQL queries separated by "GO" or ";".</param>
        /// <returns>A task representing the asynchronous operation, returning an array of integers containing the number of affected rows for each query, or null if the input is null.</returns>
        public static Task<int[]> Transaction(string queries)
            => Transaction(queries.Split(["GO", ";"], StringSplitOptions.RemoveEmptyEntries));

        /// <summary>Asynchronously executes a collection of SQL queries within a database transaction.</summary>
        /// <param name="queries">An enumerable collection of SQL query strings to execute.</param>
        /// <returns>A task representing the asynchronous operation, returning an array of integers containing the number of affected rows for each query, or null if the input is null.</returns>
        public static async Task<int[]> Transaction(IEnumerable<string> queries)
        {
            if (queries == null)
                return null;
            var result = new int[queries.Count()];
            using (var connection = new SqlConnection(Data.ConnectionString))
            {
                using var cmd = connection.CreateCommand();
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();
                cmd.Transaction = transaction;
                int i = 0;
                foreach (string query in queries)
                {
                    cmd.CommandText = query;
                    result[i++] = await cmd.ExecuteNonQueryAsync();
                }
                await transaction.CommitAsync();
            }
            return result;
        }
    }
}