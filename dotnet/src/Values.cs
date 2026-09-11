using System.Data;
using Microsoft.Data.SqlClient;

namespace nuel;

public static partial class Db
{
	/// <summary>Asynchronously executes the query and returns all values from all rows and columns across all result sets as an array of objects.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning an array of objects containing all values returned by the query.</returns>
	public static Task<object[]> Values(string query, params (string name, object value)[] parameters)
		 => Values(query, false, Data.SqlParams(parameters));

	/// <summary>Asynchronously executes the query and returns all values from all rows and columns across all result sets as an array of objects.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning an array of objects containing all values returned by the query.</returns>
	public static Task<object[]> Values(string query, bool isStoredProc, params (string name, object value)[] parameters)
		 => Values(query, isStoredProc, Data.SqlParams(parameters));

	/// <summary>Asynchronously executes the query and returns all values from all rows and columns across all result sets as an array of objects.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <returns>A task representing the asynchronous operation, returning an array of objects containing all values returned by the query.</returns>
	public static Task<object[]> Values(string query, bool isStoredProc = false)
		 => Values(query, isStoredProc, Data.NoParams);

	/// <summary>Asynchronously executes the query and returns all values from all rows and columns across all result sets as an array of objects.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The SQL parameters to apply to the command.</param>
	/// <returns>A task representing the asynchronous operation, returning an array of objects containing all values returned by the query.</returns>
	public static async Task<object[]> Values(string query, bool isStoredProc, params SqlParameter[] parameters)
	{
		using var connection = new SqlConnection(Data.ConnectionString);
		using var cmd = new SqlCommand(query, connection);
		if (isStoredProc)
			cmd.CommandType = CommandType.StoredProcedure;
		cmd.Parameters.AddRange(parameters);
		await connection.OpenAsync();
		using var reader = await cmd.ExecuteReaderAsync();
		var results = new List<object>();
		await AddValues();
		while (await reader.NextResultAsync())
			await AddValues();
		return [.. results];

		async Task AddValues()
		{
			var values = new object[reader.FieldCount];
			while (await reader.ReadAsync())
			{
				reader.GetValues(values);
				results.AddRange(values);
			}
		}
	}
}
