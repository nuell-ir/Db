using System.Data;
using Microsoft.Data.SqlClient;

namespace nuel;

public static partial class Db
{
	/// <summary>Asynchronously executes the query and returns the results in a <see cref="DataTable"/>.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning a <see cref="DataTable"/> containing the query results, or null if no rows were returned.</returns>
	public static Task<DataTable> Table(string query, params (string name, object value)[] parameters)
		 => Table(query, false, Data.SqlParams(parameters));

	/// <summary>Asynchronously executes the query and returns the results in a <see cref="DataTable"/>.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning a <see cref="DataTable"/> containing the query results, or null if no rows were returned.</returns>
	public static Task<DataTable> Table(string query, bool isStoredProc, params (string name, object value)[] parameters)
		 => Table(query, isStoredProc, Data.SqlParams(parameters));

	/// <summary>Asynchronously executes the query and returns the results in a <see cref="DataTable"/>.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <returns>A task representing the asynchronous operation, returning a <see cref="DataTable"/> containing the query results, or null if no rows were returned.</returns>
	public static Task<DataTable> Table(string query, bool isStoredProc = false)
		 => Table(query, isStoredProc, Data.NoParams);

	/// <summary>Asynchronously executes the query and returns the results in a <see cref="DataTable"/>.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The SQL parameters to apply to the command.</param>
	/// <returns>A task representing the asynchronous operation, returning a <see cref="DataTable"/> containing the query results, or null if no rows were returned.</returns>
	public static async Task<DataTable> Table(string query, bool isStoredProc, params SqlParameter[] parameters)
	{
		using var connection = new SqlConnection(Data.ConnectionString);
		using var bulkCopy = new SqlBulkCopy(connection);
		using var cmd = new SqlCommand(query, connection);
		if (isStoredProc)
			cmd.CommandType = CommandType.StoredProcedure;
		cmd.Parameters.AddRange(parameters);
		await connection.OpenAsync();
		using var reader = await cmd.ExecuteReaderAsync();
		if (!reader.HasRows)
			return null;
		var dt = new DataTable();
		await reader.ReadAsync();
		int columns = reader.FieldCount;
		for (int i = 0; i < columns; i++)
			dt.Columns.Add(reader.GetName(i), reader.GetFieldType(i));
		var values = new object[columns];
		reader.GetValues(values);
		dt.Rows.Add(values);
		while (await reader.ReadAsync())
		{
			reader.GetValues(values);
			dt.Rows.Add(values);
		}
		return dt;
	}
}
