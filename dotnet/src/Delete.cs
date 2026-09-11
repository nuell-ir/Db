namespace nuel;

public static partial class Db
{
	/// <summary>Asynchronously deletes a record from the specified database table.</summary>
	/// <typeparam name="TKey">The type of the primary key.</typeparam>
	/// <param name="id">The ID of the record to be deleted.</param>
	/// <param name="table">The name of the table where the record is stored.</param>
	/// <param name="primaryKey">The primary key column of the table. Defaults to "Id" if not specified.</param>
	/// <returns>A task representing the asynchronous operation, returning true if the record was deleted successfully; otherwise, false.</returns>
	public async static Task<bool> Delete<TKey>(TKey id, string table, string primaryKey = "Id")
	{
		ArgumentNullException.ThrowIfNull(id);

		string safeTable = Data.EscapeIdentifier(table);
		string safePk = Data.EscapeIdentifier(primaryKey);
		string query = $"DELETE FROM {safeTable} WHERE {safePk} = @id";

		return await Execute(query, false, ("@id", (object)id)).ConfigureAwait(false) > 0;
	}

	/// <summary>Asynchronously deletes a record from the specified database table.</summary>
	/// <param name="id">The ID of the record to be deleted.</param>
	/// <param name="table">The name of the table where the record is stored.</param>
	/// <param name="primaryKey">The primary key column of the table. Defaults to "Id" if not specified.</param>
	/// <returns>A task representing the asynchronous operation, returning true if the record was deleted successfully; otherwise, false.</returns>
	public static Task<bool> Delete(int id, string table, string primaryKey = "Id")
		=> Delete<int>(id, table, primaryKey);
}
