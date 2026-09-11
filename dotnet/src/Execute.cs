using System.Data;
using Microsoft.Data.SqlClient;

namespace nuel;

public static partial class Db
{
	/// <summary>Asynchronously executes the query or stored procedure against the database.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning the number of rows affected.</returns>
	public static Task<int> Execute(string query, params (string name, object value)[] parameters)
		 => Execute(query, false, parameters);

	/// <summary>Asynchronously executes the query or stored procedure against the database.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning the number of rows affected.</returns>
	public async static Task<int> Execute(string query, bool isStoredProc, params (string name, object value)[] parameters)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(query);

		await using var connection = new SqlConnection(Data.ConnectionString);
		await using var cmd = new SqlCommand(query, connection);
		if (isStoredProc)
			cmd.CommandType = CommandType.StoredProcedure;
		Data.AttachParams(cmd, parameters);
		await connection.OpenAsync().ConfigureAwait(false);
		return await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
	}

	/// <summary>Asynchronously executes the query or stored procedure against the database.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <returns>A task representing the asynchronous operation, returning the number of rows affected.</returns>
	public static Task<int> Execute(string query, bool isStoredProc = false)
		 => Execute(query, isStoredProc, Data.NoParams);

	/// <summary>Asynchronously executes the query or stored procedure against the database.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="parameters">The SQL parameters to apply to the command.</param>
	/// <returns>A task representing the asynchronous operation, returning the number of rows affected.</returns>
	public static Task<int> Execute(string query, params SqlParameter[] parameters)
		 => Execute(query, false, parameters);

	/// <summary>Asynchronously executes the query or stored procedure against the database.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The SQL parameters to apply to the command.</param>
	/// <returns>A task representing the asynchronous operation, returning the number of rows affected.</returns>
	public async static Task<int> Execute(string query, bool isStoredProc, params SqlParameter[] parameters)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(query);

		await using var connection = new SqlConnection(Data.ConnectionString);
		await using var cmd = new SqlCommand(query, connection);
		if (isStoredProc)
			cmd.CommandType = CommandType.StoredProcedure;
		Data.AttachParams(cmd, parameters);
		await connection.OpenAsync().ConfigureAwait(false);
		return await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
	}
}
