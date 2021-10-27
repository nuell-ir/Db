using System.Threading.Tasks;

namespace nuell.Sync
{
    public static partial class Db
    {
        public static bool Delete(int id, string table)
        {
            try
            {
                return Execute($"delete from [{table}] where Id={id}") == 1;
            }
            catch
            {
                return false;
            }
        }
    }
}

namespace nuell.Async
{
    public static partial class Db
    {
        public async static Task<bool> Delete(int id, string table)
        {
            try
            {
                return await Execute($"delete from {table} where Id={id}") == 1;
            }
            catch
            {
                return false;
            }
        }
    }
}