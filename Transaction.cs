using Microsoft.Data.SqlClient;

namespace nuel.Sync
{
    public static partial class Db
    {
        public static int[] Transaction(string queries)
            => Transaction(queries.Split(["GO", ";"], StringSplitOptions.RemoveEmptyEntries));

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
        public static Task<int[]> Transaction(string queries)
            => Transaction(queries.Split(["GO", ";"], StringSplitOptions.RemoveEmptyEntries));

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