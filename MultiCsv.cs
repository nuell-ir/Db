using System.Data;
using Microsoft.Data.SqlClient;

namespace nuel;

public static partial class Db
{
	/// <summary>Asynchronously converts multiple results of a query to an array of CSV strings.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning an array of CSV formatted strings.</returns>
	public static Task<string[]> MultiCsv(string query, params (string name, object value)[] parameters)
		=> MultiCsv(query, false, Data.SqlParams(parameters));

	/// <summary>Asynchronously converts multiple results of a query to an array of CSV strings.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning an array of CSV formatted strings.</returns>
	public static Task<string[]> MultiCsv(string query, bool isStoredProc, params (string name, object value)[] parameters)
		=> MultiCsv(query, isStoredProc, Data.SqlParams(parameters));

	/// <summary>Asynchronously converts multiple results of a query to an array of CSV strings.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <returns>A task representing the asynchronous operation, returning an array of CSV formatted strings.</returns>
	public static Task<string[]> MultiCsv(string query, bool isStoredProc = false)
		=> MultiCsv(query, isStoredProc, Data.NoParams);

	/// <summary>Asynchronously converts multiple results of a query to an array of CSV strings.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The SQL parameters to apply to the command.</param>
	/// <returns>A task representing the asynchronous operation, returning an array of CSV formatted strings.</returns>
	public static async Task<string[]> MultiCsv(string query, bool isStoredProc, params SqlParameter[] parameters)
	{
		using var connection = new SqlConnection(Data.ConnectionString);
		using var cmd = new SqlCommand(query, connection);
		if (isStoredProc)
			cmd.CommandType = CommandType.StoredProcedure;
		cmd.Parameters.AddRange(parameters);
		await connection.OpenAsync();
		using var reader = await cmd.ExecuteReaderAsync();
		var results = new List<string>
		{
			await reader.ReadCsv()
		};
		while (await reader.NextResultAsync())
			results.Add(await reader.ReadCsv());
		return [.. results];
	}
}

