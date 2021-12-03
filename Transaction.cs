using Microsoft.Data.SqlClient;

namespace nuell.Sync
{
    public static partial class Db
    {
        public static int[] Transaction(string queries)
            => Transaction(queries.Split(new string[] { "GO", ";" }, StringSplitOptions.RemoveEmptyEntries));

        public static int[] Transaction(IEnumerable<string> queries)
        {
            if (queries == null)
                return null;
            var result = new int[queries.Count()];
            using (var cnnct = new SqlConnection(Data.ConnectionString))
            {
                using var cmnd = cnnct.CreateCommand();
                cnnct.Open();
                using var transaction = cnnct.BeginTransaction();
                cmnd.Transaction = transaction;
                int i = 0;
                foreach (string query in queries)
                {
                    cmnd.CommandText = query;
                    result[i++] = cmnd.ExecuteNonQuery();
                }
                transaction.Commit();
            }
            return result;
        }
    }
}

namespace nuell.Async
{
    public static partial class Db
    {
        public static Task<int[]> Transaction(string queries)
            => Transaction(queries.Split(new string[] { "GO", ";" }, StringSplitOptions.RemoveEmptyEntries));

        public static async Task<int[]> Transaction(IEnumerable<string> queries)
        {
            if (queries == null)
                return null;
            var result = new int[queries.Count()];
            using (var cnnct = new SqlConnection(Data.ConnectionString))
            {
                using var cmnd = cnnct.CreateCommand();
                await cnnct.OpenAsync();
                using var transaction = cnnct.BeginTransaction();
                cmnd.Transaction = transaction;
                int i = 0;
                foreach (string query in queries)
                {
                    cmnd.CommandText = query;
                    result[i++] = await cmnd.ExecuteNonQueryAsync();
                }
                await transaction.CommitAsync();
            }
            return result;
        }
    }
}