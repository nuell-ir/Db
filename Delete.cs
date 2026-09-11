namespace nuel;

public static partial class Db
{
	/// <summary>Asynchronously deletes a record from the specified database table.</summary>
	/// <param name="id">The ID of the record to be deleted.</param>
	/// <param name="table">The name of the table where the record is stored.</param>
	/// <param name="primaryKey">The primary key column of the table. Defaults to "Id" if not specified.</param>
	/// <returns>A task representing the asynchronous operation, returning true if the record was deleted successfully; otherwise, false.</returns>
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
