namespace nuell.Sync
{
	public static partial class Db
	{
		///<summary>Deletes a record from the specified database table.</summary>
		///<param name="id">The ID of the record to be deleted.</param>
		///<param name="table">The name of the table where the record is stored.</param>
		///<param name="primaryKey">The primary key of the table. Defaults to "Id" if not specified.</param>
		///<returns>true if the record is deleted successfully, false otherwise.</returns>
		public static bool Delete(int id, string table, string primaryKey = "Id")
		{
			try
			{
				return Execute($"delete from {table} where [{primaryKey}]={id}") == 1;
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
		///<summary>Deletes a record from the specified database table.</summary>
		///<param name="id">The ID of the record to be deleted.</param>
		///<param name="table">The name of the table where the record is stored.</param>
		///<param name="primaryKey">The primary key of the table. Defaults to "Id" if not specified.</param>
		///<returns>true if the record is deleted successfully, false otherwise.</returns>
		public async static Task<bool> Delete(int id, string table, string primaryKey = "Id")
		{
			try
			{
				return await Execute($"delete from {table} where [{primaryKey}]={id}") == 1;
			}
			catch
			{
				return false;
			}
		}
	}
}